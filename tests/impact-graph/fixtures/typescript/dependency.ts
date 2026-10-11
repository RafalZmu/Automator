export function helper(value: string): string {
  return value.trim();
}

export { helper as forwardedHelper } from './dependency.js';
