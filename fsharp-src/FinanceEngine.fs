namespace QuickFinCore

open System

module FinanceEngine =
    let private expenseAmount transaction =
        if Transaction.isExpense transaction then abs transaction.Amount else 0.0m

    let private incomeAmount transaction =
        if Transaction.isIncome transaction && transaction.Kind <> Expense then abs transaction.Amount else 0.0m

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
        |> Money.clamp 0.0m 999.0m

    let budgetStatus budget transactions =
        let used = budgetUsed budget transactions
        if used >= 100.0m then OverBudget
        elif used >= 80.0m then Watch
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

        if expenses.IsEmpty then 0.0m
        else expenses |> List.average |> Money.round2

    let recurringCandidates transactions =
        transactions
        |> List.filter Transaction.isExpense
        |> List.groupBy (fun t -> t.Description.Trim().ToLowerInvariant(), Category.displayName t.Category)
        |> List.choose (fun ((description, category), items) ->
            if items.Length >= 2 then
                let average = items |> List.averageBy expenseAmount |> Money.round2
                Some(description, category, average, items.Length)
            else None)
        |> List.sortByDescending (fun (_, _, average, count) -> average * decimal count)

    let generateInsights model =
        let summary = summarize model
        let categories = categoryBreakdown model.Transactions
        let largest = findLargestExpense model.Transactions
        let recurring = recurringCandidates model.Transactions

        let budgetInsight =
            match summary.Status with
            | Healthy ->
                { Title = "Budget is under control"
                  Message = $"You have used {summary.BudgetUsed}%% of the monthly expense limit."
                  Severity = Positive }
            | Watch ->
                { Title = "Budget needs attention"
                  Message = $"You have already used {summary.BudgetUsed}%% of the monthly expense limit."
                  Severity = Warning }
            | OverBudget ->
                { Title = "Budget exceeded"
                  Message = $"Expenses are {summary.BudgetUsed}%% of the monthly limit. Review flexible categories first."
                  Severity = Warning }

        let savingsInsight =
            if summary.SavingsRate >= 20.0m then
                { Title = "Strong savings rate"
                  Message = $"Savings rate is {summary.SavingsRate}%%, which is above the recommended 20%% target."
                  Severity = Positive }
            elif summary.SavingsRate > 0.0m then
                { Title = "Savings can improve"
                  Message = $"Savings rate is {summary.SavingsRate}%%. Try moving a fixed amount to savings first."
                  Severity = Neutral }
            else
                { Title = "Negative cash flow"
                  Message = "Expenses are higher than income for the selected period."
                  Severity = Warning }

        let categoryInsight =
            categories
            |> List.tryHead
            |> Option.map (fun top ->
                { Title = $"Largest category: {top.CategoryName}"
                  Message = $"{top.CategoryName} accounts for {top.Share}%% of expenses."
                  Severity = if top.Share >= 40.0m then Warning else Neutral })

        let largestInsight =
            largest
            |> Option.map (fun expense ->
                { Title = "Largest expense"
                  Message = $"{expense.Description} was the largest single expense at {abs expense.Amount}."
                  Severity = Neutral })

        let recurringInsight =
            recurring
            |> List.tryHead
            |> Option.map (fun (description, category, average, count) ->
                { Title = "Recurring expense detected"
                  Message = $"{description} appears {count} times in {category}, averaging {average}."
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

        [ $"Balance: {summary.Balance}"
          $"Income: {summary.Income}"
          $"Expenses: {summary.Expenses}"
          $"Savings: {summary.Savings}"
          $"Savings rate: {summary.SavingsRate}%%"
          $"Budget used: {summary.BudgetUsed}%%"
          $"Status: {status}" ]
        @ (categories |> List.map (fun c -> $"{c.CategoryName}: {c.Total} ({c.Share}%%)"))

    let demoModel =
        let date (y: int) (m: int) (d: int) = DateTime(y, m, d)
        { Accounts =
            [ { Name = "Checking"; OpeningBalance = 2400.0m; Currency = "USD" }
              { Name = "Savings"; OpeningBalance = 1800.0m; Currency = "USD" } ]
          Budget =
            { Month = 5
              Year = 2026
              IncomeTarget = 5000.0m
              ExpenseLimit = 3200.0m
              SavingsGoal = 900.0m }
          Transactions =
            [ { Id = 1; PostedOn = date 2026 5 1; Description = "Salary"; Amount = 5000.0m; Kind = Income; Category = Other "Income"; Account = "Checking"; Note = None }
              { Id = 2; PostedOn = date 2026 5 2; Description = "Groceries"; Amount = 145.35m; Kind = Expense; Category = Food; Account = "Checking"; Note = Some "Weekly shopping" }
              { Id = 3; PostedOn = date 2026 5 4; Description = "Laptop stand"; Amount = 79.99m; Kind = Expense; Category = Tech; Account = "Checking"; Note = None }
              { Id = 4; PostedOn = date 2026 5 6; Description = "Course books"; Amount = 120.0m; Kind = Expense; Category = Study; Account = "Checking"; Note = None }
              { Id = 5; PostedOn = date 2026 5 10; Description = "Train ticket"; Amount = 45.0m; Kind = Expense; Category = Travel; Account = "Checking"; Note = None }
              { Id = 6; PostedOn = date 2026 5 12; Description = "Groceries"; Amount = 132.10m; Kind = Expense; Category = Food; Account = "Checking"; Note = None }
              { Id = 7; PostedOn = date 2026 5 16; Description = "Rent"; Amount = 1100.0m; Kind = Expense; Category = Housing; Account = "Checking"; Note = None }
              { Id = 8; PostedOn = date 2026 5 19; Description = "Gym"; Amount = 35.0m; Kind = Expense; Category = Health; Account = "Checking"; Note = None } ] }
