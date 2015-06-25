module FreyaMusicStore.Users

open System

open Arachne.Http

open Freya.Core
open Freya.Lenses.Http
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Router

let user =  
    freya {
        let! form = form
        return maybe {
            let! username = Map.tryFind "UserName" form
            let! password = Map.tryFind "Password" form
            let! email = Map.tryFind "Email" form
            return username,password,email
         }
    }

let createUser =
    freya {
        let! username,password,email = user |> Freya.map Option.get
        let ctx = Db.getContext()
        let _ = Db.newUser(username, passHash password, email) ctx
        return ()
    }

let seeOther _ =
    freya {
        do! Freya.setLensPartial 
                Response.Headers.location 
                (Location.Parse (String.Format("http://localhost:8080{0}", Uris.home)))
        return { Data = [||]; Description = { Charset = None; Encodings = None; MediaType = None; Languages = None } }
    }

let pipe =
    freyaMachine {
        including common
        methodsSupported (Freya.init [POST])
        malformed (user |> Freya.map Option.isNone)
        doPost createUser
        postRedirect (Freya.init true)
        handleSeeOther seeOther
    } |> FreyaMachine.toPipeline