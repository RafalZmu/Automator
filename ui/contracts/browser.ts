export type BrowserAutomationProfile = {
  id: string;
  name: string;
  startUrl: string;
  allowedHosts: string[];
  allowLocalNetwork: boolean;
};

export type BrowserActionKind =
  | 'navigate'
  | 'click'
  | 'fill'
  | 'select'
  | 'waitFor'
  | 'readTitle'
  | 'readText'
  | 'listLinks';

export type BrowserLocatorKind = 'role' | 'label' | 'placeholder' | 'text' | 'testId';

export type BrowserAutomationAction = {
  kind: BrowserActionKind;
  url?: string;
  locatorKind?: BrowserLocatorKind;
  locator?: string;
  value?: string;
  timeoutMilliseconds?: number;
};

export type BrowserAutomationLink = { text: string; url: string };
export type BrowserAutomationActionResult = {
  currentUrl: string;
  title: string | null;
  text: string | null;
  links: BrowserAutomationLink[];
};

export type BrowserRuntimeStatus = {
  installed: boolean;
  ready: boolean;
  message: string;
  progressPercent: number | null;
};

export type PlaywrightTestIdentity = {
  id: string;
  project: string | null;
  file: string;
  line: number;
  column: number | null;
  titlePath: string[];
  listEntry: string;
  tags: string[];
};

export type PlaywrightTestFile = { path: string; tests: PlaywrightTestIdentity[] };

export type PlaywrightExplorerState = {
  projectRoot: string | null;
  isManagedProject: boolean;
  files: PlaywrightTestFile[];
  tags: string[];
  staleTags: Array<{ file: string; title: string; tags: string[] }>;
  runner: { available: boolean; version: string | null; message: string };
};

const ansiEscapePattern = /\u001b\[[0-?]*[ -/]*[@-~]/g;
const playwrightListingLine = /^(?:\[(?<project>[^\]]{1,128})\]\s*[›>]\s*)?(?<file>.+?\.(?:spec|test)\.[jt]s)(?::(?<line>\d{1,8})(?::(?<column>\d{1,8}))?)?\s*[›>]\s*(?<titles>.+)$/;

