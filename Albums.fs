module FreyaMusicStore.Albums

open System

open Arachne.Http

open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Extensions.Http

type Albums = 
    { Albums : Album.AlbumDetails [] }

let get =
    freya {
        let ctx = Db.getContext()
        let albums = Db.getAlbumsDetails ctx |> Array.map Album.AlbumDetails.fromDb
        return! writeHtml ("albums", { Albums = albums } )
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk (fun _ -> get) } |> FreyaMachine.toPipeline