import { z } from 'zod';

const metadataId = z.string().min(1).max(128).regex(/^[a-zA-Z0-9._:-]+$/);
export const runActivityEntrySchema = z.strictObject({
  id: metadataId,
  moduleId: z.enum(['script-runner', 'api', 'browser-automation', 'workflows', 'scheduler']),
  actionId: metadataId,
  profileId: metadataId.nullable(),
  startedUtc: z.iso.datetime({ offset: true }),
  finishedUtc: z.iso.datetime({ offset: true }),
  status: z.enum(['success', 'information', 'warning', 'error']),
  durationMilliseconds: z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER),
  origin: z.enum(['manual', 'scheduled']),
});
export const runActivitySnapshotSchema = z.strictObject({
  contractVersion: z.literal(1),
  entries: z.array(runActivityEntrySchema).max(300),
});
export type RunActivityEntry = z.infer<typeof runActivityEntrySchema>;
export type RunActivitySnapshot = z.infer<typeof runActivitySnapshotSchema>;

export const activityModuleNames: Record<RunActivityEntry['moduleId'], string> = {
  'script-runner': 'Script Runner', api: 'API', 'browser-automation': 'Browser Automation',
  workflows: 'Workflows', scheduler: 'Scheduler',
};

/** Fixed summaries deliberately do not include provider messages or result data. */
export function activitySummary(entry: RunActivityEntry): string {
  const outcome = entry.status === 'success' ? 'completed' : entry.status === 'error' ? 'failed'
    : entry.status === 'warning' ? 'finished with a warning' : 'finished';
  return `${activityModuleNames[entry.moduleId]} ${outcome}.`;
}

export function filterActivity(entries: readonly RunActivityEntry[], query: string): RunActivityEntry[] {
  const words = query.toLowerCase().trim().split(/\s+/).filter(Boolean);
  return entries.filter((entry) => {
    const text = [activityModuleNames[entry.moduleId], entry.moduleId, entry.actionId, entry.profileId ?? '', entry.status, entry.origin].join(' ').toLowerCase();
    return words.every((word) => text.includes(word));
  });
}
