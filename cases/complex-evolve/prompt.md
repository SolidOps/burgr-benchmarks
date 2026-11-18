Add teams to this user management API.

## Domain

- A **team** has a `name` (required, at most 50 characters, unique), an optional
  `description` (at most 200 characters) and its **members**.
- A **member** belongs to one team and has a `userEmail` (required, at most 50
  characters: the email of an existing user) and `isOwner` (boolean). The same email
  appears at most once in a team.
- A team with members has exactly one owner; a team without members is allowed.
- A team that still has members cannot be deleted.

## HTTP contract

Follow the conventions of the existing endpoints:

- `/teams` offers `GET /` (a page, like `/users`), `GET /{id}`, `POST /` (201 with the
  created team), `PUT /{id}` (200 with the updated team, same body as `POST`),
  `DELETE /{id}` (204).
- A team carries `members`, the array of its members, each with its `id`. `POST` and `PUT`
  take the whole list of members: a member sent without `id` is added, a member sent with
  its `id` is updated, a member missing from the list is removed.
- Errors: 404 for an unknown id; 400 with a validation problem for an invalid body,
  including a member email that matches no user and a team with members but not exactly
  one owner; 409 when the name is already taken or the team still has members on
  deletion.
- Security: reading teams requires an authenticated caller; writing them requires the new
  right `ManageTeams` (403 without it).

## Done

Write integration tests for what you add. You are done when `dotnet build` and
`dotnet test` pass on `UM.slnx`, and the existing behavior is unchanged.
