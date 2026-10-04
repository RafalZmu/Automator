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
