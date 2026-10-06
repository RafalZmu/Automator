const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { test } = require('node:test');

const workspace = path.resolve(__dirname, '../..');
const shortcutUrl = pathToFileURL(path.join(workspace, 'ui', 'modules', 'workTimeShortcut.ts')).href;

test('Work Time S shortcut toggles only without modifiers and outside editable controls', async () => {
  const { isWorkTimeToggleShortcut } = await import(shortcutUrl);
  const target = (tagName, contentEditable = false) => ({ tagName, isContentEditable: contentEditable });

  assert.equal(isWorkTimeToggleShortcut({ key: 's', target: target('DIV') }), true);
  assert.equal(isWorkTimeToggleShortcut({ key: 'S', target: target('DIV') }), true);
  assert.equal(isWorkTimeToggleShortcut({ key: 's', ctrlKey: true, target: target('DIV') }), false);
  assert.equal(isWorkTimeToggleShortcut({ key: 'S', shiftKey: true, target: target('DIV') }), false);
  assert.equal(isWorkTimeToggleShortcut({ key: 's', target: target('INPUT') }), false);
  assert.equal(isWorkTimeToggleShortcut({ key: 's', target: target('DIV', true) }), false);
});
