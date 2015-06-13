module FreyaMusicStore.Logon

open System.Security.Claims

open Arachne.Http
open Arachne.Uri.Template

open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Router
open Freya.Router

open Microsoft.AspNet.Identity

let ok _ =
    freya {
        return! writeHtml ("logon", () )
    }

let post : Freya<unit> = (fun freyaState ->
        let ctx = Microsoft.Owin.OwinContext(freyaState.Environment)
        ctx.Authentication.SignIn(ClaimsIdentity([], DefaultAuthenticationTypes.ApplicationCookie))
        async.Return ((), freyaState)
    )

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET; POST ] } ) 
        handleOk ok
        doPost post } |> FreyaMachine.toPipeline