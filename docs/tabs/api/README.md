# API — slot 3

API stores reusable HTTP request profiles. A profile defines its method, URL, ordinary headers, named secure-header references, optional body template, response mode, timeout, and optional default JSON input. A run can use a one-off JSON input; the saved value is only the default for that profile.

The tab can import Chrome's Copy as cURL text into a profile. Import parses supported request details; it does not run the cURL command. Credentials are stored in the Windows secret store and are injected by the host when the request runs. The host applies URL and network restrictions, bounds response sizes, and supports cancellation.

## Code map

- `ui/modules/ApiView.tsx` is the profile editor, runner, response view, secret editor, and cURL import surface.
- `contracts/api.ts` validates profiles, inputs, responses, and supported cURL imports.
- `src/Automator.Application/Automation/ApiModule.cs` owns module actions and profile validation.
- `AutomationApiProfileRunner.cs` and `src/Automator.Infrastructure/Automation/SharedHttpService.cs` execute profile requests through the host.
- `WindowsCredentialSecretManager.cs` stores secure header values; `HostAddressResolver.cs` enforces resolved-host policy.
- Tests: `tests/api-view.test.mjs`, `tests/rpc-contracts.test.mjs`, and Application/Infrastructure specification projects.

Credentials and raw HTTP bodies are excluded from logs and library exports.

## Test case steps

See [TESTS.md](TESTS.md) for the API contract and HTTP service cases with their steps and expected results.
