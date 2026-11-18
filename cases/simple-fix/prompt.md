Users report a bug in this library API: a published book with no copy left is shown as
available (`isAvailable: true`). A book is available when it is published and has at
least one copy.

Fix it, and add a regression test. You are done when `dotnet build` and `dotnet test`
pass on `Library.slnx`.
