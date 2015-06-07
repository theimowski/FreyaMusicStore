module FreyaMusicStore.Program

open System
open System.IO
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


let getFileInfo (path: string) =
    let filePath = path.Trim ([| '/' |])
    let fileInfo = FileInfo (filePath)

    fileInfo

let readFile (file: FileInfo) =
    File.ReadAllBytes (file.FullName)

// Response

let fileTypes =
    [ ".jpg", MediaType.Parse "image/jpeg"
      ".png", MediaType.Parse "image/png"
      ".gif", MediaType.Parse "image/gif"
      ".css", MediaType.Css] |> Map.ofList

let represent (n: Specification) x =
    { Description =
        { Charset = None
          Encodings = None
          MediaType = Some ((function | Negotiated x -> List.head x 
                                      | _ -> MediaType.Text) n.MediaTypes)
          Languages= None }
      Data = x }

// Freya

let path =
    Freya.memo (Freya.getLens Request.path)

let fileInfo =
    Freya.memo (getFileInfo <!> path)

let file =
    Freya.memo (readFile <!> fileInfo)

let fileType =
    Freya.memo ((function | (x: FileInfo) when x.Exists -> [ Map.find x.Extension fileTypes ]
                          | _ -> [ MediaType.Text ]) <!> fileInfo)

// Machine

let existsDecision =
    (fun (x: FileInfo) -> x.Exists) <!> fileInfo

let fileHandler n =
    represent n <!> file

let lastModifiedConfiguration =
    (fun (x: FileInfo) -> x.LastWriteTimeUtc) <!> fileInfo

let mediaTypesConfiguration =
    fileType

// Resources

let files : FreyaPipeline =
    freyaMachine {
        using http
        methodsSupported (Freya.init [ GET; HEAD ])
        mediaTypesSupported mediaTypesConfiguration
        exists existsDecision
        handleOk fileHandler } |> FreyaMachine.toPipeline




let musicStore =
    freyaRouter {
        resource (UriTemplate.Parse "/") home
        resource (UriTemplate.Parse "/album/{id}") Album.album
        resource (UriTemplate.Parse "/genres") genres
        resource (UriTemplate.Parse "/genre/{name}") genre } |> FreyaRouter.toPipeline

type Project () =
    member __.Configuration () =
        OwinAppFunc.ofFreya (musicStore >?= files)

[<EntryPoint>]
let run _ =
    printfn "starting..."
    let _ = WebApp.Start<Project> ("http://localhost:8080")
    printfn "app started. Press enter to quit"
    let _ = Console.ReadLine ()
    0