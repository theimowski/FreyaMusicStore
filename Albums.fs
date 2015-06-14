module FreyaMusicStore.Albums

open System
open System.Globalization
open System.IO

open Arachne.Http

open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Lenses.Http

open Microsoft.AspNet.Identity

type Albums = 
    { Albums : Album.AlbumDetails [] }

let isMalformed = 
    freya {
        let! meth = Freya.getLens Request.meth
        match meth with 
        | POST -> let! album = readAlbum in return album.IsNone
        | _ -> return false
    }

let onUnauthorized _ =
    freya {
        return! writeHtml ("logon", {Logon.Logon.ReturnUrl = Uris.albums; Logon.Logon.ValidationMsg = ""})
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
                (Location.Parse (String.Format(String.Format("http://localhost:8080{0}", Uris.album), album.AlbumId)))
        return! writeHtml ("album", album)
    }

let onForbidden _ =
    freya {
        return! writeHtml ("forbidden", ())
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
        authorized isLoggedOn
        allowed isAdmin
        handleOk (fun _ -> get)
        handleUnauthorized onUnauthorized
        handleForbidden onForbidden
        doPost post 
        handleCreated onCreated} |> FreyaMachine.toPipeline