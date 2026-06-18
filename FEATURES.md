# QuickFin Feature Walkthrough

This document is written for a quick project review. It explains what the application does, how to try the main flows, and where the F# implementation appears in the code.

## What the App Does

QuickFin is a browser-based personal finance dashboard for planning a monthly budget. A user can load a realistic demo month, add new transactions, adjust budget targets, and immediately see how the financial picture changes.

The application focuses on four practical questions:

- How much money is available after income and expenses?
- Which categories are driving spending?
- Is the monthly budget healthy or already over limit?
- What does the transaction history suggest about recurring or unusually large expenses?

## Main User Flows

### 1. Start from a working month

The `Load demo` action restores a sample data set with accounts, income, rent, groceries, travel, study expenses, health, entertainment, and subscription spending. This gives the reviewer a complete state to inspect without entering data first.

### 2. Add a transaction

Use the `Add transaction` panel to enter a description, amount, date, transaction kind, category, and account. After pressing `Add transaction`, the dashboard updates the balance, totals, budget status, charts, insights, and ledger without a page reload.

### 3. Test a budget scenario

Use `Budget controls` to edit the income target, expense limit, and savings goal. The budget status changes between healthy, watch, and over budget according to the F# calculation rules.

### 4. Read the generated analysis

The dashboard includes category distribution, monthly trend, smart insights, and a generated summary panel. The generated summary is useful for checking that the finance engine is producing coherent text output from the same transaction model used by the UI.

## Where F# Is Used

QuickFin is not a JavaScript-first app with a small F# helper. The main web application is authored in F# and compiled to browser JavaScript by WebSharper.

| Area | F# evidence |
|---|---|
| Domain model | `fsharp-src/Domain.fs` defines transactions, accounts, budgets, categories, summaries, insights, and demo data as F# records and discriminated unions. |
| Finance logic | `FinanceEngine` in `Domain.fs` calculates income, expenses, savings rate, budget usage, category breakdowns, monthly trends, largest expenses, recurring candidates, and summary text. |
| Browser UI | `fsharp-src/Client.fs` renders the dashboard using WebSharper UI elements, `Var<DashboardModel>` state, `Doc.BindView`, and F# event handlers. |
| Web app entry | `fsharp-src/Main.fs` defines the WebSharper HTML application and routes the generated page. |
| Static deployment | `wsconfig.json`, `package.json`, and `esbuild.config.mjs` compile and bundle the F# WebSharper output into `fsharp-src/build/` for GitHub Pages. |

## How to Review the F# Web App

1. Open the live GitHub Pages link from `README.md`.
2. Press `Load demo`.
3. Add a transaction such as `Bus pass`, `42.50`, `Expense`, `Travel`.
4. Change the expense limit in `Budget controls`.
5. Check that the summary cards, category chart, trend chart, smart insights, ledger, and generated summary all update together.
6. Compare the behavior with `Domain.fs` and `Client.fs` to see the F# model, calculations, and browser UI implementation.

## Build Output

The release build command is:

```bash
cd fsharp-src
dotnet build QuickFinCore.fsproj -c Release
```

The deployable static site is generated at:

```text
fsharp-src/build/
```

GitHub Actions runs the same build and deploys that generated directory to GitHub Pages.
