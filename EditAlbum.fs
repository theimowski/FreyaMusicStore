module FreyaMusicStore.EditAlbum

open System

open Arachne.Http
open Arachne.Uri.Template

open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Router
open Freya.Router

type IdAndName = 
    { Id : int
      Name : string }

type EditAlbum = 
    { Album : Album.Album
      Genres : IdAndName [] 
      Artists : IdAndName [] }

let id =
    freya {
        let! id = Freya.getLensPartial (Route.Atom_ "0")
        match Int32.TryParse id.Value with
        | true, id -> return Some id
        | _ -> return None
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
        return Db.getAlbum id.Value ctx |> Option.isSome
    }

let ok _ =
    freya {
        let! id = id
        let ctx = Db.getContext()
        let album = Db.getAlbum id.Value ctx |> Option.get |> Album.Album.fromDb
        let genres = Db.getGenres ctx |> Array.map (fun g -> { Id = g.GenreId; Name = g.Name })
        let artists = Db.getArtists ctx |> Array.map (fun a -> { Id = a.ArtistId; Name = a.Name })
        return! writeHtml ("editAlbum", { Album = album; Genres = genres; Artists = artists } )
    }

let uri =
    freya {
        let! id = id
        return String.Format(Uris.editAlbum, id.Value)
    }

let pipe = 
    freyaMachine {
        methodsSupported ( freya { return [ GET ] } ) 
        malformed isMalformed
        including common
        including (protectAuthenticated [ GET ] uri)
        including (protectAdmin [ GET ])
        exists doesExist
        handleOk ok } |> FreyaMachine.toPipeline