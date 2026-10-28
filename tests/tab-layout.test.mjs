import test from 'node:test';
import assert from 'node:assert/strict';
import {
  clampRow, keyOfSlot, normalizeTabOrder, rowOfSlot, shiftRow, slotAtKey, slotsInRow, swapSlots, tabRowCount,
} from '../ui/tabLayout.ts';

const slots = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

test('rows hold nine tabs, so the tenth tab is key 1 of the second row', () => {
  const order = normalizeTabOrder(undefined, slots);
  assert.deepEqual(order, slots);
  assert.equal(tabRowCount(9), 1);
  assert.equal(tabRowCount(10), 2);
  assert.deepEqual(slotsInRow(order, 0), [1, 2, 3, 4, 5, 6, 7, 8, 9]);
  assert.deepEqual(slotsInRow(order, 1), [10]);
  assert.equal(slotAtKey(order, 1, 1), 10);
  assert.equal(slotAtKey(order, 1, 2), undefined);
  assert.equal(slotAtKey(order, 0, 0), undefined, 'there is no 0 key');
  assert.equal(keyOfSlot(order, 10), 1);
  assert.equal(rowOfSlot(order, 10), 1);
  assert.equal(rowOfSlot(order, 9), 0);
});

test('normalizing keeps the launcher first, drops unknown and repeated slots, and appends missing ones', () => {
  assert.deepEqual(normalizeTabOrder([3, 2, 3, 99, 1, 0], slots), [1, 3, 2, 4, 5, 6, 7, 8, 9, 10]);
  assert.deepEqual(normalizeTabOrder([10, 5], slots), [1, 10, 5, 2, 3, 4, 6, 7, 8, 9]);
  assert.deepEqual(normalizeTabOrder([], [1, 2]), [1, 2]);
});

test('row switching wraps around and clamps to the rows that exist', () => {
  assert.equal(shiftRow(0, 1, 10), 1);
  assert.equal(shiftRow(1, 1, 10), 0);
  assert.equal(shiftRow(0, -1, 10), 1);
  assert.equal(shiftRow(0, 1, 9), 0, 'a single row never moves');
  assert.equal(clampRow(5, 10), 1);
  assert.equal(clampRow(-2, 10), 0);
  assert.equal(clampRow(Number.NaN, 10), 0);
});

test('swapping exchanges two positions and their numbers, and never moves the launcher', () => {
  const order = normalizeTabOrder(undefined, slots);
  const swapped = swapSlots(order, 3, 2);
  assert.deepEqual(swapped, [1, 3, 2, 4, 5, 6, 7, 8, 9, 10]);
  assert.equal(keyOfSlot(swapped, 3), 2);
  assert.equal(keyOfSlot(swapped, 2), 3);
  assert.deepEqual(swapSlots(order, 1, 5), order);
  assert.deepEqual(swapSlots(order, 5, 1), order);
  assert.deepEqual(swapSlots(order, 5, 5), order);
  assert.deepEqual(swapSlots(order, 5, 77), order);
  assert.notEqual(swapSlots(order, 2, 3), order, 'the input order is not mutated');
  assert.deepEqual(order, slots);
  // Swapping across rows moves a tab onto the other row's key.
  const across = swapSlots(order, 10, 4);
  assert.equal(slotAtKey(across, 1, 1), 4);
  assert.equal(slotAtKey(across, 0, 4), 10);
});
