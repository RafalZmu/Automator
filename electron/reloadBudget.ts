export type ReloadBudgetOptions = {
  /** Reloads allowed inside one window. */
  maxReloads?: number;
  /** Length of the sliding window in milliseconds. */
  windowMs?: number;
  /** Clock in milliseconds; injectable for tests. */
  now?: () => number;
};

export type ReloadBudget = {
  /** Returns true and records a reload when one is allowed; false once the budget for the current window is spent. */
  tryConsume(): boolean;
};

/** Sliding-window limit for automatic renderer reloads, so a crash loop stops instead of reloading forever. */
export function createReloadBudget({ maxReloads = 3, windowMs = 60_000, now = Date.now }: ReloadBudgetOptions = {}): ReloadBudget {
  const reloads: number[] = [];
  return {
    tryConsume() {
      const current = now();
      while (reloads.length > 0 && current - reloads[0] >= windowMs) reloads.shift();
      if (reloads.length >= maxReloads) return false;
      reloads.push(current);
      return true;
    },
  };
}
