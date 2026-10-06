// Add the new kind here and in contracts/bundled-tab-view-kinds.json when a bundled view is introduced.
export const BUNDLED_TAB_VIEW_KINDS = ['launcher', 'script-runner', 'api', 'browser-automation', 'workflows', 'scheduler', 'focus-sessions', 'website-launcher', 'reserved'] as const;
export type BundledTabViewKind = typeof BUNDLED_TAB_VIEW_KINDS[number];
