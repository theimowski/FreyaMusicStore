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



let ok _ =
    freya {
        return! writeHtml ("logon", {Logon.ReturnUrl = Uris.home; Logon.ValidationMsg = ""} )
    }

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


let doUnauthorized _ =
    freya {
        let! query = query 
        let returnPath = defaultArg (Map.tryFind "returnUrl" query) Uris.home

        return! writeHtml ("logon", {Logon.ReturnUrl = returnPath; Logon.ValidationMsg = "User name or password invalid. Try admin/admin"} )
    }

let isAuthorized = 
    freya {
        let! meth = Freya.getLens Request.meth
        if meth = POST then
            let! user = checkCredentials
            return user.IsSome
        else
            return true
    }

let upgradeCarts userName = 
    freya {
        let! cartId = getRequestCookie "cartId"
        cartId |> Option.iter (fun cartId ->
            let ctx = Db.getContext()
            Db.upgradeCarts (cartId, userName) ctx
        )
    }

let post =
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

let redirect = 
    freya {
        let! user = checkCredentials
        return user.IsSome
    }

let delete = 
    freya {
        let! loggedOn = isAuthenticated
        if loggedOn then
            do! signOut
    }

let doSeeOther _ =
    freya {
        let! query = query 
        let returnPath = defaultArg (Map.tryFind "returnUrl" query) Uris.home

        do! Freya.setLensPartial 
                Response.Headers.location 
                (Location.Parse (String.Format("http://localhost:8080{0}", returnPath)))
        return! writeHtml ("logon", {Logon.ReturnUrl = Uris.home; Logon.ValidationMsg = ""} )
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET; POST; DELETE ] } ) 
        authorized isAuthorized
        handleUnauthorized doUnauthorized
        postRedirect redirect
        handleSeeOther doSeeOther
        handleOk ok
        doPost post 
        doDelete delete } |> FreyaMachine.toPipeline