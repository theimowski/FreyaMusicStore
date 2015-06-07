module FreyaMusicStore.App

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


let musicStore =
    freyaRouter {
        resource (UriTemplate.Parse "/") home
        resource (UriTemplate.Parse "/album/{id}") Album.album
        resource (UriTemplate.Parse "/genres") genres
        resource (UriTemplate.Parse "/genre/{name}") genre } |> FreyaRouter.toPipeline

type Project () =
    member __.Configuration () =
        OwinAppFunc.ofFreya (musicStore >?= StaticFiles.pipe)

[<EntryPoint>]
let run _ =
    printfn "starting..."
    let _ = WebApp.Start<Project> ("http://localhost:8080")
    printfn "app started. Press enter to quit"
    let _ = Console.ReadLine ()
    0