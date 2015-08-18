module FreyaMusicStore.Albums

open System
open System.Globalization
open System.IO

open Arachne.Http

open Chiron

open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Lenses.Http

open Microsoft.AspNet.Identity

let isMalformed = 
    freya {
        let! meth = Freya.getLens Request.Method_
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
                Response.Headers.Location_ 
                (Location.Parse (String.Format(Uris.endpoint + Uris.album, album.AlbumId)))
        return! writeHtml ("album", album)
    }

let fetch : Freya<Db.DbContext -> _> = 
    Db.getAlbumsDetails >> Array.map Album.AlbumDetails.fromDb |> Freya.init

let pipe = 
    freyaMachine {
        methodsSupported ( Freya.init [ GET; POST ] ) 
        malformed isMalformed
        including (protectAuthenticated [ GET; POST ] (Freya.init Uris.albums))
        including (protectAdmin [ GET; POST ])
        including (res fetch "albums")
        doPost post 
        handleCreated onCreated} |> FreyaMachine.toPipeline