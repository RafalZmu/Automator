export type ScriptRunnerInterpreter = 'python' | 'bash' | 'powershell';

export type ScriptFileFilter = {
  name: string;
  extensions: string[];
};

export function isScriptRunnerInterpreter(value: unknown): value is ScriptRunnerInterpreter {
  return value === 'python' || value === 'bash' || value === 'powershell';
}

export function scriptFileFilter(interpreter: ScriptRunnerInterpreter): ScriptFileFilter {
  switch (interpreter) {
    case 'python': return { name: 'Python scripts', extensions: ['py'] };
    case 'bash': return { name: 'Bash scripts', extensions: ['sh'] };
    case 'powershell': return { name: 'PowerShell scripts', extensions: ['ps1'] };
  }
}
