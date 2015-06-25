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

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( Freya.init [GET] ) 
        handleOk ok} |> FreyaMachine.toPipeline