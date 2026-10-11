export function helper(value: string): string {
  return value.trim();
}

export function select(value: string): string;
export function select(value: number): number;
export function select(value: string | number): string | number {
  return value;
}

export { helper as forwardedHelper } from './dependency.js';
