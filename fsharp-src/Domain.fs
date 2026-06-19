namespace QuickFinCore

open System
open WebSharper

[<JavaScript>]
type TransactionKind =
    | Income
    | Expense
    | Transfer

[<JavaScript>]
type Category =
    | Food
    | Tech
    | Study
    | Travel
    | Housing
    | Health
    | Entertainment
    | Utilities
    | Subscriptions
    | Other of string

[<JavaScript>]
type Account =
    { Name: string
      OpeningBalance: float
      Currency: string }

[<JavaScript>]
type Transaction =
    { Id: int
      PostedOn: DateTime
      Description: string
      Amount: float
      Kind: TransactionKind
      Category: Category
      Account: string
      Note: string option }

[<JavaScript>]
type Budget =
    { Month: int
      Year: int
      IncomeTarget: float
      ExpenseLimit: float
      SavingsGoal: float }

[<JavaScript>]
type CategoryTotal =
    { CategoryName: string
      Total: float
      Count: int
      Share: float }

[<JavaScript>]
type MonthlyPoint =
    { Year: int
      Month: int
      Income: float
      Expense: float
      Savings: float }

[<JavaScript>]
type BudgetStatus =
    | Healthy
    | Watch
    | OverBudget

[<JavaScript>]
type DashboardSummary =
    { Balance: float
      Income: float
      Expenses: float
      Savings: float
      SavingsRate: float
      BudgetUsed: float
      Status: BudgetStatus }

[<JavaScript>]
type InsightSeverity =
    | Positive
    | Neutral
    | Warning

[<JavaScript>]
type Insight =
    { Title: string
      Message: string
      Severity: InsightSeverity }

[<JavaScript>]
type DashboardModel =
    { Accounts: Account list
      Transactions: Transaction list
      Budget: Budget }

[<JavaScript>]
type ReviewRange =
    | CurrentMonth
    | Last3Months
    | AllTime

[<JavaScript>]
module ReviewRange =
    let displayName reviewRange =
        match reviewRange with
        | CurrentMonth -> "Current month"
        | Last3Months -> "Last 3 months"
        | AllTime -> "All time"

    let options =
        [ CurrentMonth; Last3Months; AllTime ]

[<JavaScript>]
type ForecastSnapshot =
    { NetFlow: float
      RunwayMonths: float
      ProjectedSavings: float
      ProjectedBalance: float
      RecurringBaseline: float }

[<JavaScript>]
type SpendingSlice =
    { Label: string
      Amount: float
      Share: float }

[<JavaScript>]
type AccountSnapshot =
    { AccountName: string
      OpeningBalance: float
      Balance: float
      RangeIncome: float
      RangeExpenses: float
      RangeNetFlow: float
      TransactionCount: int
      Share: float }

[<JavaScript>]
type ScenarioSnapshot =
    { Income: float
      Expenses: float
      Savings: float
      BudgetUsed: float
      Status: BudgetStatus
      ProjectedBalance: float
      DeltaIncome: float
      DeltaExpenses: float
      DeltaSavings: float }

[<JavaScript>]
type GoalPlan =
    { TargetAmount: float
      CurrentAmount: float
      RemainingAmount: float
      Progress: float
      HorizonMonths: int
      RequiredMonthlyContribution: float
      AverageMonthlySavings: float
      Feasible: bool }

[<JavaScript>]
module Category =
    let displayName category =
        match category with
        | Food -> "Food"
        | Tech -> "Tech"
        | Study -> "Study"
        | Travel -> "Travel"
        | Housing -> "Housing"
        | Health -> "Health"
        | Entertainment -> "Entertainment"
        | Utilities -> "Utilities"
        | Subscriptions -> "Subscriptions"
        | Other value -> value

    let options =
        [ Food
          Tech
          Study
          Travel
          Housing
          Health
          Entertainment
          Utilities
          Subscriptions
          Other "Income"
          Other "Other" ]

    let fromName (value: string) =
        match value.Trim().ToLower() with
        | "food" -> Food
        | "tech" -> Tech
        | "study" -> Study
        | "travel" -> Travel
        | "housing" -> Housing
        | "health" -> Health
        | "entertainment" -> Entertainment
        | "utilities" -> Utilities
        | "subscriptions" -> Subscriptions
        | "income" -> Other "Income"
        | "" -> Other "Uncategorized"
        | other -> Other other

