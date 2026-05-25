# agents.md

You are an AI coding assistant for this C# project.

## Rules

- Always follow `.editorconfig`
- Treat `.editorconfig` as the highest-priority code style rule
- Follow nearby files for naming, structure, and local conventions
- Prefer C# and .NET best practices when they do not conflict with `.editorconfig`
- Keep implementations simple, clear, and production-ready

## Validate formatting after code changes

After writing or modifying code, run `dotnet format` to validate formatting.

- Use `dotnet format` as a required verification step after code changes
- Fix any formatting issues reported by the command before finishing
- Do not skip this step unless the project cannot run the command in the current environment
- If `dotnet format` cannot be executed, clearly state that it was not run and why

## Prefer concise modern C# syntax

Use modern C# syntax when it makes the code shorter and clearer.

Examples include:

- auto-property initializers
- getter-only properties
- expression-bodied members
- `nameof`
- string interpolation
- null-conditional and null-coalescing operators
- inline `out var`
- pattern matching
- tuples and deconstruction
- target-typed `new()`
- `using` declarations
- file-scoped namespaces
- global usings
- `init` accessors
- records
- primary constructors

Prefer property or field initializers over constructor boilerplate for simple defaults.
Prefer primary constructors for simple state initialization.

Do not use newer syntax when it is unsupported, conflicts with `.editorconfig`, conflicts with existing repository style, or makes the code harder to read.

## One type per file

Prefer one top-level type per file.
Keep the file name aligned with the main type name whenever possible.

Exception:

- test files may contain multiple related types when convenient

Avoid placing multiple unrelated types in the same file.

## Use DI and keep code decoupled

Use dependency injection consistently.

- Prefer constructor injection
- Use the Microsoft DI framework in the standard way
- Depend on abstractions when it improves decoupling
- Keep services small and focused
- Keep business logic separate from infrastructure
- Compose dependencies at the application boundary

Avoid:

- creating dependencies with `new` inside business logic
- hidden dependencies
- service locator patterns
- unnecessary coupling between layers
- meaningless abstractions
