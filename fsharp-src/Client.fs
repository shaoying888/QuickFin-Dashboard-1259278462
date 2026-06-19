namespace QuickFinCore

open System
open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open WebSharper.UI.Notation

[<JavaScript>]
module Client =
    type private FilterState =
        { ReviewRange: Var<ReviewRange>
          Search: Var<string>
          Category: Var<string>
          Kind: Var<string> }

    type private ScenarioDraft =
        { ExtraIncome: Var<string>
          ExpenseCut: Var<string>
          ExtraCost: Var<string> }

    type private GoalDraft =
        { GoalAmount: Var<string>
          HorizonMonths: Var<string> }

    type private DraftState =
        { Description: Var<string>
          Amount: Var<string>
          Kind: Var<TransactionKind>
          Category: Var<Category>
          Account: Var<string>
          Date: Var<string>
          Note: Var<string> }

    type private TransactionPreset =
        { Label: string
          Description: string
          Amount: string
          Kind: TransactionKind
          Category: Category
          Account: string
          Date: string
          Note: string }

    type private StatItem =
        { Label: string
          Value: string
          Note: string }

    type private AppPage =
        | Overview
        | Transactions
        | Planning
        | Notes

    module private AppPage =
        let displayName page =
            match page with
            | Overview -> "Overview"
            | Transactions -> "Transactions"
            | Planning -> "Planning"
            | Notes -> "Notes"

    let private onboardingStorageKey = "quickfin.onboarding.seen"

    let private presets =
        [ { Label = "Salary"
            Description = "Salary deposit"
            Amount = "5050.00"
            Kind = Income
            Category = Other "Income"
            Account = "Checking"
            Date = "2026-06-01"
            Note = "Monthly paycheck" }
          { Label = "Rent"
            Description = "Rent"
            Amount = "1100.00"
            Kind = Expense
            Category = Housing
            Account = "Checking"
            Date = "2026-06-02"
            Note = "Recurring housing payment" }
          { Label = "Groceries"
            Description = "Groceries"
            Amount = "128.40"
            Kind = Expense
            Category = Food
            Account = "Checking"
            Date = "2026-06-09"
            Note = "Weekly shopping" }
          { Label = "Transit"
            Description = "Train ticket"
            Amount = "45.00"
            Kind = Expense
            Category = Travel
            Account = "Checking"
            Date = "2026-06-07"
            Note = "Weekend transit" }
          { Label = "Cloud tools"
            Description = "Cloud tools"
            Amount = "19.00"
            Kind = Expense
            Category = Subscriptions
            Account = "Checking"
            Date = "2026-06-11"
            Note = "Monthly software" } ]

    let private categoryFilterOptions =
        "All categories"
        :: (Category.options |> List.map Category.displayName)

    let private kindFilterOptions =
        [ "All kinds"
          "Income"
          "Expense"
          "Transfer" ]

    let private statItem label value note =
        { Label = label
          Value = value
          Note = note }

    let private onboardingSeen () =
        JS.Inline<bool>(
            """
            try {
                return window.localStorage.getItem($0) === "1";
            } catch (e) {
                return false;
            }
            """,
            onboardingStorageKey)

    let private markOnboardingSeen () =
        JS.Inline<unit>(
            """
            try {
                window.localStorage.setItem($0, "1");
            } catch (e) {}
            """,
            onboardingStorageKey)

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

    let private parseNonNegative (value: string) (fallback: float) =
        match Double.TryParse(value) with
        | true, parsed when parsed >= 0.0 -> parsed
        | _ -> fallback

    let private parsePositiveInt (value: string) (fallback: int) =
        match Int32.TryParse(value) with
        | true, parsed when parsed > 0 -> parsed
        | _ -> fallback

    let private createFilters () =
        { ReviewRange = Var.Create CurrentMonth
          Search = Var.Create ""
          Category = Var.Create "All categories"
          Kind = Var.Create "All kinds" }

    let private createScenarioDraft () =
        { ExtraIncome = Var.Create "0.00"
          ExpenseCut = Var.Create "0.00"
          ExtraCost = Var.Create "0.00" }

    let private createGoalDraft () =
        { GoalAmount = Var.Create "900.00"
          HorizonMonths = Var.Create "6" }

    let private createDraft () =
        { Description = Var.Create ""
          Amount = Var.Create "0.00"
          Kind = Var.Create Expense
          Category = Var.Create Food
          Account = Var.Create "Checking"
          Date = Var.Create (dateValue (DateTime(2026, 6, 18)))
          Note = Var.Create "" }

    let private resetDraft (draft: DraftState) =
        draft.Description := ""
        draft.Amount := "0.00"
        draft.Kind := Expense
        draft.Category := Food
        draft.Account := "Checking"
        draft.Date := dateValue (DateTime(2026, 6, 18))
        draft.Note := ""

    let private applyPreset (draft: DraftState) (preset: TransactionPreset) =
        draft.Description := preset.Description
        draft.Amount := preset.Amount
        draft.Kind := preset.Kind
        draft.Category := preset.Category
        draft.Account := preset.Account
        draft.Date := preset.Date
        draft.Note := preset.Note

    let private nextTransactionId (model: DashboardModel) =
        model.Transactions
        |> List.map _.Id
        |> function
            | [] -> 1
            | ids -> (List.max ids) + 1

    let private addDraftTransaction (state: Var<DashboardModel>) (draft: DraftState) =
        let model = state.Value
        let tx =
            { Id = nextTransactionId model
              PostedOn = parseDate draft.Date.Value (DateTime(2026, 6, 18))
              Description =
                if String.IsNullOrWhiteSpace draft.Description.Value then "Untitled transaction"
                else draft.Description.Value.Trim()
              Amount = parsePositive draft.Amount.Value 0.0
              Kind = draft.Kind.Value
              Category = draft.Category.Value
              Account =
                if String.IsNullOrWhiteSpace draft.Account.Value then "Checking"
                else draft.Account.Value.Trim()
              Note =
                if String.IsNullOrWhiteSpace draft.Note.Value then None
                else Some(draft.Note.Value.Trim()) }

        state := { model with Transactions = tx :: model.Transactions }
        draft.Description := ""
        draft.Amount := "0.00"
        draft.Note := ""

    let private containsText (query: string) (value: string) =
        if String.IsNullOrWhiteSpace query then true
        else
            let needle = query.Trim().ToLower()
            value.ToLower().IndexOf(needle) >= 0

    let private matchesLedgerFilters (search: string) (category: string) (kind: string) (transaction: Transaction) =
        let noteText = transaction.Note |> Option.defaultValue ""
        let kindMatch =
            kind = "All kinds" || kindText transaction.Kind = kind
        let categoryMatch =
            category = "All categories" || Category.displayName transaction.Category = category
        let textMatch =
            let haystack =
                String.concat " "
                    [ transaction.Description
                      transaction.Account
                      Category.displayName transaction.Category
                      noteText
                      Money.format (abs transaction.Amount) ]

            containsText search haystack

        kindMatch && categoryMatch && textMatch

    let private monthYearLabel year month =
        monthLabel month + " " + string year

    let private currentMonthComparison (transactions: Transaction list) =
        match FinanceEngine.monthlyTrend transactions |> List.rev with
        | current :: previous :: _ ->
            let incomeDelta, expenseDelta, savingsDelta = FinanceEngine.monthComparison current previous
            Some (current, previous, incomeDelta, expenseDelta, savingsDelta)
        | _ -> None

    let private formatDelta value =
        (if value >= 0.0 then "+" else "-") + Money.format (abs value)

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

    let private onboardingStep label title body =
        div [ attr.``class`` "onboarding-step" ] [
            span [] [ text label ]
            div [] [
                strong [] [ text title ]
                p [] [ text body ]
            ]
        ]

    let private demoFact label value caption =
        div [ attr.``class`` "demo-fact" ] [
            span [] [ text label ]
            strong [] [ text value ]
            p [] [ text caption ]
        ]

    let private joinLines lines =
        match lines with
        | [] -> ""
        | first :: rest -> rest |> List.fold (fun acc line -> acc + "\n" + line) first

    let private renderStatCard item =
        div [ attr.``class`` "stat-card" ] [
            span [] [ text item.Label ]
            strong [] [ text item.Value ]
            p [] [ text item.Note ]
        ]

    let private pageTab (activePage: Var<AppPage>) page =
        Doc.BindView (fun current ->
            button [
                attr.``class`` (if current = page then "nav-tab active" else "nav-tab")
                attr.title (AppPage.displayName page)
                on.click (fun _ _ -> activePage := page)
            ] [ text (AppPage.displayName page) ]
        ) activePage.View

    let private heroStats (state: Var<DashboardModel>) =
        Doc.BindView (fun model ->
            let summary = FinanceEngine.summarize model
            let reviewTransactions = FinanceEngine.transactionsForRange CurrentMonth model
            let accounts = FinanceEngine.accountSnapshots reviewTransactions model
            let topAccount = accounts |> List.tryHead
            let stats =
                [ statItem "Transactions" (string model.Transactions.Length) "Tracked across the demo month"
                  statItem "Budget used" (Money.formatPercent summary.BudgetUsed) "Against the monthly limit"
                  statItem "Average savings" (Money.format (FinanceEngine.averageMonthlySavings model.Transactions)) "Across all months"
                  statItem "Top account" (
                      match topAccount with
                      | Some account -> account.AccountName
                      | None -> "No account"
                  ) (
                      match topAccount with
                      | Some account -> Money.format account.Balance
                      | None -> "Add sample data"
                  ) ]

            div [ attr.``class`` "hero-stats" ] [
                for item in stats do
                    renderStatCard item
            ]
        ) state.View

    let private monthComparisonPanel (state: Var<DashboardModel>) =
        Doc.BindView (fun model ->
            let comparison = currentMonthComparison model.Transactions
            div [ attr.``class`` "panel pad" ] [
                div [ attr.``class`` "section-title" ] [
                    h3 [] [ text "Month comparison" ]
                    span [] [ text "Latest two months" ]
                ]
                match comparison with
                | Some (current, previous, incomeDelta, expenseDelta, savingsDelta) ->
                    div [ attr.``class`` "comparison-grid" ] [
                        metric "Income delta" (formatDelta incomeDelta) (monthYearLabel current.Year current.Month + " vs " + monthYearLabel previous.Year previous.Month)
                        metric "Expense delta" (formatDelta expenseDelta) "Spending change"
                        metric "Savings delta" (formatDelta savingsDelta) "Cash flow movement"
                    ]
                | None ->
                    div [ attr.``class`` "ledger-empty" ] [
                        strong [] [ text "Need at least two months" ]
                        p [] [ text "Load the demo month to see month-over-month movement." ]
                    ]
            ]
        ) state.View

    let private renderSummaryPanel (state: Var<DashboardModel>) (filters: FilterState) =
        Doc.BindView (fun model ->
            Doc.BindView (fun reviewRange ->
                let reviewSummary = FinanceEngine.summarizeForRange reviewRange model
                let reviewTransactions = FinanceEngine.transactionsForRange reviewRange model
                let forecast = FinanceEngine.projectedSnapshot reviewRange model

                div [ attr.``class`` "panel pad summary-panel" ] [
                    div [ attr.``class`` "section-title" ] [
                        h3 [] [ text (ReviewRange.displayName reviewRange + " snapshot") ]
                        span [] [ text (string reviewTransactions.Length + " transactions") ]
                    ]
                    div [ attr.``class`` "summary-grid" ] [
                        metric "Balance" (Money.format reviewSummary.Balance) "Across all accounts"
                        metric "Income" (Money.format reviewSummary.Income) "For the selected range"
                        metric "Expenses" (Money.format reviewSummary.Expenses) ("Budget used " + Money.formatPercent reviewSummary.BudgetUsed)
                        metric "Savings" (Money.format reviewSummary.Savings) ("Rate " + Money.formatPercent reviewSummary.SavingsRate)
                        metric "Projected balance" (Money.format forecast.ProjectedBalance) "If this pattern repeats"
                        metric "Recurring baseline" (Money.format forecast.RecurringBaseline) "Top repeating spend"
                    ]
                ]
            ) filters.ReviewRange.View
        ) state.View

    let private renderAccountPanel (state: Var<DashboardModel>) (filters: FilterState) =
        Doc.BindView (fun model ->
            Doc.BindView (fun reviewRange ->
                let reviewTransactions = FinanceEngine.transactionsForRange reviewRange model
                let accounts = FinanceEngine.accountSnapshots reviewTransactions model

                div [ attr.``class`` "panel pad" ] [
                    div [ attr.``class`` "section-title" ] [
                        h3 [] [ text "Account view" ]
                        span [] [ text "Balances and range activity" ]
                    ]
                    if accounts.IsEmpty then
                        div [ attr.``class`` "ledger-empty" ] [
                            strong [] [ text "No account data yet" ]
                            p [] [ text "Load the demo month to see account-level breakdowns." ]
                        ]
                    else
                        div [ attr.``class`` "account-grid" ] [
                            for account in accounts do
                                div [ attr.``class`` "account-card" ] [
                                    div [ attr.``class`` "account-head" ] [
                                        div [] [
                                            strong [] [ text account.AccountName ]
                                            p [] [ text (string account.TransactionCount + " transactions in range") ]
                                        ]
                                        span [ attr.``class`` "meta-pill" ] [ text (Money.formatPercent account.Share) ]
                                    ]
                                    div [ attr.``class`` "account-mini-grid" ] [
                                        renderStatCard (statItem "Balance" (Money.format account.Balance) "Running balance")
                                        renderStatCard (statItem "Net flow" (Money.format account.RangeNetFlow) "Selected range")
                                        renderStatCard (statItem "Income" (Money.format account.RangeIncome) "Range inflow")
                                        renderStatCard (statItem "Expenses" (Money.format account.RangeExpenses) "Range outflow")
                                    ]
                                ]
                        ]
                ]
            ) filters.ReviewRange.View
        ) state.View

    let private renderCategoryBreakdownPanel (state: Var<DashboardModel>) (filters: FilterState) =
        Doc.BindView (fun model ->
            Doc.BindView (fun reviewRange ->
                let reviewTransactions = FinanceEngine.transactionsForRange reviewRange model
                let slices = FinanceEngine.spendingSlices reviewTransactions

                div [ attr.``class`` "panel pad" ] [
                    div [ attr.``class`` "section-title" ] [
                        h3 [] [ text "Category mix" ]
                        span [] [ text "Top spend drivers" ]
                    ]
                    if slices.IsEmpty then
                        div [ attr.``class`` "insight neutral" ] [
                            strong [] [ text "No expense categories yet" ]
                            p [] [ text "Add a transaction to see where money is going." ]
                        ]
                    else
                        div [ attr.``class`` "bars" ] [
                            for slice in slices do
                                div [] [
                                    div [ attr.``class`` "bar-label" ] [
                                        span [] [ text slice.Label ]
                                        span [] [ text (Money.format slice.Amount + " / " + Money.formatPercent slice.Share) ]
                                    ]
                                    div [ attr.``class`` "bar-track" ] [
                                        div [ attr.``class`` "bar-fill"; attr.style ("--w:" + string (percentWidth slice.Share) + "%") ] []
                                    ]
                                ]
                        ]
                ]
            ) filters.ReviewRange.View
        ) state.View

    let private renderTimelinePanel (state: Var<DashboardModel>) =
        Doc.BindView (fun model ->
            let points = FinanceEngine.monthlyTrend model.Transactions
            div [ attr.``class`` "panel pad" ] [
                div [ attr.``class`` "section-title" ] [
                    h3 [] [ text "Monthly trend" ]
                    span [] [ text "Savings by month" ]
                ]
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
                                span [] [ text (monthYearLabel point.Year point.Month) ]
                                div [ attr.``class`` "bar-track" ] [
                                    div [ attr.``class`` "bar-fill"; attr.style ("--w:" + string incomeWidth + "%") ] []
                                ]
                                span [] [ text (Money.format point.Savings) ]
                            ]
                    ]
            ]
        ) state.View

    let private renderForecastPanel (state: Var<DashboardModel>) (filters: FilterState) =
        Doc.BindView (fun model ->
            Doc.BindView (fun reviewRange ->
                let forecast = FinanceEngine.projectedSnapshot reviewRange model
                let comparison = currentMonthComparison model.Transactions

                div [ attr.``class`` "panel pad" ] [
                    div [ attr.``class`` "section-title" ] [
                        h3 [] [ text "Forecast" ]
                        span [] [ text "Forward view" ]
                    ]
                    div [ attr.``class`` "forecast-grid" ] [
                        metric "Runway" (if forecast.RunwayMonths = 0.0 then "n/a" else string forecast.RunwayMonths + " mo") "At the current expense pace"
                        metric "Recurring baseline" (Money.format forecast.RecurringBaseline) "Repeated spend from history"
                        metric "Projected balance" (Money.format forecast.ProjectedBalance) "If this pattern repeats"
                        metric "Latest month delta" (
                            match comparison with
                            | Some (_, _, _, _, savingsDelta) -> formatDelta savingsDelta
                            | None -> "Need 2 months"
                        ) "Compared with the previous month"
                    ]
                    match comparison with
                    | Some (current, previous, incomeDelta, expenseDelta, savingsDelta) ->
                        p [ attr.``class`` "forecast-note" ] [
                            text (
                                monthYearLabel current.Year current.Month
                                + " vs "
                                + monthYearLabel previous.Year previous.Month
                                + ": income "
                                + formatDelta incomeDelta
                                + ", expenses "
                                + formatDelta expenseDelta
                                + ", savings "
                                + formatDelta savingsDelta)
                        ]
                    | None ->
                        p [ attr.``class`` "forecast-note" ] [
                            text "Add another month of activity to unlock month-over-month comparison."
                        ]
                ]
            ) filters.ReviewRange.View
        ) state.View

    let private renderRecurringPanel (state: Var<DashboardModel>) =
        Doc.BindView (fun model ->
            let recurring = FinanceEngine.recurringCandidates model.Transactions |> List.truncate 4
            let baseline = FinanceEngine.predictableMonthlyCosts model.Transactions

            div [ attr.``class`` "panel pad" ] [
                div [ attr.``class`` "section-title" ] [
                    h3 [] [ text "Recurring spend" ]
                    span [] [ text (Money.format baseline + " monthly baseline") ]
                ]
                if recurring.IsEmpty then
                    div [ attr.``class`` "insight neutral" ] [
                        strong [] [ text "No recurring patterns yet" ]
                        p [] [ text "Recurring spend appears once the same expense shows up more than once." ]
                    ]
                else
                    div [ attr.``class`` "recurring-list" ] [
                        for (description, category, average, count) in recurring do
                            let total = average * float count |> Money.round2
                            div [ attr.``class`` "recurring-item" ] [
                                div [] [
                                    strong [] [ text description ]
                                    p [] [ text (category + " - " + string count + " occurrences") ]
                                ]
                                div [ attr.``class`` "recurring-amount" ] [
                                    text (Money.format total)
                                ]
                            ]
                    ]
            ]
        ) state.View

    let private renderScenarioPanel (state: Var<DashboardModel>) =
        let draft = createScenarioDraft()

        Doc.BindView (fun model ->
            Doc.BindView (fun extraIncome ->
                Doc.BindView (fun expenseCut ->
                    Doc.BindView (fun extraCost ->
                        let summary = FinanceEngine.summarize model
                        let scenario =
                            FinanceEngine.simulateScenario
                                model.Budget
                                summary
                                (parseNonNegative extraIncome 0.0)
                                (parseNonNegative expenseCut 0.0)
                                (parseNonNegative extraCost 0.0)

                        div [ attr.``class`` "panel pad" ] [
                            div [ attr.``class`` "section-title" ] [
                                h3 [] [ text "What-if scenario" ]
                                span [] [ text "Simulate a change before it happens" ]
                            ]
                            div [ attr.``class`` "form-grid" ] [
                                field "Extra income" (Doc.InputType.Text [ attr.placeholder "0.00" ] draft.ExtraIncome)
                                field "Expense cut" (Doc.InputType.Text [ attr.placeholder "0.00" ] draft.ExpenseCut)
                                field "Extra cost" (Doc.InputType.Text [ attr.placeholder "0.00" ] draft.ExtraCost)
                            ]
                            div [ attr.``class`` "scenario-grid" ] [
                                renderStatCard (statItem "Projected income" (Money.format scenario.Income) "After the change")
                                renderStatCard (statItem "Projected expenses" (Money.format scenario.Expenses) "After the change")
                                renderStatCard (statItem "Projected savings" (Money.format scenario.Savings) "Difference from now")
                                renderStatCard (statItem "Projected balance" (Money.format scenario.ProjectedBalance) "Account level impact")
                            ]
                            div [ attr.``class`` "budget-row scenario-row" ] [
                                span [] [ text "Scenario status" ]
                                strong [] [ text (statusText scenario.Status) ]
                            ]
                            div [ attr.``class`` "control-actions" ] [
                                button [
                                    attr.``class`` "button secondary"
                                    on.click (fun _ _ ->
                                        draft.ExtraIncome := "0.00"
                                        draft.ExpenseCut := "0.00"
                                        draft.ExtraCost := "0.00")
                                ] [ text "Reset scenario" ]
                            ]
                        ]
                    ) draft.ExtraCost.View
                ) draft.ExpenseCut.View
            ) draft.ExtraIncome.View
        ) state.View

    let private renderGoalPanel (state: Var<DashboardModel>) =
        let draft = createGoalDraft()

        Doc.BindView (fun model ->
            Doc.BindView (fun goalAmountText ->
                Doc.BindView (fun horizonMonthsText ->
                    let summary = FinanceEngine.summarize model
                    let goalAmount = parsePositive goalAmountText 900.0
                    let horizonMonths = parsePositiveInt horizonMonthsText 6
                    let plan = FinanceEngine.savingsPlan horizonMonths goalAmount (FinanceEngine.averageMonthlySavings model.Transactions) summary

                    div [ attr.``class`` "panel pad" ] [
                        div [ attr.``class`` "section-title" ] [
                            h3 [] [ text "Savings target" ]
                            span [] [ text "Goal progress and feasibility" ]
                        ]
                        div [ attr.``class`` "form-grid" ] [
                            field "Goal amount" (Doc.InputType.Text [ attr.placeholder "900.00" ] draft.GoalAmount)
                            field "Horizon months" (Doc.InputType.Text [ attr.placeholder "6" ] draft.HorizonMonths)
                        ]
                        div [ attr.``class`` "budget-preview" ] [
                            div [ attr.``class`` "budget-row" ] [
                                span [] [ text "Target progress" ]
                                strong [] [ text (Money.formatPercent plan.Progress) ]
                            ]
                            div [ attr.``class`` "bar-track budget-track" ] [
                                div [ attr.``class`` "bar-fill"; attr.style ("--w:" + string (percentWidth plan.Progress) + "%") ] []
                            ]
                            div [ attr.``class`` "scenario-grid" ] [
                                renderStatCard (statItem "Current savings" (Money.format plan.CurrentAmount) "From the selected range")
                                renderStatCard (statItem "Remaining" (Money.format plan.RemainingAmount) "Still to reach the goal")
                                renderStatCard (statItem "Monthly need" (Money.format plan.RequiredMonthlyContribution) "To hit the target")
                                renderStatCard (statItem "Average savings" (Money.format plan.AverageMonthlySavings) "Historical average")
                            ]
                        ]
                        div [ attr.``class`` "budget-row scenario-row" ] [
                            span [] [ text "Feasible in horizon" ]
                            strong [] [ text (if plan.Feasible then "Yes" else "Not yet") ]
                        ]
                    ]
                ) draft.HorizonMonths.View
            ) draft.GoalAmount.View
        ) state.View

    let private renderInsightsPanel (state: Var<DashboardModel>) (filters: FilterState) =
        Doc.BindView (fun model ->
            Doc.BindView (fun reviewRange ->
                let reviewTransactions = FinanceEngine.transactionsForRange reviewRange model
                let reviewModel = { model with Transactions = reviewTransactions }
                let insights = FinanceEngine.generateInsights reviewModel

                div [ attr.``class`` "panel pad" ] [
                    div [ attr.``class`` "section-title" ] [
                        h3 [] [ text "Smart insights" ]
                        span [] [ text "Rule based" ]
                    ]
                    if insights.IsEmpty then
                        div [ attr.``class`` "insight neutral" ] [
                            strong [] [ text "No insights yet" ]
                            p [] [ text "Add more transactions to unlock more analysis." ]
                        ]
                    else
                        div [ attr.``class`` "insight-list" ] [
                            for insight in insights do
                                div [ attr.``class`` (insightClass insight.Severity) ] [
                                    strong [] [ text insight.Title ]
                                    p [] [ text insight.Message ]
                                ]
                        ]
                ]
            ) filters.ReviewRange.View
        ) state.View

    let private renderLedgerPanel (state: Var<DashboardModel>) (filters: FilterState) =
        Doc.BindView (fun model ->
            Doc.BindView (fun reviewRange ->
                Doc.BindView (fun search ->
                    Doc.BindView (fun category ->
                        Doc.BindView (fun kind ->
                            let reviewTransactions = FinanceEngine.transactionsForRange reviewRange model
                            let ordered =
                                reviewTransactions
                                |> List.filter (matchesLedgerFilters search category kind)
                                |> List.sortByDescending (fun t -> t.PostedOn, t.Id)

                            div [ attr.``class`` "panel pad" ] [
                                div [ attr.``class`` "section-title" ] [
                                    h3 [] [ text "Transaction ledger" ]
                                    span [] [ text (string ordered.Length + " of " + string reviewTransactions.Length + " records") ]
                                ]
                                if reviewTransactions.IsEmpty then
                                    div [ attr.``class`` "ledger-empty" ] [
                                        strong [] [ text "No transactions in this review range" ]
                                        p [] [ text "Switch the period or load the demo month to inspect the ledger." ]
                                    ]
                                elif ordered.IsEmpty then
                                    div [ attr.``class`` "ledger-empty" ] [
                                        strong [] [ text "No matching transactions" ]
                                        p [] [ text "Adjust search, category, or kind filters." ]
                                    ]
                                else
                                    div [ attr.``class`` "transaction-list" ] [
                                        for item in ordered do
                                            let amountClass =
                                                if Transaction.isIncome item then "amount income"
                                                elif Transaction.isExpense item then "amount expense"
                                                else "amount"

                                            div [ attr.``class`` "transaction" ] [
                                                div [ attr.``class`` "transaction-body" ] [
                                                    h4 [] [ text item.Description ]
                                                    div [ attr.``class`` "transaction-meta" ] [
                                                        span [ attr.``class`` "meta-pill" ] [ text (dateValue item.PostedOn) ]
                                                        span [ attr.``class`` "meta-pill" ] [ text (kindText item.Kind) ]
                                                        span [ attr.``class`` "meta-pill" ] [ text (Category.displayName item.Category) ]
                                                        span [ attr.``class`` "meta-pill" ] [ text item.Account ]
                                                    ]
                                                    match item.Note with
                                                    | Some note when not (String.IsNullOrWhiteSpace note) ->
                                                        p [ attr.``class`` "transaction-note" ] [ text note ]
                                                    | _ -> text ""
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
                            ]
                        ) filters.Kind.View
                    ) filters.Category.View
                ) filters.Search.View
            ) filters.ReviewRange.View
        ) state.View

    let private hero (state: Var<DashboardModel>) =
        Doc.BindView (fun model ->
            let summary = FinanceEngine.summarize model
            div [ attr.``class`` "hero panel" ] [
                div [] [
                    span [ attr.``class`` (statusClass summary.Status) ] [ text (statusText summary.Status) ]
                    h2 [] [ text "Plan smarter with a live finance dashboard." ]
                    p [] [
                        text "QuickFin turns transactions into budget decisions with live totals, account views, review ranges, search, recurring spend, and practical forecast panels."
                    ]
                    div [ attr.``class`` "source-tags" ] [
                        codePill "Live budget"
                        codePill "Review ranges"
                        codePill "Forecast"
                    ]
                    div [ attr.``class`` "hero-actions" ] [
                        button [
                            attr.``class`` "button"
                            on.click (fun _ _ -> state := FinanceEngine.demoModel)
                        ] [ text "Load demo" ]
                    ]
                ]
                div [ attr.``class`` "mini-column" ] [
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
                    heroStats state
                ]
            ]) state.View

    let private onboardingGuide (state: Var<DashboardModel>) (isOpen: Var<bool>) =
        Doc.BindView (fun visible ->
            if visible then
                div [ attr.``class`` "onboarding-overlay" ] [
                    div [ attr.``class`` "onboarding-card panel" ] [
                        div [ attr.``class`` "onboarding-head" ] [
                            div [] [
                                span [ attr.``class`` "status-pill" ] [ text "First use guide" ]
                                h2 [] [ text "Start with a complete finance month." ]
                                p [] [ text "Load the sample data, switch the review range, and watch the dashboard update from the same state." ]
                            ]
                            button [
                                attr.``class`` "close-button"
                                attr.title "Close guide"
                                on.click (fun _ _ -> isOpen := false)
                            ] [ text "Close" ]
                        ]
                        div [ attr.``class`` "onboarding-body" ] [
                            div [ attr.``class`` "onboarding-path" ] [
                                onboardingStep "1" "Load the demo" "Use the sample income, rent, groceries, study, travel, and subscription records as a ready review month."
                                onboardingStep "2" "Change the view" "Switch the review range or narrow the ledger with search, category, and kind filters."
                                onboardingStep "3" "Try a preset" "Quick-fill a common transaction, then open the implementation notes to see the F# source path."
                            ]
                            div [ attr.``class`` "demo-panel" ] [
                                strong [] [ text "Demo preview" ]
                                p [] [ text "The sample month is intentionally varied so the charts, forecast, recurring spend, and ledger filters have something meaningful to show." ]
                                div [ attr.``class`` "demo-facts" ] [
                                    demoFact "Income" "$5,050" "June salary sample"
                                    demoFact "Budget" "$3,200" "Expense limit"
                                    demoFact "Records" (string FinanceEngine.demoModel.Transactions.Length) "Transactions across categories"
                                ]
                            ]
                        ]
                        div [ attr.``class`` "onboarding-actions" ] [
                            button [
                                attr.``class`` "button"
                                on.click (fun _ _ ->
                                    markOnboardingSeen()
                                    state := FinanceEngine.demoModel
                                    isOpen := false)
                            ] [ text "Start with demo" ]
                            button [
                                attr.``class`` "button secondary"
                                on.click (fun _ _ ->
                                    markOnboardingSeen()
                                    isOpen := false)
                            ] [ text "Explore dashboard" ]
                        ]
                    ]
                ]
            else
                text "") isOpen.View

    let private controlPanel (state: Var<DashboardModel>) (filters: FilterState) =
        Doc.BindView (fun model ->
            Doc.BindView (fun reviewRange ->
                let reviewTransactions = FinanceEngine.transactionsForRange reviewRange model

                div [ attr.``class`` "panel pad control-panel" ] [
                    div [ attr.``class`` "section-title" ] [
                        h3 [] [ text "Review controls" ]
                        span [] [ text (ReviewRange.displayName reviewRange + " - " + string reviewTransactions.Length + " records") ]
                    ]
                    div [ attr.``class`` "range-switch" ] [
                        for range in ReviewRange.options do
                            Doc.BindView (fun current ->
                                button [
                                    attr.``class`` (if current = range then "range-chip active" else "range-chip")
                                    on.click (fun _ _ -> filters.ReviewRange := range)
                                ] [ text (ReviewRange.displayName range) ]
                            ) filters.ReviewRange.View
                    ]
                    div [ attr.``class`` "control-grid" ] [
                        wideField "Search ledger" (Doc.InputType.Text [ attr.placeholder "Description, note, account" ] filters.Search)
                        field "Category" (Doc.InputType.Select [ attr.title "Category" ] id categoryFilterOptions filters.Category)
                        field "Kind" (Doc.InputType.Select [ attr.title "Kind" ] id kindFilterOptions filters.Kind)
                    ]
                    div [ attr.``class`` "control-actions" ] [
                        button [
                            attr.``class`` "button secondary"
                            on.click (fun _ _ ->
                                filters.ReviewRange := CurrentMonth
                                filters.Search := ""
                                filters.Category := "All categories"
                                filters.Kind := "All kinds")
                        ] [ text "Clear filters" ]
                        button [
                            attr.``class`` "button"
                            on.click (fun _ _ -> state := FinanceEngine.demoModel)
                        ] [ text "Load demo" ]
                    ]
                ]
            ) filters.ReviewRange.View
        ) state.View

    let private featureGuide () =
        div [ attr.``class`` "panel pad" ] [
            div [ attr.``class`` "section-title" ] [
                h3 [] [ text "Feature guide" ]
                span [] [ text "What to try" ]
            ]
            div [ attr.``class`` "guide-list" ] [
                guideItem "Live planning" "Use the form to add a transaction and watch the balance, spending, savings, and category bars update immediately."
                guideItem "Review filters" "Switch the review range, search the ledger, or narrow by category and kind without leaving the page."
                guideItem "What-if and goals" "Use the scenario and savings target panels to test decisions and show a concrete planning workflow."
            ]
        ]

    let private workflowPanel () =
        div [ attr.``class`` "panel pad" ] [
            div [ attr.``class`` "section-title" ] [
                h3 [] [ text "Try this flow" ]
                span [] [ text "Three quick steps" ]
            ]
            div [ attr.``class`` "workflow-steps" ] [
                workflowStep "01" "Load the demo month" "Start from a realistic sample so every chart and analysis block has meaningful data."
                workflowStep "02" "Change a number" "Adjust a budget field, add a transaction, or run a scenario to show the live reactivity."
                workflowStep "03" "Read the output" "Use the forecast, account view, and generated summary to explain what the F# app is doing."
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
                    "Accounts, transactions, budgets, categories, summaries, review ranges, and forecast data are F# records and discriminated unions."
                    [ "Domain.fs" ]
                implementationItem
                    "Browser UI"
                    "The dashboard uses WebSharper UI, reactive Vars, BindView, and F# event handlers for controls, forecast panels, and the ledger."
                    [ "Client.fs" ]
                implementationItem
                    "Static web deployment"
                    "The build compiles F# to JavaScript, bundles it with esbuild, and publishes the generated site to GitHub Pages."
                    [ "Main.fs"; "Main.html"; "wsconfig.json" ]
            ]
        ]

    let private reportPanel (state: Var<DashboardModel>) (filters: FilterState) =
        Doc.BindView (fun model ->
            Doc.BindView (fun reviewRange ->
                let reviewTransactions = FinanceEngine.transactionsForRange reviewRange model
                let reviewModel = { model with Transactions = reviewTransactions }

                div [ attr.``class`` "panel pad" ] [
                    div [ attr.``class`` "section-title" ] [
                        h3 [] [ text "Generated summary" ]
                        span [] [ text (ReviewRange.displayName reviewRange + " export") ]
                    ]
                    pre [ attr.``class`` "report-box" ] [
                        text (FinanceEngine.exportSummaryLines reviewModel |> joinLines)
                    ]
                ]
            ) filters.ReviewRange.View
        ) state.View

    let private transactionForm (state: Var<DashboardModel>) =
        let draft = createDraft()

        div [ attr.``class`` "panel pad" ] [
            div [ attr.``class`` "section-title" ] [
                h3 [] [ text "Add transaction" ]
                span [] [ text "State updates live" ]
            ]
            div [ attr.``class`` "preset-row" ] [
                for preset in presets do
                    button [
                        attr.``class`` "preset-button"
                        on.click (fun _ _ -> applyPreset draft preset)
                    ] [ text preset.Label ]
            ]
            div [ attr.``class`` "form-grid" ] [
                wideField "Description" (Doc.InputType.Text [ attr.placeholder "Description" ] draft.Description)
                field "Amount" (Doc.InputType.Text [ attr.placeholder "0.00" ] draft.Amount)
                field "Date" (Doc.InputType.Date [] draft.Date)
                field "Kind" (
                    Doc.InputType.Select [ attr.title "Transaction kind" ] kindText [ Income; Expense; Transfer ] draft.Kind
                )
                field "Category" (
                    Doc.InputType.Select [ attr.title "Category" ] Category.displayName Category.options draft.Category
                )
                field "Account" (Doc.InputType.Text [ attr.placeholder "Checking" ] draft.Account)
                wideField "Note" (Doc.InputType.Text [ attr.placeholder "Optional note" ] draft.Note)
            ]
            div [ attr.``class`` "hero-actions" ] [
                button [
                    attr.``class`` "button"
                    on.click (fun _ _ ->
                        addDraftTransaction state draft)
                ] [ text "Add transaction" ]
                button [
                    attr.``class`` "button secondary"
                    on.click (fun _ _ -> resetDraft draft)
                ] [ text "Clear form" ]
            ]
        ]

    let private budgetPanel (state: Var<DashboardModel>) =
        let current = state.Value.Budget
        let incomeTarget = Var.Create (string current.IncomeTarget)
        let expenseLimit = Var.Create (string current.ExpenseLimit)
        let savingsGoal = Var.Create (string current.SavingsGoal)

        Doc.BindView (fun model ->
            let currentBudget = model.Budget
            let summary = FinanceEngine.summarize model
            let savingsGap = Money.round2 (summary.Savings - currentBudget.SavingsGoal)

            div [ attr.``class`` "panel pad" ] [
                div [ attr.``class`` "section-title" ] [
                    h3 [] [ text "Budget controls" ]
                    span [] [ text "All views update together" ]
                ]
                div [ attr.``class`` "budget-preview" ] [
                    div [ attr.``class`` "budget-row" ] [
                        span [] [ text "Current status" ]
                        strong [] [ text (statusText summary.Status) ]
                    ]
                    div [ attr.``class`` "bar-track budget-track" ] [
                        div [ attr.``class`` "bar-fill"; attr.style ("--w:" + string (percentWidth summary.BudgetUsed) + "%") ] []
                    ]
                    div [ attr.``class`` "demo-facts budget-facts" ] [
                        demoFact "Income target" (Money.format currentBudget.IncomeTarget) "Planned monthly income"
                        demoFact "Expense limit" (Money.format currentBudget.ExpenseLimit) "Ceiling for spending"
                        demoFact "Savings gap" (Money.format savingsGap) "Goal minus current savings"
                    ]
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
                            let updated =
                                { model.Budget with
                                    IncomeTarget = parsePositive incomeTarget.Value model.Budget.IncomeTarget
                                    ExpenseLimit = parsePositive expenseLimit.Value model.Budget.ExpenseLimit
                                    SavingsGoal = parsePositive savingsGoal.Value model.Budget.SavingsGoal }
                            state := { model with Budget = updated })
                    ] [ text "Apply budget" ]
                ]
            ]
        ) state.View

    let private analytics (state: Var<DashboardModel>) (filters: FilterState) =
        div [] [
            renderSummaryPanel state filters
            div [ attr.``class`` "grid-two" ] [
                renderAccountPanel state filters
                renderCategoryBreakdownPanel state filters
                renderTimelinePanel state
            ]
            div [ attr.``class`` "grid-two" ] [
                renderForecastPanel state filters
                renderRecurringPanel state
            ]
            div [ attr.``class`` "grid-two" ] [
                renderInsightsPanel state filters
                renderGoalPanel state
            ]
        ]

    let private pageIntro (state: Var<DashboardModel>) =
        div [ attr.``class`` "panel pad page-intro" ] [
            div [ attr.``class`` "section-title" ] [
                h3 [] [ text "Project snapshot" ]
                span [] [ text "What the app covers" ]
            ]
            div [ attr.``class`` "intro-grid" ] [
                div [] [
                    strong [] [ text "Demo data, custom entries, and live filters" ]
                    p [] [ text "The page gives reviewers a working finance month, editable budget numbers, a transaction ledger, and a range switcher so they can inspect different slices of data fast." ]
                ]
                div [] [
                    strong [] [ text "Real F# computation behind the screens" ]
                    p [] [ text "The UI is fed by typed domain records and derived finance functions for forecasts, account balances, scenario simulation, and savings planning." ]
                ]
            ]
        ]

    let private overviewPage (state: Var<DashboardModel>) (filters: FilterState) (activePage: Var<AppPage>) =
        div [ attr.``class`` "page-stack" ] [
            hero state
            pageIntro state
            analytics state filters
            monthComparisonPanel state
        ]

    let private transactionsPage (state: Var<DashboardModel>) (filters: FilterState) =
        div [ attr.``class`` "page-stack" ] [
            controlPanel state filters
            renderLedgerPanel state filters
            reportPanel state filters
        ]

    let private planningPage (state: Var<DashboardModel>) =
        div [ attr.``class`` "page-stack" ] [
            transactionForm state
            budgetPanel state
            renderScenarioPanel state
        ]

    let private notesPage (state: Var<DashboardModel>) (filters: FilterState) =
        div [ attr.``class`` "page-stack" ] [
            workflowPanel()
            featureGuide()
            fsharpEvidence()
            renderGoalPanel state
            renderInsightsPanel state filters
        ]

    let Main () =
        let state = Var.Create FinanceEngine.demoModel
        let filters = createFilters()
        let showOnboarding = Var.Create (not (onboardingSeen()))
        let activePage = Var.Create Overview

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
                    div [ attr.``class`` "topbar-actions" ] [
                        div [ attr.``class`` "page-tabs" ] [
                            pageTab activePage Overview
                            pageTab activePage Transactions
                            pageTab activePage Planning
                            pageTab activePage Notes
                        ]
                        button [
                            attr.``class`` "guide-button"
                            attr.title "Open first use guide"
                            on.click (fun _ _ -> showOnboarding := true)
                        ] [ text "Guide" ]
                    ]
                ]
            ]
            onboardingGuide state showOnboarding
            div [ attr.``class`` "layout" ] [
                Doc.BindView (fun page ->
                    match page with
                    | Overview -> overviewPage state filters activePage
                    | Transactions -> transactionsPage state filters
                    | Planning -> planningPage state
                    | Notes -> notesPage state filters
                ) activePage.View
            ]
        ]
