# Testing Plan — Application Service Unit Tests

## Overview

Extend the existing `tests/EmployeeManagement.Tests` project with unit tests for the four
Application-layer services introduced in the debt-backlog implementation:

- `EmployeeService`
- `DepartmentService`
- `UserService`
- `RoleService`

**Approach:** Mirror the pattern already established in `AuthenticationTests.cs` — construct
a real `ApplicationDbContext` backed by `UseInMemoryDatabase(Guid.NewGuid().ToString())`,
wrap it in a real `UnitOfWork` and `AuditLogService`, then instantiate the concrete service
under test. No mocking framework is required; EF Core In-Memory is the collaborator.

**Scope:** Unit tests only. No WebApplicationFactory / controller integration tests in this pass.

**Coverage target:** Every public method on each service, covering at minimum: the happy path,
the primary 404 not-found path, and every distinct validation / conflict guard.

---

## Shared Infrastructure (no sub-task file needed)

All four test classes share the same two static helpers used in `AuthenticationTests`:

```
CreateDbContext()   → new ApplicationDbContext with UseInMemoryDatabase(Guid.NewGuid())
```

Each test class also needs a typed `Create<ServiceName>()` factory that wires
`UnitOfWork` + `AuditLogService` and returns the concrete service.

Seed helpers are kept private to the test class that needs them, to keep each class
self-contained and readable.

---

## Sub-Task 1 — DepartmentServiceTests

**Status:** [x] done

### Intent
`DepartmentService` is the simplest service (no external dependencies beyond UoW + Audit).
Starting here validates the pattern before tackling more complex services.

### Methods to cover
| Method | Scenarios |
|---|---|
| `GetAllAsync` | Returns empty list; returns seeded departments ordered by name |
| `GetByIdAsync` | Returns correct DTO; throws 404 when not found |
| `CreateAsync` | Happy path — persists entity, returns new Id; throws 400 on blank Name/Code; throws 409 on duplicate Name; throws 409 on duplicate Code |
| `UpdateAsync` | Happy path — mutates entity; throws 404 when not found; throws 409 on duplicate Name (different row); allows same Name on same row |
| `DeleteAsync` | Happy path — removes entity; throws 404 when not found; throws 409 when employees are assigned |

### Expected Outcomes
- 12+ test methods in `DepartmentServiceTests.cs`
- All pass against the in-memory provider

### Todo List
1. Create `tests/EmployeeManagement.Tests/DepartmentServiceTests.cs`
2. Add `CreateDbContext()` helper (identical to `AuthenticationTests` — can be extracted later)
3. Add `CreateService(ApplicationDbContext)` factory returning `DepartmentService`
4. Add a `SeedDepartmentAsync(context, name, code)` private helper
5. Add a `SeedEmployeeAsync(context, deptId)` private helper (needed for the delete-with-employees guard)
6. Implement all test methods listed above

### Relevant Context
- Service: `src/EmployeeManagement.Infrastructure/Services/DepartmentService.cs`
- DTO: `src/EmployeeManagement.Application/Departments/Dtos/UpsertDepartmentRequest.cs`
- Entity: `src/EmployeeManagement.Domain/Entities/Department.cs`, `Employee.cs`
- Error type: `src/EmployeeManagement.Application/Common/Exceptions/ApiException.cs`

---

## Sub-Task 2 — RoleServiceTests

**Status:** [x] done

### Intent
`RoleService` adds the permission-assignment flow (`AssignPermissionsAsync`) on top of
standard CRUD — tests the replace-all pattern and the "role in use" delete guard.

### Methods to cover
| Method | Scenarios |
|---|---|
| `GetAllAsync` | Returns all roles with correct counts |
| `CreateAsync` | Happy path; throws 400 on blank Name; throws 409 on duplicate Name |
| `UpdateAsync` | Happy path; throws 404; throws 409 on duplicate Name (different row); allows same Name on same row |
| `DeleteAsync` | Happy path; throws 404; throws 409 when role has users |
| `GetPermissionsAsync` | Returns seeded permissions ordered by Module then Action |
| `AssignPermissionsAsync` | Happy path — replaces existing mappings; throws 404 on bad role; throws 400 on invalid permission IDs |

### Expected Outcomes
- 14+ test methods in `RoleServiceTests.cs`
- All pass

### Todo List
1. Create `tests/EmployeeManagement.Tests/RoleServiceTests.cs`
2. Add `CreateService(ApplicationDbContext)` factory
3. Add `SeedRoleAsync`, `SeedPermissionAsync`, `SeedUserWithRoleAsync` helpers
4. Implement all test methods listed above

### Relevant Context
- Service: `src/EmployeeManagement.Infrastructure/Services/RoleService.cs`
- DTOs: `src/EmployeeManagement.Application/Roles/Dtos/`
- Entities: `src/EmployeeManagement.Domain/Entities/Role.cs`, `Permission.cs`, `UserRole.cs`, `RolePermission.cs`

---

## Sub-Task 3 — UserServiceTests

**Status:** [x] done

### Intent
`UserService` introduces `PasswordHasher<User>` and `AuthOptions` as additional constructor
dependencies. Tests cover CRUD, role assignment (replace-all), status toggle, and the
self-delete guard.

