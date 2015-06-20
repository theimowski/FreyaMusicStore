module FreyaMusicStore.Album

open System
open System.Globalization

open Arachne.Http

open Chiron
open Chiron.Operators

open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Lenses.Http

open Microsoft.AspNet.Identity

type Album = 
    { AlbumId : int
      Title : string
      ArtistId : int
      GenreId : int
      Price : decimal
      AlbumArtUrl : string }

    static member fromDb (a : Db.Album) =
        { AlbumId = a.AlbumId
          Title = a.Title 
          ArtistId = a.ArtistId
          GenreId = a.GenreId
          Price = a.Price
          AlbumArtUrl = a.AlbumArtUrl }

    static member ToJson (x: Album) =
            Json.write "albumId" x.AlbumId
         *> Json.write "title" x.Title
         *> Json.write "artistId" x.ArtistId
         *> Json.write "genreId" x.GenreId
         *> Json.write "price" x.Price
         *> Json.write "albumArtUrl" x.AlbumArtUrl

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

    static member ToJson (x: AlbumDetails) =
            Json.write "albumId" x.AlbumId
         *> Json.write "title" x.Title
         *> Json.write "albumArtUrl" x.AlbumArtUrl
         *> Json.write "price" x.Price
         *> Json.write "artist" x.Artist
         *> Json.write "genre" x.Genre

let id =
    freya {
        let! id = Freya.getLensPartial (Route.atom "0")
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
        return meth = GET || meth = PUT
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

    
let editAlbum = 
    freya {
        let! id = id
        let id = id.Value
        let! album = readAlbum
        let album = album.Value

        let ctx = Db.getContext()
        let a = Db.getAlbum id ctx |> Option.get

        a.Title <- album.Title
        a.ArtistId <- album.ArtistId
        a.GenreId <- album.GenreId
        a.Price <- album.Price
        a.AlbumArtUrl <- album.AlbumArtUrl

        ctx.SubmitUpdates()
        let details = Db.getAlbumDetails a.AlbumId ctx 
        return AlbumDetails.fromDb details.Value
    } |> Freya.memo

let isAuthorized = 
    freya {
        let! meth = Freya.getLens Request.meth
        match meth with 
        | GET -> return true
        | _ -> return! isAuthenticated
    }

let isAllowed = 
    freya {
        let! meth = Freya.getLens Request.meth
        match meth with 
        | GET -> return true
        | _ -> return! isAdmin
    }

let onUnauthorized _ =
    freya {
        return! writeHtml ("logon", {Logon.Logon.ReturnUrl = Uris.albums; Logon.Logon.ValidationMsg = ""})
    }

let onForbidden _ =
    freya {
        return! writeHtml ("forbidden", ())
    }

let put = 
    freya {
        let! _ = editAlbum
        return ()
    }

let pipe = 
    freyaMachine {
        including common
        malformed isMalformed
        authorized isAuthorized
        allowed isAllowed
        handleForbidden onForbidden
        handleUnauthorized onUnauthorized
        exists doesExist
        methodsSupported ( freya { return [ GET; PUT; DELETE ] } ) 
        handleOk ok
        respondWithEntity entity
        created (Freya.init false)
        doDelete delete
        doPut put } |> FreyaMachine.toPipeline