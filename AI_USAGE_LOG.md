# AI Usage Log

## Purpose

This file records how AI assistance was used during the Employee Management System project. It tracks prompts, accepted suggestions, modified suggestions, rejected suggestions, and the validation performed before accepting AI-generated output.

## Metrics Summary

Last updated: 2026-07-22

| Metric | Count | Notes |
|---|---:|---|
| Total AI prompts used | 19 | Includes analysis, planning, documentation, stack decision updates, AI log guidance, requirement coverage refinement, gap review, gap fixes, Phase 1-7 implementation, and zoneless change-detection debugging |
| User-originated prompts | 19 | Prompts explicitly requested by the user |
| Agent-initiated prompts | 0 | Autonomous prompts/actions started by the agent without a direct user prompt |
| Accepted AI suggestions | 11 | Suggestions used without material rework |
| Modified AI suggestions | 3 | Suggestions adapted to project decisions |
| Rejected AI suggestions | 2 | zone.js/`provideZoneChangeDetection()` and `ApplicationRef.tick()` interceptor approaches, both reverted |
| Project documents created | 4 | Requirement analysis, implementation plan, AI usage log, root-level README |
| Project documents updated | 10 | Angular 22 decision, requirement coverage refinement, additional requirements alignment, gap fixes, and AI usage updates |
| Backend projects scaffolded | 5 | API, Application, Domain, Infrastructure, Tests |
| Frontend projects scaffolded | 1 | Angular 22 client app |
| Build validations performed | 10 | Multiple backend solution builds and Angular build validations, all passing |
| Test validations performed | 3 | Authentication and password policy unit tests executed and passing, including a re-run after the code-review gap fixes |

## Prompts Used

| ID | Date | Source | Area | Prompt / Request Summary | Tool |
|---|---|---|---|---|---|
| P1 | 2026-07-22 | User | Requirements | Analyze the assessment document and provide requirement analysis and implementation plan. | GitHub Copilot / GPT-5.4 |
| P2 | 2026-07-22 | User | Documentation | Create a Markdown file with requirement analysis and implementation plan. | GitHub Copilot / GPT-5.4 |
| P3 | 2026-07-22 | User | Documentation | Create separate files for requirement analysis and implementation plan. | GitHub Copilot / GPT-5.4 |
| P4 | 2026-07-22 | User | Stack Decision | Update the plan to use Angular 22. | GitHub Copilot / GPT-5.4 |
| P5 | 2026-07-22 | User | Governance | Provide suggestions for logging AI usage against required assessment points. | GitHub Copilot / GPT-5.4 |
| P6 | 2026-07-22 | User | Governance | Create the AI usage log and keep metrics updated for this project. | GitHub Copilot / GPT-5.4 |
| P7 | 2026-07-22 | User | Documentation | Update both Markdown files to include the assessment's Additional Requirements explicitly. | GitHub Copilot / GPT-5.4 |
| P8 | 2026-07-22 | User | Review | Review requirement analysis and implementation plan against the assessment document and identify missing gaps. | GitHub Copilot / GPT-5.4 |
| P9 | 2026-07-22 | User | Documentation | Apply fixes for identified gaps (excluding duration) across both planning documents. | GitHub Copilot / GPT-5.4 |
| P10 | 2026-07-22 | User | Implementation | Start Phase 1 implementation: scaffold .NET 8 solution (API/Application/Domain/Infrastructure/Tests), configure Serilog/exception middleware/DI/health checks/versioning/rate limiting/caching, add Docker support, and scaffold the Angular 22 client. | GitHub Copilot / GPT-5.4 |
| P11 | 2026-07-22 | User | Implementation | Implement Phase 2 and Phase 3: database schema entities/configurations/migration/SQL script, runtime seeding for permissions/roles/admin user, JWT auth with refresh tokens, lockout and password policy flows, authorization policies, and auth unit tests. | GitHub Copilot / GPT-5.4 |
| P12 | 2026-07-22 | User | Implementation | Start Phase 4, 5, and 6 backend implementation: employees/departments/users/roles CRUD and policy guards, dashboard/settings/audit endpoints, and report exports (CSV/Excel/PDF baseline) with successful build/test validation. | GitHub Copilot / GPT-5.4 |
| P13 | 2026-07-22 | User | Implementation | Implement Phase 7 Angular 22 feature modules (auth, dashboard, employees, departments, users, roles, settings, audit, reports) wired to the backend API. | GitHub Copilot / Claude Sonnet 5 |
| P14 | 2026-07-22 | User | Debugging | Diagnose why all list/grid pages showed "Loading..." indefinitely despite the API returning correct data. | GitHub Copilot / Claude Sonnet 5 |
| P15 | 2026-07-22 | User | Debugging | Attempt fix via zone.js + `provideZoneChangeDetection()` to restore automatic change detection. | GitHub Copilot / Claude Sonnet 5 |
| P16 | 2026-07-22 | User | Debugging | Attempt fix via a global HTTP interceptor calling `ApplicationRef.tick()` after responses. | GitHub Copilot / Claude Sonnet 5 |
| P17 | 2026-07-22 | User | Debugging | Apply `ChangeDetectorRef.markForCheck()` in each affected component after async state mutation; verify via Playwright browser automation. | GitHub Copilot / Claude Sonnet 5 |
| P18 | 2026-07-22 | User | Review | Perform a code, technical, and functional review of the full codebase against the assessment document and report what is missing. | GitHub Copilot / Claude Sonnet 5 |
| P19 | 2026-07-22 | User | Implementation | Fix gaps identified in P18: Employee Designation/Salary/Manager fields and validations, missing Users/Roles endpoints, frontend photo upload/pagination/forgot-password UI, root README, and this AI usage log. | GitHub Copilot / Claude Sonnet 5 |

