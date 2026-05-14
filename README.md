# QuickFin - Integrated Wealth Dashboard 💰

QuickFin is a modern, glass-morphism financial dashboard designed for personal wealth management. Track expenses, monitor budgets, analyze spending patterns, and make informed financial decisions with an intuitive, responsive interface.

## Motivation
Managing personal finances shouldn't be complicated. QuickFin provides a sleek, real-time overview of your financial status with smart insights and category-based analytics. Perfect for students and professionals who want to stay on top of their spending without the overhead of complex accounting software.

## Features
- **Real-Time Dashboard**: View total balance, monthly budget, and expenses at a glance.
- **Transaction Tracking**: Log and categorize expenses (Food, Tech, Study, Travel).
- **Budget Monitoring**: Visual pie chart showing spending vs. budget with color warnings.
- **Spending Analytics**: Analyze trends over time and category distribution.
- **Smart Insights**: AI-powered advice based on your spending patterns.
- **Dark/Light Theme**: Toggle between luxury dark mode and clean light mode.
- **Fully Responsive**: Beautiful experience on desktop, tablet, and mobile.

## Try Live
https://shaoying888.github.io/QuickFin-Dashboard-1259278462/index.html
## How to Build/Run
1. Clone the repository.
2. Open `index.html` in any modern web browser.
3. No build step required (Pure JavaScript/CSS).
4. Use "Load Demo" to populate sample transactions.

## F# Core Project
The live website is a client-only GitHub Pages app. To satisfy the F# course implementation requirement, this repository also includes a buildable .NET 8 F# project in `fsharp-src/`.

The F# project mirrors the same business logic used by the dashboard experience: account balances, transaction classification, budget usage, category totals, monthly trend data, and smart financial insight generation.

Build and run it with:
```bash
cd fsharp-src
dotnet build QuickFinCore.fsproj
dotnet run --project QuickFinCore.fsproj
```

## Project Requirement Coverage
| Requirement | Evidence in this repository |
|---|---|
| Public repository | Hosted on GitHub under `shaoying888/QuickFin-Dashboard-1259278462` |
| Web application | Client-only personal finance dashboard in `index.html`, `style.css`, and `script.js` |
| Online demo | GitHub Pages link listed in the Try Live section |
| README documentation | Motivation, features, run instructions, screenshot, live link, and F# notes are included |
| Screenshot | `screenshot.png` is referenced below |
| Automatic deployment | `.github/workflows/deploy.yml` deploys the site through GitHub Actions |
| F# source code | `fsharp-src/` contains a .NET 8 F# project with over 300 lines of domain and analytics logic |
| Consistent development history | Git history contains staged commits for UI, styling, analytics, deployment, documentation, and F# core logic |

## Screenshots
![Dashboard Overview](screenshot.png)

## License
MIT
