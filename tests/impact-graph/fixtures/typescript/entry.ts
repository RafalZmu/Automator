import { helper } from '@core/dependency';
import { forwardedHelper as viaReexport } from './dependency.js';

// helper();
const example = "helper()";

export function entryPoint(value: string): string {
  const namedArrow = (input: string) => helper(input);
  const assignedArrow = value => viaReexport(value);
  const mapped = [value].map(item => helper(item));
  new Service();
  missingSymbol();
  console.log(value);
  return helper(namedArrow(assignedArrow(mapped[0])));
}

class Service {
  constructor() {}
  run() { return helper('method'); }
}