### Prompt Source Legend

- User: Prompt/request explicitly entered by the user.
- Agent: Autonomous work started by the agent without a direct user prompt (for example, self-initiated follow-up analysis).

## Accepted AI Suggestions

| Ref | Suggestion | Why Accepted | Impact |
|---|---|---|---|
| P1 | Structured the assessment into clear requirement groups: authentication, dashboard, employee, department, user, role, settings, audit, and reports. | This matched the source document and made the scope easier to implement. | Improved project clarity |
| P1 | Proposed a phased implementation approach starting with setup, database, authentication, core modules, then reporting. | This was practical for the assessment time window and reduced delivery risk. | Improved execution planning |
| P3 | Split the original combined documentation into dedicated requirement and implementation documents. | Separate artifacts are easier to review and maintain. | Improved documentation quality |
| P7 | Added an explicit Additional Requirements section and aligned the implementation plan with the assessment's cross-cutting technical expectations. | This closes a documentation coverage gap and makes assessment compliance easier to verify. | Improved requirement traceability |
| P9 | Fixed identified gaps: split Git/AI tooling, removed duplicate Non-Functional Requirements section, added self-referencing Manager FK, clarified EmployeeDocuments purpose, added password policy and last-login tasks, added caching target, and added API versioning note. | Closed concrete traceability gaps found during review against the source assessment. | Improved requirement-to-plan traceability |
| P10 | Scaffolded layered .NET 8 solution (API, Application, Domain, Infrastructure, Tests) with project references matching the planned architecture; implemented generic repository/unit-of-work pattern, global exception middleware, Serilog, API versioning, rate limiting, response caching, and EF Core health check; scaffolded Angular 22 client via Angular CLI. | Matched the Phase 1 implementation plan exactly and both backend and frontend build successfully. | Working project skeleton ready for Phase 2 (database design) |
| P11 | Implemented normalized EF Core schema for users/roles/permissions/refresh tokens/employees/departments/settings/audit logs/documents with constraints and indexes, generated migration and SQL script, and implemented JWT login/refresh/logout/forgot-reset/change password with lockout and password expiry checks plus role/permission authorization policies and tests. | Directly aligned to Phase 2 and Phase 3 implementation requirements with runnable and validated backend behavior. | Database and authentication foundation completed for remaining feature modules |
| P12 | Implemented Phase 4-6 backend baseline APIs for Employee, Department, User, Role, Dashboard, Settings, Audit, and Reports modules with authorization policies, pagination/filtering/sorting, safe department deletion, and export endpoints. | Established runnable business-module endpoints aligned to planned module sequence and cross-cutting authorization requirements. | Working API baseline for frontend integration and further business-rule refinement |
| P13 | Implemented Angular 22 standalone feature modules for all business areas, wired to backend APIs via a shared ApiService/AuthService. | Matched the Phase 7 implementation plan and produced a working end-to-end UI. | Full-stack feature parity reached |
| P17 | Injected `ChangeDetectorRef` and called `markForCheck()` after every async state mutation across 9 components. | Confirmed via Angular DevTools (`ng.applyChanges`) that this was a change-detection issue, not a data issue; fix verified live for every affected page. | Resolved the app-wide "stuck Loading..." bug |
| P19 | Added Designation/Salary/Manager fields to the Employee entity/DB/API/UI (with EF Core migration + SQL script), added missing Users PUT/DELETE and Roles DELETE endpoints, added phone/joining-date/salary validation, added frontend pagination, employee photo upload UI, and forgot/change-password pages. | Directly closed the gaps identified in the P18 review against the assessment document. | Closed the majority of the identified functional and technical gaps |

## Modified AI Suggestions

