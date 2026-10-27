import assert from 'node:assert/strict';
import { test } from 'node:test';
import { computeOverlayBounds, overlayItemsScript, formatOverlayDuration, overlayHtml, readOverlayTimers, timerOverlayItems, OVERLAY_LINE_HEIGHT, OVERLAY_MAX_ITEMS, OVERLAY_WIDTH } from '../electron/screenOverlay.ts';

const timer = (extra = {}) => ({ id: 't1', description: 'Write report', status: 'running', elapsedMilliseconds: 61_000, sampledUtc: '2026-10-26T10:00:00Z', ...extra });
const at = (iso) => Date.parse(iso);

test('durations show hours, minutes and seconds', () => {
  assert.equal(formatOverlayDuration(0), '0:00:00');
  assert.equal(formatOverlayDuration(3_661_000), '1:01:01');
  assert.equal(formatOverlayDuration(-5), '0:00:00');
});

test('only the running timer is shown and its clock advances since the last sample', () => {
  const items = timerOverlayItems([timer(), timer({ id: 't2', status: 'paused' })], at('2026-10-26T10:00:09Z'));
  assert.deepEqual(items, [{ id: 't1', text: '0:01:10  Write report' }]);
});

test('no running timer means no overlay lines', () => {
  assert.deepEqual(timerOverlayItems([timer({ status: 'paused' })], Date.now()), []);
  assert.deepEqual(timerOverlayItems([], Date.now()), []);
});

test('long descriptions are shortened', () => {
  const [item] = timerOverlayItems([timer({ description: 'x'.repeat(100) })], at('2026-10-26T10:00:00Z'));
  assert.ok(item.text.length < 60 && item.text.endsWith('…'));
});

test('malformed snapshots are ignored', () => {
  assert.deepEqual(readOverlayTimers(null), []);
  assert.deepEqual(readOverlayTimers({ timers: [{ id: 1 }, timer()] }), [timer()]);
});

test('overlay anchors to the bottom-right of the work area', () => {
  const bounds = computeOverlayBounds({ x: 0, y: 0, width: 1920, height: 1040 });
  assert.equal(bounds.width, OVERLAY_WIDTH);
  assert.equal(bounds.height, OVERLAY_MAX_ITEMS * OVERLAY_LINE_HEIGHT);
  assert.equal(bounds.x + bounds.width, 1920 - 12);
  assert.equal(bounds.y + bounds.height, 1040 - 12);
});

test('overlay page is transparent, sets text safely and has no background', () => {
  const html = overlayHtml(true);
  assert.ok(html.includes('background:'));
  assert.ok(!html.includes('<script'));
  const script = overlayItemsScript(['</div><img src=x onerror=1>']);
  assert.ok(script.includes('textContent'));
  assert.ok(!script.includes('innerHTML'));
});
