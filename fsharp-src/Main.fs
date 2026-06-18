namespace QuickFinCore

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server
open WebSharper.UI.Templating

[<JavaScript>]
module Templates =
    type MainTemplate = Template<"Main.html", ClientLoad.FromDocument, ServerLoad.PerRequest>

type EndPoint =
    | [<EndPoint "GET /">] Home

module Site =
    open WebSharper.UI.Html
    open type WebSharper.UI.ClientServer

    let HomePage _ctx =
        Content.Page(
            Templates.MainTemplate()
                .Title("QuickFin F# Dashboard")
                .Body([ client (Client.Main()) ])
                .Doc()
        )

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | Home -> HomePage ctx
        )

[<Sealed>]
type Website() =
    interface IWebsite<EndPoint> with
        member _.Sitelet = Site.Main
        member _.Actions = [ Home ]

[<assembly: Website(typeof<Website>)>]
do ()
