/**
 * A small markdown reader for Quick notes. It produces a plain tree that the view turns into React elements, never HTML, so note
 * text cannot inject markup. It supports headings, paragraphs, block quotes, rules, fenced code, bullet, numbered and task lists,
 * and bold (**), italic (*), strikethrough, inline code, and http(s) links.
 */
export type Inline =
  | { kind: 'text'; text: string }
  | { kind: 'strong' | 'em' | 'del'; children: Inline[] }
  | { kind: 'code'; text: string }
  | { kind: 'link'; href: string; children: Inline[] };

export type ListItem = { inlines: Inline[]; task?: { checked: boolean; line: number } };

export type Block =
  | { kind: 'heading'; level: number; inlines: Inline[] }
  | { kind: 'paragraph'; inlines: Inline[] }
  | { kind: 'quote'; inlines: Inline[] }
  | { kind: 'rule' }
  | { kind: 'code'; text: string }
  | { kind: 'list'; ordered: boolean; items: ListItem[] };

const FENCE = /^\s*(`{3,}|~{3,})/;
const TASK = /^(\s*(?:[-*+]|\d+[.)])\s+)\[([ xX])\](\s|$)/;
const BULLET = /^\s*[-*+]\s+(.*)$/;
const NUMBERED = /^\s*\d+[.)]\s+(.*)$/;
const HEADING = /^\s{0,3}(#{1,6})\s+(.*?)\s*#*\s*$/;
const RULE = /^\s{0,3}([-*_])(?:\s*\1){2,}\s*$/;

const splitLines = (body: string) => body.split(/\r\n|\r|\n/);

/** Line indexes (into the body's lines) that belong to fenced code, which never count as checkboxes. */
function codeLineFlags(lines: readonly string[]): boolean[] {
  const flags: boolean[] = [];
  let fence: string | null = null;
  for (const line of lines) {
    const match = FENCE.exec(line);
    if (fence === null) {
      flags.push(match !== null);
      if (match) fence = match[1][0];
    } else {
      flags.push(true);
      if (match && match[1][0] === fence && line.trim().replace(/[`~]/g, '') === '') fence = null;
    }
  }
  return flags;
}

export function countCheckboxes(body: string): { total: number; unticked: number } {
  const lines = splitLines(body);
  const code = codeLineFlags(lines);
  let total = 0;
  let unticked = 0;
  lines.forEach((line, index) => {
    if (code[index]) return;
    const match = TASK.exec(line);
    if (!match) return;
    total += 1;
    if (match[2] === ' ') unticked += 1;
  });
  return { total, unticked };
}

/** True when the note has at least one markdown checkbox that is not ticked. */
export function hasUntickedCheckbox(body: string): boolean {
  return countCheckboxes(body).unticked > 0;
}

/** Flips the checkbox on the given body line. Returns the body unchanged when that line is not a checkbox. */
export function toggleCheckbox(body: string, line: number): string {
  const parts = body.split(/(\r\n|\r|\n)/);
  const index = line * 2;
  if (!Number.isInteger(line) || line < 0 || index >= parts.length) return body;
  const code = codeLineFlags(splitLines(body));
  if (code[line]) return body;
  const match = TASK.exec(parts[index]);
  if (!match) return body;
  const mark = match[2] === ' ' ? 'x' : ' ';
  parts[index] = `${match[1]}[${mark}]${parts[index].slice(match[0].length - match[3].length)}`;
  return parts.join('');
}

const INLINE = /(`+)([^`]|[^`][\s\S]*?[^`])\1(?!`)|\[([^\]]+)\]\((https?:\/\/[^\s)]+)\)|(\*\*)(?=\S)([\s\S]*?\S)\5|~~(?=\S)([\s\S]*?\S)~~|(\*)(?=\S)([\s\S]*?\S)\8/g;

export function parseInline(text: string): Inline[] {
  const out: Inline[] = [];
  let last = 0;
  for (const match of text.matchAll(INLINE)) {
    const start = match.index ?? 0;
    if (start > last) out.push({ kind: 'text', text: text.slice(last, start) });
    if (match[2] !== undefined) out.push({ kind: 'code', text: match[2].trim() });
    else if (match[3] !== undefined) out.push({ kind: 'link', href: match[4], children: parseInline(match[3]) });
    else if (match[6] !== undefined) out.push({ kind: 'strong', children: parseInline(match[6]) });
    else if (match[7] !== undefined) out.push({ kind: 'del', children: parseInline(match[7]) });
    else out.push({ kind: 'em', children: parseInline(match[9]) });
    last = start + match[0].length;
  }
  if (last < text.length) out.push({ kind: 'text', text: text.slice(last) });
  return out;
}

export function parseMarkdown(body: string): Block[] {
  const lines = splitLines(body);
  const blocks: Block[] = [];
  let paragraph: string[] = [];
  let quote: string[] = [];
  let list: { ordered: boolean; items: ListItem[] } | null = null;
  const flush = () => {
    if (paragraph.length) blocks.push({ kind: 'paragraph', inlines: parseInline(paragraph.join(' ')) });
    if (quote.length) blocks.push({ kind: 'quote', inlines: parseInline(quote.join(' ')) });
    if (list) blocks.push({ kind: 'list', ...list });
    paragraph = [];
    quote = [];
    list = null;
  };

  for (let index = 0; index < lines.length; index++) {
    const line = lines[index];
    const fence = FENCE.exec(line);
    if (fence) {
      flush();
      const code: string[] = [];
      for (index++; index < lines.length; index++) {
        const close = FENCE.exec(lines[index]);
        if (close && close[1][0] === fence[1][0] && lines[index].trim().replace(/[`~]/g, '') === '') break;
        code.push(lines[index]);
      }
      blocks.push({ kind: 'code', text: code.join('\n') });
      continue;
    }
    if (line.trim() === '') { flush(); continue; }
    const heading = HEADING.exec(line);
    if (heading) { flush(); blocks.push({ kind: 'heading', level: heading[1].length, inlines: parseInline(heading[2]) }); continue; }
    if (RULE.test(line)) { flush(); blocks.push({ kind: 'rule' }); continue; }
    if (/^\s{0,3}>/.test(line)) {
      if (paragraph.length || list) flush();
      quote.push(line.replace(/^\s{0,3}>\s?/, ''));
      continue;
    }
    const task = TASK.exec(line);
    const bullet = BULLET.exec(line);
    const numbered = NUMBERED.exec(line);
    if (task || bullet || numbered) {
      const ordered = /^\s*\d/.test(line);
      if (paragraph.length || quote.length || (list && list.ordered !== ordered)) flush();
      list ??= { ordered, items: [] };
      if (task) list.items.push({ inlines: parseInline(line.slice(task[0].length - task[3].length).trim()), task: { checked: task[2] !== ' ', line: index } });
      else list.items.push({ inlines: parseInline((bullet ?? numbered)![1]) });
      continue;
    }
    if (quote.length || list) flush();
    paragraph.push(line.trim());
  }
  flush();
  return blocks;
}
