module FreyaMusicStore.Logon

open Arachne.Http
open Arachne.Uri.Template

open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Router
open Freya.Router

let ok _ =
    freya {
        return! writeHtml ("logon", () )
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk ok } |> FreyaMachine.toPipeline