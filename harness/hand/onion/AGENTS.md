# AGENTS.md

The API of this repository is an ASP.NET Core API in onion architecture, with EF Core,
written by hand.

## Projects

For a module `M`, in the namespace `<Namespace>.M.<Layer>`; the dependencies point to the
domain:

| Project | Content | References |
|---|---|---|
| `M.Domain` | Aggregate roots and entities (a `Guid Id`), value objects (records), enums, repository interfaces. | nothing |
| `M.Application` | DTOs, inputs with their validation, one service per aggregate root with its use cases (query, get, add, update, remove) and business rules, other application services, rights, `AddMApplication()`. | Domain |
| `M.Infrastructure` | The `DbContext` (value objects stored as JSON in their owner's row), repositories, unit of work, `AddMInfrastructure(configureDatabase)`. | Application |
| `M.Api` | Minimal API endpoints, mapping the outcome of a use case to an HTTP response, authorization, `Program.cs` (the host). | Application, Infrastructure |
| `M.Tests` | Integration tests. | Api |

A `Framework/` folder in a project holds the base classes shared by every aggregate
(entity, repository, result, unit of work, test base): reuse and extend them rather than
duplicating their logic.

## Conventions

- A use case returns a value or an error; the API maps not found to 404, invalid to 400
  (a validation problem with the errors by property path), conflict to 409, forbidden to
  403.
- Nothing is saved until the unit of work commits; a unique index or foreign key violated
  on commit is a conflict.
- Queries are paged: `page`, `pageSize`, `orderBy`, `descending`.
- Authentication is configured in `Program.cs`; rights are `right` claims, `*` grants
  every right.

## Workflow

1. Write or change the code, layer by layer.
2. `dotnet build`, then `dotnet test` on the solution.

## Testing

Integration tests through the real endpoints, with `WebApplicationFactory<Program>`
(`Microsoft.AspNetCore.Mvc.Testing`), a throw-away SQLite database per test and a test
authentication. Test each business rule, service method and right, plus one round trip
per aggregate root.
