type WorkTimeShortcutEvent = {
  key: string;
  ctrlKey?: boolean;
  metaKey?: boolean;
  altKey?: boolean;
  shiftKey?: boolean;
  target?: EventTarget | { tagName?: string; isContentEditable?: boolean } | null;
};

/** True when an unmodified S press can be handled without stealing text input. */
export function isWorkTimeToggleShortcut(event: WorkTimeShortcutEvent): boolean {
  if (event.key.toLocaleLowerCase() !== 's' || event.ctrlKey || event.metaKey || event.altKey || event.shiftKey) return false;
  const target = event.target;
  if (!target) return true;
  const isContentEditable = 'isContentEditable' in target && target.isContentEditable === true;
  const tagName = 'tagName' in target && typeof target.tagName === 'string' ? target.tagName : '';
  return !isContentEditable && !['INPUT', 'TEXTAREA', 'SELECT'].includes(tagName.toLocaleUpperCase());
}
