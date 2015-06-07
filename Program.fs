open System
open System.Text
open System.Reflection

open Arachne.Http
open Arachne.Http.Cors
open Arachne.Language
open Arachne.Uri.Template

open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Extensions.Http.Cors
open Freya.Machine.Router
open Freya.Lenses.Http
open Freya.Router

open Microsoft.Owin.Hosting

type Container = {
    Greeting : string
}

open RazorEngine.Templating

try 
    let template = IO.File.ReadAllText("index.cshtml")
    RazorEngine.Engine.Razor.Compile(template, "mylayout")
with e ->
    ()

let inline write (view : string, model : 'a) =
    freya {
        let contents = IO.File.ReadAllText(view + ".cshtml")
        let result =
            RazorEngine.Engine.Razor.RunCompile(contents, "templateKey", typeof<'a>, model)

        return {
            Data = Encoding.UTF8.GetBytes result
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

let albumId =
    freya {
        let! id = Freya.getLensPartial (Route.atom "id")
        match Int32.TryParse id.Value with
        | true, id -> return Some id
        | _ -> return None
    }

let getAlbum =
    freya {
        let! id = albumId
        let ctx = Db.getContext()
        let album = Db.getAlbumDetails id.Value ctx |> Option.get |> View.toAlbum
        return! write ("album", album)
    }

let albumMalformed = 
    freya {
        let! id = albumId
        return Option.isNone id
    }

let albumExists = 
    freya {
        let! id = albumId
        let ctx = Db.getContext()
        return Db.getAlbumDetails id.Value ctx |> Option.isSome
    }
    
let getGenre =
    freya {
        let! name = Freya.getLensPartial (Route.atom "name")
        return! write ("home", {Greeting = sprintf "Genre: %s" name.Value})
    }

let home =
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } )
        handleOk (fun _ -> freya { return!  write ("home", {Greeting = "Hello World!" } ) } ) } |> FreyaMachine.toPipeline

let album = 
    freyaMachine {
        including common
        malformed albumMalformed
        exists albumExists
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk (fun _ -> getAlbum) } |> FreyaMachine.toPipeline

let genres = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk (fun _ -> freya { return!  write ("home", {Greeting =  "many albums!" } ) } ) } |> FreyaMachine.toPipeline

let genre =
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } )
        handleOk (fun _ -> getGenre ) } |> FreyaMachine.toPipeline

let defaults =
    freyaMachine {
        using http
        using httpCors

        corsHeadersSupported (Freya.init [ "accept"; "content-type" ])
        corsMethodsSupported (Freya.init [ GET; OPTIONS ])
        corsOriginsSupported (Freya.init AccessControlAllowOriginRange.Any)

        charsetsSupported (Freya.init [ Charset.Utf8 ])
        languagesSupported (Freya.init [ LanguageTag.Parse "en" ]) }

let private resourceAssembly =
    Assembly.GetExecutingAssembly ()

let resource key =
    use stream = IO.File.OpenRead(key)
    use reader = new IO.StreamReader (stream)

    Encoding.UTF8.GetBytes (reader.ReadToEnd ())

let private cssContent =
    resource "Site.css"

let private placeholderContent =
    resource "placeholder.gif"

let private firstNegotiatedOrElse def =
    function | Negotiated (x :: _) -> x
             | _ -> def

let represent n x =
    { Description =
        { Charset = Some (n.Charsets |> firstNegotiatedOrElse Charset.Utf8)
          Encodings = None
          MediaType = Some (n.MediaTypes |> firstNegotiatedOrElse MediaType.Text)
          Languages = Some [ n.Languages |> firstNegotiatedOrElse (LanguageTag.Parse "en") ] }
      Data = x }


let private getContent content n =
    represent n <!> Freya.init content

let private getCss =
    getContent cssContent

let private getPlaceholder = 
    getContent placeholderContent

let private css =
    freyaMachine {
        including defaults
        mediaTypesSupported (Freya.init [ MediaType.Css ])
        handleOk getCss } |> FreyaMachine.toPipeline

let private placeholder =
    freyaMachine {
        including defaults
        mediaTypesSupported (Freya.init [ MediaType.Parse "image/gif" ])
        handleOk getPlaceholder } |> FreyaMachine.toPipeline

let musicStore =
    freyaRouter {
        resource (UriTemplate.Parse "/Site.css") css
        resource (UriTemplate.Parse "/placeholder.gif") placeholder
        resource (UriTemplate.Parse "/") home
        resource (UriTemplate.Parse "/album/{id}") album
        resource (UriTemplate.Parse "/genres") genres
        resource (UriTemplate.Parse "/genre/{name}") genre } |> FreyaRouter.toPipeline

type Project () =
    member __.Configuration () =
        OwinAppFunc.ofFreya musicStore

[<EntryPoint>]
let run _ =
    printfn "starting..."
    let _ = WebApp.Start<Project> ("http://localhost:8080")
    printfn "app started. Press enter to quit"
    let _ = Console.ReadLine ()
    0