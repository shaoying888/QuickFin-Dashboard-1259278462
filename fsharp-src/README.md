# QuickFin WebSharper F# App

This folder contains the main QuickFin web application. WebSharper compiles the authored F# UI and finance logic to browser JavaScript, so the app runs as a static site on GitHub Pages.

## Files

- `Domain.fs` defines the typed model, demo data, finance calculations, review ranges, forecasts, scenario simulation, and goal planning.
- `Client.fs` renders the dashboard UI, interactive panels, filters, presets, and forms.
- `Main.fs` wires the WebSharper site entry point.
- `Main.html` provides the responsive template and CSS.

## Build

```bash
dotnet build QuickFinCore.fsproj -c Release
```

The build compiles F#, runs WebSharper, bundles the generated JavaScript, and writes the deployable site to `build/`.

## Preview

```bash
cd build
python -m http.server 8080
```

Open `http://localhost:8080/`.

## What To Look For

- Demo loading and onboarding.
- Review-range switching.
- Search and filtering in the ledger.
- Account overview and category mix.
- Forecast, recurring spend, scenario simulation, and savings target panels.
- Generated summary output.
