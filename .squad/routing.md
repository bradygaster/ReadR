# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, project structure, cross-cutting decisions | Mr. White | Solution layout, service boundaries, technical trade-offs |
| Blazor UI, components, pages, layout, CSS | Mr. Orange | Razor components, `Routes.razor`, `wwwroot`, responsive UI |
| Azure Storage (Blobs/Queues), feed data & caching | Mr. Pink | `AzureBlobFeedService`, `FeedCacheService`, `FileFeedService`, `feed-urls.txt` |
| AI/Chat features, `Microsoft.Extensions.AI` integration | Mr. Blue | `ChatService`, summarization prompts, model/deployment config |
| .NET Aspire orchestration, deployment, CI/CD | Mr. Brown | `ReadR.AppHost`, `azure.yaml`, GitHub Actions workflows, `azd` |
| Testing, edge cases, verification | Nice Guy Eddie | Unit/integration tests, feed parsing edge cases, regression checks |
| Docs, README, contributor guidance | Joe Cabot | README updates, `prompts.md`, onboarding docs |
| Code review | Mr. White | Review PRs, check quality, suggest improvements |
| Scope & priorities | Mr. White | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |
| RAI review | Rai | Content safety, bias checks, credential detection, ethical review |
| Fact-checking / devil's advocate | Fact Checker | Verify claims, challenge assumptions, pre-mortems |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| squad:mrwhite | Architecture / lead work | Mr. White |
| squad:mrorange | Frontend work | Mr. Orange |
| squad:mrpink | Storage/data work | Mr. Pink |
| squad:mrblue | AI/chat work | Mr. Blue |
| squad:mrbrown | Aspire/DevOps work | Mr. Brown |
| squad:niceguyeddie | Testing work | Nice Guy Eddie |
| squad:joecabot | Docs/DevRel work | Joe Cabot |
