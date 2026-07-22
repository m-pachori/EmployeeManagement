# Employee Management System Implementation Plan

## Recommended Delivery Strategy

This assessment is broad for a 4 to 6 hour window, so implementation should focus on a strong end-to-end core rather than full enterprise-level depth everywhere. Prioritize the central business flow first, then add supporting modules in decreasing business impact order.

## Additional Requirements

The following assessment requirements should be implemented as explicit cross-cutting workstreams rather than treated as secondary polish items:

- Global exception middleware
- Serilog logging
- Dependency injection
- Repository pattern
- Unit tests
- Swagger documentation
- Docker support
- Rate limiting
- API versioning
- Caching
- Health check endpoint

## Phase 1: Solution Setup

Create the base solution structure:

- Backend API project using .NET 8 Web API
- Domain/Application layer for business models and services
- Infrastructure layer for EF Core, repositories, authentication, and logging
- Unit test project
- Frontend project using Angular 22

Initial technical setup:

- Swagger
- Serilog
- Global exception middleware
- Dependency injection registrations
- Repository pattern setup
- Health checks
- API versioning (prefix all routes with `/api/v1/...`)
- Rate limiting
- Response/data caching strategy
- Docker configuration
- Unit test project baseline and test infrastructure

## Phase 2: Database Design and Persistence

Design the SQL Server schema and EF Core entities for:

- Users, roles, permissions, and refresh tokens
- Employees and departments
- Settings and audit logs
- Employee documents

Implementation tasks:

- Create normalized schema
- Add all keys and constraints
- Add required indexes
- Create EF Core DbContext and migrations
- Generate SQL scripts for submission
- Seed admin user, roles, and base permissions

## Phase 3: Authentication and Authorization

Implement authentication first because it affects every secured module.

Backend tasks:

- JWT token generation
- Refresh token generation and storage
- Password hashing
- Login and logout flows
- Forgot password flow
- Change password flow
- Account lockout logic
- Password expiry checks
- Password complexity policy enforcement (minimum 8 characters, uppercase, lowercase, number, special character)
- Persist last login timestamp on successful authentication for dashboard reporting

Authorization tasks:

- Role-based access
- Permission-based policies
- Secure endpoints by module and action

## Phase 4: Core Modules

### Employee Module

Implement this first as the main business module.

- Employee CRUD endpoints
- Validation rules
- Search filters
- Pagination
- Sorting
- Photo upload support
- Export support

### Department Module

- Department CRUD endpoints
- Validation for duplicate names if needed
- Prevent unsafe deletion when employees are attached

### User Module

- User CRUD endpoints
- Role assignment
- User status management

### Role and Permission Module

- Role CRUD
- Permission lookup/management
- Role-permission mapping

## Phase 5: Dashboard, Settings, and Audit

Dashboard:

- Employee count
- Department count
- Recent activity feed
- Last login data
- Cache dashboard aggregate counts and department/status lookups to reduce repeated query load

Settings:

- Company settings
- Application settings
- SMTP settings
- Audit settings

Audit:

- Capture login/logout events
- Capture CRUD changes
- Capture settings changes
- Store created-by and updated-by metadata where applicable

## Phase 6: Reporting and Export

Implement reporting incrementally:

1. CSV export first
2. Excel export next
3. PDF export last

Suggested report endpoints:

- Employee report
- Department report
- User report
- Login activity report

## Phase 7: Frontend Implementation

Recommended page order:

1. Login
2. Protected layout and navigation
3. Dashboard
4. Employee list and employee form
5. Department management
6. User management
7. Role and permission management
8. Settings
9. Audit log
10. Reports

Frontend implementation details:

- Route guards for authentication
- Shared API service
- Reusable table component for search, sort, pagination
- Reusable form validation patterns
- File upload handling for employee photos
- Export actions from list/report pages

## Phase 8: Testing and Validation

Minimum test targets:

- Authentication service
- Password policy validation
- Employee validation rules
- Department service rules
- Token refresh logic

Validation checklist:

- Verify all required endpoints in Swagger
- Verify authorization rules
- Verify audit entries are created
- Verify pagination and sorting behavior
- Verify export generation
- Verify Docker startup
- Verify health check endpoint

## Suggested Priority Order for Assessment Completion

If time is limited, implement in this order:

1. Project setup and database foundation
2. JWT authentication and refresh token flow
3. Employee CRUD with search, sorting, pagination, and validation
4. Department CRUD
5. User CRUD with roles
6. Dashboard aggregates
7. Audit logging
8. Settings module
9. Reporting and export polish

## Recommended Folder Structure

Backend:

- src/API
- src/Application
- src/Domain
- src/Infrastructure
- tests/API.Tests or tests/Application.Tests

Frontend:

- src/app or src/pages
- src/features/auth
- src/features/dashboard
- src/features/employees
- src/features/departments
- src/features/users
- src/features/roles
- src/features/settings
- src/features/audit
- src/features/reports
- src/shared

## Key Risks and Mitigation

- Password reset with SMTP can take time: keep the flow simple and configurable.
- PDF export can consume time: implement CSV and Excel first, then PDF.
- Fine-grained permissions can expand quickly: define a stable permission matrix early.
- Audit logging is easy to delay and hard to retrofit: build it from the beginning.

## Final Submission Checklist

- Complete source code
- Working database migrations or SQL scripts
- README with setup steps
- AI usage log
- Screenshots or demo video if available