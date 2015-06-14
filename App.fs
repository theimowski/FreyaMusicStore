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

open Microsoft.AspNet.Identity
open Microsoft.Owin.Security
open Microsoft.Owin.Security.Cookies

let musicStore =
    freyaRouter {
        resource (UriTemplate.Parse Uris.home) Home.pipe
        resource (UriTemplate.Parse Uris.albums) Albums.pipe
        resource (UriTemplate.Parse Uris.newAlbum) NewAlbum.pipe
        resource (UriTemplate.Parse Uris.album) Album.pipe
        resource (UriTemplate.Parse Uris.editAlbum) EditAlbum.pipe
        resource (UriTemplate.Parse Uris.genres) Genres.pipe
        resource (UriTemplate.Parse Uris.genre) Genre.pipe
        
        resource (UriTemplate.Parse Uris.logon) Logon.pipe
        resource (UriTemplate.Parse Uris.register) Register.pipe } |> FreyaRouter.toPipeline

type Project () =
    member __.Configuration (appBuilder : Owin.IAppBuilder) =
        

        appBuilder.Use(
            typeof<CookieAuthenticationMiddleware>, 
            appBuilder, 
            CookieAuthenticationOptions(
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                AuthenticationMode = AuthenticationMode.Active))  |> ignore

        Microsoft.Owin.Extensions.IntegratedPipelineExtensions.UseStageMarker(appBuilder, Owin.PipelineStage.Authenticate) |> ignore
        
        appBuilder.Use( OwinMidFunc.ofFreya (musicStore >?= StaticFiles.pipe) ) |> ignore
        ()
        

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