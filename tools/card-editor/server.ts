/* CARD EDITOR — a tiny local web app for browsing and editing the mod's localization text.
 *
 * The loc files are flat maps of "ID.field" -> string (see KnifeHero/localization/eng/*.json). This
 * server groups those keys by ID so the UI can show one editable block per card/power/relic, then
 * writes the file back with the original key ORDER preserved — the diffs stay readable, which is the
 * whole point of editing text in a browser instead of by hand.
 *
 * Zero dependencies: node's http + fs, TypeScript stripped at runtime.
 *   node --experimental-strip-types tools/card-editor/server.ts     (or: npm start)
 */
import { createServer } from "node:http";
import { readFile, writeFile, readdir, rename } from "node:fs/promises";
import { join, dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { scan, applyEdits, RARITIES, type StatEdit } from "./source.ts";

const HERE = dirname(fileURLToPath(import.meta.url));
const REPO = resolve(HERE, "..", "..");
const LOC = join(REPO, "KnifeHero", "localization", "eng");
const CODE = join(REPO, "KnifeHeroCode");
const PORT = Number(process.env.PORT ?? 4400);

type LocFile = Record<string, string>;

/* Only ever touch .json files directly inside the loc dir. `resolve` collapses any ".." a caller
 * sneaks into the name, and we then require the result to still live in LOC. */
function locPath(name: string): string {
  const path = resolve(LOC, name);
  if (!path.startsWith(LOC + "/") || !path.endsWith(".json")) {
    throw new Error(`refusing to touch ${name}`);
  }
  return path;
}

async function readLoc(name: string): Promise<LocFile> {
  return JSON.parse(await readFile(locPath(name), "utf8"));
}

/* Write via a temp file + rename so an interrupted save can't leave a half-written loc file that the
 * game would fail to parse. Key order comes from `order` (the file as it was) with any brand-new keys
 * appended, so a save is a minimal diff rather than a reshuffle. */
async function writeLoc(name: string, data: LocFile, order: string[], trailingNewline: boolean): Promise<void> {
  const seen = new Set(order);
  const keys = [...order.filter((k) => k in data), ...Object.keys(data).filter((k) => !seen.has(k))];
  const out: LocFile = {};
  for (const k of keys) out[k] = data[k];

  const path = locPath(name);
  const tmp = `${path}.tmp`;
  // Match the file's existing trailing-newline habit — the loc files don't have one, and silently
  // adding it makes every save show a spurious last-line change in the diff.
  await writeFile(tmp, JSON.stringify(out, null, 2) + (trailingNewline ? "\n" : ""), "utf8");
  await rename(tmp, path);
}

/* "KNIFEHERO-FANCY_FOOTWORK.description" -> id "KNIFEHERO-FANCY_FOOTWORK", field "description".
 * Split on the FIRST dot: ids contain no dots, but a field can (e.g. "upgrade.description"). */
function group(data: LocFile) {
  const entries = new Map<string, { id: string; fields: Record<string, string> }>();
  for (const [key, value] of Object.entries(data)) {
    const dot = key.indexOf(".");
    const id = dot === -1 ? key : key.slice(0, dot);
    const field = dot === -1 ? "" : key.slice(dot + 1);
    const entry = entries.get(id) ?? { id, fields: {} };
    entry.fields[field] = value;
    entries.set(id, entry);
  }
  return [...entries.values()];
}

function json(res: import("node:http").ServerResponse, code: number, body: unknown): void {
  const payload = JSON.stringify(body);
  res.writeHead(code, { "content-type": "application/json", "content-length": Buffer.byteLength(payload) });
  res.end(payload);
}

async function body(req: import("node:http").IncomingMessage): Promise<string> {
  const chunks: Buffer[] = [];
  for await (const chunk of req) chunks.push(chunk as Buffer);
  return Buffer.concat(chunks).toString("utf8");
}

const server = createServer(async (req, res) => {
  const url = new URL(req.url ?? "/", `http://${req.headers.host}`);
  try {
    if (url.pathname === "/" || url.pathname === "/index.html") {
      const html = await readFile(join(HERE, "index.html"));
      res.writeHead(200, { "content-type": "text/html; charset=utf-8" });
      return void res.end(html);
    }

    // The list of editable loc files.
    if (url.pathname === "/api/files") {
      const files = (await readdir(LOC)).filter((f) => f.endsWith(".json")).sort();
      return json(res, 200, { files });
    }

    /* One file, grouped into entries for display. For cards.json we also join in each card's stats
     * parsed out of its C# class, so cost/rarity/values/upgrades edit alongside the text. Other loc
     * files have no source counterpart and just come back as text. */
    if (url.pathname === "/api/file" && req.method === "GET") {
      const name = url.searchParams.get("name") ?? "cards.json";
      const data = await readLoc(name);
      const entries = group(data);
      if (name === "cards.json") {
        const cards = await scan(CODE, REPO);
        for (const entry of entries) {
          const src = cards.get(entry.id);
          if (src) (entry as Record<string, unknown>).source = src;
        }
      }
      return json(res, 200, { name, entries, rarities: RARITIES });
    }

    /* Save. The client sends only the keys it CHANGED, so two tabs editing different cards don't
     * clobber each other — we re-read the file and apply the patch on top of what's on disk now. */
    if (url.pathname === "/api/file" && req.method === "PUT") {
      const name = url.searchParams.get("name") ?? "cards.json";
      const patch = JSON.parse(await body(req)) as LocFile;
      const raw = await readFile(locPath(name), "utf8");
      const data = JSON.parse(raw) as LocFile;
      const order = Object.keys(data);
      for (const [key, value] of Object.entries(patch)) data[key] = value;
      await writeLoc(name, data, order, raw.endsWith("\n"));
      return json(res, 200, { saved: Object.keys(patch).length });
    }

    /* Stat edits — these write to .cs files, so they're a separate endpoint from the loc text and
     * every edit is re-resolved against the current file inside applyEdits. */
    if (url.pathname === "/api/stats" && req.method === "PUT") {
      const edits = JSON.parse(await body(req)) as StatEdit[];
      const applied = await applyEdits(CODE, REPO, edits);
      return json(res, 200, { applied });
    }

    json(res, 404, { error: "not found" });
  } catch (err) {
    json(res, 500, { error: String(err instanceof Error ? err.message : err) });
  }
});

server.listen(PORT, "127.0.0.1", () => {
  console.log(`card editor → http://127.0.0.1:${PORT}  (editing ${LOC})`);
});
