namespace QuickFinCore

open System
open WebSharper
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open WebSharper.UI.Notation

[<JavaScript>]
module Client =
    let private monthLabel month =
        match month with
        | 1 -> "Jan"
        | 2 -> "Feb"
        | 3 -> "Mar"
        | 4 -> "Apr"
        | 5 -> "May"
        | 6 -> "Jun"
        | 7 -> "Jul"
        | 8 -> "Aug"
        | 9 -> "Sep"
        | 10 -> "Oct"
        | 11 -> "Nov"
        | 12 -> "Dec"
        | _ -> "M" + string month

    let private statusText status =
        match status with
        | Healthy -> "Healthy"
        | Watch -> "Watch"
        | OverBudget -> "Over budget"

    let private statusClass status =
        match status with
        | Healthy -> "status-pill"
        | Watch -> "status-pill watch"
        | OverBudget -> "status-pill warning"

    let private insightClass severity =
        match severity with
        | Positive -> "insight"
        | Neutral -> "insight neutral"
        | Warning -> "insight warning"

    let private kindText kind =
        match kind with
        | Income -> "Income"
        | Expense -> "Expense"
        | Transfer -> "Transfer"

    let private dateValue (date: DateTime) =
        date.ToString("yyyy-MM-dd")

    let private parseDate (value: string) (fallback: DateTime) =
        match DateTime.TryParse(value) with
        | true, parsed -> parsed
        | false, _ -> fallback

    let private parsePositive (value: string) (fallback: float) =
        match Double.TryParse(value) with
        | true, parsed when parsed > 0.0 -> parsed
        | _ -> fallback

    let private percentWidth value =
        value |> Money.clamp 0.0 100.0 |> Money.round2

    let private metric label value caption =
        div [ attr.``class`` "metric panel" ] [
            small [] [ text label ]
            strong [] [ text value ]
            span [] [ text caption ]
        ]

    let private field labelText fieldDoc =
        div [ attr.``class`` "field" ] [
            label [] [ text labelText ]
            fieldDoc
        ]

    let private wideField labelText fieldDoc =
        div [ attr.``class`` "field wide" ] [
            label [] [ text labelText ]
            fieldDoc
        ]

    let private codePill value =
        span [ attr.``class`` "code-pill" ] [ text value ]

    let private guideItem title body =
        div [ attr.``class`` "guide-item" ] [
            strong [] [ text title ]
            p [] [ text body ]
        ]

    let private implementationItem title body files =
        div [ attr.``class`` "implementation-item" ] [
            div [] [
                strong [] [ text title ]
                p [] [ text body ]
            ]
            div [ attr.``class`` "code-row" ] [
                for file in files do
                    codePill file
            ]
        ]

    let private workflowStep number title body =
        div [ attr.``class`` "step-card" ] [
            span [] [ text number ]
            strong [] [ text title ]
            p [] [ text body ]
        ]

    let private joinLines lines =
        match lines with
        | [] -> ""
        | first :: rest -> rest |> List.fold (fun acc line -> acc + "\n" + line) first

    let private renderSummary model =
        let summary = FinanceEngine.summarize model
        div [ attr.``class`` "summary-grid" ] [
            metric "Balance" (Money.format summary.Balance) "Across checking and savings"
            metric "Income" (Money.format summary.Income) ("Target " + Money.format model.Budget.IncomeTarget)
            metric "Expenses" (Money.format summary.Expenses) ("Budget used " + Money.formatPercent summary.BudgetUsed)
            metric "Savings" (Money.format summary.Savings) ("Rate " + Money.formatPercent summary.SavingsRate)
        ]

    let private renderCategoryBreakdown model =
        let items = FinanceEngine.categoryBreakdown model.Transactions
        if items.IsEmpty then
            div [ attr.``class`` "insight neutral" ] [
                strong [] [ text "No expense categories yet" ]
                p [] [ text "Add a transaction to see where money is going." ]
            ]
        else
            div [ attr.``class`` "bars" ] [
                for item in items do
                    div [] [
                        div [ attr.``class`` "bar-label" ] [
                            span [] [ text item.CategoryName ]
                            span [] [ text (Money.format item.Total + " / " + Money.formatPercent item.Share) ]
                        ]
                        div [ attr.``class`` "bar-track" ] [
                            div [ attr.``class`` "bar-fill"; attr.style ("--w:" + string (percentWidth item.Share) + "%") ] []
                        ]
                    ]
            ]

    let private renderInsights model =
        let insights = FinanceEngine.generateInsights model
        div [ attr.``class`` "insight-list" ] [
            for insight in insights do
                div [ attr.``class`` (insightClass insight.Severity) ] [
                    strong [] [ text insight.Title ]
                    p [] [ text insight.Message ]
                ]
        ]

    let private renderTimeline model =
        let points = FinanceEngine.monthlyTrend model.Transactions
        if points.IsEmpty then
            div [ attr.``class`` "insight neutral" ] [
                strong [] [ text "No trend data" ]
                p [] [ text "Transactions will build the monthly trend automatically." ]
            ]
        else
            div [ attr.``class`` "timeline" ] [
                for point in points do
                    let incomeWidth =
                        if point.Income = 0.0 then 0.0
                        else Money.percent point.Savings point.Income |> percentWidth

                    div [ attr.``class`` "month-row" ] [
                        span [] [ text (monthLabel point.Month + " " + string point.Year) ]
                        div [ attr.``class`` "bar-track" ] [
                            div [ attr.``class`` "bar-fill"; attr.style ("--w:" + string incomeWidth + "%") ] []
                        ]
                        span [] [ text (Money.format point.Savings) ]
                    ]
            ]

    let private renderTransactions (state: Var<DashboardModel>) model =
        let ordered =
            model.Transactions
            |> List.sortByDescending (fun t -> t.PostedOn, t.Id)

        div [ attr.``class`` "transaction-list" ] [
            for item in ordered do
                let amountClass =
                    if Transaction.isIncome item then "amount income"
                    elif Transaction.isExpense item then "amount expense"
                    else "amount"

                div [ attr.``class`` "transaction" ] [
                    div [] [
                        h4 [] [ text item.Description ]
                        p [] [
                            text (dateValue item.PostedOn + " - " + kindText item.Kind + " - " + Category.displayName item.Category + " - " + item.Account)
                        ]
                        button [
                            attr.``class`` "link-button"
                            attr.title "Remove transaction"
                            on.click (fun _ _ ->
                                let updated = FinanceEngine.removeTransaction item.Id state.Value
                                state := updated)
                        ] [ text "Remove" ]
                    ]
                    div [ attr.``class`` amountClass ] [
                        text (Money.formatSigned item.Kind item.Amount)
                    ]
                ]
        ]

    let private hero (state: Var<DashboardModel>) =
        Doc.BindView (fun model ->
            let summary = FinanceEngine.summarize model
            div [ attr.``class`` "hero panel" ] [
                div [] [
                    span [ attr.``class`` (statusClass summary.Status) ] [ text (statusText summary.Status) ]
                    h2 [] [ text "Plan smarter with a live finance dashboard." ]
                    p [] [
                        text "QuickFin helps turn transactions into budget decisions with live totals, category breakdowns, monthly trends, and practical spending insights."
                    ]
                    div [ attr.``class`` "source-tags" ] [
                        codePill "Live budget"
                        codePill "Category insights"
                        codePill "Static web app"
                    ]
                    div [ attr.``class`` "hero-actions" ] [
                        button [
                            attr.``class`` "button"
                            on.click (fun _ _ -> state := FinanceEngine.demoModel)
                        ] [ text "Load demo" ]
                        button [
                            attr.``class`` "button secondary"
                            on.click (fun _ _ ->
                                let cleared = { state.Value with Transactions = [] }
                                state := cleared)
                        ] [ text "Clear transactions" ]
                    ]
                ]
                div [ attr.``class`` "mini-board" ] [
                    div [ attr.``class`` "mini-board-head" ] [
                        strong [] [ text "Savings outlook" ]
                        span [] [ text (Money.formatPercent summary.SavingsRate + " saved") ]
                    ]
                    div [ attr.``class`` "spark-bars" ] [
                        span [ attr.style "height:42%" ] []
                        span [ attr.style "height:64%" ] []
                        span [ attr.style ("height:" + string (percentWidth summary.BudgetUsed) + "%") ] []
                        span [ attr.style "height:54%" ] []
                        span [ attr.style ("height:" + string (percentWidth summary.SavingsRate) + "%") ] []
                        span [ attr.style "height:72%" ] []
                    ]
                ]
            ]) state.View

    let private workflowPanel () =
        div [ attr.``class`` "workflow-steps" ] [
            workflowStep "01" "Load a working month" "Start from demo transactions that cover income, housing, food, study, travel, and subscriptions."
            workflowStep "02" "Change the plan" "Add transactions or edit budget targets and every chart updates from the same finance model."
            workflowStep "03" "Read the result" "Review budget health, trend, category share, recurring expenses, and the generated finance summary."
        ]

    let private featureGuide () =
        div [ attr.``class`` "panel pad" ] [
            div [ attr.``class`` "section-title" ] [
                h3 [] [ text "Feature guide" ]
                span [] [ text "What to try" ]
            ]
            div [ attr.``class`` "guide-list" ] [
                guideItem "Live planning" "Use the form to add a transaction and watch the balance, spending, savings, and category bars update immediately."
                guideItem "Budget scenario testing" "Change income, expense limit, or savings goal to see whether the month becomes healthy, watch, or over budget."
                guideItem "Readable insights" "The app turns raw transactions into plain-language notes about the biggest category, cash flow, and recurring spending."
            ]
        ]

    let private fsharpEvidence () =
        div [ attr.``class`` "panel pad" ] [
            div [ attr.``class`` "section-title" ] [
                h3 [] [ text "Implementation notes" ]
                span [] [ text "Built with F#" ]
            ]
            div [ attr.``class`` "implementation-list" ] [
                implementationItem
                    "Typed finance model"
                    "Accounts, transactions, budgets, categories, summaries, and insights are F# records and discriminated unions."
                    [ "Domain.fs" ]
                implementationItem
                    "Browser UI"
                    "The dashboard uses WebSharper UI, reactive Vars, BindView, and F# event handlers instead of a hand-written JavaScript app."
                    [ "Client.fs" ]
                implementationItem
                    "Static web deployment"
                    "The build compiles F# to JavaScript, bundles it with esbuild, and publishes the generated site to GitHub Pages."
                    [ "Main.fs"; "wsconfig.json" ]
            ]
        ]

    let private reportPanel (state: Var<DashboardModel>) =
        Doc.BindView (fun model ->
            div [ attr.``class`` "panel pad" ] [
                div [ attr.``class`` "section-title" ] [
                    h3 [] [ text "Generated summary" ]
                    span [] [ text "Export view" ]
                ]
                pre [ attr.``class`` "report-box" ] [
                    text (FinanceEngine.exportSummaryLines model |> joinLines)
                ]
            ]) state.View

    let private transactionForm (state: Var<DashboardModel>) =
        let description = Var.Create "Coffee with project team"
        let amount = Var.Create "8.50"
        let kind = Var.Create Expense
        let category = Var.Create Food
        let account = Var.Create "Checking"
        let date = Var.Create (dateValue (DateTime(2026, 6, 18)))

        div [ attr.``class`` "panel pad" ] [
            div [ attr.``class`` "section-title" ] [
                h3 [] [ text "Add transaction" ]
                span [] [ text "State updates live" ]
            ]
            div [ attr.``class`` "form-grid" ] [
                wideField "Description" (Doc.InputType.Text [ attr.placeholder "Description" ] description)
                field "Amount" (Doc.InputType.Text [ attr.placeholder "0.00" ] amount)
                field "Date" (Doc.InputType.Date [] date)
                field "Kind" (
                    Doc.InputType.Select [ attr.title "Transaction kind" ] kindText [ Income; Expense; Transfer ] kind
                )
                field "Category" (
                    Doc.InputType.Select [ attr.title "Category" ] Category.displayName Category.options category
                )
                field "Account" (Doc.InputType.Text [ attr.placeholder "Checking" ] account)
            ]
            div [ attr.``class`` "hero-actions" ] [
                button [
                    attr.``class`` "button"
                    on.click (fun _ _ ->
                        let model = state.Value
                        let nextId =
                            model.Transactions
                            |> List.map _.Id
                            |> function
                                | [] -> 1
                                | ids -> (List.max ids) + 1

                        let tx =
                            { Id = nextId
                              PostedOn = parseDate date.Value (DateTime(2026, 6, 18))
                              Description =
                                if String.IsNullOrWhiteSpace(description.Value) then "Untitled transaction"
                                else description.Value.Trim()
                              Amount = parsePositive amount.Value 0.0
                              Kind = kind.Value
                              Category = category.Value
                              Account =
                                if String.IsNullOrWhiteSpace(account.Value) then "Checking"
                                else account.Value.Trim()
                              Note = None }

                        state := { model with Transactions = tx :: model.Transactions }
                        description := ""
                        amount := "0.00")
                ] [ text "Add transaction" ]
            ]
        ]

    let private budgetPanel (state: Var<DashboardModel>) =
        let current = state.Value.Budget
        let incomeTarget = Var.Create (string current.IncomeTarget)
        let expenseLimit = Var.Create (string current.ExpenseLimit)
        let savingsGoal = Var.Create (string current.SavingsGoal)

        div [ attr.``class`` "panel pad" ] [
            div [ attr.``class`` "section-title" ] [
                h3 [] [ text "Budget controls" ]
                span [] [ text "All views update together" ]
            ]
            div [ attr.``class`` "form-grid" ] [
                field "Income target" (Doc.InputType.Text [ attr.placeholder "5000" ] incomeTarget)
                field "Expense limit" (Doc.InputType.Text [ attr.placeholder "3200" ] expenseLimit)
                wideField "Savings goal" (Doc.InputType.Text [ attr.placeholder "900" ] savingsGoal)
            ]
            div [ attr.``class`` "hero-actions" ] [
                button [
                    attr.``class`` "button"
                    on.click (fun _ _ ->
                        let model = state.Value
                        let updated =
                            { model.Budget with
                                IncomeTarget = parsePositive incomeTarget.Value model.Budget.IncomeTarget
                                ExpenseLimit = parsePositive expenseLimit.Value model.Budget.ExpenseLimit
                                SavingsGoal = parsePositive savingsGoal.Value model.Budget.SavingsGoal }
                        state := { model with Budget = updated })
                ] [ text "Apply budget" ]
            ]
        ]

    let private analytics state =
        Doc.BindView (fun model ->
            div [] [
                renderSummary model
                div [ attr.``class`` "grid-two" ] [
                    div [ attr.``class`` "panel pad" ] [
                        div [ attr.``class`` "section-title" ] [
                            h3 [] [ text "Category distribution" ]
                            span [] [ text "Expense share" ]
                        ]
                        renderCategoryBreakdown model
                    ]
                    div [ attr.``class`` "panel pad" ] [
                        div [ attr.``class`` "section-title" ] [
                            h3 [] [ text "Monthly trend" ]
                            span [] [ text "Savings by month" ]
                        ]
                        renderTimeline model
                    ]
                ]
                div [ attr.``class`` "grid-two" ] [
                    div [ attr.``class`` "panel pad" ] [
                        div [ attr.``class`` "section-title" ] [
                            h3 [] [ text "Smart insights" ]
                            span [] [ text "Rule based" ]
                        ]
                        renderInsights model
                    ]
                    div [ attr.``class`` "panel pad" ] [
                        div [ attr.``class`` "section-title" ] [
                            h3 [] [ text "Transaction ledger" ]
                            span [] [ text (string model.Transactions.Length + " records") ]
                        ]
                        renderTransactions state model
                    ]
                ]
            ]) state.View

    let Main () =
        let state = Var.Create FinanceEngine.demoModel

        div [ attr.``class`` "app-shell" ] [
            header [ attr.``class`` "topbar" ] [
                div [ attr.``class`` "topbar-inner" ] [
                    div [ attr.``class`` "brand" ] [
                        div [ attr.``class`` "brand-mark" ] [ text "QF" ]
                        div [] [
                            h1 [] [ text "QuickFin" ]
                            p [] [ text "Personal finance planning dashboard" ]
                        ]
                    ]
                    div [ attr.``class`` "badge-row" ] [
                        span [ attr.``class`` "badge" ] [ text "Live analytics" ]
                        span [ attr.``class`` "badge" ] [ text "Budget planner" ]
                        span [ attr.``class`` "badge" ] [ text "GitHub Pages ready" ]
                    ]
                ]
            ]
            div [ attr.``class`` "layout" ] [
                div [] [
                    hero state
                    workflowPanel()
                    analytics state
                ]
                div [ attr.``class`` "side-stack" ] [
                    featureGuide()
                    transactionForm state
                    budgetPanel state
                    fsharpEvidence()
                    reportPanel state
                ]
            ]
        ]
