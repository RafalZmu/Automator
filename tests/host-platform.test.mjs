import assert from 'node:assert/strict';
import { test } from 'node:test';
import { computePanelBounds, getWindowsDisplayCapabilities } from '../electron/platform.ts';

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
    { width: 520, height: 550 },
  );
  assert.deepEqual(bounds, { x: -1280, y: 0, width: 420, height: 300 });
});

test('panel placement remains inside a negative-origin secondary monitor', () => {
  const bounds = computePanelBounds(
    { x: -10, y: 1070 },
    { x: -1920, y: 0, width: 1920, height: 1080 },
    { width: 520, height: 550 },
  );
  assert.ok(bounds.x >= -1920 && bounds.x + bounds.width <= 0);
  assert.ok(bounds.y >= 0 && bounds.y + bounds.height <= 1080);
});
