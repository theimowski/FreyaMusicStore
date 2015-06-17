module FreyaMusicStore.Checkout

open Arachne.Http

open Freya.Core
open Freya.Lenses.Http
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Router

let ok _ =
    freya {
        return! writeHtml ("checkout", () )
    }

let post =
    freya {
        let ctx = Db.getContext()
        let! a = getAuth
        let userName = a.Value.UserName
        Db.placeOrder userName ctx
    }

let seeOther _ =
    freya {
        return! writeHtml ("checkoutComplete", () )
    }

let onUnauthorized _ =
    freya {
        return! writeHtml ("logon", {Logon.Logon.ReturnUrl = Uris.checkout; Logon.Logon.ValidationMsg = ""})
    }

let pipe = 
    freyaMachine {
        including common
        authorized isAuthenticated
        handleUnauthorized onUnauthorized
        methodsSupported ( freya { return [ GET; POST ] } ) 
        postRedirect (Freya.init true)
        handleSeeOther seeOther
        handleOk ok
        doPost post } |> FreyaMachine.toPipeline