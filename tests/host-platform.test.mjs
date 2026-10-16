import assert from 'node:assert/strict';
import { test } from 'node:test';
import { computePanelBounds, computeSidePanelBounds, easeOutCubic, getWindowsDisplayCapabilities, interpolateBounds } from '../electron/platform.ts';

test('Windows 10 keeps transparent outer corners with a solid CSS surface', () => {
  assert.deepEqual(getWindowsDisplayCapabilities('win32', '10.0.19045'), {
    acrylicSupported: false,
    nativeCornersSupported: false,
    transparentWindow: true,
    backgroundColor: '#00000000',
    backgroundMaterial: undefined,
  });
});

test('Windows 11 21H2 uses an opaque solid surface with native corners', () => {
  const capability = getWindowsDisplayCapabilities('win32', '10.0.22000');
  assert.equal(capability.acrylicSupported, false);
  assert.equal(capability.nativeCornersSupported, true);
  assert.equal(capability.transparentWindow, false);
  assert.equal(capability.backgroundColor, '#F0F5F6');
});

test('Windows 11 22H2 enables native Acrylic without a transparent window', () => {
  const capability = getWindowsDisplayCapabilities('win32', '10.0.22621');
  assert.equal(capability.acrylicSupported, true);
  assert.equal(capability.nativeCornersSupported, true);
  assert.equal(capability.transparentWindow, false);
  assert.equal(capability.backgroundMaterial, 'acrylic');
});

test('panel placement clamps both its origin and dimensions to small work areas', () => {
  const bounds = computePanelBounds(
    { x: 500, y: 400 },
    { x: -1280, y: 0, width: 420, height: 300 },
    { width: 760, height: 800 },
  );
  assert.deepEqual(bounds, { x: -1280, y: 0, width: 420, height: 300 });
});

test('panel placement remains inside a negative-origin secondary monitor', () => {
  const bounds = computePanelBounds(
    { x: -10, y: 1070 },
    { x: -1920, y: 0, width: 1920, height: 1080 },
    { width: 760, height: 800 },
  );
  assert.deepEqual({ width: bounds.width, height: bounds.height }, { width: 760, height: 800 });
  assert.ok(bounds.x >= -1920 && bounds.x + bounds.width <= 0);
  assert.ok(bounds.y >= 0 && bounds.y + bounds.height <= 1080);
});

test('side panel grows left without moving the content and shrinks back to the same window', () => {
  const workArea = { x: 0, y: 0, width: 1920, height: 1040 };
  const closed = { x: 600, y: 100, width: 760, height: 800 };
  const open = computeSidePanelBounds(closed, workArea, 760, 300, true);
  assert.deepEqual(open, { x: 300, y: 100, width: 1060, height: 800 });
  assert.equal(open.x + 300, closed.x);
  assert.deepEqual(computeSidePanelBounds(open, workArea, 760, 300, false), closed);
});

test('side panel clamps to the left edge and the right edge of the work area', () => {
  const workArea = { x: 100, y: 0, width: 1920, height: 1040 };
  const nearLeft = computeSidePanelBounds({ x: 150, y: 10, width: 760, height: 800 }, workArea, 760, 300, true);
  assert.deepEqual(nearLeft, { x: 100, y: 10, width: 1060, height: 800 });
  // The panel needs room on the left, so the content shifts right; closing leaves it where it ended up.
  assert.deepEqual(computeSidePanelBounds(nearLeft, workArea, 760, 300, false), { x: 400, y: 10, width: 760, height: 800 });
  const nearRight = computeSidePanelBounds({ x: 1300, y: 0, width: 760, height: 800 }, workArea, 760, 300, true);
  assert.equal(nearRight.x + nearRight.width, 2020);
  const tiny = computeSidePanelBounds({ x: 0, y: 0, width: 500, height: 400 }, { x: 0, y: 0, width: 600, height: 500 }, 760, 300, true);
  assert.deepEqual(tiny, { x: 0, y: 0, width: 600, height: 400 });
});

test('side panel animation eases from start to end and rounds to whole pixels', () => {
  const from = { x: 600, y: 100, width: 760, height: 800 };
  const to = { x: 300, y: 100, width: 1060, height: 800 };
  assert.deepEqual(interpolateBounds(from, to, 0), from);
  assert.deepEqual(interpolateBounds(from, to, 1), to);
  assert.deepEqual(interpolateBounds(from, to, 5), to);
  const half = interpolateBounds(from, to, 0.5);
  assert.ok(half.x < 600 && half.x > 300 && Math.abs(half.x + half.width - 1360) <= 1);
  assert.ok(easeOutCubic(0.5) > 0.5);
});
