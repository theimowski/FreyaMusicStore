module FreyaMusicStore.Register

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

type Register = {
    ValidationMsg : string
}

let ok _ =
    freya {
        return! writeHtml ("register", {ValidationMsg = ""} )
    }

let post =
    freya {
        let! form = form
        let user = maybe {
            let! username = Map.tryFind "UserName" form
            let! password = Map.tryFind "Password" form
            let! email = Map.tryFind "Email" form
            let ctx = Db.getContext()
            return Db.newUser(username, passHash password, email) ctx
        }
        return ()
    }

let redirect = Freya.init true

let doSeeOther _ =
    freya {
        do! Freya.setLensPartial 
                Response.Headers.location 
                (Location.Parse (String.Format("http://localhost:8080{0}", Uris.home)))
        return! writeHtml ("register", {ValidationMsg = ""} )
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET; POST ] } ) 
        postRedirect redirect
        handleSeeOther doSeeOther
        handleOk ok
        doPost post } |> FreyaMachine.toPipeline