/** Parses the stable, human-readable entry format emitted by `playwright test --list`. */
export function parsePlaywrightTestList(output: string): PlaywrightTestIdentity[] {
  const tests = new Map<string, PlaywrightTestIdentity>();
  for (const sourceLine of output.split(/\r?\n/)) {
    const line = sourceLine.replace(ansiEscapePattern, '').trim();
    const match = playwrightListingLine.exec(line);
    if (!match?.groups) continue;
    const file = match.groups.file.replaceAll('\\', '/').replace(/^\.\//, '');
    const project = match.groups.project?.trim() || null;
    const row = Number(match.groups.line ?? 0);
    const column = match.groups.column === undefined ? null : Number(match.groups.column);
    const titlePath = match.groups.titles.split(/\s*[›>]\s*/).map((title) => title.trim()).filter(Boolean);
    if (!Number.isSafeInteger(row) || row < 0 || !titlePath.length || file.length > 2048
        || titlePath.some((title) => title.length > 512)) continue;
    const listEntry = `${project ? `[${project}] › ` : ''}${file}${row ? `:${row}${column === null ? '' : `:${column}`}` : ''} › ${titlePath.join(' › ')}`;
    const id = JSON.stringify([project, file.toLocaleLowerCase('en-US'), row, column, titlePath]);
    tests.set(id, { id, project, file, line: row, column, titlePath, listEntry, tags: [] });
  }
  return [...tests.values()];
}

/** Returns a normalized project-relative test filename, or null when it is unsafe/unsupported. */
export function validatePlaywrightSectionPath(value: string): string | null {
  const path = value.trim().replaceAll('\\', '/');
  if (!path || path.length > 512 || path.startsWith('/') || /^[a-z]:/i.test(path) || path.includes('\0')) return null;
  const parts = path.split('/');
  if (parts.some((part) => !part || part === '.' || part === '..' || /[<>:"|?*]/.test(part) || /[. ]$/.test(part))) return null;
  if (!/\.(?:spec|test)\.ts$/i.test(path)) return null;
  return parts.join('/');
}

export function readPlaywrightExplorerState(value: unknown): PlaywrightExplorerState | null {
  if (!isRecord(value) || (value.projectRoot !== null && typeof value.projectRoot !== 'string')
      || !Array.isArray(value.files) || !Array.isArray(value.tags) || !Array.isArray(value.staleTags)
      || !isRecord(value.runner)) return null;
  const files = value.files.flatMap((candidate): PlaywrightTestFile[] => {
    if (!isRecord(candidate) || typeof candidate.path !== 'string' || !Array.isArray(candidate.tests)) return [];
    const tests = candidate.tests.flatMap((testValue): PlaywrightTestIdentity[] => {
      if (!isRecord(testValue) || typeof testValue.id !== 'string' || typeof testValue.file !== 'string'
          || typeof testValue.listEntry !== 'string' || !Array.isArray(testValue.titlePath) || !Array.isArray(testValue.tags)
          || !testValue.tags.every((tag) => typeof tag === 'string')
          || !testValue.titlePath.every((title) => typeof title === 'string')
          || typeof testValue.line !== 'number' || !Number.isInteger(testValue.line)
          || (testValue.column !== null && typeof testValue.column !== 'number')
          || (testValue.project !== null && typeof testValue.project !== 'string')) return [];
      return [{
        id: testValue.id,
        project: testValue.project as string | null,
        file: testValue.file,
        line: testValue.line,
        column: testValue.column as number | null,
        titlePath: testValue.titlePath as string[],
        listEntry: testValue.listEntry,
        tags: testValue.tags as string[],
      }];
    });
    return [{ path: candidate.path, tests }];
  });
  const staleTags = value.staleTags.flatMap((candidate) => isRecord(candidate)
    && typeof candidate.file === 'string' && typeof candidate.title === 'string' && Array.isArray(candidate.tags)
    && candidate.tags.every((tag) => typeof tag === 'string')
    ? [{ file: candidate.file, title: candidate.title, tags: candidate.tags as string[] }] : []);
  if (typeof value.runner.available !== 'boolean') return null;
  return {
    projectRoot: value.projectRoot as string | null,
    isManagedProject: value.isManagedProject === true,
    files,
    tags: value.tags.filter((tag): tag is string => typeof tag === 'string').slice(0, 128),
    staleTags,
    runner: {
      available: value.runner.available,
      version: typeof value.runner.version === 'string' ? value.runner.version : null,
      message: typeof value.runner.message === 'string' ? value.runner.message.slice(0, 512) : 'Runner status unavailable.',
    },
  };
}

const profileIdPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;
const locatorKinds = new Set<BrowserLocatorKind>(['role', 'label', 'placeholder', 'text', 'testId']);
const actionKinds = new Set<BrowserActionKind>([
  'navigate', 'click', 'fill', 'select', 'waitFor', 'readTitle', 'readText', 'listLinks',
]);
const locatorActionKinds = new Set<BrowserActionKind>(['click', 'fill', 'select', 'waitFor', 'readText']);

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function normalizeAllowedHost(host: string): string | null {
  const trimmed = host.trim();
  if (!trimmed || trimmed.includes('*') || /[\s/@?#:/\\]/.test(trimmed)) return null;
  try {
    const parsed = new URL(`https://${trimmed}`);
    if (parsed.username || parsed.password || parsed.port || parsed.pathname !== '/' || parsed.search || parsed.hash) return null;
    return parsed.hostname.toLowerCase().replace(/\.$/, '');
  } catch {
    return null;
  }
}

function normalizeHost(host: string): string {
  return host.toLowerCase().replace(/\.$/, '');
}

function readWebUrl(value: string): URL | null {
  try {
    const parsed = new URL(value);
    if (!['http:', 'https:'].includes(parsed.protocol) || parsed.username || parsed.password) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function validateBrowserProfile(value: unknown): string[] {
  if (!isRecord(value)) return ['Browser profile data is invalid.'];
  const issues: string[] = [];
  if (typeof value.id !== 'string' || !profileIdPattern.test(value.id)) {
    issues.push('Profile key must be a short lowercase identifier.');
  }
  if (typeof value.name !== 'string' || value.name.trim().length === 0 || value.name.length > 128) {
    issues.push('Name is required and must be at most 128 characters.');
  }

  const startUrl = typeof value.startUrl === 'string' ? value.startUrl.trim() : '';
  const parsedUrl = readWebUrl(startUrl);
  if (!parsedUrl || startUrl.length > 8192) {
    issues.push('Start URL must be an absolute HTTP or HTTPS address without credentials.');
  }

  if (!Array.isArray(value.allowedHosts) || value.allowedHosts.length < 1 || value.allowedHosts.length > 64
      || !value.allowedHosts.every((host) => typeof host === 'string')) {
    issues.push('Add between 1 and 64 exact allowed hosts.');
  } else {
    const hosts = value.allowedHosts as string[];
    const normalized = hosts.map(normalizeAllowedHost);
    if (normalized.some((host) => host === null)) {
      issues.push('Allowed hosts must be exact host names without wildcards, ports, or paths.');
    }
    if (new Set(normalized).size !== normalized.length) issues.push('Allowed hosts must be unique.');
    if (parsedUrl && !normalized.includes(normalizeHost(parsedUrl.hostname))) {
      issues.push('The start URL host must appear in the exact allowed-host list.');
    }
  }
  if (typeof value.allowLocalNetwork !== 'boolean') issues.push('Local network access must be enabled or disabled.');
  return [...new Set(issues)];
}

export function readBrowserProfiles(value: unknown): BrowserAutomationProfile[] {
  if (!isRecord(value) || !Array.isArray(value.profiles)) return [];
  return value.profiles.flatMap((candidate) => {
    if (!isRecord(candidate) || typeof candidate.id !== 'string' || typeof candidate.name !== 'string'
        || typeof candidate.startUrl !== 'string' || !Array.isArray(candidate.allowedHosts)
        || !candidate.allowedHosts.every((host) => typeof host === 'string')
        || typeof candidate.allowLocalNetwork !== 'boolean') return [];
    const profile: BrowserAutomationProfile = {
      id: candidate.id,
      name: candidate.name,
      startUrl: candidate.startUrl,
      allowedHosts: [...candidate.allowedHosts],
      allowLocalNetwork: candidate.allowLocalNetwork,
    };
    return validateBrowserProfile(profile).length === 0 ? [profile] : [];
  });
}

export function validateBrowserAction(value: unknown, profile: BrowserAutomationProfile): string[] {
  if (!isRecord(value) || typeof value.kind !== 'string' || !actionKinds.has(value.kind as BrowserActionKind)) {
    return ['Browser action is unsupported.'];
  }
  const action = value as Record<string, unknown>;
  const kind = action.kind as BrowserActionKind;
  const issues: string[] = [];

  if (kind === 'navigate') {
    const url = typeof action.url === 'string' ? action.url.trim() : '';
    const parsed = readWebUrl(url);
    if (!parsed || url.length > 4096) issues.push('Enter an absolute HTTP or HTTPS URL without credentials.');
    else if (!profile.allowedHosts.some((host) => normalizeHost(host) === normalizeHost(parsed.hostname))) {
      issues.push('The URL host must appear in this profile’s exact allowed-host list.');
    }
  }

  if (locatorActionKinds.has(kind)) {
    if (typeof action.locatorKind !== 'string' || !locatorKinds.has(action.locatorKind as BrowserLocatorKind)) {
      issues.push('Choose a supported locator type.');
    }
    if (typeof action.locator !== 'string' || !action.locator.trim() || action.locator.length > 2048) {
      issues.push('Locator is required and must be at most 2,048 characters.');
    }
  }

  if (kind === 'fill' || kind === 'select') {
    if (typeof action.value !== 'string' || action.value.length > 32 * 1024) {
      issues.push('Action value is required and must be at most 32 KiB.');
    }
  } else if (action.value !== undefined && (typeof action.value !== 'string' || action.value.length > 32 * 1024)) {
    issues.push('Action value must be a string of at most 32 KiB.');
  }

  if (action.timeoutMilliseconds !== undefined
      && (typeof action.timeoutMilliseconds !== 'number' || !Number.isInteger(action.timeoutMilliseconds)
        || action.timeoutMilliseconds < 250 || action.timeoutMilliseconds > 30_000)) {
    issues.push('Timeout must be between 250 and 30,000 milliseconds.');
  }
  return issues;
}

export function readBrowserRuntimeStatus(value: unknown): BrowserRuntimeStatus | null {
  if (!isRecord(value) || typeof value.installed !== 'boolean' || typeof value.ready !== 'boolean') return null;
  return {
    installed: value.installed,
    ready: value.ready,
    message: typeof value.message === 'string' ? value.message.slice(0, 512) : 'Browser runtime status unavailable.',
    progressPercent: typeof value.progressPercent === 'number' && Number.isFinite(value.progressPercent)
      ? Math.min(100, Math.max(0, Math.floor(value.progressPercent)))
      : null,
  };
}

export function readBrowserActionResult(value: unknown): BrowserAutomationActionResult | null {
  if (!isRecord(value) || typeof value.currentUrl !== 'string' || value.currentUrl.length > 8192) return null;
  if ((value.title !== null && value.title !== undefined && (typeof value.title !== 'string' || value.title.length > 4096))
      || (value.text !== null && value.text !== undefined && (typeof value.text !== 'string' || value.text.length > 128 * 1024))) return null;
  const links = Array.isArray(value.links) ? value.links.slice(0, 64).flatMap((candidate) => {
    if (!isRecord(candidate) || typeof candidate.text !== 'string' || candidate.text.length > 512
        || typeof candidate.url !== 'string' || candidate.url.length > 2048) return [];
    const url = readWebUrl(candidate.url);
    return url ? [{ text: candidate.text, url: url.href }] : [];
  }) : [];
  return {
    currentUrl: value.currentUrl,
    title: typeof value.title === 'string' ? value.title : null,
    text: typeof value.text === 'string' ? value.text : null,
    links,
  };
}

export type PlaywrightRunKind = 'file' | 'test' | 'tag';
export type PlaywrightRunState = 'success' | 'failed' | 'timedOut' | 'canceled' | 'error';

export type PlaywrightFailureReason = 'testTimeout' | 'hookTimeout' | 'expectTimeout' | 'assertion' | 'actionTimeout' | 'runnerLimit' | 'error' | 'interrupted';

/** `status` is passed, flaky (passed only after a failed attempt), failed, timedOut, skipped or interrupted. */
export type PlaywrightTestOutcome = {
  title: string; file: string; line: number; project: string | null; status: string;
  reason: PlaywrightFailureReason | null; error: string | null;
  /** The deepest step that failed, such as expect.toBeVisible or locator.click. */
  step: string | null; failureLine: number | null; snippet: string | null; attempts: number;
};

const failureReasons = new Set<string>(['testTimeout', 'hookTimeout', 'expectTimeout', 'assertion', 'actionTimeout', 'runnerLimit', 'error', 'interrupted']);

export const failureReasonLabels: Record<PlaywrightFailureReason, string> = {
  testTimeout: 'Test timed out', hookTimeout: 'Hook or fixture timed out', expectTimeout: 'Expect timed out',
  assertion: 'Expect failed', actionTimeout: 'Action timed out', runnerLimit: 'Runner limit reached',
  error: 'Error', interrupted: 'Interrupted',
};

export const failureReasonHints: Record<PlaywrightFailureReason, string> = {
  testTimeout: 'The whole test ran past its time limit.',
  hookTimeout: 'A beforeEach or beforeAll hook, or a fixture, ran past its time limit.',
  expectTimeout: 'A retrying expect, such as toBeVisible or toHaveText, kept failing until its timeout.',
  assertion: 'An expect compared a value that did not match.',
  actionTimeout: 'A page action, such as click or fill, waited past its timeout.',
  runnerLimit: 'Automator stopped the run at its time limit while this test was still running.',
  error: 'The test threw an error that is not a timeout.',
  interrupted: 'The run ended before this test finished.',
};

export const outcomeStatusLabels: Record<string, string> = {
  passed: 'Passed', flaky: 'Flaky', failed: 'Failed', timedOut: 'Timed out', skipped: 'Skipped', interrupted: 'Interrupted',
};

/** One finished Playwright run kept in the library database. */
export type PlaywrightRunRecord = {
  id: string;
  kind: PlaywrightRunKind;
  target: string;
  state: PlaywrightRunState;
  message: string;
  startedUtc: string;
  finishedUtc: string;
  durationMilliseconds: number;
  exitCode: number | null;
  summary: PlaywrightRunSummary | null;
  tests: PlaywrightTestOutcome[];
  outputTruncated: boolean;
  projectRoot: string;
};

/** A run that the backend is executing right now, whichever window or agent started it. */
export type PlaywrightActiveRun = { id: string; kind: PlaywrightRunKind; target: string; startedUtc: string };

const runKinds = new Set<string>(['file', 'test', 'tag']);
const runStates = new Set<string>(['success', 'failed', 'timedOut', 'canceled', 'error']);

function readCount(value: unknown): number {
  return typeof value === 'number' && Number.isFinite(value) ? Math.max(0, Math.floor(value)) : 0;
}

function readIsoTime(value: unknown): string | null {
  return typeof value === 'string' && value.length <= 40 && Number.isFinite(Date.parse(value)) ? value : null;
}

export function readPlaywrightTestOutcomes(value: unknown): PlaywrightTestOutcome[] {
  if (!Array.isArray(value)) return [];
  return value.slice(0, 256).flatMap((test): PlaywrightTestOutcome[] => {
    if (!isRecord(test) || typeof test.title !== 'string' || typeof test.file !== 'string' || typeof test.status !== 'string') return [];
    return [{ title: test.title.slice(0, 512), file: test.file.slice(0, 512), line: readCount(test.line),
      project: typeof test.project === 'string' ? test.project.slice(0, 128) : null, status: test.status.slice(0, 32),
      reason: typeof test.reason === 'string' && failureReasons.has(test.reason) ? test.reason as PlaywrightFailureReason : null,
      error: typeof test.error === 'string' && test.error ? test.error.slice(0, 600) : null,
      step: typeof test.step === 'string' && test.step ? test.step.slice(0, 200) : null,
      failureLine: readCount(test.failureLine) || null,
      snippet: typeof test.snippet === 'string' && test.snippet ? test.snippet.slice(0, 900) : null,
      attempts: Math.max(1, readCount(test.attempts)) }];
  });
}

export type PlaywrightRunSummary = { passed: number; failed: number; skipped: number; interrupted: number; timedOut: number; flaky: number };

export function readPlaywrightRunSummary(value: unknown): PlaywrightRunRecord['summary'] {
  if (!isRecord(value)) return null;
  return { passed: readCount(value.passed), failed: readCount(value.failed), skipped: readCount(value.skipped), interrupted: readCount(value.interrupted),
    timedOut: readCount(value.timedOut), flaky: readCount(value.flaky) };
}

export type PlaywrightOutcomeCounts = PlaywrightRunSummary;

/** Counts a run's outcomes: its stored summary when present, otherwise its listed tests. */
export function countPlaywrightOutcomes(run: PlaywrightRunRecord): PlaywrightOutcomeCounts {
  if (run.summary) return { ...run.summary };
  const counts: PlaywrightOutcomeCounts = { passed: 0, failed: 0, skipped: 0, interrupted: 0, timedOut: 0, flaky: 0 };
  for (const test of run.tests) {
    if (test.status === 'passed') counts.passed++;
    else if (test.status === 'flaky') counts.flaky++;
    else if (test.status === 'timedOut') counts.timedOut++;
    else if (test.status === 'skipped') counts.skipped++;
    else if (test.status === 'interrupted') counts.interrupted++;
    else counts.failed++;
  }
  return counts;
}

export function readPlaywrightRunHistory(value: unknown): { runs: PlaywrightRunRecord[]; active: PlaywrightActiveRun[] } | null {
  if (!isRecord(value) || !Array.isArray(value.runs)) return null;
  const runs = value.runs.slice(0, 200).flatMap((candidate): PlaywrightRunRecord[] => {
    if (!isRecord(candidate) || typeof candidate.id !== 'string' || typeof candidate.target !== 'string'
        || typeof candidate.kind !== 'string' || !runKinds.has(candidate.kind)
        || typeof candidate.state !== 'string' || !runStates.has(candidate.state)) return [];
    const startedUtc = readIsoTime(candidate.startedUtc);
    const finishedUtc = readIsoTime(candidate.finishedUtc);
    if (!startedUtc || !finishedUtc) return [];
    return [{
      id: candidate.id,
      kind: candidate.kind as PlaywrightRunKind,
      target: candidate.target.slice(0, 512),
      state: candidate.state as PlaywrightRunState,
      message: typeof candidate.message === 'string' ? candidate.message.slice(0, 1024) : '',
      startedUtc,
      finishedUtc,
      durationMilliseconds: readCount(candidate.durationMilliseconds),
      exitCode: typeof candidate.exitCode === 'number' && Number.isInteger(candidate.exitCode) ? candidate.exitCode : null,
      summary: readPlaywrightRunSummary(candidate.summary),
      tests: readPlaywrightTestOutcomes(candidate.tests),
      outputTruncated: candidate.outputTruncated === true,
      projectRoot: typeof candidate.projectRoot === 'string' ? candidate.projectRoot.slice(0, 1024) : '',
    }];
  });
  const active = Array.isArray(value.active) ? value.active.slice(0, 16).flatMap((candidate): PlaywrightActiveRun[] => {
    if (!isRecord(candidate) || typeof candidate.id !== 'string' || typeof candidate.target !== 'string'
        || typeof candidate.kind !== 'string' || !runKinds.has(candidate.kind)) return [];
    const startedUtc = readIsoTime(candidate.startedUtc);
    return startedUtc ? [{ id: candidate.id, kind: candidate.kind as PlaywrightRunKind, target: candidate.target.slice(0, 512), startedUtc }] : [];
  }) : [];
  return { runs, active };
}
