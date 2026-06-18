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

    let normalize transaction =
        { transaction with Amount = Money.round2 transaction.Amount }

[<JavaScript>]
module FinanceEngine =
    let private expenseAmount transaction =
        if Transaction.isExpense transaction then abs transaction.Amount else 0.0

    let private incomeAmount transaction =
        if Transaction.isIncome transaction && transaction.Kind <> Expense then abs transaction.Amount else 0.0

    let normalize model =
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
        if used >= 100.0 then OverBudget
        elif used >= 80.0 then Watch
        else Healthy

    let summarize model =
        let normalized = normalize model
        let income = totalIncome normalized.Transactions
        let expenses = totalExpenses normalized.Transactions
        { Balance = currentBalance normalized
          Income = income
          Expenses = expenses
          Savings = income - expenses |> Money.round2
          SavingsRate = savingsRate normalized.Transactions
          BudgetUsed = budgetUsed normalized.Budget normalized.Transactions
          Status = budgetStatus normalized.Budget normalized.Transactions }

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

    let highSpendingCategories threshold transactions =
        categoryBreakdown transactions
        |> List.filter (fun item -> item.Share >= threshold)

    let findLargestExpense transactions =
        transactions
        |> List.filter Transaction.isExpense
        |> List.sortByDescending (fun t -> abs t.Amount)
        |> List.tryHead

    let averageExpense transactions =
        let expenses =
            transactions
            |> List.filter Transaction.isExpense
            |> List.map expenseAmount

        if expenses.IsEmpty then 0.0
        else expenses |> List.average |> Money.round2

    let recurringCandidates transactions =
        transactions
        |> List.filter Transaction.isExpense
        |> List.groupBy (fun t -> t.Description.Trim().ToLower(), Category.displayName t.Category)
        |> List.choose (fun ((description, category), items) ->
            if items.Length >= 2 then
                let average = items |> List.averageBy expenseAmount |> Money.round2
                Some(description, category, average, items.Length)
            else None)
        |> List.sortByDescending (fun (_, _, average, count) -> average * float count)

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
          "Status: " + status ]
        @ (categories |> List.map (fun c -> c.CategoryName + ": " + Money.format c.Total + " (" + Money.formatPercent c.Share + ")"))

    let demoModel =
        let date (y: int) (m: int) (d: int) = DateTime(y, m, d)
        { Accounts =
            [ { Name = "Checking"; OpeningBalance = 2400.0; Currency = "USD" }
              { Name = "Savings"; OpeningBalance = 1800.0; Currency = "USD" } ]
          Budget =
            { Month = 5
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
              { Id = 17; PostedOn = date 2026 6 5; Description = "Concert"; Amount = 92.0; Kind = Expense; Category = Entertainment; Account = "Checking"; Note = None } ] }
