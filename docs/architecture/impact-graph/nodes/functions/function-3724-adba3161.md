---
graph_id: "function-3724-adba3161"
graph_layer: "function"
graph_kind: "function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/function"
  - "impact-graph/area/renderer"
  - "impact-graph/language/typescript"
generated_by: "scripts/impact-graph.mjs"
---

# workTimeEntryDurationMilliseconds

(entry: Pick<WorkTimeEntry, 'startedUtc' | 'stoppedUtc' | 'durationMilliseconds'>, workingPeriodOnly?: boolean) => number

## Connections

- impact (called by): [[work-time-view|FocusSessionsView]] (ui/modules/FocusSessionsView.tsx:177)
- impact (called by): [[function-17327-8429078d|FocusSessionsView.map.callback@284]] (ui/modules/FocusSessionsView.tsx:301)
- impact (called by): [[function-4908-440ec2e2|calculateWorkTimeReportTotals]] (ui/modules/workTimeViewModel.ts:151)
- impact (called by): [[function-8051-a8ef0eac|createWorkTimeCsv.map.callback@199]] (ui/modules/workTimeViewModel.ts:203)
- impact (called by): [[function-8051-a8ef0eac|createWorkTimeCsv.map.callback@199]] (ui/modules/workTimeViewModel.ts:204)

## Source anchors

- `ui/modules/workTimeViewModel.ts#L93`
