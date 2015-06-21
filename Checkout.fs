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

let pipe = 
    freyaMachine {
        methodsSupported ( freya { return [ GET; POST ] } ) 
        including common
        including (protectAuthenticated [ GET; POST ] (Freya.init Uris.checkout))
        postRedirect (Freya.init true)
        handleSeeOther seeOther
        handleOk ok
        doPost post } |> FreyaMachine.toPipeline