[<JavaScript>]
module Money =
    let round2 (amount: float) : float =
        Math.Round(amount * 100.0) / 100.0

    let percent (numerator: float) (denominator: float) =
        if denominator = 0.0 then 0.0
        else round2 ((numerator / denominator) * 100.0)

    let clamp (minValue: float) (maxValue: float) (value: float) =
        value |> max minValue |> min maxValue

    let format (amount: float) =
        "$" + (round2 amount).ToString("N2")

    let formatSigned kind amount =
        let prefix =
            match kind with
            | Income -> "+"
            | Expense -> "-"
            | Transfer -> ""

        prefix + format (abs amount)

    let formatPercent (value: float) =
        (round2 value).ToString("N1") + "%"

[<JavaScript>]
module Transaction =
    let signedAmount transaction =
        match transaction.Kind with
        | Income -> abs transaction.Amount
        | Expense -> -abs transaction.Amount
        | Transfer -> transaction.Amount

    let isExpense transaction =
        transaction.Kind = Expense || transaction.Amount < 0.0

    let isIncome transaction =
        transaction.Kind = Income || transaction.Amount > 0.0

    let monthKey transaction =
        transaction.PostedOn.Year, transaction.PostedOn.Month

    let normalize (transaction: Transaction) : Transaction =
        { transaction with Amount = Money.round2 transaction.Amount }

