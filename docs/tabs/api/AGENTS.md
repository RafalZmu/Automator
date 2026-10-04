# API tab contributor guide

Read the root AGENTS.md first. The API tab runs saved request profiles through host-owned HTTP and secret services.

## Security and extension rules

- Keep API profile DTO validation aligned between `contracts/api.ts` and `ApiModule.cs`.
- Route requests through the API profile runner and shared host HTTP service. Do not add renderer-side fetch, Node networking, or a second HTTP stack.
- Keep URL/host resolution and local-network policy in the host. Preserve the configured host policy and cancellation/timeout handling; do not weaken it to make a failing request pass.
- Credential values belong in Windows Credential Manager through the secret capability. Profiles and exports may contain opaque secret names/references, never secret values.
- Do not log HTTP bodies, credentials, cookies, or raw response bodies. Keep response display bounded and preserve text-vs-JSON parsing behavior.
- Chrome Copy as cURL import is input parsing only. Never execute imported shell text. Add supported cURL flags to the parser and tests before changing import behavior.
- Request JSON is per run with an optional saved profile default. Body templates use the documented input placeholder; global variables should remain typed and validated.

## Code and checks

UI and import parser: `ui/modules/ApiView.tsx` and `contracts/api.ts`.
Profile execution and policy: `src/Automator.Application/Automation/ApiModule.cs`, `AutomationApiProfileRunner.cs`, and `src/Automator.Infrastructure/Automation/SharedHttpService.cs`.
Secret storage and host resolution: `WindowsCredentialSecretManager.cs` and `HostAddressResolver.cs`.
Tests: `tests/api-view.test.mjs`, RPC/backend tests, and Application/Infrastructure specs.

Update this guide and README when profile fields, import support, or network policy changes.
