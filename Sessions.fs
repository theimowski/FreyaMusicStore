module FreyaMusicStore.Sessions

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

let checkCredentials = 
    freya {
        let! form = form
        return maybe {
            let! username = form |> Map.tryFind "UserName"
            let! password = form |> Map.tryFind "Password"
            let ctx = Db.getContext()
            return! Db.validateUser(username, passHash password) ctx
        }
    } |> Freya.memo

let upgradeCarts userName = 
    freya {
        let! cartId = getRequestCookie "cartId"
        cartId |> Option.iter (fun cartId ->
            let ctx = Db.getContext()
            Db.upgradeCarts (cartId, userName) ctx
        )
    }

let authenticate =
    freya {
        let! user = checkCredentials
        match user with
        | Some user ->
            do! signIn {UserName = user.UserName; Role = user.Role}
            do! upgradeCarts user.UserName
            do! deleteResponseCookie "cartId"
        | _ ->
            ()
    }

let seeOther _ =
    freya {
        let! query = query 
        let returnPath = defaultArg (Map.tryFind "returnUrl" query) Uris.home

        do! Freya.setLensPartial 
                Response.Headers.Location_ 
                (Location.Parse (Uris.endpoint + returnPath))
        return { Data = [||]; Description = { Charset = None; Encodings = None; MediaType = None; Languages = None } }
    }

let doUnauthorized _ =
    freya {
        let! query = query 
        let returnPath = defaultArg (Map.tryFind "returnUrl" query) Uris.home

        return! writeHtml ("logon", {Logon.ReturnUrl = returnPath; Logon.ValidationMsg = "User name or password invalid. Try admin/admin"} )
    }

let pipe =
    freyaMachine {
        including common
        methodsSupported (Freya.init [POST])
        authorized (checkCredentials |> Freya.map Option.isSome)
        handleUnauthorized doUnauthorized
        doPost authenticate
        postRedirect (Freya.init true)
        handleSeeOther seeOther
    } |> FreyaMachine.toPipeline