[<JavaScript>]
module FinanceEngine =
    let private expenseAmount transaction =
        if Transaction.isExpense transaction then abs transaction.Amount else 0.0

    let private incomeAmount transaction =
        if Transaction.isIncome transaction && transaction.Kind <> Expense then abs transaction.Amount else 0.0

    let private budgetStatusFromUsed used =
        if used >= 100.0 then OverBudget
        elif used >= 80.0 then Watch
        else Healthy

    let normalize (model: DashboardModel) : DashboardModel =
        { model with Transactions = model.Transactions |> List.map Transaction.normalize }

    let totalOpeningBalance model =
        model.Accounts
        |> List.sumBy _.OpeningBalance
        |> Money.round2

    let totalIncome transactions =
        transactions
        |> List.sumBy incomeAmount
        |> Money.round2

    let totalExpenses transactions =
        transactions
        |> List.sumBy expenseAmount
        |> Money.round2

    let currentBalance model =
        model.Transactions
        |> List.sumBy Transaction.signedAmount
        |> (+) (totalOpeningBalance model)
        |> Money.round2

    let savings transactions =
        totalIncome transactions - totalExpenses transactions
        |> Money.round2

    let savingsRate transactions =
        Money.percent (savings transactions) (totalIncome transactions)

    let budgetUsed budget transactions =
        Money.percent (totalExpenses transactions) budget.ExpenseLimit
        |> Money.clamp 0.0 999.0

    let budgetStatus budget transactions =
        let used = budgetUsed budget transactions
        budgetStatusFromUsed used

    let summarizeForTransactions (model: DashboardModel) (transactions: Transaction list) =
        let income = totalIncome transactions
        let expenses = totalExpenses transactions
        { Balance = currentBalance model
          Income = income
          Expenses = expenses
          Savings = income - expenses |> Money.round2
          SavingsRate = savingsRate transactions
          BudgetUsed = budgetUsed model.Budget transactions
          Status = budgetStatus model.Budget transactions }

    let summarize (model: DashboardModel) =
        let normalized = normalize model
        summarizeForTransactions normalized normalized.Transactions

    let categoryBreakdown transactions =
        let expenseTransactions =
            transactions
            |> List.filter Transaction.isExpense

        let total = totalExpenses expenseTransactions

        expenseTransactions
        |> List.groupBy (fun t -> Category.displayName t.Category)
        |> List.map (fun (name, items) ->
            let categoryTotal = items |> List.sumBy expenseAmount |> Money.round2
            { CategoryName = name
              Total = categoryTotal
              Count = items.Length
              Share = Money.percent categoryTotal total })
        |> List.sortByDescending _.Total

    let monthlyTrend transactions =
        transactions
        |> List.groupBy Transaction.monthKey
        |> List.map (fun ((year, month), items) ->
            let income = totalIncome items
            let expense = totalExpenses items
            { Year = year
              Month = month
              Income = income
              Expense = expense
              Savings = income - expense |> Money.round2 })
        |> List.sortBy (fun point -> point.Year, point.Month)

    let averageMonthlySavings transactions =
        let points = monthlyTrend transactions
        if points.IsEmpty then 0.0
        else points |> List.averageBy _.Savings |> Money.round2

    let accountSnapshots (reviewTransactions: Transaction list) (model: DashboardModel) =
        let normalized = normalize model
        let allTransactionsByAccount =
            normalized.Transactions
            |> List.groupBy (fun t -> t.Account)
            |> Map.ofList

        let rangeTransactionsByAccount =
            reviewTransactions
            |> List.groupBy (fun t -> t.Account)
            |> Map.ofList

        let accountNames =
            [ yield! normalized.Accounts |> List.map _.Name
              yield! normalized.Transactions |> List.map _.Account ]
            |> List.distinct
            |> List.sort

        let snapshots =
            accountNames
            |> List.map (fun accountName ->
                let openingBalance =
                    normalized.Accounts
                    |> List.tryFind (fun account -> account.Name = accountName)
                    |> Option.map _.OpeningBalance
                    |> Option.defaultValue 0.0

                let allItems = Map.tryFind accountName allTransactionsByAccount |> Option.defaultValue []
                let rangeItems = Map.tryFind accountName rangeTransactionsByAccount |> Option.defaultValue []
                let balance = Money.round2 (openingBalance + (allItems |> List.sumBy Transaction.signedAmount))
                let rangeIncome = totalIncome rangeItems
                let rangeExpenses = totalExpenses rangeItems
                let rangeNetFlow = Money.round2 (rangeIncome - rangeExpenses)

                { AccountName = accountName
                  OpeningBalance = openingBalance
                  Balance = balance
                  RangeIncome = rangeIncome
                  RangeExpenses = rangeExpenses
                  RangeNetFlow = rangeNetFlow
                  TransactionCount = rangeItems.Length
                  Share = 0.0 })

        let totalBalance =
            snapshots
            |> List.sumBy _.Balance
            |> Money.round2

        snapshots
        |> List.map (fun snapshot ->
            { snapshot with
                Share =
                    if totalBalance <= 0.0 then 0.0
                    else Money.percent snapshot.Balance totalBalance })
        |> List.sortByDescending _.Balance

    let rec transactionsForRange reviewRange (model: DashboardModel) =
        let normalized = normalize model
        let currentYear = normalized.Budget.Year
        let currentMonth = normalized.Budget.Month

        match reviewRange with
        | CurrentMonth ->
            normalized.Transactions
            |> List.filter (fun t -> t.PostedOn.Year = currentYear && t.PostedOn.Month = currentMonth)
        | Last3Months ->
            normalized.Transactions
            |> List.filter (fun t ->
                let monthIndex = t.PostedOn.Year * 12 + t.PostedOn.Month
                let currentIndex = currentYear * 12 + currentMonth
                monthIndex >= currentIndex - 2 && monthIndex <= currentIndex)
        | AllTime ->
            normalized.Transactions

    and spendingSlices (transactions: Transaction list) =
        let items =
            transactions
            |> categoryBreakdown
            |> List.sortByDescending _.Total

        let top = items |> List.truncate 4
        let restTotal = items |> List.skip 4 |> List.sumBy _.Total |> Money.round2
        let total = totalExpenses transactions

        let slices =
            top
            |> List.map (fun item ->
                { Label = item.CategoryName
                  Amount = item.Total
                  Share = if total = 0.0 then 0.0 else Money.percent item.Total total })

        if restTotal > 0.0 then
            slices
            @ [ { Label = "Other"
                  Amount = restTotal
                  Share = if total = 0.0 then 0.0 else Money.percent restTotal total } ]
        else
            slices

    and summarizeForRange reviewRange (model: DashboardModel) =
        let normalized = normalize model
        let transactions = transactionsForRange reviewRange normalized
        summarizeForTransactions normalized transactions

    and projectedSnapshot reviewRange (model: DashboardModel) =
        let normalized = normalize model
        let transactions = transactionsForRange reviewRange normalized
        let summary = summarizeForTransactions normalized transactions
        let netFlow = Money.round2 (summary.Income - summary.Expenses)
        let runwayMonths =
            if summary.Expenses = 0.0 then 0.0
            else Money.round2 (summary.Balance / summary.Expenses)
        let recurringBaseline = predictableMonthlyCosts normalized.Transactions
        let projectedBalance = Money.round2 (summary.Balance + netFlow)

        { NetFlow = netFlow
          RunwayMonths = runwayMonths
          ProjectedSavings = summary.Savings
          ProjectedBalance = projectedBalance
          RecurringBaseline = recurringBaseline }

    and highSpendingCategories threshold transactions =
        categoryBreakdown transactions
        |> List.filter (fun item -> item.Share >= threshold)

    and findLargestExpense transactions =
        transactions
        |> List.filter Transaction.isExpense
        |> List.sortByDescending (fun t -> abs t.Amount)
        |> List.tryHead

    and averageExpense transactions =
        let expenses =
            transactions
            |> List.filter Transaction.isExpense
            |> List.map expenseAmount

        if expenses.IsEmpty then 0.0
        else expenses |> List.average |> Money.round2

    and recurringCandidates transactions =
        transactions
        |> List.filter Transaction.isExpense
        |> List.groupBy (fun t -> t.Description.Trim().ToLower(), Category.displayName t.Category)
        |> List.choose (fun ((description, category), items) ->
            if items.Length >= 2 then
                let average = items |> List.averageBy expenseAmount |> Money.round2
                Some(description, category, average, items.Length)
            else None)
        |> List.sortByDescending (fun (_, _, average, count) -> average * float count)

    and predictableMonthlyCosts (transactions: Transaction list) =
        recurringCandidates transactions
        |> List.truncate 3
        |> List.sumBy (fun (_, _, average, count) -> average * float count)
        |> Money.round2

    and monthComparison (current: MonthlyPoint) (previous: MonthlyPoint) =
        let incomeDelta = Money.round2 (current.Income - previous.Income)
        let expenseDelta = Money.round2 (current.Expense - previous.Expense)
        let savingsDelta = Money.round2 (current.Savings - previous.Savings)
        incomeDelta, expenseDelta, savingsDelta

    let simulateScenario (budget: Budget) (summary: DashboardSummary) extraIncome expenseCut extraCost =
        let income = Money.round2 (summary.Income + extraIncome)
        let expenses = Money.round2 (max 0.0 (summary.Expenses - expenseCut) + extraCost)
        let savings = Money.round2 (income - expenses)
        let budgetUsed = Money.percent expenses budget.ExpenseLimit |> Money.clamp 0.0 999.0
        let status = budgetStatusFromUsed budgetUsed
        let projectedBalance = Money.round2 (summary.Balance + (savings - summary.Savings))

        { Income = income
          Expenses = expenses
          Savings = savings
          BudgetUsed = budgetUsed
          Status = status
          ProjectedBalance = projectedBalance
          DeltaIncome = Money.round2 (income - summary.Income)
          DeltaExpenses = Money.round2 (expenses - summary.Expenses)
          DeltaSavings = Money.round2 (savings - summary.Savings) }

    let savingsPlan horizonMonths targetAmount averageMonthlySavings (summary: DashboardSummary) =
        let current = summary.Savings
        let remaining = Money.round2 (max 0.0 (targetAmount - current))
        let months = max 1 horizonMonths
        let requiredMonthlyContribution = Money.round2 (remaining / float months)
        let progress =
            if targetAmount <= 0.0 then 0.0
            else Money.percent (Money.clamp 0.0 targetAmount current) targetAmount

        { TargetAmount = targetAmount
          CurrentAmount = current
          RemainingAmount = remaining
          Progress = progress
          HorizonMonths = months
          RequiredMonthlyContribution = requiredMonthlyContribution
          AverageMonthlySavings = averageMonthlySavings
          Feasible = averageMonthlySavings >= requiredMonthlyContribution }

    let generateInsights model =
        let summary = summarize model
        let categories = categoryBreakdown model.Transactions
        let largest = findLargestExpense model.Transactions
        let recurring = recurringCandidates model.Transactions

        let budgetInsight =
            match summary.Status with
            | Healthy ->
                { Title = "Budget is under control"
                  Message = "You have used " + Money.formatPercent summary.BudgetUsed + " of the monthly expense limit."
                  Severity = Positive }
            | Watch ->
                { Title = "Budget needs attention"
                  Message = "You have already used " + Money.formatPercent summary.BudgetUsed + " of the monthly expense limit."
                  Severity = Warning }
            | OverBudget ->
                { Title = "Budget exceeded"
                  Message = "Expenses are " + Money.formatPercent summary.BudgetUsed + " of the monthly limit. Review flexible categories first."
                  Severity = Warning }

        let savingsInsight =
            if summary.SavingsRate >= 20.0 then
                { Title = "Strong savings rate"
                  Message = "Savings rate is " + Money.formatPercent summary.SavingsRate + ", above the recommended 20% target."
                  Severity = Positive }
            elif summary.SavingsRate > 0.0 then
                { Title = "Savings can improve"
                  Message = "Savings rate is " + Money.formatPercent summary.SavingsRate + ". Try moving a fixed amount to savings first."
                  Severity = Neutral }
            else
                { Title = "Negative cash flow"
                  Message = "Expenses are higher than income for the selected period."
                  Severity = Warning }

        let categoryInsight =
            categories
            |> List.tryHead
            |> Option.map (fun top ->
                { Title = "Largest category: " + top.CategoryName
                  Message = top.CategoryName + " accounts for " + Money.formatPercent top.Share + " of expenses."
                  Severity = if top.Share >= 40.0 then Warning else Neutral })

        let largestInsight =
            largest
            |> Option.map (fun expense ->
                { Title = "Largest expense"
                  Message = expense.Description + " was the largest single expense at " + Money.format (abs expense.Amount) + "."
                  Severity = Neutral })

        let recurringInsight =
            recurring
            |> List.tryHead
            |> Option.map (fun (description, category, average, count) ->
                { Title = "Recurring expense detected"
                  Message = description + " appears " + string count + " times in " + category + ", averaging " + Money.format average + "."
                  Severity = Neutral })

        [ Some budgetInsight
          Some savingsInsight
          categoryInsight
          largestInsight
          recurringInsight ]
        |> List.choose id

    let transactionsForMonth year month transactions =
        transactions
        |> List.filter (fun t -> t.PostedOn.Year = year && t.PostedOn.Month = month)

    let withTransaction transaction model =
        { model with Transactions = transaction :: model.Transactions }

    let removeTransaction id model =
        { model with Transactions = model.Transactions |> List.filter (fun t -> t.Id <> id) }

    let updateBudget budget model =
        { model with Budget = budget }

    let exportSummaryLines model =
        let summary = summarize model
        let categories = categoryBreakdown model.Transactions
        let accounts = accountSnapshots model.Transactions model
        let averageSavings = averageMonthlySavings model.Transactions
        let status =
            match summary.Status with
            | Healthy -> "Healthy"
            | Watch -> "Watch"
            | OverBudget -> "Over budget"

        [ "Balance: " + Money.format summary.Balance
          "Income: " + Money.format summary.Income
          "Expenses: " + Money.format summary.Expenses
          "Savings: " + Money.format summary.Savings
          "Savings rate: " + Money.formatPercent summary.SavingsRate
          "Budget used: " + Money.formatPercent summary.BudgetUsed
          "Average monthly savings: " + Money.format averageSavings
          "Status: " + status ]
        @ (categories |> List.map (fun c -> c.CategoryName + ": " + Money.format c.Total + " (" + Money.formatPercent c.Share + ")"))
        @ (if accounts.IsEmpty then []
           else
               "Accounts:"
               :: (accounts |> List.map (fun account -> account.AccountName + ": " + Money.format account.Balance + " (" + string account.TransactionCount + " tx)")))

    let demoModel =
        let date (y: int) (m: int) (d: int) = DateTime(y, m, d)
        { Accounts =
            [ { Name = "Checking"; OpeningBalance = 2400.0; Currency = "USD" }
              { Name = "Savings"; OpeningBalance = 1800.0; Currency = "USD" } ]
          Budget =
            { Month = 6
              Year = 2026
              IncomeTarget = 5000.0
              ExpenseLimit = 3200.0
              SavingsGoal = 900.0 }
          Transactions =
            [ { Id = 1; PostedOn = date 2026 4 1; Description = "Salary"; Amount = 5000.0; Kind = Income; Category = Other "Income"; Account = "Checking"; Note = None }
              { Id = 2; PostedOn = date 2026 4 2; Description = "Groceries"; Amount = 131.35; Kind = Expense; Category = Food; Account = "Checking"; Note = Some "Weekly shopping" }
              { Id = 3; PostedOn = date 2026 4 8; Description = "Textbook"; Amount = 82.99; Kind = Expense; Category = Study; Account = "Checking"; Note = None }
              { Id = 4; PostedOn = date 2026 4 13; Description = "Train pass"; Amount = 68.0; Kind = Expense; Category = Travel; Account = "Checking"; Note = None }
              { Id = 5; PostedOn = date 2026 5 1; Description = "Salary"; Amount = 5000.0; Kind = Income; Category = Other "Income"; Account = "Checking"; Note = None }
              { Id = 6; PostedOn = date 2026 5 2; Description = "Groceries"; Amount = 145.35; Kind = Expense; Category = Food; Account = "Checking"; Note = Some "Weekly shopping" }
              { Id = 7; PostedOn = date 2026 5 4; Description = "Laptop stand"; Amount = 79.99; Kind = Expense; Category = Tech; Account = "Checking"; Note = None }
              { Id = 8; PostedOn = date 2026 5 6; Description = "Course books"; Amount = 120.0; Kind = Expense; Category = Study; Account = "Checking"; Note = None }
              { Id = 9; PostedOn = date 2026 5 10; Description = "Train ticket"; Amount = 45.0; Kind = Expense; Category = Travel; Account = "Checking"; Note = None }
              { Id = 10; PostedOn = date 2026 5 12; Description = "Groceries"; Amount = 132.10; Kind = Expense; Category = Food; Account = "Checking"; Note = None }
              { Id = 11; PostedOn = date 2026 5 16; Description = "Rent"; Amount = 1100.0; Kind = Expense; Category = Housing; Account = "Checking"; Note = None }
              { Id = 12; PostedOn = date 2026 5 19; Description = "Gym"; Amount = 35.0; Kind = Expense; Category = Health; Account = "Checking"; Note = None }
              { Id = 13; PostedOn = date 2026 5 23; Description = "Cloud tools"; Amount = 19.0; Kind = Expense; Category = Subscriptions; Account = "Checking"; Note = None }
              { Id = 14; PostedOn = date 2026 6 1; Description = "Salary"; Amount = 5050.0; Kind = Income; Category = Other "Income"; Account = "Checking"; Note = None }
              { Id = 15; PostedOn = date 2026 6 2; Description = "Rent"; Amount = 1100.0; Kind = Expense; Category = Housing; Account = "Checking"; Note = None }
              { Id = 16; PostedOn = date 2026 6 4; Description = "Groceries"; Amount = 127.44; Kind = Expense; Category = Food; Account = "Checking"; Note = None }
              { Id = 17; PostedOn = date 2026 6 5; Description = "Concert"; Amount = 92.0; Kind = Expense; Category = Entertainment; Account = "Checking"; Note = None }
              { Id = 18; PostedOn = date 2026 6 7; Description = "Train ticket"; Amount = 45.0; Kind = Expense; Category = Travel; Account = "Checking"; Note = Some "Weekend transit" }
              { Id = 19; PostedOn = date 2026 6 9; Description = "Groceries"; Amount = 138.2; Kind = Expense; Category = Food; Account = "Checking"; Note = Some "Bulk refill" }
              { Id = 20; PostedOn = date 2026 6 11; Description = "Cloud tools"; Amount = 19.0; Kind = Expense; Category = Subscriptions; Account = "Checking"; Note = Some "Monthly software" }
              { Id = 21; PostedOn = date 2026 6 14; Description = "Gym"; Amount = 35.0; Kind = Expense; Category = Health; Account = "Checking"; Note = Some "Membership" } ] }
