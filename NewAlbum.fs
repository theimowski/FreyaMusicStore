module FreyaMusicStore.NewAlbum

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

type CreateAlbum = 
    { Genres : IdAndName [] 
      Artists : IdAndName [] }

let ok _ =
    freya {
        let ctx = Db.getContext()
        let genres = Db.getGenres ctx |> Array.map (fun g -> { Id = g.GenreId; Name = g.Name })
        let artists = Db.getArtists ctx |> Array.map (fun a -> { Id = a.ArtistId; Name = a.Name })
        return! writeHtml ("newAlbum", { Genres = genres; Artists = artists } )
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk ok } |> FreyaMachine.toPipeline