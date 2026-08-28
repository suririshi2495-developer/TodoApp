# TodoApp

A small to-do list REST API built with ASP.NET Core (.NET 8) and controllers.

## Status

Work in progress. Currently scaffolded only.

## Structure

- `src/TodoApp.Core` domain model, guards, interfaces, service logic. Depends on nothing.
- `src/TodoApp.Infrastructure` JSON file storage and the system clock.
- `src/TodoApp.Api` controllers, DTOs, validation, middleware, DI wiring.
- `tests/TodoApp.Core.Tests` unit tests.
- `tests/TodoApp.Api.Tests` integration tests.

## Run

Open `TodoApp.sln` in Visual Studio, set `TodoApp.Api` as the startup project, press F5.

## Test

Test menu, Run All Tests.