/* SOURCE — reading and rewriting the card STATS, which live in C# and not in the loc JSON.
 *
 * A card's numbers are spread across three places in its class:
 *
 *     class BrickHammer() : KnifeHeroCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
 *                                         ^cost                          ^rarity
 *         CanonicalVars => { new DamageVar(6m, …), new IntVar("Per", 3m) }     ← base values
 *         OnUpgrade()   => DynamicVars.Damage.UpgradeValueBy(2m)              ← upgrade bumps
 *
 * We parse those out for display and splice single numbers/identifiers back in on save. The class
 * declaration regex is deliberately the same shape tools/cards.py already depends on, so the editor
 * and the generated CARDS_IMPLEMENTED.md agree on what a card even is.
 *
 * ── WHY SPLICING AND NOT REWRITING ──────────────────────────────────────────────────────────────
 * This edits real source files, so it does the narrowest possible thing: replace the exact character
 * span of one literal. Comments, formatting and the surrounding code are untouched by construction —
 * there is no code generation step that could reformat a file. Anything the parser doesn't recognize
 * is simply not offered as editable, so a card with hand-rolled logic degrades to "text only" rather
 * than being mangled.
 */
import { readFile, writeFile, rename } from "node:fs/promises";
import { readdir } from "node:fs/promises";
import { join, resolve, relative } from "node:path";

export const RARITIES = [
  "Basic", "Common", "Uncommon", "Rare", "Ancient", "Event", "Token", "Status", "Curse", "Quest",
];

/* Same declaration shape tools/cards.py matches. The `d` flag gives us capture-group offsets, which
 * is the whole trick — we need to know where the cost digit and the rarity word physically are. */
const CARD_RE =
  /class\s+(\w+)\(\)\s*:\s*(KnifeHeroCard|CreatureCard|PrideCard)\(\s*(-?\d+)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)\s*,\s*TargetType\.(\w+)/dg;

// new DamageVar(6m, …) / new BlockVar(5m, …) / new IntVar("Per", 3m)
const VAR_RE = /new\s+(\w+?)Var\(\s*(?:"(\w+)"\s*,\s*)?(-?\d+(?:\.\d+)?)m/dg;

// DynamicVars.Damage.UpgradeValueBy(2m) / DynamicVars["Per"].UpgradeValueBy(1m)
const UPGRADE_RE = /DynamicVars(?:\.(\w+)|\[\s*"(\w+)"\s*\])\.UpgradeValueBy\(\s*(-?\d+(?:\.\d+)?)m/dg;

export type Span = { start: number; end: number };
export type Stat = { name: string; value: string } & Span;

export type CardSource = {
  cls: string;
  base: string;
  file: string;          // repo-relative, for display
  locId: string;         // KNIFEHERO-BRICK_HAMMER
  cost: Stat;
  rarity: Stat;
  type: string;          // read-only: changing it would desync the card's own OnPlay
  target: string;        // read-only, same reason
  vars: Stat[];
  upgrades: Stat[];
};

/** BrickHammer -> BRICK_HAMMER, matching the loc key convention (and tools/cards.py's snake()). */
export function snake(name: string): string {
  return name.replace(/(?<!^)(?=[A-Z])/g, "_").toUpperCase();
}

/* Find the class body's `{ … }` span by brace matching, skipping over comments and string literals
 * so a brace inside `"{Damage}"` or a `/* … *\/` block can't throw off the count. Bounding the body
 * properly matters: without it we'd scan until the next card class and happily attribute a helper
 * class's numbers to the card above it. */
function bodyOf(text: string, from: number): Span | null {
  let i = text.indexOf("{", from);
  if (i === -1) return null;
  const start = i;
  let depth = 0;

  while (i < text.length) {
    const c = text[i];
    const next = text[i + 1];

    if (c === "/" && next === "/") { i = text.indexOf("\n", i); if (i === -1) return null; continue; }
    if (c === "/" && next === "*") { i = text.indexOf("*/", i); if (i === -1) return null; i += 2; continue; }
    if (c === '"' || c === "'") {
      const quote = c;
      i++;
      while (i < text.length && text[i] !== quote) i += text[i] === "\\" ? 2 : 1;
      i++;
      continue;
    }
    if (c === "{") depth++;
    else if (c === "}" && --depth === 0) return { start, end: i + 1 };
    i++;
  }
  return null;
}

function stat(name: string, m: RegExpExecArray, group: number, offset = 0): Stat {
  const [s, e] = m.indices![group]!;
  return { name, value: m[group]!, start: s + offset, end: e + offset };
}

/** Parse every card class in one .cs file. */
export function parseFile(text: string, file: string): CardSource[] {
  const out: CardSource[] = [];
  CARD_RE.lastIndex = 0;

  for (let m = CARD_RE.exec(text); m; m = CARD_RE.exec(text)) {
    const body = bodyOf(text, m.index + m[0].length);
    const chunk = body ? text.slice(body.start, body.end) : "";
    const at = body?.start ?? 0;

    const vars: Stat[] = [];
    VAR_RE.lastIndex = 0;
    for (let v = VAR_RE.exec(chunk); v; v = VAR_RE.exec(chunk)) {
      // IntVar carries its name as a string arg; DamageVar/BlockVar ARE their name.
      vars.push(stat(v[2] ?? v[1]!, v, 3, at));
    }

    const upgrades: Stat[] = [];
    UPGRADE_RE.lastIndex = 0;
    for (let u = UPGRADE_RE.exec(chunk); u; u = UPGRADE_RE.exec(chunk)) {
      upgrades.push(stat((u[1] ?? u[2])!, u, 3, at));
    }

    out.push({
      cls: m[1]!,
      base: m[2]!,
      file,
      locId: `KNIFEHERO-${snake(m[1]!)}`,
      cost: stat("cost", m, 3),
      rarity: stat("rarity", m, 5),
      type: m[4]!,
      target: m[6]!,
      vars,
      upgrades,
    });
  }
  return out;
}

async function* csFiles(dir: string): AsyncGenerator<string> {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) yield* csFiles(path);
    else if (entry.name.endsWith(".cs")) yield path;
  }
}

