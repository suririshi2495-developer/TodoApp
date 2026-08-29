# TodoApp

A to-do list REST API built with ASP.NET Core on .NET 8. Items are kept in a JSON file, so the list survives a restart.

## Running it

With the .NET 8 SDK:

    dotnet run --project src/TodoApp.Api

Swagger UI is at `/swagger` when running in Development.

With Docker:

    docker build -t todoapp .
    docker run --rm -p 8080:8080 -v todo-data:/data todoapp

The named volume is what makes the data outlive the container. Without `-v`, the list is gone when the container stops.

Add `-e ASPNETCORE_ENVIRONMENT=Development` to the run command if you want Swagger in the container.

## Endpoints

All bodies are JSON. Errors come back as RFC 7807 problem documents with the media type `application/problem+json`.

- `POST /api/todos` - create an item. 201 with a `Location` header, or 400 if the body is invalid.
- `GET /api/todos` - list items. 200.
- `GET /api/todos/{id}` - one item. 200, or 404.
- `PUT /api/todos/{id}` - replace title, description and due date. 200, 400 or 404.
- `POST /api/todos/{id}/complete` - 200, 404, or 409 if it is already complete.
- `POST /api/todos/{id}/incomplete` - 200, 404, or 409 if it is already incomplete.
- `DELETE /api/todos/{id}` - 204, or 404.

An item has an id, a required title, an optional description, an optional due date, a completion flag and a creation timestamp.

## Filtering and sorting

`GET /api/todos` accepts two optional query parameters:

- `filter` - `all`, `completed`, `incomplete` or `overdue`. Defaults to `all`.
- `sort` - `createdAt`, `dueDate` or `title`. Defaults to `createdAt`.

Both are case-insensitive. An unrecognised value returns 400 rather than falling back to the default, so a typo shows up instead of quietly returning the wrong list.

Items with no due date sort last under `dueDate`. An item is overdue when it has a due date in the past and is not yet complete.

## Storage

The file path comes from `Storage:FilePath` in configuration, or the `Storage__FilePath` environment variable. The application refuses to start if it is not set - a silent default would mean writing somewhere nobody chose.

Writes go to a temporary file and are then moved into place, so a crash mid-write cannot leave a half-written list behind. If the file is found to be unreadable on startup it is renamed aside rather than deleted, and the application starts empty.

Access is serialised within the process. This is a single-instance store; it is not safe to point two running copies at the same file.

## Design

Three projects, and the dependency direction is enforced by the compiler rather than by convention.

- `TodoApp.Core` - the `TodoItem` entity, the rules that keep it valid, `TodoService`, and the `ITodoRepository` and `IClock` interfaces. It references no other project and no framework packages.
- `TodoApp.Infrastructure` - the JSON file repository, an in-memory repository used by tests, and the system clock.
- `TodoApp.Api` - controllers, request and response types, mapping, and the exception handler that turns domain exceptions into status codes.

The interfaces are owned by Core and implemented in Infrastructure, so the domain does not know that storage is a file. They are wired together in `Program.cs`, which is the only place that knows both sides.

Validation exists twice on purpose. DataAnnotations on the request types reject an oversized payload at the edge; the `TodoItem` constructor is the authority and enforces the same rules for every caller, including tests and any future entry point.

Requests and responses are separate types from the entity, mapped by hand in `TodoMapper`. The mapping is a dozen lines and it is readable, which a mapping library's configuration would not be.

## Tests

    dotnet test

135 tests, no mocking library - the fakes are hand-written and a few lines each.

- `TodoApp.Core.Tests` - entity rules, service behaviour, and the query parsing.
- `TodoApp.Infrastructure.Tests` - both repositories against the same expectations, including a corrupt file and a restart.
- `TodoApp.Api.Tests` - the endpoints end to end through `WebApplicationFactory`, against an in-memory repository.

## Not included

Authentication, CORS and rate limiting are deliberately absent. Nothing in the exercise calls for them, and each would be a few lines in `Program.cs` plus a decision about who the callers are, which is not a decision this project has enough information to make.

The container serves plain HTTP. TLS belongs at whatever sits in front of it.