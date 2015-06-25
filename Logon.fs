module FreyaMusicStore.Logon

open Arachne.Http
open Arachne.Uri.Template

open Freya.Core
open Freya.Core.Operators
open Freya.Lenses.Http
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Router
open Freya.Router

let ok _ =
    freya {
        return! writeHtml ("logon", {Logon.ReturnUrl = Uris.home; Logon.ValidationMsg = ""} )
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( Freya.init [ GET ] ) 
        handleOk ok } |> FreyaMachine.toPipeline