---
graph_id: "function-4069-0238aa4f"
graph_layer: "function"
graph_kind: "function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/function"
  - "impact-graph/area/desktop"
  - "impact-graph/language/typescript"
generated_by: "scripts/impact-graph.mjs"
---

# writeLog

(event: string, message: string, properties?: Record<string, unknown>) => void

## Connections

- impact (called by): [[function-10500-b0a5d35c|applyBackendState]] (electron/main.ts:237)
- impact (called by): [[function-12258-255fcdb9|applyBackendState.then.callback@258]] (electron/main.ts:259)
- impact (called by): [[function-12512-27f86aba|applyBackendState.catch.callback@263]] (electron/main.ts:263)
- impact (called by): [[function-12777-dad0afe3|startBackend]] (electron/main.ts:358)
- impact (called by): [[function-12777-dad0afe3|startBackend]] (electron/main.ts:370)
- impact (called by): [[function-15337-a011c05a|startBackend.on.callback@316]] (electron/main.ts:322)
- impact (called by): [[function-15337-a011c05a|startBackend.on.callback@316]] (electron/main.ts:328)
- impact (called by): [[function-16371-e1479bd8|startBackend.on.callback@334]] (electron/main.ts:338)
- impact (called by): [[function-18544-719ca784|failBackend]] (electron/main.ts:386)
- impact (called by): [[function-18940-89eff3fc|showFailureWindow]] (electron/main.ts:397)
- impact (called by): [[function-19462-ecc3317d|createTray]] (electron/main.ts:411)
- impact (called by): [[function-19462-ecc3317d|createTray]] (electron/main.ts:417)
- impact (called by): [[function-22484-bcb88a8e|registerIpc.handle.callback@479]] (electron/main.ts:491)
- impact (called by): [[function-39031-b2a504f6|showLauncher]] (electron/main.ts:781)
- impact (called by): [[function-40112-b84a7c0c|presentPanel]] (electron/main.ts:816)
- impact (called by): [[function-41204-bd2f0544|requestPrimaryControlFocus]] (electron/main.ts:870)
- impact (called by): [[function-41204-bd2f0544|requestPrimaryControlFocus]] (electron/main.ts:888)
- impact (called by): [[function-41204-bd2f0544|requestPrimaryControlFocus]] (electron/main.ts:901)
- impact (called by): [[function-41204-bd2f0544|requestPrimaryControlFocus]] (electron/main.ts:907)
- impact (called by): [[function-46288-b674615c|updateWindowContext]] (electron/main.ts:942)
- impact (called by): [[function-46927-40299552|closeLauncher]] (electron/main.ts:956)
- impact (called by): [[function-47392-17d7f975|maybeLogReady]] (electron/main.ts:972)
- impact (called by): [[function-48737-f52a0e66|createWorkspaceWindow]] (electron/main.ts:1058)
- impact (called by): [[function-50833-7fa7eb97|createWorkspaceWindow.on.callback@1040]] (electron/main.ts:1041)
- impact (called by): [[function-51203-c2e68d0d|createWorkspaceWindow.on.callback@1044]] (electron/main.ts:1045)
- impact (called by): [[function-51851-865d1fea|createWindow]] (electron/main.ts:1064)
- impact (called by): [[function-54370-f9d5db4d|createWindow.on.callback@1116]] (electron/main.ts:1117)
- impact (called by): [[function-54744-2869fc5d|createWindow.on.callback@1121]] (electron/main.ts:1122)
- impact (called by): [[function-55951-f936c9b0|createWindow.once.callback@1137]] (electron/main.ts:1142)
- impact (called by): [[function-55951-f936c9b0|createWindow.once.callback@1137]] (electron/main.ts:1151)
- impact (called by): [[function-57072-613878ca|createWindow.on.callback@1162]] (electron/main.ts:1162)
- impact (called by): [[function-57242-32a4be3d|createWindow.on.callback@1164]] (electron/main.ts:1164)
- impact (called by): [[function-57518-08c7826a|createWindow.then.callback@1168]] (electron/main.ts:1168)
- impact (called by): [[function-57907-8d737740|createWindow.catch.callback@1175]] (electron/main.ts:1175)
- impact (called by): [[function-59942-1cb75238|initializeApp]] (electron/main.ts:1224)
- impact (called by): [[function-59942-1cb75238|initializeApp]] (electron/main.ts:1234)
- impact (called by): [[function-60906-afefb1ec|catch.callback@1244]] (electron/main.ts:1245)
- impact (called by): [[function-61088-3e157163|on.callback@1249]] (electron/main.ts:1254)
- impact (called by): [[function-61414-35619a6f|catch.callback@1258]] (electron/main.ts:1258)

## Source anchors

- `electron/main.ts#L93`
