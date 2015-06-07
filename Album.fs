module FreyaMusicStore.Album

open System

open Arachne.Http

open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Extensions.Http

let id =
    freya {
        let! id = Freya.getLensPartial (Route.atom "id")
        match Int32.TryParse id.Value with
        | true, id -> return Some id
        | _ -> return None
    }

let get =
    freya {
        let! id = id
        let ctx = Db.getContext()
        let album = Db.getAlbumDetails id.Value ctx |> Option.get |> View.toAlbum
        return! write ("album", album)
    }

let isMalformed = 
    freya {
        let! id = id
        return Option.isNone id
    }

let doesExist = 
    freya {
        let! id = id
        let ctx = Db.getContext()
        return Db.getAlbumDetails id.Value ctx |> Option.isSome
    }

let pipe = 
    freyaMachine {
        including common
        malformed isMalformed
        exists doesExist
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk (fun _ -> get) } |> FreyaMachine.toPipeline