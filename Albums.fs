module FreyaMusicStore.Albums

open System
open System.IO

open Arachne.Http

open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Lenses.Http

type Albums = 
    { Albums : Album.AlbumDetails [] }

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

let readStream (x: Stream) =
    use reader = new StreamReader (x)
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
    match Decimal.TryParse s with
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

let isMalformed = 
    freya {
        let! meth = Freya.getLens Request.meth
        match meth with 
        | POST -> let! album = readAlbum in return album.IsNone
        | _ -> return false
    }

let createAlbum =
    freya {
        let! album = readAlbum
        let album = album.Value
        let ctx = Db.getContext()
        let album = 
            Db.createAlbum(
                album.ArtistId, 
                album.GenreId, 
                album.Price, 
                album.Title,
                album.AlbumArtUrl) 
                ctx
        let details = Db.getAlbumDetails album.AlbumId ctx 
        return Album.AlbumDetails.fromDb details.Value
    } |> Freya.memo

let post =
    freya {
        let! _ = createAlbum
        return ()
    }

let onCreated _ =
    freya {
        let! album = createAlbum
        do! Freya.setLensPartial 
                Response.Headers.location 
                (Location.Parse (sprintf "http://localhost:8080/album/%d" album.AlbumId))
        return! writeHtml ("album", album)
    }

let get =
    freya {
        let ctx = Db.getContext()
        let albums = Db.getAlbumsDetails ctx |> Array.map Album.AlbumDetails.fromDb
        return! writeHtml ("albums", { Albums = albums } )
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET; POST ] } ) 
        malformed isMalformed
        handleOk (fun _ -> get)
        doPost post 
        handleCreated onCreated} |> FreyaMachine.toPipeline