# AGENTS.md

Guidance for AI coding agents working in this repository.

## What this repo is

A personal C#/.NET learning playground ("developer_growing"). It is a monorepo of **two independent solutions** plus zipped reference samples. There is **no shared build, no CI, and no root-level project file** — always scope commands to a specific solution or project.

| Path | Contents |
| --- | --- |
| `t1_frame/` | Main backend solution: ASP.NET Core + ABP (`Abp.*` 9.4.1 and `Volo.Abp.*` 8.3.4), plus many standalone sample/experiment projects (console, workers, Kafka / RabbitMQ / MassTransit, WinForms, WPF, MVC). |
| `t1_frame_wpf/` | Desktop UI scaffold built with **Avalonia + Prism + ReactiveUI** (despite the `wpf` name). |
| `samples/` | Zipped third-party reference repos (`Blog.zip`, `aspnet-core.zip`). Not built. |

Environment SDK is .NET 8.0.300 (`dotnet --version`); SDKs 2.1–8.0 are installed. Projects target `net6.0`, `net7.0`, or `net8.0` and are not unified.

## Commands

Run from the repo root, always naming the project/solution explicitly. Examples:

```powershell
# Build (working examples)
dotnet build t1_frame/src/webapi/t1_frame.webapi.abp/t1_frame.webapi.abp.csproj
dotnet build t1_frame/src/core/t1_frame.domain/t1_frame.domain.csproj
dotnet build t1_frame/test/t1_frame.test/t1_frame.test.csproj

# Test
dotnet test t1_frame/test/t1_frame.test/t1_frame.test.csproj
dotnet test t1_frame/test/t1_frame.test/t1_frame.test.csproj --filter "FullyQualifiedName~Should_Resolve_Controller"
```

`t1_frame.test` is the **only real test project** (NUnit 3 + `Microsoft.AspNetCore.TestHost`, targets `net7.0`). Its single fixture is marked `[Explicit]`, so a plain `dotnet test` discovers but does **not** execute it — pass a `--filter` to run explicit tests.

EF Core migrations (MySQL via Pomelo; design-time factory is `MytestContextFactory`):

```powershell
dotnet tool restore --tool-manifest t1_frame/test/t1_frame.webapi/.config/dotnet-tools.json
dotnet ef migrations add <Name> --project t1_frame/src/core/t1_frame.entityframeworkcore.migrations
```

Two local tool manifests pin different `dotnet-ef` versions (tests: 9.0.6, `webapi.abp`: 10.0.5) — restore the one you need.

## Known build breakages (pre-existing — do not "fix" unless asked)

- **`t1_frame/t1_frame.sln` does not build as a whole**: `error NU1605` — `t1_frame.entityframeworkcore.migrations` needs `Pomelo.EntityFrameworkCore.MySql >= 9.0.0` while `t1_frame.entityframeworkcore` / `t1_frame.webapi` pin `8.0.3`. Build individual projects instead. The ABP API (`t1_frame.webapi.abp`) builds clean (warnings only).
- **`t1_frame_wpf/t1_frame_wpf.sln` does not build**: it imports `build/avalonia.app.props` / `build/common.props`, but `build/` is in `.gitignore` and absent from the repo. Several csproj files also leave `<TargetFramework>` commented out or use the undefined `$(AvaloniaPackageVersion)`, producing `invalid framework identifier`. To build this app you must first recreate the missing `build/*.props` (define `TargetFramework`, `AvaloniaVersion`, `AvaloniaPackageVersion`, Prism packages). The self-contained `t1_frame_wpf/src/app/avalonia.app.extra/avalonia.app.extra.Desktop/` project *does* build (`net7.0`, Avalonia 11 preview via its own `Directory.Build.props`), but the `avalonia.app.extra.sln` fails because the iOS/Android/Web projects require uninstalled workloads.
- The build is **warning-heavy** (nullable `CS8618` / `CS8602`, etc.). Warnings are acceptable here; do not attempt a repo-wide cleanup.
- Many `Main` entry points are renamed (`Main1`) or deliberately dead, and files contain large commented-out experiment blocks. Treat these as intentional.

## Conventions — `t1_frame`

- Solution layout: `src/{common,core,application,webapi}` + `test/`. Project names are dotted C# namespaces, e.g. `t1_frame.application.abp`, `t1_frame.entityframeworkcore`, `t1_frame.response.abp`.
- **Two parallel stacks coexist**: classic `Abp.*` 9.4.1 (Castle) and modern `Volo.Abp.*` 8.3.4 (Autofac). Match whichever stack the file already uses; do not unify.
- ABP modules: `class XxxModule : AbpModule` with `[DependsOn(...)]`, `ConfigureServices(ServiceConfigurationContext)`, and application init in `OnApplicationInitialization`. App services: `class XxxAppService : ApplicationService, IXxxAppService` with `async virtual Task` methods and `ObjectMapper.Map<,>`.
- Namespace does not always match the folder; keep the namespace already present in the file.
- Types/methods are `PascalCase`; interfaces are `I`-prefixed.
- **DB entities in `t1_frame.domain` use `snake_case` public properties** (they map to MySQL columns), inherit `Entity<long>`, and use `[Table("t1_*")]` / data-annotation attributes. Preserve this style.
- 4-space indentation, Allman braces, one type per file. `Nullable` and `ImplicitUsings` are enabled, yet `using` directives are still written explicitly (often with unused ones).
- Chinese XML doc comments and Chinese log/exception strings are common. Keep comments in their existing language; do not translate.

## Conventions — `t1_frame_wpf`

- Avalonia 11 (`.axaml` + code-behind), Prism (`PrismApplication`, `IModule`, regions), ReactiveUI (`ViewModelBase : ReactiveObject`).
- MVVM by naming convention: `ConfigureViewModelLocator` resolves a View to `avalonia.model.ViewModels.{ViewName}ViewModel`. New view models belong in the `avalonia.model` project.
- Layout: views in `src/view/*` and `src/app/avalonia.app`; models/viewmodels in `src/common/avalonia.model`; helpers in `src/common/avalonia.utils`; API clients in `src/services/avalonia.openapi`.
- `test/test.props` is a sample Avalonia app, **not** a test project.

## External services / configuration

The ABP API wires Redis, RabbitMQ, Kafka, MongoDB, and MySQL in `t1_frame/src/webapi/t1_frame.webapi.abp/HostWebHostModule.cs`; defaults assume localhost. Redis is opt-in via `Redis:IsEnabled=true`. The design-time MySQL connection is hardcoded in `MytestContextFactory` (`localhost:3306`, `root/123456`, database `t1_frame_mysql`). `t1_frame/test/t1_frame.test/appsettings.json` contains a legacy hardcoded connection string/credential — leave it untouched.

## Git

Default branch is `main`. Commit messages are usually short and in Chinese.
