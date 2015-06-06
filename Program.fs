open System
open System.Text

open Arachne.Http
open Arachne.Language
open Arachne.Uri.Template

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Extensions.Http.Cors
open Freya.Machine.Router
open Freya.Lenses.Http
open Freya.Router

open Microsoft.Owin.Hosting

let inline write (text : string) =
    freya {
        return {
            Data = Encoding.UTF8.GetBytes text
            Description =
                { Charset = Some Charset.Utf8
                  Encodings = None
                  MediaType = Some MediaType.Html
                  Languages = Some [ LanguageTag.Parse "en" ] } } }

let commonMediaTypes =
    freya {
        return [
            MediaType.Html ] }

let common =
    freyaMachine {
        using http
        mediaTypesSupported commonMediaTypes }

let getAlbum =
    freya {
        let! id = Freya.getLensPartial (Route.atom "id")
        return! write (sprintf "Album no %d" (Int32.Parse id.Value))
    }

let albumMalformed = 
    freya {
        let! id = Freya.getLensPartial (Route.atom "id")
        match Int32.TryParse id.Value with
        | true, _ -> return false
        | _ -> return true
    }
    
let getGenre =
    freya {
        let! name = Freya.getLensPartial (Route.atom "name")
        return! write (sprintf "Genre: %s" name.Value)
    }

let home =
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } )
        handleOk (fun _ -> freya { return! write "Hello World!" } ) } |> FreyaMachine.toPipeline

let album = 
    freyaMachine {
        including common
        malformed albumMalformed
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk (fun _ -> getAlbum) } |> FreyaMachine.toPipeline

let albums = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk (fun _ -> freya { return! write "many albums!" } ) } |> FreyaMachine.toPipeline

let genre =
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } )
        handleOk (fun _ -> getGenre ) } |> FreyaMachine.toPipeline

let musicStore =
    freyaRouter {
        resource (UriTemplate.Parse "/") home
        resource (UriTemplate.Parse "/album/{id}") album
        resource (UriTemplate.Parse "/albums") albums
        resource (UriTemplate.Parse "/genre/{name}") genre } |> FreyaRouter.toPipeline

type Project () =
    member __.Configuration () =
        OwinAppFunc.ofFreya musicStore

[<EntryPoint>]
let run _ =
    let _ = WebApp.Start<Project> ("http://localhost:8080")
    let _ = Console.ReadLine ()
    0