| Ref | Original Suggestion | Modification Applied | Reason |
|---|---|---|---|
| P1 | Suggested frontend stack as Angular 18+ or React based on the assessment brief. | Updated the plan to Angular 22 only. | Final project technology decision was Angular 22. |
| P2 | Created one combined Markdown document for requirements and implementation plan. | Reworked into two separate files. | Better document separation and easier review. |
| P10 | Initially added `Asp.Versioning.Mvc`, `Asp.Versioning.Mvc.ApiExplorer`, and EF Core packages without a version constraint. | Pinned all backend packages to net8.0-compatible major versions (8.x) after NU1202 errors surfaced. | The installed .NET 10 SDK on this machine caused NuGet to resolve the newest (net10.0-only) package versions by default; explicit version pinning was required for net8.0 compatibility. |
| P19 | Initially considered a hard delete for Users/Roles with no safety checks. | Added guards instead: block deleting your own user account, and block deleting a Role that still has users assigned (mirroring the existing safe-department-deletion pattern already in the codebase). | Prevents accidental account lockout/orphaned data and stays consistent with existing codebase conventions. |

## Rejected AI Suggestions

| Ref | Rejected Suggestion | Reason Rejected |
|---|---|---|
| P15 | Install zone.js and call `provideZoneChangeDetection()` to restore automatic change detection under Angular 22's zoneless default (fix for the app-wide "stuck Loading..." bug). | Rejected after confirming zone.js's monkey-patches of XHR/fetch never actually took effect under the Vite/esbuild-based dev server (`XMLHttpRequest.prototype.send.toString()` still showed native code); did not fix the rendering bug, so the dependency and config change were reverted. |
| P16 | Add a global HTTP interceptor that calls `ApplicationRef.tick()` after every response to force re-render (second attempted fix for the same bug). | Rejected after confirming via console logging that `tick()` was being invoked but does not force-check components Angular hasn't marked dirty in zoneless mode; the bug persisted, so the interceptor file was deleted. |

## Validation Performed Before Accepting AI-Generated Output

The following validation was performed on AI-generated project documentation and planning output:

- Compared the AI-generated requirement summary against the assessment document to ensure all listed modules and constraints were captured.
- Verified that the required API endpoints, database tables, validation rules, and non-functional requirements were explicitly included.
- Reviewed the implementation plan to confirm it aligned with the chosen stack: .NET 8, Angular 22, SQL Server, Entity Framework Core, and JWT authentication.
- Confirmed that document changes reflected user decisions, including the switch to Angular 22 and the split into separate files.
- Confirmed that the assessment's Additional Requirements section was explicitly represented in both planning documents.
- Re-reviewed both documents line-by-line against the source assessment text to identify and confirm concrete traceability gaps before applying fixes.
- Checked the created Markdown files after editing to ensure they existed and contained the expected content.
- Verified the local toolchain (.NET SDK 8/10, Node 24, Angular CLI 22) before scaffolding any project.
- Rebuilt the backend solution after every structural change (project creation, references, packages, code) and resolved all compiler errors before proceeding.
- Built the Angular client to confirm the scaffolded frontend compiles and bundles successfully.
- Confirmed package versions were compatible with the target net8.0 framework, since default package resolution picked net10.0-only versions on this machine.
- Executed backend unit tests for authentication and password policy scenarios and confirmed passing results.
- Rebuilt solution and re-ran backend tests after Phase 4-6 API module additions; build succeeded and tests passed.
- Used Playwright browser automation to log in and visually confirm each fixed page rendered data correctly, and used `window.ng.getComponent`/`ng.applyChanges` in browser devtools to distinguish a change-detection bug from a data/API bug before selecting a fix approach (P14-P17).
- Rebuilt the backend and re-ran all unit tests (10/10 passing), and rebuilt the Angular client, after the P19 gap-fix changes (Employee entity fields, new API endpoints, new frontend pages) to confirm no regressions.
- Generated and reviewed the EF Core migration and its companion SQL script for the new Employee fields/index before treating the P19 change as complete.

## Project Artifacts Influenced by AI

| File | AI Contribution |
|---|---|
| REQUIREMENT_ANALYSIS.md | Requirement extraction and structuring |
| IMPLEMENTATION_PLAN.md | Implementation sequencing and delivery planning |
| AI_USAGE_LOG.md | Governance record and metrics tracking |

## Update Rules For Ongoing Work

Update this file whenever AI is used for project work.

- Add a new row in Prompts Used for each meaningful prompt or request.
- Set the `Source` column for each new prompt (`User` or `Agent`).
- Increase metrics summary counts when new activity occurs.
- Keep `User-originated prompts` and `Agent-initiated prompts` counts in sync with the Prompts Used table.
- Add entries to Accepted AI Suggestions when output is used substantially as-is.
- Add entries to Modified AI Suggestions when output is adapted before use.
- Add entries to Rejected AI Suggestions when output is discarded, with a concrete reason.
- Extend the Validation section when new types of validation are performed, such as build, unit test, lint, Swagger, or manual UI verification.

## Next Suggested Metrics To Track

As implementation begins, also track:

- Backend code files generated with AI assistance
- Frontend components/services generated with AI assistance
- Number of AI-assisted fixes after validation failures
- Number of build validations performed
- Number of test validations performed
- Number of rejected suggestions due to security, performance, or requirement mismatch