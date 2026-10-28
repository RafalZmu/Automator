// Tab bar layout rules. Mirrors TabLayout in Automator.Core: a slot is a tab's stable module identity,
// a position is where that slot sits in the user's order, and rows hold nine positions each (keys 1 through 9).
export const TAB_ROW_SIZE = 9;
/** The launcher stays at the first position; it hosts the search box and is never moved. */
export const PINNED_SLOT = 1;

/** Returns every slot exactly once: the launcher first, then the requested order, then anything missing in slot order. */
export function normalizeTabOrder(requested: readonly number[] | undefined, slots: readonly number[]): number[] {
  const known = new Set(slots);
  const seen = new Set<number>();
  const order: number[] = [];
  const add = (slot: number) => {
    if (known.has(slot) && !seen.has(slot)) { seen.add(slot); order.push(slot); }
  };
  add(PINNED_SLOT);
  for (const slot of requested ?? []) add(slot);
  for (const slot of [...slots].sort((a, b) => a - b)) add(slot);
  return order;
}

export function tabRowCount(tabCount: number): number {
  return Math.max(1, Math.ceil(tabCount / TAB_ROW_SIZE));
}

export function clampRow(row: number, tabCount: number): number {
  return Math.min(Math.max(0, Math.trunc(row) || 0), tabRowCount(tabCount) - 1);
}

/** The slots shown in one row, in order. */
export function slotsInRow(order: readonly number[], row: number): number[] {
  return order.slice(row * TAB_ROW_SIZE, (row + 1) * TAB_ROW_SIZE);
}

/** The slot behind key 1 through 9 of a row, or undefined when that position holds no tab. */
export function slotAtKey(order: readonly number[], row: number, key: number): number | undefined {
  if (!Number.isInteger(key) || key < 1 || key > TAB_ROW_SIZE || row < 0) return undefined;
  return order[row * TAB_ROW_SIZE + key - 1];
}

export function positionOfSlot(order: readonly number[], slot: number): number {
  return order.indexOf(slot);
}

export function rowOfSlot(order: readonly number[], slot: number): number {
  const position = positionOfSlot(order, slot);
  return position < 0 ? 0 : Math.floor(position / TAB_ROW_SIZE);
}

/** The number shown on a tab and used to select it: 1 through 9 within its row. */
export function keyOfSlot(order: readonly number[], slot: number): number {
  const position = positionOfSlot(order, slot);
  return position < 0 ? 0 : (position % TAB_ROW_SIZE) + 1;
}

/** Wraps around both ends so the arrows can keep cycling. */
export function shiftRow(row: number, delta: number, tabCount: number): number {
  const rows = tabRowCount(tabCount);
  return (((row + delta) % rows) + rows) % rows;
}

/** Swaps the positions of two slots. The launcher cannot be moved or displaced, so a swap involving it returns the order unchanged. */
export function swapSlots(order: readonly number[], first: number, second: number): number[] {
  const next = [...order];
  const a = next.indexOf(first);
  const b = next.indexOf(second);
  if (a < 0 || b < 0 || a === b || first === PINNED_SLOT || second === PINNED_SLOT) return next;
  [next[a], next[b]] = [next[b], next[a]];
  return next;
}
