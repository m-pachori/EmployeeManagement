# Employee Management System Requirement Analysis

## Project Overview

Build an Employee Management System from scratch using AI-assisted development tools while following software engineering best practices. The chosen target stack is:

- .NET 8 Web API
- Angular 22
- SQL Server with Entity Framework Core
- JWT authentication
- Swagger
- Git for source control
- AI coding assistant (GitHub Copilot, ChatGPT, Cursor, or similar) for AI-assisted development

## Functional Requirements

### 1. Authentication and Security

The system must support:

- User login with JWT authentication
- Logout
- Refresh token flow
- Forgot password
- Change password
- Password hashing
- Account lockout
- Password expiry

### 2. Dashboard

The dashboard must display:

- Employee count
- Department count
- Recent activity
- Last login information

### 3. Employee Management

The employee module must support:

- Create employee
- Edit employee
- Delete employee
- Search employees
- Pagination
- Sorting
- Export to Excel
- Export to PDF
- Photo upload

Required employee fields:

- Employee Code
- First Name
- Last Name
- Email
- Phone
- Department
- Designation
- Salary
- Joining Date
- Manager
- Status
- Created By/Date
- Updated By/Date

### 4. Department Management

The department module must support full CRUD operations.

### 5. User Management

The user module must support:

- Create user
- Edit user
- Delete user
- View user list
- Assign roles

### 6. Role and Permission Management

The system must support:

- Role CRUD
- Permission CRUD or controlled management
- Role-permission mapping
- Permission-based authorization

### 7. Settings

The settings module must support:

- Company settings
- Application settings
- SMTP settings
- Audit settings

### 8. Audit Log

The system must capture and expose audit log information for important actions.

### 9. Reports

The system must support reports for:

- Employees
- Departments
- Users
- Login activity

Export formats:

- Excel
- PDF
- CSV

## Required APIs

Authentication:

- POST /login
- POST /logout
- POST /refresh-token
- POST /forgot-password
- POST /change-password

Business modules:

- GET/POST/PUT/DELETE Employees
- GET/POST/PUT/DELETE Departments
- GET/POST/PUT/DELETE Users
- GET/POST/PUT/DELETE Roles
- GET/POST Settings

All endpoints must be versioned (for example, `/api/v1/login`, `/api/v1/employees`) to satisfy the API versioning requirement.

## Database Requirements

The database must be normalized and include at least the following tables:

- Users
- Roles
- Permissions
- RolePermissions
- Employees
- Departments
- Settings
- AuditLogs
- RefreshTokens
- EmployeeDocuments

Note: `EmployeeDocuments` stores the employee's uploaded photo as well as any additional supporting documents, so it should support a document type/category column to distinguish the profile photo from other attachments.

Required database design elements:

- Primary keys
- Foreign keys, including a self-referencing foreign key on Employees (ManagerId → EmployeeId) to support the Manager field
- Unique constraints
- Check constraints
- Default constraints

Required indexing:

- Clustered index on EmployeeID
- Nonclustered indexes on Email, DepartmentID, Status
- Composite index on DepartmentID and Status

The database and queries should be optimized for:

- Search
- Pagination
- Dashboard metrics
- Reporting

## Validation Rules

- Employee email must be unique
- Phone must be numeric
- Salary must be positive
- Joining date cannot be in the future
- Department is mandatory
- Password must contain at least 8 characters, uppercase, lowercase, number, and special character

## Additional Requirements

The assessment explicitly requires the following cross-cutting technical capabilities:

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

## AI Usage Log Requirement

The submission must include an AI usage log with:

- Prompts used
- Accepted AI suggestions
- Modified AI suggestions
- Rejected AI suggestions with reasons
- Validation performed before accepting AI-generated code

## Deliverables

- Source code
- SQL scripts
- README with setup instructions
- AI usage log
- Optional screenshots or demo video