/** Every card class in the mod, keyed by loc id. */
export async function scan(codeRoot: string, repo: string): Promise<Map<string, CardSource>> {
  const cards = new Map<string, CardSource>();
  for await (const path of csFiles(codeRoot)) {
    const text = await readFile(path, "utf8");
    for (const card of parseFile(text, relative(repo, path))) cards.set(card.locId, card);
  }
  return cards;
}

export type StatEdit = {
  cls: string;
  kind: "cost" | "rarity" | "var" | "upgrade";
  name?: string;    // required for var/upgrade
  value: string;
};

function validate(edit: StatEdit): string | null {
  if (edit.kind === "rarity") {
    return RARITIES.includes(edit.value) ? null : `unknown rarity "${edit.value}"`;
  }
  // Costs, base values and bumps are all plain numbers. -1 cost means X/unplayable, so allow it.
  if (!/^-?\d+(\.\d+)?$/.test(edit.value)) return `"${edit.value}" is not a number`;
  return null;
}

/* Apply stat edits. Everything is re-parsed from what's on disk RIGHT NOW — no span the client is
 * holding ever gets trusted, so an edit can't land on a stale offset and corrupt a file. Edits are
 * applied back-to-front within a file so earlier splices don't shift later offsets. */
export async function applyEdits(codeRoot: string, repo: string, edits: StatEdit[]): Promise<number> {
  for (const edit of edits) {
    const bad = validate(edit);
    if (bad) throw new Error(bad);
  }

  const cards = await scan(codeRoot, repo);
  const byFile = new Map<string, StatEdit[]>();
  for (const edit of edits) {
    const card = [...cards.values()].find((c) => c.cls === edit.cls);
    if (!card) throw new Error(`no card class named ${edit.cls}`);
    byFile.set(card.file, [...(byFile.get(card.file) ?? []), edit]);
  }

  let applied = 0;
  for (const [file, fileEdits] of byFile) {
    const path = resolve(repo, file);
    let text = await readFile(path, "utf8");

    const spans: Array<Span & { value: string }> = [];
    for (const edit of fileEdits) {
      // Re-parse per edit so each one is resolved against the CURRENT text, not a cached scan.
      const card = parseFile(text, file).find((c) => c.cls === edit.cls);
      if (!card) throw new Error(`no card class named ${edit.cls} in ${file}`);

      const target =
        edit.kind === "cost" ? card.cost
        : edit.kind === "rarity" ? card.rarity
        : (edit.kind === "var" ? card.vars : card.upgrades).find((s) => s.name === edit.name);
      if (!target) throw new Error(`${edit.cls} has no ${edit.kind} "${edit.name}"`);

      if (target.value !== edit.value) spans.push({ ...target, value: edit.value });
    }

    if (spans.length === 0) continue;
    spans.sort((a, b) => b.start - a.start);   // back-to-front keeps offsets valid
    for (const s of spans) text = text.slice(0, s.start) + s.value + text.slice(s.end);

    const tmp = `${path}.tmp`;
    await writeFile(tmp, text, "utf8");
    await rename(tmp, path);
    applied += spans.length;
  }
  return applied;
}
