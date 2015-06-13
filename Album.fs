module FreyaMusicStore.Album

open System
open System.Globalization

open Arachne.Http

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



// TODO : Extract


type NewAlbum =
    { Title : string
      ArtistId : int
      GenreId : int
      Price : decimal
      AlbumArtUrl : string }

type MaybeBuilder() =
    member __.Bind(m, f) = Option.bind f m
    member __.Return(x) = Some x

let maybe = MaybeBuilder()

let readStream (x: IO.Stream) =
    use reader = new IO.StreamReader (x)
    reader.ReadToEndAsync()
    |> Async.AwaitTask

let readBody () =
    freya {
        let! body = Freya.getLens Request.body
        return! Freya.fromAsync readStream body } |> Freya.memo

let kv (s: string) =
    match s.Split([| '=' |]) with
    | [|k;v|] -> Some(k,v)
    | _ -> None

let both f (x,y) = f x, f y
let decode = System.Net.WebUtility.UrlDecode

let form () =
    freya {
        let! body = readBody ()
        return body.Split([| '&' |]) |> Array.choose kv |> Array.map (both decode) |> Map.ofArray } |> Freya.memo

let mInt (s: string) = 
    match Int32.TryParse s with
    | true, x -> Some x
    | _ -> None

let mDec (s: string) = 
    match Decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) with
    | true, x -> Some x
    | _ -> None


let readAlbum =
    freya {
        let! form = form ()
        let album =
            maybe {
                let! title = form |> Map.tryFind "Title"
                let! artistId = form |> Map.tryFind "ArtistId" |> Option.bind mInt
                let! genreId = form |> Map.tryFind "GenreId" |> Option.bind mInt
                let! price = form |> Map.tryFind "Price" |> Option.bind mDec
                let! albumArtUrl = form |> Map.tryFind "ArtUrl"
                return 
                    { Title = title
                      ArtistId = artistId
                      GenreId = genreId
                      Price = price
                      AlbumArtUrl = albumArtUrl }
            }
        return album
    } |> Freya.memo


/// 

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
        | _ -> return! (fun freyaState -> 
            let ctx = Microsoft.Owin.OwinContext(freyaState.Environment)
            let result = ctx.Authentication.AuthenticateAsync(DefaultAuthenticationTypes.ApplicationCookie) |> Async.AwaitTask |> Async.RunSynchronously
            async.Return (result <> null, freyaState)
        )

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
        exists doesExist
        methodsSupported ( freya { return [ GET; PUT; DELETE ] } ) 
        handleOk ok
        respondWithEntity entity
        created (Freya.init false)
        doDelete delete
        doPut put } |> FreyaMachine.toPipeline