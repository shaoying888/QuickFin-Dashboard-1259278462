# QuickFin - Personal Finance Planning Dashboard

QuickFin is a WebSharper F# web app for planning a monthly budget, reviewing spending, and showing that the core UI and finance logic are written in F# rather than as a thin JavaScript shell.

## Why This Project

The project is aimed at a very ordinary problem: keeping track of income, expenses, targets, and recurring costs without opening a heavy finance tool.

## Main Features

- First-use guide with demo entry.
- Review-range switcher for current month, last 3 months, and all time.
- Ledger search and filters.
- Transaction presets and manual entry form.
- Budget controls with live status changes.
- Account overview, category mix, timeline, forecast, recurring spend, what-if scenarios, and savings target planning.
- Generated summary panel for quick export-style review.
- F# implementation evidence shown in the app and documented in the repo.

## Live Demo

https://shaoying888.github.io/QuickFin-Dashboard-1259278462/

## Build and Run

Requirements:

- .NET SDK 8.0 or newer
- Node.js 20 or newer

Build:

```bash
cd fsharp-src
dotnet build QuickFinCore.fsproj -c Release
```

Preview locally:

```bash
cd fsharp-src/build
python -m http.server 8080
```

Open:

```text
http://127.0.0.1:8080/
```

## Screenshot

![Dashboard Overview](screenshot.png)

## F# Source Map

| File | Purpose |
|---|---|
| `fsharp-src/Domain.fs` | Typed domain model, demo data, range summaries, forecasts, scenario simulation, and goal planning |
| `fsharp-src/Client.fs` | WebSharper UI, state handling, dashboard rendering, filters, and interactive panels |
| `fsharp-src/Main.fs` | WebSharper site entry point |
| `fsharp-src/Main.html` | Responsive layout and styling |

## Notes

This is a static GitHub Pages deployment. The app is built from F# source, then bundled for browser delivery.