### Methods to cover
| Method | Scenarios |
|---|---|
| `GetAllAsync` | Returns users with their role names |
| `CreateAsync` | Happy path with roles; happy path without roles; throws 400 on blank required fields; throws 400 on weak password; throws 409 on duplicate username; throws 409 on duplicate email; throws 400 on invalid role IDs |
| `UpdateAsync` | Happy path; throws 404; throws 409 on duplicate email (different row); allows same email on same row |
| `AssignRolesAsync` | Happy path replaces mappings; throws 404; throws 400 on invalid role IDs |
| `UpdateStatusAsync` | Deactivates user; reactivates user; throws 404 |
| `DeleteAsync` | Happy path; throws 404; throws 400 when deleting own account |

### Expected Outcomes
- 17+ test methods in `UserServiceTests.cs`
- All pass

### Todo List
1. Create `tests/EmployeeManagement.Tests/UserServiceTests.cs`
2. Add `CreateService(ApplicationDbContext, AuthOptions?)` factory (injecting `PasswordHasher<User>` + `IOptions<AuthOptions>`)
3. Add `SeedUserAsync`, `SeedRoleAsync` helpers
4. Implement all test methods listed above

### Relevant Context
- Service: `src/EmployeeManagement.Infrastructure/Services/UserService.cs`
- DTOs: `src/EmployeeManagement.Application/Users/Dtos/`
- Auth: `src/EmployeeManagement.Infrastructure/Authentication/AuthOptions.cs`, `PasswordPolicyValidator.cs`
- Pattern for `PasswordHasher` + `AuthOptions`: `AuthenticationTests.CreateAuthService()`

---

## Sub-Task 4 — EmployeeServiceTests

**Status:** [x] done

### Intent
`EmployeeService` is the most complex service. Tests cover list/search/pagination,
CRUD with cross-entity validation (department exists, manager exists, self-manager guard),
and photo upload validation (size, extension, magic bytes). Photo save-to-disk is tested
using `Path.GetTempPath()` as the `contentRootPath` so no production filesystem is touched.

### Methods to cover
| Method | Scenarios |
|---|---|
| `GetEmployeesAsync` | Returns all; filters by search keyword; filters by departmentId; filters by status; pagination (page 2); sort by lastName asc |
| `GetByIdAsync` | Returns correct DTO; throws 404 |
| `CreateAsync` | Happy path; throws 400 on blank required fields; throws 400 on future DateOfJoining; throws 400 on negative salary; throws 400 on invalid department; throws 400 on non-existent manager; throws 400 on self-manager; throws 409 on duplicate code; throws 409 on duplicate email |
| `UpdateAsync` | Happy path; throws 404; throws 400 on self-manager; throws 409 on duplicate code (different row); allows same code on same row |
| `DeleteAsync` | Happy path; throws 404 |
| `UploadPhotoAsync` | Throws 400 on zero size; throws 400 on oversized; throws 400 on wrong extension; throws 400 on wrong MIME type; throws 400 on invalid magic bytes; throws 404 on missing employee; happy path writes file and returns URL |

### Expected Outcomes
- 25+ test methods in `EmployeeServiceTests.cs`
- All pass

### Todo List
1. Create `tests/EmployeeManagement.Tests/EmployeeServiceTests.cs`
2. Add `CreateService(ApplicationDbContext)` factory
3. Add `SeedDepartmentAsync`, `SeedEmployeeAsync` helpers
4. Add `MakeJpegStream(bool validMagicBytes)` helper for photo upload tests (writes `FF D8 FF ...` or a fake header)
5. Implement all test methods listed above; use `Path.GetTempPath()` as `contentRootPath` in photo tests

### Relevant Context
- Service: `src/EmployeeManagement.Infrastructure/Services/EmployeeService.cs`
- DTOs: `src/EmployeeManagement.Application/Employees/Dtos/`
- Entities: `src/EmployeeManagement.Domain/Entities/Employee.cs`, `Department.cs`
- JPEG magic bytes constant: `[0xFF, 0xD8, 0xFF]` (in `EmployeeService`)
- `EF.Functions.Like` is not supported by the In-Memory provider — the search test must seed
  data and assert results use the InMemory fallback (substring match), OR the search tests
  assert filter parameters are passed correctly by checking counts rather than relying on
  SQL LIKE behaviour. Document this limitation with a comment in the test.

---

## Notes

- No new NuGet packages are required. The existing `Microsoft.EntityFrameworkCore.InMemory`
  (8.0.29) and `xunit` (2.5.3) packages in `EmployeeManagement.Tests.csproj` are sufficient.
- The `EF.Functions.Like` limitation with In-Memory provider affects only `GetEmployeesAsync`
  search tests. At runtime against SQL Server, `Like` is translated to a `LIKE` predicate.
  In tests, the in-memory provider falls back to string evaluation that may differ — seed data
  should be chosen so the search test is meaningful regardless.
- Each sub-task is independent and can be implemented and reviewed individually.
- After all four sub-tasks are complete, run `dotnet test` to confirm the final count.
