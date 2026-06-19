# QuickFin Feature Walkthrough

This document is for a quick review of the app. It focuses on what the app does, what the reviewer should click, and where the F# logic lives.

## What the App Does

QuickFin is a browser-based personal finance dashboard. It lets you load a demo month, add or remove transactions, adjust budget targets, compare review ranges, and inspect the computed results from several angles.

## Main User Flows

### 1. Start from a working month

Press `Load demo` to load sample income, housing, food, travel, health, subscriptions, and entertainment data.

### 2. Read the overview first

The hero area and project snapshot explain the app goals, the reviewer flow, and the F# basis of the implementation.

### 3. Switch the review range

Use `Current month`, `Last 3 months`, and `All time` to change what the summary, charts, account view, forecast, and export summary use.

### 4. Add or remove transactions

Use the transaction form and presets to add entries quickly. The ledger also supports search and filtering, and each transaction can be removed.

### 5. Test budget and planning ideas

Use the budget controls, what-if scenario panel, and savings target panel to demonstrate live F# calculations beyond a basic dashboard.

## Where F# Is Used

| Area | F# evidence |
|---|---|
| Domain model | `fsharp-src/Domain.fs` defines typed transactions, accounts, budgets, ranges, forecasts, scenarios, and savings plans. |
| Finance logic | `FinanceEngine` in `Domain.fs` calculates summaries, range slices, recurring spend, account snapshots, scenario outputs, and goal plans. |
| Browser UI | `fsharp-src/Client.fs` renders the WebSharper dashboard, manages state, and wires the forms and panels together. |
| Static app entry | `fsharp-src/Main.fs` and `fsharp-src/Main.html` serve the compiled browser application. |

## How To Review

1. Open the live demo.
2. Load the demo data.
3. Switch between the review ranges.
4. Add a transaction.
5. Change the budget.
6. Try the scenario panel and savings target panel.
7. Read the generated summary at the bottom.

## Build Output

```bash
cd fsharp-src
dotnet build QuickFinCore.fsproj -c Release
```

The deployable site is written to `fsharp-src/build/`.
