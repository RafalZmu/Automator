import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createReloadBudget } from '../electron/reloadBudget.ts';

test('reload budget allows three reloads per minute, denies the fourth, and recovers after the window', () => {
  let clock = 1_000;
  const budget = createReloadBudget({ now: () => clock });
  assert.equal(budget.tryConsume(), true);
  assert.equal(budget.tryConsume(), true);
  assert.equal(budget.tryConsume(), true);
  assert.equal(budget.tryConsume(), false, 'the fourth reload inside the window is denied');
  clock += 59_999;
  assert.equal(budget.tryConsume(), false, 'the budget stays spent until the oldest reload leaves the window');
  clock += 1;
  assert.equal(budget.tryConsume(), true, 'a reload is allowed again once the window has passed');
});

test('reload budget slides: each reload expires independently', () => {
  let clock = 0;
  const budget = createReloadBudget({ maxReloads: 2, windowMs: 1_000, now: () => clock });
  assert.equal(budget.tryConsume(), true);
  clock = 500;
  assert.equal(budget.tryConsume(), true);
  assert.equal(budget.tryConsume(), false);
  clock = 1_000;
  assert.equal(budget.tryConsume(), true, 'only the reload at t=0 has expired');
  assert.equal(budget.tryConsume(), false);
});

test('reload budgets are independent per window', () => {
  let clock = 0;
  const workspace = createReloadBudget({ maxReloads: 1, now: () => clock });
  const launcher = createReloadBudget({ maxReloads: 1, now: () => clock });
  assert.equal(workspace.tryConsume(), true);
  assert.equal(workspace.tryConsume(), false);
  assert.equal(launcher.tryConsume(), true, 'spending the workspace budget does not spend the launcher budget');
});
