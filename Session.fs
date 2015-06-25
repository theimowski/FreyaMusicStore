module FreyaMusicStore.Session

open Arachne.Http
open Arachne.Uri.Template

open Freya.Core
open Freya.Core.Operators
open Freya.Lenses.Http
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Router
open Freya.Router

let sessionId = Freya.getLensPartial (Route.atom "0") |> Freya.map Option.get

let sessionOwner =
    freya {
        let! sessionId = sessionId
        let! auth = getAuth
        match auth with
        | Some auth when auth.UserName = sessionId ->
            return true
        | _ ->
            return false
    }

let pipe =
    freyaMachine {
        including common
        methodsSupported (Freya.init [DELETE])
        authorized isAuthenticated
        allowed sessionOwner
        doDelete signOut
    } |> FreyaMachine.toPipeline