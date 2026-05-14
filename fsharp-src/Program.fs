open QuickFinCore

[<EntryPoint>]
let main _ =
    let model = FinanceEngine.demoModel
    let lines = FinanceEngine.exportSummaryLines model
    let insights = FinanceEngine.generateInsights model

    printfn "QuickFin F# core model"
    printfn "======================"

    lines
    |> List.iter (printfn "%s")

    printfn ""
    printfn "Insights"
    printfn "--------"

    insights
    |> List.iter (fun insight -> printfn "%s: %s" insight.Title insight.Message)

    0
