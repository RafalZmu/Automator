export type WindowBounds = { x: number; y: number; width: number; height: number };

export type WindowsDisplayCapabilities = {
  acrylicSupported: boolean;
  nativeCornersSupported: boolean;
  transparentWindow: boolean;
  backgroundColor: string | undefined;
  backgroundMaterial: 'acrylic' | undefined;
};

export function getWindowsDisplayCapabilities(
  platform: string,
  osRelease: string,
  buildOverride?: string,
): WindowsDisplayCapabilities {
  const windows = platform === 'win32';
  const build = Number((buildOverride ?? osRelease).split('.')[2] ?? 0);
  const nativeCornersSupported = windows && Number.isFinite(build) && build >= 22000;
  const acrylicSupported = windows && Number.isFinite(build) && build >= 22621;

  return {
    acrylicSupported,
    nativeCornersSupported,
    transparentWindow: !nativeCornersSupported,
    backgroundColor: acrylicSupported ? undefined : nativeCornersSupported ? '#F0F5F6' : '#00000000',
    backgroundMaterial: acrylicSupported ? 'acrylic' : undefined,
  };
}

export function computePanelBounds(
  pointer: { x: number; y: number },
  workArea: { x: number; y: number; width: number; height: number },
  preferred: { width: number; height: number },
  verticalGap = 18,
): WindowBounds {
  const width = Math.max(1, Math.min(preferred.width, workArea.width));
  const height = Math.max(1, Math.min(preferred.height, workArea.height));
  const left = Math.max(workArea.x, Math.min(Math.round(pointer.x - width / 2), workArea.x + workArea.width - width));
  const top = Math.max(workArea.y, Math.min(pointer.y + verticalGap, workArea.y + workArea.height - height));
  return { x: left, y: top, width, height };
}

export const SIDE_PANEL_ANIMATION_MS = 220;
export const SIDE_PANEL_WIDTH = 520;

/**
 * Bounds for a window that grows to the left by `panelWidth` when `open`, or shrinks back from that width.
 * `current` is the window as it stands now: the base-width window when opening, the widened one when closing.
 * Growth keeps the content's screen position, and is shifted only as far as the work area requires.
 */
export function computeSidePanelBounds(
  current: WindowBounds,
  workArea: WindowBounds,
  baseWidth: number,
  panelWidth: number,
  open: boolean,
): WindowBounds {
  const base = Math.max(1, Math.min(baseWidth, workArea.width));
  const total = Math.max(base, Math.min(baseWidth + Math.max(0, panelWidth), workArea.width));
  const width = open ? total : base;
  const contentLeft = open ? current.x : current.x + Math.max(0, current.width - base);
  const desiredX = open ? contentLeft - (width - base) : contentLeft;
  const x = Math.max(workArea.x, Math.min(desiredX, workArea.x + workArea.width - width));
  return { x, y: current.y, width, height: current.height };
}

export function easeOutCubic(progress: number): number {
  const t = Math.max(0, Math.min(1, progress));
  return 1 - (1 - t) ** 3;
}

export function interpolateBounds(from: WindowBounds, to: WindowBounds, progress: number): WindowBounds {
  const t = easeOutCubic(progress);
  const mix = (a: number, b: number) => Math.round(a + (b - a) * t);
  return { x: mix(from.x, to.x), y: mix(from.y, to.y), width: mix(from.width, to.width), height: mix(from.height, to.height) };
}
