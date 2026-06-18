# QuickFin WebSharper F# App

This folder contains the main QuickFin web application. It is not a separate backend or console helper: WebSharper compiles the authored F# UI and finance logic to browser JavaScript and emits a static site that can run on GitHub Pages.

## Files

- `Domain.fs` defines accounts, transactions, budgets, summaries, trends, and insight generation.
- `Client.fs` renders the interactive browser dashboard using WebSharper UI.
- `Main.fs` wires the WebSharper HTML application and route.
- `Main.html` provides the responsive page template and CSS.
- `esbuild.config.mjs` bundles the generated WebSharper JavaScript into `build/Scripts/WebSharper/all.js`.

## Build

```bash
dotnet build QuickFinCore.fsproj -c Release
```

The build installs the local npm dependency, compiles F# with WebSharper, runs esbuild, and writes the deployable static site to `build/`.

## Preview

```bash
cd build
python -m http.server 8080
```

Open `http://localhost:8080/`.

On first load, a short onboarding panel points reviewers to the demo data and the main review flow.
