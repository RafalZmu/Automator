import assert from 'node:assert/strict';
import { test } from 'node:test';
import { computeOverlayBounds, defaultOverlayAnchor, formatOverlayDuration, isOverlayPosition, normalizeOverlayColor, overlayHtml, overlayItemsScript, overlayShadow, overlayStyleScript, readOverlayTimers, resolveOverlayAnchor, timerOverlayItems, DEFAULT_OVERLAY_COLOR, OVERLAY_LINE_HEIGHT, OVERLAY_MAX_ITEMS, OVERLAY_PADDING_Y, OVERLAY_WIDTH } from '../electron/screenOverlay.ts';

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

test('the default anchor is the bottom-right of the work area and the window grows up and to the left of it', () => {
  const anchor = defaultOverlayAnchor({ x: 0, y: 0, width: 1920, height: 1040 });
  assert.deepEqual(anchor, { x: 1920 - 12, y: 1040 - 12 });
  const bounds = computeOverlayBounds(anchor);
  assert.equal(bounds.width, OVERLAY_WIDTH);
  assert.equal(bounds.height, OVERLAY_MAX_ITEMS * OVERLAY_LINE_HEIGHT + 2 * OVERLAY_PADDING_Y);
  assert.equal(bounds.x + bounds.width, anchor.x);
  assert.equal(bounds.y + bounds.height, anchor.y);
});

test('the window shrinks to the measured text, keeps its anchor and never exceeds the largest size', () => {
  const anchor = { x: 2000, y: 900 };
  const bounds = computeOverlayBounds(anchor, { width: 240.2, height: 44 });
  assert.deepEqual({ width: bounds.width, height: bounds.height }, { width: 241, height: 44 });
  assert.equal(bounds.x + bounds.width, 2000);
  assert.equal(bounds.y + bounds.height, 900);
  assert.equal(computeOverlayBounds(anchor, { width: 9999, height: 9999 }).width, OVERLAY_WIDTH);
});

test('a remembered position is kept on a visible display, otherwise the primary display default is used', () => {
  const primary = { x: 0, y: 0, width: 1920, height: 1040 };
  const second = { x: 1920, y: -200, width: 2560, height: 1400 };
  assert.deepEqual(resolveOverlayAnchor({ x: 2500, y: 300 }, [primary, second], primary), { x: 2500, y: 300 });
  assert.deepEqual(resolveOverlayAnchor({ x: 2500, y: 300 }, [primary], primary), defaultOverlayAnchor(primary));
  // The corner may sit over the taskbar or on the very edge of the screen (outside the work area) and is kept.
  assert.deepEqual(resolveOverlayAnchor({ x: 1920, y: 1080 }, [{ ...primary, height: 1080 }], primary), { x: 1920, y: 1080 });
  assert.deepEqual(resolveOverlayAnchor(null, [primary, second], primary), defaultOverlayAnchor(primary));
});

test('overlay colour falls back to white and only accepts #rrggbb', () => {
  assert.equal(normalizeOverlayColor('#ABCDEF'), '#abcdef');
  assert.equal(normalizeOverlayColor('red'), DEFAULT_OVERLAY_COLOR);
  assert.equal(normalizeOverlayColor(undefined), DEFAULT_OVERLAY_COLOR);
  assert.equal(isOverlayPosition({ x: 1, y: 2 }), true);
  assert.equal(isOverlayPosition({ x: 1, y: NaN }), false);
});

test('overlay page is text only with a halo, sets text safely and runs no script of its own', () => {
  const html = overlayHtml('#102030');
  assert.ok(html.includes('--overlay-text:#102030'));
  assert.ok(html.includes('background:transparent'));
  assert.ok(!html.includes('backdrop-filter') && !html.includes('var(--overlay-bg)'));
  assert.ok(!html.includes('<script'));
  const script = overlayItemsScript(['</div><img src=x onerror=1>']);
  assert.ok(script.includes('textContent'));
  assert.ok(!script.includes('innerHTML'));
});

test('the halo is dark behind light text and light behind dark text', () => {
  assert.ok(overlayShadow('#ffffff').startsWith('rgba(0,0,0'));
  assert.ok(overlayShadow('#101418').startsWith('rgba(255,255,255'));
});

test('the style script recolours the page and toggles move mode', () => {
  const moving = overlayStyleScript('#ff0000', true);
  assert.ok(moving.includes('"#ff0000"') && moving.includes("toggle('moving',true)"));
  assert.ok(overlayStyleScript('#ff0000', false).includes("toggle('moving',false)"));
});
