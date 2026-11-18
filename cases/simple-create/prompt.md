Build the HTTP API of a small warehouse inventory in this repository.

## Domain

- A **supplier** has a `name` (required, at most 100 characters, unique) and an optional
  `email` (at most 100 characters).
- A **product** has a `sku` (required, at most 20 characters, unique), a `name` (required,
  at most 100 characters), a `stock` and a `reorderLevel` (integers, neither negative), a
  `status` (`Active` = 0, `Discontinued` = 1) and the supplier it comes from (`supplierId`,
  required).
- `needsReorder` is computed, never stored: true when the product is `Active` and its
  `stock` is at or below its `reorderLevel`.

## HTTP contract

- JSON with camelCase property names; an enum is sent and returned as its integer value.
- Every object has a server-generated GUID `id`.
- `/suppliers` and `/products` each offer: `GET /` (array of all the objects), `GET /{id}`,
  `POST /` (201 with the created object), `PUT /{id}` (200 with the updated object, same
  body as `POST`), `DELETE /{id}` (204).
- `GET /suppliers/{id}` includes `products`, the array of its products.
- A product returns its `supplierId` and `needsReorder`.
- Errors: 404 for an unknown id; 400 with a validation problem for an invalid body,
  including an unknown `supplierId` or `status`; 409 when a unique value is already taken.

## Runtime

- The API project is `src/Warehouse/Warehouse.csproj`, in `Warehouse.slnx`.
- SQLite, with the connection string `ConnectionStrings:Warehouse`; the schema is created
  at startup.
- Write integration tests in `src/Warehouse.Tests`. You are done when `dotnet build` and
  `dotnet test` pass on `Warehouse.slnx`.
