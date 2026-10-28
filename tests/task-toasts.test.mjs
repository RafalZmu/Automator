import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { test } from 'node:test';
import { computeToastBounds, createToastTracker, toastContent, toastHtml, TOAST_HEIGHT, TOAST_WIDTH } from '../electron/taskToasts.ts';
import { loadNotificationPrefs, saveNotificationPrefs } from '../electron/notificationPrefs.ts';

const entry = (id, finished, extra = {}) => ({ id, moduleId: 'workflows', actionId: 'runProfile', profileId: 'Nightly backup',
  startedUtc: '2026-10-04T12:00:00Z', finishedUtc: finished, status: 'success', durationMilliseconds: 1000, origin: 'manual', ...extra });

test('tracker ignores history on first snapshot, then returns only new runs oldest first', () => {
  const tracker = createToastTracker();
  assert.deepEqual(tracker.next([entry('a', '2026-10-04T12:00:01Z')]), []);
  assert.deepEqual(tracker.next([entry('a', '2026-10-04T12:00:01Z')]), []);
  const fresh = tracker.next([entry('c', '2026-10-04T12:00:09Z'), entry('b', '2026-10-04T12:00:05Z'), entry('a', '2026-10-04T12:00:01Z')]);
  assert.deepEqual(fresh.map((item) => item.id), ['b', 'c']);
  assert.deepEqual(tracker.next([entry('c', '2026-10-04T12:00:09Z')]), []);
});

test('toast content is fixed metadata text with success or failure outcome', () => {
  assert.deepEqual(toastContent(entry('a', '2026-10-04T12:00:01Z')), { title: 'Nightly backup', detail: 'Succeeded · Workflows', tone: 'success' });
  assert.equal(toastContent(entry('a', '2026-10-04T12:00:01Z', { status: 'error', profileId: null })).title, 'runProfile');
  assert.equal(toastContent(entry('a', '2026-10-04T12:00:01Z', { status: 'error', origin: 'scheduled' })).detail, 'Failed · Workflows');
});

test('toasts anchor to the bottom-right of the work area and stack upward', () => {
  const area = { x: 0, y: 0, width: 1920, height: 1040 };
  const first = computeToastBounds(area, 0);
  const second = computeToastBounds(area, 1);
  assert.equal(first.x + TOAST_WIDTH, 1920 - 16);
  assert.equal(first.y + TOAST_HEIGHT, 1040 - 16);
  assert.ok(second.y + TOAST_HEIGHT < first.y);
  assert.equal(computeToastBounds({ x: 100, y: 50, width: 800, height: 600 }, 0).x, 100 + 800 - TOAST_WIDTH - 16);
});

test('toast html escapes task names and carries no script', () => {
  const html = toastHtml({ title: '<img src=x onerror=alert(1)>', detail: 'a & b', tone: 'error' }, true);
  assert.ok(!html.includes('<img'));
  assert.ok(html.includes('&lt;img'));
  assert.ok(!html.includes('<script'));
});

test('notification preferences default on and persist the toggle', () => {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'prefs-'));
  const file = path.join(dir, 'notifications.json');
  assert.deepEqual(loadNotificationPrefs(file), { taskToasts: true, timerOverlay: true, overlayColor: '#ffffff', overlayPosition: null });
  saveNotificationPrefs(file, { taskToasts: false, timerOverlay: false, overlayColor: '#112233', overlayPosition: { x: 1500, y: 800 } });
  assert.deepEqual(loadNotificationPrefs(file), { taskToasts: false, timerOverlay: false, overlayColor: '#112233', overlayPosition: { x: 1500, y: 800 } });
  fs.writeFileSync(file, '{bad');
  assert.deepEqual(loadNotificationPrefs(file), { taskToasts: true, timerOverlay: true, overlayColor: '#ffffff', overlayPosition: null });
  fs.rmSync(dir, { recursive: true, force: true });
});
