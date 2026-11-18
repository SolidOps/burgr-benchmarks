# AGENTS.md

The API of this repository is an ASP.NET Core Minimal API with EF Core, in a single
project, written by hand.

## Layout

In the API project, under its root namespace:

| Folder | Content |
|---|---|
| `Domain/` | The entities: a class per aggregate, a `Guid Id`, its properties, a foreign key `XId` and a navigation `X` for each reference, a `List<Y>` for each collection, the enums. Calculated properties are read-only properties computed from the others. |
| `Contracts/` | Per entity, `XDto` (the read model returned by the API, `static XDto From(X)`) and `XInput` (the body of `POST` and `PUT`, with `Validate()` returning the errors by property, and `ApplyTo(X)`). |
| `Infrastructure/` | The `DbContext`: a `DbSet` per entity, keys, maximum lengths, unique indexes, enums, calculated properties ignored. |
| `Api/` | A static class with `Map<Project>Endpoints()`: a route group per entity, `GET /`, `GET /{id}`, `POST /` (201), `PUT /{id}`, `DELETE /{id}` (204). 400 with a validation problem (unknown reference included), 404, 409 when a unique index or a foreign key is violated. |
| `Program.cs` | Database provider, schema creation (`EnsureCreated`), `app.Map<Project>Endpoints()`, `public partial class Program;` for the tests. |

## Workflow

1. Write or change the code.
2. `dotnet build`, then `dotnet test` on the solution.

## Testing

Integration tests through the real endpoints, with `WebApplicationFactory<Program>`
(`Microsoft.AspNetCore.Mvc.Testing`) and a throw-away SQLite database per test. Test the
business rules (calculated properties, validation rules, uniqueness, references), plus one
round trip per entity.
