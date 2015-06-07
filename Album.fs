module FreyaMusicStore.Album

open System

open Arachne.Http

open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Lenses.Http

type Album = 
    { AlbumId : int
      Title : string
      AlbumArtUrl : string }

    static member fromDb (a : Db.Album) =
        { AlbumId = a.AlbumId
          Title = a.Title 
          AlbumArtUrl = a.AlbumArtUrl }

type AlbumDetails = 
    { AlbumId : int
      Title : string
      AlbumArtUrl : string
      Price : decimal
      Artist : string
      Genre : string }

    static member fromDb (a : Db.AlbumDetails) = 
        { AlbumId = a.AlbumId
          Title = a.Title
          AlbumArtUrl = a.AlbumArtUrl
          Price = a.Price
          Artist = a.Artist
          Genre = a.Genre }

let id =
    freya {
        let! id = Freya.getLensPartial (Route.atom "id")
        match Int32.TryParse id.Value with
        | true, id -> return Some id
        | _ -> return None
    }

let ok _ =
    freya {
        let! id = id
        let ctx = Db.getContext()
        let album = Db.getAlbumDetails id.Value ctx |> Option.get |> AlbumDetails.fromDb
        return! writeHtml ("album", album)
    }

let entity =
    freya {
        let! meth = Freya.getLens Request.meth
        return meth = GET
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

let delete =
    freya {
        let! id = id
        let ctx = Db.getContext()
        let album = Db.getAlbum id.Value ctx |> Option.get
        album.Delete()
        ctx.SubmitUpdates()
        return ()
    }

let pipe = 
    freyaMachine {
        including common
        malformed isMalformed
        exists doesExist
        methodsSupported ( freya { return [ GET; DELETE ] } ) 
        handleOk ok
        respondWithEntity entity
        doDelete delete } |> FreyaMachine.toPipeline