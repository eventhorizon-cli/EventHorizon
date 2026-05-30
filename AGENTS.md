# agents.md

You are an AI coding assistant for this repository.

## Global Rules

- Always follow `.editorconfig`.
- Treat `.editorconfig` as the highest-priority code style rule.
- Follow nearby files for naming, structure, formatting, and local conventions.
- Keep implementations simple, clear, maintainable, and production-ready.
- Match the language, framework, and architectural style of the target folder.
- Do not apply backend-specific rules to frontend code.
- Do not apply frontend-specific rules to backend code.

## Documentation Rules

When modifying code, always consider whether related documentation must be updated.

- Update relevant documentation together with code changes when behavior, configuration, APIs, commands, architecture, setup steps, or user-facing functionality changes.
- Keep documentation consistent with the implemented behavior.
- Prefer updating nearby documentation, README files, inline usage examples, API docs, configuration docs, or developer guides as appropriate.
- Do not update documentation unnecessarily for purely internal refactors that do not change behavior, usage, configuration, or public contracts.
- If documentation should be updated but cannot be updated in the current task, clearly state what documentation is affected and why it was not changed.

## C# Rules for `src`

These rules apply to C# and .NET projects under the `src` folder.

### General C# Guidelines

- Prefer C# and .NET best practices when they do not conflict with `.editorconfig`.
- Follow the existing style and architecture of nearby files.
- Keep code readable and avoid unnecessary complexity.

### Formatting Validation

After writing or modifying code in C# projects under `src`, run:

```bash
dotnet format
```

Rules:

- Use `dotnet format` as a required verification step after C# code changes.
- Fix any formatting issues reported by the command before finishing.
- Do not skip this step unless the command cannot be executed in the current environment.
- If `dotnet format` cannot be executed, clearly state that it was not run and explain why.

### Prefer Concise Modern C# Syntax

Use modern C# syntax when it makes the code shorter, clearer, and consistent with the repository style.

Examples include:

- auto-property initializers
- getter-only properties
- expression-bodied members
- `nameof`
- string interpolation
- null-conditional operators
- null-coalescing operators
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

Guidelines:

- Prefer property or field initializers over constructor boilerplate for simple defaults.
- Prefer primary constructors for simple state initialization.
- Do not use newer syntax when it:
    - is unsupported by the project,
    - conflicts with `.editorconfig`,
    - conflicts with existing repository style,
    - or makes the code harder to read.

### One Type Per File

Prefer one top-level type per file.

Guidelines:

- Keep the file name aligned with the main type name whenever possible.
- Test files may contain multiple related types when convenient.
- Avoid placing multiple unrelated types in the same file.

### Comments Must Be Written in English

All code comments must be written in English.

This includes:

- inline comments
- XML documentation comments
- TODO/FIXME comments
- explanatory comments in tests
- comments in configuration or registration code

Guidelines:

- Avoid mixing languages in comments.
- If a comment is necessary, make it concise, clear, and useful.
- Prefer self-explanatory code over excessive comments.

### Use Dependency Injection and Keep Code Decoupled

Use dependency injection consistently.

Guidelines:

- Prefer constructor injection.
- Use the Microsoft DI framework in the standard way.
- Depend on abstractions when it improves decoupling.
- Keep services small and focused.
- Keep business logic separate from infrastructure.
- Compose dependencies at the application boundary or composition root.

#### DI Registration Rules

Services registered in dependency injection should be registered through abstraction interfaces when they are consumed outside their own implementation.

Prefer:

```csharp
services.AddScoped<IUserService, UserService>();
services.AddSingleton<IClock, SystemClock>();
services.AddTransient<IEmailSender, EmailSender>();
```

Avoid registering concrete services directly when they are consumed by application or business logic:

```csharp
services.AddScoped<UserService>();
```

Rules:

- Define an interface for DI-registered services that are consumed outside their own implementation.
- Inject interfaces into consumers instead of concrete classes.
- Keep interfaces focused and meaningful.
- Do not create meaningless abstractions for:
    - simple data objects,
    - options classes,
    - framework types,
    - or types that are not services.
- Concrete implementations may depend on other abstractions through constructor injection.
- Register implementations at the application boundary or composition root.

Avoid:

- creating dependencies with `new` inside business logic,
- hidden dependencies,
- service locator patterns,
- unnecessary coupling between layers,
- meaningless abstractions.

## Frontend Rules for `eventhorizon-workbench`

These rules apply only to the frontend project under `eventhorizon-workbench`.

### General Frontend Guidelines

- Follow `.editorconfig`.
- Follow the project's existing frontend conventions.
- Follow nearby frontend files for:
    - naming,
    - component structure,
    - state management,
    - styling,
    - testing patterns.
- Prefer simple, maintainable, and production-ready UI implementations.
- Keep components focused and composable.
- Avoid applying C# or .NET-specific patterns in frontend code.

### Frontend Validation

Use the package manager, formatter, linter, and build tools already used by the frontend project.

After frontend code changes:

- Run the appropriate validation commands defined by that project when available.
- If frontend validation commands cannot be executed, clearly state that they were not run and explain why.
