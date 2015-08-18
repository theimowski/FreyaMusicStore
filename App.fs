module FreyaMusicStore.App

open System
open System.IO

open Arachne.Http
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

type Project () =
    member __.Configuration (appBuilder : Owin.IAppBuilder) =
        
        let musicStore =
            freyaRouter {
                resource (UriTemplate.Parse Uris.home) Home.pipe
                resource (UriTemplate.Parse Uris.albums) Albums.pipe
                resource (UriTemplate.Parse Uris.album) Album.pipe
                resource (UriTemplate.Parse Uris.genres) Genres.pipe
                resource (UriTemplate.Parse Uris.genre) Genre.pipe
        
                resource (UriTemplate.Parse Uris.newAlbum) NewAlbum.pipe
                resource (UriTemplate.Parse Uris.editAlbum) EditAlbum.pipe
                resource (UriTemplate.Parse Uris.logon) Logon.pipe
                resource (UriTemplate.Parse Uris.register) Register.pipe 
        
                resource (UriTemplate.Parse Uris.sessions) Sessions.pipe
                resource (UriTemplate.Parse Uris.session) Session.pipe
                resource (UriTemplate.Parse Uris.users) Users.pipe
        
                resource (UriTemplate.Parse Uris.cart) Cart.pipe
                resource (UriTemplate.Parse Uris.checkout) Checkout.pipe } |> FreyaRouter.toPipeline

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
    let config = RazorEngine.Configuration.TemplateServiceConfiguration()
    config.CachingProvider <- new DefaultCachingProvider(Action<_>(ignore))
    let service = RazorEngineService.Create(config)
    RazorEngine.Engine.Razor <- service
    DirectoryInfo(".").EnumerateFiles("*.cshtml") |> Seq.iter (fun fi ->
        let name = Path.GetFileNameWithoutExtension(fi.Name)
        let contents = File.ReadAllText(fi.Name)
        RazorEngine.Engine.Razor.Compile(contents, name)
    )
    printfn "starting app..."
    let _ = WebApp.Start<Project> (Uris.endpoint)
    printfn "app started. Press enter to quit"
    let _ = Console.ReadLine ()
    0
