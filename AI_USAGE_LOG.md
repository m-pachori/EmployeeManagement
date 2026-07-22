# AI Usage Log

## Purpose

This file records how AI assistance was used during the Employee Management System project. It tracks prompts, accepted suggestions, modified suggestions, rejected suggestions, and the validation performed before accepting AI-generated output.

## Metrics Summary

Last updated: 2026-07-22

| Metric | Count | Notes |
|---|---:|---|
| Total AI prompts used | 12 | Includes analysis, planning, documentation, stack decision updates, AI log guidance, requirement coverage refinement, gap review, gap fixes, and Phase 1-6 implementation |
| Accepted AI suggestions | 8 | Suggestions used without material rework |
| Modified AI suggestions | 2 | Suggestions adapted to project decisions |
| Rejected AI suggestions | 0 | No fully rejected suggestions recorded yet |
| Project documents created | 3 | Requirement analysis, implementation plan, AI usage log |
| Project documents updated | 9 | Angular 22 decision, requirement coverage refinement, additional requirements alignment, gap fixes, and AI usage updates |
| Backend projects scaffolded | 5 | API, Application, Domain, Infrastructure, Tests |
| Frontend projects scaffolded | 1 | Angular 22 client app |
| Build validations performed | 8 | Multiple backend solution builds and Angular build validations, all passing |
| Test validations performed | 2 | Authentication and password policy unit tests executed and passing |

## Prompts Used

| ID | Date | Area | Prompt / Request Summary | Tool |
|---|---|---|---|---|
| P1 | 2026-07-22 | Requirements | Analyze the assessment document and provide requirement analysis and implementation plan. | GitHub Copilot / GPT-5.4 |
| P2 | 2026-07-22 | Documentation | Create a Markdown file with requirement analysis and implementation plan. | GitHub Copilot / GPT-5.4 |
| P3 | 2026-07-22 | Documentation | Create separate files for requirement analysis and implementation plan. | GitHub Copilot / GPT-5.4 |
| P4 | 2026-07-22 | Stack Decision | Update the plan to use Angular 22. | GitHub Copilot / GPT-5.4 |
| P5 | 2026-07-22 | Governance | Provide suggestions for logging AI usage against required assessment points. | GitHub Copilot / GPT-5.4 |
| P6 | 2026-07-22 | Governance | Create the AI usage log and keep metrics updated for this project. | GitHub Copilot / GPT-5.4 |
| P7 | 2026-07-22 | Documentation | Update both Markdown files to include the assessment's Additional Requirements explicitly. | GitHub Copilot / GPT-5.4 |
| P8 | 2026-07-22 | Review | Review requirement analysis and implementation plan against the assessment document and identify missing gaps. | GitHub Copilot / GPT-5.4 |
| P9 | 2026-07-22 | Documentation | Apply fixes for identified gaps (excluding duration) across both planning documents. | GitHub Copilot / GPT-5.4 |
| P10 | 2026-07-22 | Implementation | Start Phase 1 implementation: scaffold .NET 8 solution (API/Application/Domain/Infrastructure/Tests), configure Serilog/exception middleware/DI/health checks/versioning/rate limiting/caching, add Docker support, and scaffold the Angular 22 client. | GitHub Copilot / GPT-5.4 |
| P11 | 2026-07-22 | Implementation | Implement Phase 2 and Phase 3: database schema entities/configurations/migration/SQL script, runtime seeding for permissions/roles/admin user, JWT auth with refresh tokens, lockout and password policy flows, authorization policies, and auth unit tests. | GitHub Copilot / GPT-5.4 |
| P12 | 2026-07-22 | Implementation | Start Phase 4, 5, and 6 backend implementation: employees/departments/users/roles CRUD and policy guards, dashboard/settings/audit endpoints, and report exports (CSV/Excel/PDF baseline) with successful build/test validation. | GitHub Copilot / GPT-5.4 |

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

## Modified AI Suggestions

| Ref | Original Suggestion | Modification Applied | Reason |
|---|---|---|---|
| P1 | Suggested frontend stack as Angular 18+ or React based on the assessment brief. | Updated the plan to Angular 22 only. | Final project technology decision was Angular 22. |
| P2 | Created one combined Markdown document for requirements and implementation plan. | Reworked into two separate files. | Better document separation and easier review. |
| P10 | Initially added `Asp.Versioning.Mvc`, `Asp.Versioning.Mvc.ApiExplorer`, and EF Core packages without a version constraint. | Pinned all backend packages to net8.0-compatible major versions (8.x) after NU1202 errors surfaced. | The installed .NET 10 SDK on this machine caused NuGet to resolve the newest (net10.0-only) package versions by default; explicit version pinning was required for net8.0 compatibility. |

## Rejected AI Suggestions

No fully rejected AI suggestions have been recorded yet.

When a suggestion is rejected later, record it in the format below:

| Ref | Rejected Suggestion | Reason Rejected |
|---|---|---|
| Example | Suggested client-side-only security checks for access control. | Rejected because authorization must be enforced on the API side. |

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

## Project Artifacts Influenced by AI

| File | AI Contribution |
|---|---|
| REQUIREMENT_ANALYSIS.md | Requirement extraction and structuring |
| IMPLEMENTATION_PLAN.md | Implementation sequencing and delivery planning |
| AI_USAGE_LOG.md | Governance record and metrics tracking |

## Update Rules For Ongoing Work

Update this file whenever AI is used for project work.

- Add a new row in Prompts Used for each meaningful prompt or request.
- Increase metrics summary counts when new activity occurs.
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