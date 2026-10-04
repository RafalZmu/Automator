export type FocusSessionState = 'idle' | 'running' | 'paused' | 'interrupted';
export type FocusPhase = 'focus' | 'break';

export type FocusSettings = { focusMinutes: number; breakMinutes: number };

export type FocusSession = {
  state: FocusSessionState;
  sessionId: string | null;
  phase: FocusPhase | null;
  phaseEndsAtUtc: string | null;
  remainingMilliseconds: number | null;
  completedFocusPhases: number;
  settings: FocusSettings;
};

export type FocusSessionHistory = {
  id: string;
  startedUtc: string;
  finishedUtc: string;
  completedFocusPhases: number;
  finalState: FocusSessionState;
  durationMilliseconds: number;
};

export type FocusSnapshot = { settings: FocusSettings; session: FocusSession; history: FocusSessionHistory[] };

type JsonObject = Record<string, unknown>;

function isObject(value: unknown): value is JsonObject {
  return Boolean(value && typeof value === 'object' && !Array.isArray(value));
}

function isDate(value: unknown): value is string {
  return typeof value === 'string' && Number.isFinite(Date.parse(value));
}

function isNullableDate(value: unknown): value is string | null {
  return value === null || isDate(value);
}

function isSessionState(value: unknown): value is FocusSessionState {
  return value === 'idle' || value === 'running' || value === 'paused' || value === 'interrupted';
}

function isPhase(value: unknown): value is FocusPhase | null {
  return value === null || value === 'focus' || value === 'break';
}

function isSettings(value: unknown): value is FocusSettings {
  if (!isObject(value)) return false;
  return Number.isInteger(value.focusMinutes) && Number(value.focusMinutes) >= 1 && Number(value.focusMinutes) <= 240
    && Number.isInteger(value.breakMinutes) && Number(value.breakMinutes) >= 1 && Number(value.breakMinutes) <= 120;
}

function readHistoryEntry(value: unknown): FocusSessionHistory | null {
  if (!isObject(value) || typeof value.id !== 'string' || !value.id
      || !isDate(value.startedUtc) || !isDate(value.finishedUtc)
      || !Number.isInteger(value.completedFocusPhases) || Number(value.completedFocusPhases) < 0
      || !isSessionState(value.finalState)
      || typeof value.durationMilliseconds !== 'number' || !Number.isFinite(value.durationMilliseconds)
      || value.durationMilliseconds < 0) return null;
  return value as FocusSessionHistory;
}

export function readFocusSnapshot(value: unknown): FocusSnapshot | null {
  if (!isObject(value) || !isObject(value.session) || !Array.isArray(value.history) || !isSettings(value.settings)) return null;
  const session = value.session;
  if (!isSessionState(session.state)
      || !(session.sessionId === null || typeof session.sessionId === 'string')
      || !isPhase(session.phase)
      || !isNullableDate(session.phaseEndsAtUtc)
      || !(session.remainingMilliseconds === null || (typeof session.remainingMilliseconds === 'number'
        && Number.isFinite(session.remainingMilliseconds) && session.remainingMilliseconds >= 0))
      || !Number.isInteger(session.completedFocusPhases) || Number(session.completedFocusPhases) < 0
      || !isSettings(session.settings)) return null;
  const history = value.history.map(readHistoryEntry).filter((entry): entry is FocusSessionHistory => entry !== null).slice(-100);
  return { settings: value.settings, session: session as unknown as FocusSession, history };
}

/** The host deadline is authoritative while running; paused time comes from the saved host remainder. */
export function remainingFocusMilliseconds(session: Pick<FocusSession, 'state' | 'phaseEndsAtUtc' | 'remainingMilliseconds'>, now = Date.now()): number {
  if (session.state === 'paused') return Math.max(0, session.remainingMilliseconds ?? 0);
  if (session.state !== 'running' || !session.phaseEndsAtUtc) return 0;
  const deadline = Date.parse(session.phaseEndsAtUtc);
  return Number.isFinite(deadline) ? Math.max(0, deadline - now) : 0;
}

export function formatFocusClock(milliseconds: number): string {
  const totalSeconds = Math.max(0, Math.ceil(milliseconds / 1000));
  const seconds = totalSeconds % 60;
  const totalMinutes = Math.floor(totalSeconds / 60);
  const minutes = totalMinutes % 60;
  const hours = Math.floor(totalMinutes / 60);
  const twoDigits = (value: number) => String(value).padStart(2, '0');
  return hours > 0 ? `${hours}:${twoDigits(minutes)}:${twoDigits(seconds)}` : `${twoDigits(totalMinutes)}:${twoDigits(seconds)}`;
}

export function focusPhaseProgress(session: FocusSession, remainingMilliseconds: number): number {
  if (!session.phase) return 0;
  const phaseLength = session.phase === 'focus' ? session.settings.focusMinutes : session.settings.breakMinutes;
  const totalMilliseconds = phaseLength * 60_000;
  if (totalMilliseconds <= 0) return 0;
  return Math.min(1, Math.max(0, 1 - remainingMilliseconds / totalMilliseconds));
}
