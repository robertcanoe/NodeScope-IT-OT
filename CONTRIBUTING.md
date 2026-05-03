# Contributing

Thanks for considering a contribution to NodeScope IT/OT.

## Development setup

1. Start PostgreSQL:

```bash
docker compose up -d
```

2. Configure JWT signing key for local runs:

```bash
export Jwt__SigningKey="dev-signing-key-please-change-32chars+"
```

3. Run API and worker:

```bash
dotnet run --project src/NodeScope.Api/NodeScope.Api.csproj
dotnet run --project src/NodeScope.Worker/NodeScope.Worker.csproj
```

4. Run frontend:

```bash
cd frontend/nodescope-web
pnpm install
pnpm start
```

## Coding guidelines

- Keep the layered architecture intact: Domain → Application → Infrastructure → API/Worker.
- Prefer primary constructors for DI where possible.
- Use async/await for all I/O work and call `ConfigureAwait(false)` in library code.
- Keep secrets out of source control and appsettings files.

## Tests

Run .NET builds and tests from the solution root:

```bash
dotnet build src/NodeScope.sln
dotnet test src/NodeScope.sln
```

## Submitting changes

1. Create a feature branch.
2. Ensure the solution builds and tests pass.
3. Update `CHANGELOG.md` under the `Unreleased` section if behavior changes.
4. Open a pull request with a concise summary and test evidence.
