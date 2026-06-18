# QuickFin - WebSharper F# Finance Dashboard

QuickFin is a client-only personal finance web application built with F# and WebSharper. The browser UI, reactive state, transaction model, budget calculations, charts, and financial insights are authored in F# under `fsharp-src/` and compiled to JavaScript for GitHub Pages.

## Motivation

Personal finance tools can be either too simple to be useful or too heavy for everyday planning. QuickFin focuses on a student-friendly workflow: load sample data, add expenses or income, adjust the monthly budget, and immediately see how the dashboard changes.

The project was rebuilt as a WebSharper F# web app so the submitted application demonstrates F# web development directly instead of only using F# for a separate console or backend-style component.

## Features

- Reactive F# dashboard state with `Var<DashboardModel>` and live recomputation.
- Transaction entry with date, account, kind, category, and amount.
- Budget controls for income target, expense limit, and savings goal.
- Summary cards for balance, income, expenses, savings, savings rate, and budget usage.
- Category distribution bars generated from expense data.
- Monthly savings trend calculated from transaction history.
- Smart insights for budget health, savings rate, largest category, largest expense, and recurring expenses.
- Responsive WebSharper UI compiled to a static site for GitHub Pages.

## Try Live

https://shaoying888.github.io/QuickFin-Dashboard-1259278462/

## Build and Run

Requirements:

- .NET SDK 8.0 or newer
- Node.js 20 or newer

Build the F# WebSharper site:

```bash
cd fsharp-src
dotnet build QuickFinCore.fsproj -c Release
```

The static site is generated in:

```text
fsharp-src/build/
```

To preview locally after building:

```bash
cd fsharp-src/build
python -m http.server 8080
```

Then open:

```text
http://localhost:8080/
```

## F# Source Structure

| File | Purpose |
|---|---|
| `fsharp-src/Domain.fs` | Domain types, demo data, finance calculations, category analytics, trend generation, and insight rules |
| `fsharp-src/Client.fs` | Browser-side WebSharper UI, reactive state, forms, dashboard rendering, and user interactions |
| `fsharp-src/Main.fs` | WebSharper Sitelet and HTML application entry point |
| `fsharp-src/Main.html` | HTML/CSS template used by WebSharper |
| `fsharp-src/esbuild.config.mjs` | Bundles WebSharper output into the static `all.js` used by GitHub Pages |

## Requirement Coverage

| Requirement | Evidence |
|---|---|
| Public repository | GitHub repository under `shaoying888/QuickFin-Dashboard-1259278462` |
| Web application | WebSharper F# static web app in `fsharp-src/` |
| F# implementation | `Domain.fs`, `Client.fs`, and `Main.fs` contain the authored F# application logic and UI |
| Online demo | GitHub Pages link in the Try Live section |
| Automatic deployment | `.github/workflows/deploy.yml` builds the F# WebSharper app and deploys `fsharp-src/build` |
| README | Project motivation, features, build/run instructions, screenshot, and live link are documented here |
| Screenshot | Included below as `screenshot.png` |

## Screenshot

![Dashboard Overview](screenshot.png)

## License

MIT
