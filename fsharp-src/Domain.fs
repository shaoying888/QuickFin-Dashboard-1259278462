namespace QuickFinCore

open System

type TransactionKind =
    | Income
    | Expense
    | Transfer

type Category =
    | Food
    | Tech
    | Study
    | Travel
    | Housing
    | Health
    | Entertainment
    | Other of string

type Account =
    { Name: string
      OpeningBalance: decimal
      Currency: string }

type Transaction =
    { Id: int
      PostedOn: DateTime
      Description: string
      Amount: decimal
      Kind: TransactionKind
      Category: Category
      Account: string
      Note: string option }

type Budget =
    { Month: int
      Year: int
      IncomeTarget: decimal
      ExpenseLimit: decimal
      SavingsGoal: decimal }

type CategoryTotal =
    { CategoryName: string
      Total: decimal
      Count: int
      Share: decimal }

type MonthlyPoint =
    { Year: int
      Month: int
      Income: decimal
      Expense: decimal
      Savings: decimal }

type BudgetStatus =
    | Healthy
    | Watch
    | OverBudget

type DashboardSummary =
    { Balance: decimal
      Income: decimal
      Expenses: decimal
      Savings: decimal
      SavingsRate: decimal
      BudgetUsed: decimal
      Status: BudgetStatus }

type InsightSeverity =
    | Positive
    | Neutral
    | Warning

type Insight =
    { Title: string
      Message: string
      Severity: InsightSeverity }

type DashboardModel =
    { Accounts: Account list
      Transactions: Transaction list
      Budget: Budget }

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
        | Other value -> value

    let fromName (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "food" -> Food
        | "tech" -> Tech
        | "study" -> Study
        | "travel" -> Travel
        | "housing" -> Housing
        | "health" -> Health
        | "entertainment" -> Entertainment
        | "" -> Other "Uncategorized"
        | other -> Other other

module Money =
    let zero = 0.0m

    let round2 (amount: decimal) =
        Math.Round(amount, 2, MidpointRounding.AwayFromZero)

    let percent numerator denominator =
        if denominator = 0.0m then 0.0m
        else round2 ((numerator / denominator) * 100.0m)

    let clamp minValue maxValue value =
        value |> max minValue |> min maxValue

module Transaction =
    let signedAmount transaction =
        match transaction.Kind with
        | Income -> abs transaction.Amount
        | Expense -> -abs transaction.Amount
        | Transfer -> transaction.Amount

    let isExpense transaction =
        transaction.Kind = Expense || transaction.Amount < 0.0m

    let isIncome transaction =
        transaction.Kind = Income || transaction.Amount > 0.0m

    let monthKey transaction =
        transaction.PostedOn.Year, transaction.PostedOn.Month

    let normalize transaction =
        { transaction with Amount = Money.round2 transaction.Amount }
