import test from 'node:test';
import assert from 'node:assert/strict';
import { MAX_NOTES, MAX_NOTE_BODY_LENGTH, parseNotesSettings, readNotesSettings, validateNotesSettings } from '../ui/contracts/notes.ts';
import { countCheckboxes, hasUntickedCheckbox, parseInline, parseMarkdown, toggleCheckbox } from '../ui/modules/notesMarkdown.ts';

const todo = { id: 'todo', title: 'Todo', body: '- [ ] first\n- [x] second' };
const plain = { id: 'plain', title: 'Plain', body: 'Just text' };

test('parses notes without changing their order or text', () => {
  const settings = { notes: [todo, plain] };
  assert.deepEqual(parseNotesSettings(settings), settings);
});

test('allows an empty body, line breaks and tabs, but no other control characters', () => {
  assert.deepEqual(validateNotesSettings({ notes: [{ ...plain, body: '' }, { ...todo, body: 'a\r\n\tb' }] }), []);
  for (const body of ['bell\u0007', 'nul\u0000', 'escape\u001b[0m']) {
    assert.ok(validateNotesSettings({ notes: [{ ...plain, body }] }).some((error) => /text/i.test(error)), `${JSON.stringify(body)} should be rejected`);
  }
});

test('requires a title of at most 128 characters without control characters', () => {
  for (const title of ['', '   ', 'a'.repeat(129), 'tab\there']) {
    assert.ok(validateNotesSettings({ notes: [{ ...plain, title }] }).some((error) => /title/i.test(error)), `${JSON.stringify(title)} should be rejected`);
  }
});

test('limits the body length', () => {
  assert.deepEqual(validateNotesSettings({ notes: [{ ...plain, body: 'a'.repeat(MAX_NOTE_BODY_LENGTH) }] }), []);
  assert.ok(validateNotesSettings({ notes: [{ ...plain, body: 'a'.repeat(MAX_NOTE_BODY_LENGTH + 1) }] }).length > 0);
});

test('requires a unique valid key for each note', () => {
  assert.ok(validateNotesSettings({ notes: [todo, { ...plain, id: 'todo' }] }).some((error) => /key/i.test(error)));
  assert.ok(validateNotesSettings({ notes: [{ ...plain, id: 'bad id' }] }).some((error) => /key/i.test(error)));
});

test('rejects malformed settings, too many notes, and an oversized payload', () => {
  assert.throws(() => parseNotesSettings({ notes: 'not-an-array' }));
  assert.throws(() => parseNotesSettings({}));
  const many = Array.from({ length: MAX_NOTES + 1 }, (_, index) => ({ ...plain, id: `n${index}` }));
  assert.ok(validateNotesSettings({ notes: many }).length > 0);
  const large = Array.from({ length: 10 }, (_, index) => ({ ...plain, id: `n${index}`, body: 'x'.repeat(MAX_NOTE_BODY_LENGTH) }));
  assert.ok(validateNotesSettings({ notes: large }).some((error) => /64 KiB/.test(error)));
});

test('reads an invalid saved list as empty instead of exposing partial data', () => {
  assert.deepEqual(readNotesSettings({ notes: [todo, { ...plain, title: '' }] }), { notes: [] });
  assert.deepEqual(readNotesSettings(undefined), { notes: [] });
});

test('counts unticked checkboxes and ignores checkbox-like text in code and prose', () => {
  assert.deepEqual(countCheckboxes('- [ ] a\n* [x] b\n+ [X] c\n1. [ ] d'), { total: 4, unticked: 2 });
  assert.deepEqual(countCheckboxes('no boxes\n- plain\n[ ] not a list item\n- [] empty'), { total: 0, unticked: 0 });
  assert.deepEqual(countCheckboxes('```\n- [ ] in code\n```\n- [x] real'), { total: 1, unticked: 0 });
  assert.equal(hasUntickedCheckbox('- [x] a\n- [x] b'), false);
  assert.equal(hasUntickedCheckbox('- [x] a\n- [ ] b'), true);
  assert.equal(hasUntickedCheckbox(''), false);
});

test('toggles one checkbox line and keeps the rest of the note byte for byte', () => {
  const body = 'Title\r\n- [ ] first\r\n- [x] second *kept*\r\n\r\n```\r\n- [ ] code\r\n```';
  assert.equal(toggleCheckbox(body, 1), body.replace('- [ ] first', '- [x] first'));
  assert.equal(toggleCheckbox(body, 2), body.replace('- [x] second', '- [ ] second'));
  assert.equal(toggleCheckbox(toggleCheckbox(body, 1), 1), body);
  assert.equal(toggleCheckbox(body, 0), body, 'a line that is not a checkbox is unchanged');
  assert.equal(toggleCheckbox(body, 5), body, 'a checkbox inside code is unchanged');
  assert.equal(toggleCheckbox(body, 99), body);
  assert.equal(toggleCheckbox(body, -1), body);
});

test('renders blocks as a tree and records the line of each task', () => {
  const blocks = parseMarkdown('# Title\n\nSome **bold** text\nsecond line\n\n- [ ] one\n- [x] two\n- plain\n\n1. a\n2. b\n\n> quote\n\n---\n\n```js\nconst x = 1;\n```');
  assert.deepEqual(blocks.map((block) => block.kind), ['heading', 'paragraph', 'list', 'list', 'quote', 'rule', 'code']);
  assert.equal(blocks[0].level, 1);
  assert.deepEqual(blocks[2].items.map((item) => item.task), [{ checked: false, line: 5 }, { checked: true, line: 6 }, undefined]);
  assert.equal(blocks[3].ordered, true);
  assert.equal(blocks[6].text, 'const x = 1;');
});

test('parses inline markup and only links http and https addresses', () => {
  assert.deepEqual(parseInline('a **b** *c* ~~d~~ `e`'), [
    { kind: 'text', text: 'a ' }, { kind: 'strong', children: [{ kind: 'text', text: 'b' }] }, { kind: 'text', text: ' ' },
    { kind: 'em', children: [{ kind: 'text', text: 'c' }] }, { kind: 'text', text: ' ' },
    { kind: 'del', children: [{ kind: 'text', text: 'd' }] }, { kind: 'text', text: ' ' }, { kind: 'code', text: 'e' },
  ]);
  assert.deepEqual(parseInline('[site](https://example.com/a?b=1)'), [{ kind: 'link', href: 'https://example.com/a?b=1', children: [{ kind: 'text', text: 'site' }] }]);
  assert.deepEqual(parseInline('[bad](javascript:alert(1))'), [{ kind: 'text', text: '[bad](javascript:alert(1))' }]);
  assert.deepEqual(parseInline('snake_case_name <b>x</b>'), [{ kind: 'text', text: 'snake_case_name <b>x</b>' }]);
});
