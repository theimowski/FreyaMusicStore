module FreyaMusicStore.Logon

open System
open System.Security.Claims

open Arachne.Http
open Arachne.Uri.Template

open Freya.Core
open Freya.Core.Operators
open Freya.Lenses.Http
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Router
open Freya.Router

open Microsoft.AspNet.Identity

type Logon = {
    ReturnUrl : string
    ValidationMsg : string
}

let ok _ =
    freya {
        return! writeHtml ("logon", {ReturnUrl = Uris.home; ValidationMsg = ""} )
    }

let authenticate : Freya<unit> = (fun freyaState ->
        let ctx = Microsoft.Owin.OwinContext(freyaState.Environment)
        ctx.Authentication.SignIn(ClaimsIdentity([], DefaultAuthenticationTypes.ApplicationCookie))
        async.Return ((), freyaState)
    )


let correctCredentials = 
    freya {
        let! form = form()
        let username = form |> Map.tryFind "UserName"
        let password = form |> Map.tryFind "Password"
        match username, password with
        | Some "admin", Some "admin" ->
            return true
        | _ ->
            return false
    } |> Freya.memo


let doUnauthorized _ =
    freya {
        let! query = query 
        let returnPath = defaultArg (Map.tryFind "returnUrl" query) Uris.home

        return! writeHtml ("logon", {ReturnUrl = returnPath; ValidationMsg = "User name or password invalid. Try admin/admin"} )
    }

let isAuthorized = 
    freya {
        let! meth = Freya.getLens Request.meth
        if meth = POST then
            return! correctCredentials
        else
            return true
    }

let post =
    freya {
        let! correctCredentials = correctCredentials
        if correctCredentials then
            do! authenticate
    }

let doSeeOther _ =
    freya {
        let! query = query 
        let returnPath = defaultArg (Map.tryFind "returnUrl" query) Uris.home

        do! Freya.setLensPartial 
                Response.Headers.location 
                (Location.Parse (String.Format("http://localhost:8080{0}", returnPath)))
        return! writeHtml ("logon", {ReturnUrl = Uris.home; ValidationMsg = ""} )
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET; POST ] } ) 
        authorized isAuthorized
        handleUnauthorized doUnauthorized
        postRedirect correctCredentials
        handleSeeOther doSeeOther
        handleOk ok
        doPost post } |> FreyaMachine.toPipeline