Build the HTTP API of a ticketing service in this repository, in onion architecture.

## Domain

- A **venue** has a `name` (required, at most 100 characters, unique), an `address`
  (`street` at most 100 characters, `city` at most 50, `postalCode` at most 10, all
  required) and a `capacity` (integer).
- A **show** has a `title` (required, at most 200 characters), a `startsAt` date and time
  (UTC), a `status` (`Draft` = 0, `OnSale` = 1, `Cancelled` = 2), the venue where it takes
  place (`venueId`, required) and its **ticket tiers**.
- A **ticket tier** belongs to one show and has a `name` (required, at most 50
  characters), a `price` (decimal), a `quota` and a `sold` count (integers). `sold` is
  read-only: only bookings change it, starting at 0.
- The quotas of the tiers of a show add up to at most the capacity of its venue.
- `isSoldOut` is computed on a show: true when it has tiers and every tier is sold out
  (`sold` ≥ `quota`).
- A **booking** records `showId`, `tier` (the tier name), `customerEmail`, `quantity` and
  `totalPrice` (the tier price × quantity). Bookings are only created by the box office.
- A venue that still has shows cannot be deleted; a show that has bookings cannot be
  deleted. Deleting a show deletes its tiers.

## Box office

`POST /box-office/book` with `{ showId, tier, customerEmail, quantity }` books tickets and
returns 200 with the booking. In this order:

- 400 when `quantity` is not between 1 and 10, or `customerEmail` has no `@`;
- 404 when the show does not exist;
- 409 when the show is not `OnSale`, or starts in the past;
- 400 when the show has no tier with that name;
- 409 when the tier has fewer tickets left than `quantity`;
- otherwise the tier's `sold` grows by `quantity` and the booking is saved, together.

## HTTP contract

- JSON with camelCase property names; an enum is sent and returned as its integer value.
- Every venue, show, tier and booking has a server-generated GUID `id`.
- `/venues` and `/shows` each offer `GET /`, `GET /{id}`, `POST /` (201 with the created
  object), `PUT /{id}` (200 with the updated object, same body as `POST`), `DELETE /{id}`
  (204). `/bookings` offers `GET /` and `GET /{id}` only.
- `GET /` returns a page: `{ items, pageNumber, pageSize, totalCount }`, with the query
  parameters `page` (from 1, default 1), `pageSize` (1 to 200, default 50), `orderBy` (the
  name of a simple property of the object, in PascalCase: `Name`, `Title`…) and
  `descending` (boolean). An invalid paging parameter is a 400.
- A venue carries its `address` as a nested object; `GET /venues/{id}` also includes
  `shows`, the array of its shows.
- A show carries `venueId` and `ticketTiers`, the array of its tiers, each with its `id`.
  `POST` and `PUT` take the whole list of tiers: a tier sent without `id` is added, a tier
  sent with its `id` is updated, a tier missing from the list is removed. `sold` is ignored
  in the body. `GET /shows/{id}` returns `isSoldOut`.
- Errors: 404 for an unknown id; 400 with a validation problem for an invalid body,
  including an unknown `venueId` or `status`, and tiers exceeding the venue capacity; 409
  when a unique value is already taken or a deletion is refused.

## Security

- Every endpoint requires an authenticated caller: a JWT bearer token signed with HMAC
  SHA-256 by the key `Jwt:Key` of the configuration (the key's UTF-8 bytes, right-padded
  with spaces to 32 bytes). Validate the signature and the lifetime only, not the issuer
  nor the audience. No valid token: 401.
- The rights of the caller are the `right` claims of the token; the right `*` grants
  every right. Writing venues requires `ManageVenues`, writing shows requires
  `ManageShows`, reading bookings requires `ManageShows`. Missing right: 403. Reading
  venues and shows, and booking, only require an authenticated caller.

## Runtime

- The projects are `src/Ticketing.Domain`, `src/Ticketing.Application`,
  `src/Ticketing.Infrastructure`, `src/Ticketing.Api` (the API, the host) and
  `src/Ticketing.Tests`, in `Ticketing.slnx`.
- SQLite, with the connection string `ConnectionStrings:Ticketing`; the schema is created
  at startup.
- Write integration tests in `src/Ticketing.Tests`. You are done when `dotnet build` and
  `dotnet test` pass on `Ticketing.slnx`.
