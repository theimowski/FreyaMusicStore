module FreyaMusicStore.App

open System
open System.IO

open Arachne.Uri.Template

open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Router
open Freya.Router


let musicStore =
    freyaRouter {
        resource (UriTemplate.Parse Uris.home) Home.pipe
        resource (UriTemplate.Parse Uris.albums) Albums.pipe
        resource (UriTemplate.Parse Uris.newAlbum) NewAlbum.pipe
        resource (UriTemplate.Parse Uris.album) Album.pipe
        resource (UriTemplate.Parse Uris.editAlbum) EditAlbum.pipe
        resource (UriTemplate.Parse Uris.genres) Genres.pipe
        resource (UriTemplate.Parse Uris.genre) Genre.pipe } |> FreyaRouter.toPipeline

type Project () =
    member __.Configuration () =
        OwinAppFunc.ofFreya (musicStore >?= StaticFiles.pipe)

open Microsoft.Owin.Hosting
open RazorEngine.Templating

[<EntryPoint>]
let run _ =
    printfn "register layout"
    RazorEngine.Engine.Razor.Compile(File.ReadAllText("index.cshtml"), "mylayout")
    printfn "starting app..."
    let _ = WebApp.Start<Project> ("http://localhost:8080")
    printfn "app started. Press enter to quit"
    let _ = Console.ReadLine ()
    0