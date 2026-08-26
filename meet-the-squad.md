# Meet Your Squad 🧑‍🤝‍🧑

**Universe:** Reservoir Dogs

Your AI team for **ReadR** — a .NET RSS Feed Reader built with ASP.NET Core Blazor Server, .NET Aspire, and Azure.

## The Crew

| Name | Role | Specialty | How to talk to them |
|------|------|-----------|----------------------|
| Mr. White | Lead / Architect | Solution architecture, code review, prioritization | `squad:mrwhite` label or "Mr. White, ..." |
| Mr. Orange | Blazor Frontend Engineer | Razor components, layout, responsive CSS | `squad:mrorange` label |
| Mr. Pink | Azure Storage & Data Engineer | Blob/Queue storage, feed caching & parsing | `squad:mrpink` label |
| Mr. Blue | AI / Chat Integration Engineer | `Microsoft.Extensions.AI`, chat summarization | `squad:mrblue` label |
| Mr. Brown | Aspire & DevOps Engineer | Aspire orchestration, `azd` deploy, CI/CD | `squad:mrbrown` label |
| Nice Guy Eddie | Test & Quality Engineer | Tests, regressions, edge cases | `squad:niceguyeddie` label |
| Joe Cabot | DevRel / Docs | README, onboarding docs | `squad:joecabot` label |

## Always-On Support

| Name | Role | Job |
|------|------|-----|
| Scribe | Session Logger | Records decisions and orchestration logs automatically |
| Ralph | Work Monitor | Continuously scans for actionable work |
| Rai | RAI Reviewer | Checks for safety, bias, and credential leaks |
| Fact Checker | Fact Checker | Verifies claims, plays devil's advocate on plans |

## How to Work With Your Squad

- Apply a `squad:{name}` label (color `9B8FCC`) to an issue to route it to a specific crew member.
- Comment `/squad status` for a roster and lifecycle check.
- Comment `/squad research`, `/squad plan`, `/squad triage`, or `/squad implement` to move planned work forward.
- See `.squad/routing.md` for the full domain → agent routing table.

## What Happened Here

Squad analyzed the repository and found:

- **Languages/frameworks:** C# / .NET 9, ASP.NET Core Blazor Server, .NET Aspire
- **Structure:** `ReadR.Frontend` (Blazor UI + services), `ReadR.AppHost` (Aspire orchestration), `ReadR.ServiceDefaults` (shared config), plus `start`/`end` workshop folders
- **Cloud integration:** Azure Blob Storage/Queues for feed data, Azure OpenAI/AI Foundry for chat summarization, Azure Container Apps deployment via `azd`
- **CI/CD:** GitHub Actions workflow (`readr-dev-github.yml`) builds, tests, publishes, and deploys to Azure Web App

Based on this, Squad cast a 7-member crew covering architecture, frontend, storage/data, AI integration, DevOps/Aspire, testing, and docs — themed after **Reservoir Dogs** (small ensemble, function-over-authority naming).

---
*Cast on 2026-08-26*
