[<AutoOpen>]
module FreyaMusicStore.Common

open System

open Freya.Core

module Async =
    let map f x = async { let! v = x in return f v }

module Option =
    let fromNullable = function | null -> None | x -> Some x

module Tuple =
    let map f (x,y) = f x, f y

[<AutoOpen>]
module Utils =
    
    let passHash (pass: string) =
        use sha = Security.Cryptography.SHA256.Create()
        Text.Encoding.UTF8.GetBytes(pass)
        |> sha.ComputeHash
        |> Array.map (fun b -> b.ToString("x2"))
        |> String.concat ""

    type MaybeBuilder() =
        member __.Bind(m, f) = Option.bind f m
        member __.Return(x) = Some x
        member __.ReturnFrom(x) = x

    let maybe = MaybeBuilder()

[<AutoOpen>]
module Katana = 
    open System.Security.Claims

    open Microsoft.AspNet.Identity
    open Microsoft.Owin
    open Microsoft.Owin.Security

    let getEnv: Freya<FreyaEnvironment> = (fun freyaState -> async { return freyaState.Environment, freyaState })

    let authResult (ctx: OwinContext) =
        ctx.Authentication.AuthenticateAsync(DefaultAuthenticationTypes.ApplicationCookie) 
        |> Async.AwaitTask 
        |> Async.map Option.fromNullable

    let hasAdminRole (authResult: AuthenticateResult) =
        authResult.Identity.HasClaim(Predicate(fun claim -> claim.Type = ClaimTypes.Role && claim.Value = "admin"))

    let userName (authResult: AuthenticateResult) =
        authResult.Identity.Claims |> Seq.find (fun c -> c.Type = ClaimTypes.Name) |> fun x -> x.Value

    let owinContext = getEnv |> Freya.map (fun env -> OwinContext(env))

    let getAuthResult = owinContext |> Freya.bind (Freya.fromAsync authResult) |> Freya.memo

[<AutoOpen>]
module Auth =
    
    let isLoggedOn = getAuthResult |> Freya.map Option.isSome
    
    let isAdmin = getAuthResult |> Freya.map (Option.exists hasAdminRole)

[<AutoOpen>]
module Cookies =

    let setResponseCookie key value = owinContext |> Freya.map (fun ctx -> ctx.Response.Cookies.Append(key, value))

    let getRequestCookie key = owinContext |> Freya.map (fun ctx -> ctx.Request.Cookies.[key] |> Option.fromNullable)

    let deleteResponseCookie key = owinContext |> Freya.map (fun ctx -> ctx.Response.Cookies.Delete(key))

[<AutoOpen>]
module Parsing =
    open System.IO
    open System.Globalization

    open Freya.Lenses.Http

    let mInt (s: string) = 
        match Int32.TryParse s with
        | true, x -> Some x
        | _ -> None

    let mDec (s: string) = 
        match Decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) with
        | true, x -> Some x
        | _ -> None

    let keyValue (s: string) =
        match s.Split([| '=' |]) with
        | [|k;v|] -> Some(k,v)
        | _ -> None

    let decode = System.Net.WebUtility.UrlDecode

    let toMap (s: string) =
        s.Split([| '&' |]) 
        |> Array.choose keyValue 
        |> Array.map (Tuple.map decode) 
        |> Map.ofArray

    let readStream (x: Stream) =
        use reader = new StreamReader (x)
        reader.ReadToEndAsync()
        |> Async.AwaitTask

    let query = 
        Freya.getLens Request.query 
        |> Freya.map toMap 
        |> Freya.memo

    let form = 
        Freya.getLens Request.body 
        |> Freya.bind (Freya.fromAsync readStream) 
        |> Freya.map toMap 
        |> Freya.memo

    type AlbumForm =
        { Title : string
          ArtistId : int
          GenreId : int
          Price : decimal
          AlbumArtUrl : string }

    let readAlbum =
        freya {
            let! form = form
            let album =
                maybe {
                    let! title = form |> Map.tryFind "Title"
                    let! artistId = form |> Map.tryFind "ArtistId" |> Option.bind mInt
                    let! genreId = form |> Map.tryFind "GenreId" |> Option.bind mInt
                    let! price = form |> Map.tryFind "Price" |> Option.bind mDec
                    let! albumArtUrl = form |> Map.tryFind "ArtUrl"
                    return 
                        { Title = title
                          ArtistId = artistId
                          GenreId = genreId
                          Price = price
                          AlbumArtUrl = albumArtUrl }
                }
            return album
        } |> Freya.memo

[<AutoOpen>]
module Machine = 
    open Arachne.Http

    open Freya.Machine
    open Freya.Machine.Extensions.Http

    let common =
        freyaMachine {
            using http
            mediaTypesSupported (Freya.init [MediaType.Html]) }


[<AutoOpen>]
module Html =
    open System.IO
    open System.Text

    open Arachne.Http
    open Arachne.Language
    
    open Freya.Machine.Extensions.Http

    open RazorEngine.Templating

    let inline writeHtml (view : string, model : 'a) =
        freya {
            let contents = File.ReadAllText(view + ".cshtml")
            let! authResult = getAuthResult
            let! cartId = getRequestCookie "cartId"
            let viewBag = DynamicViewBag()
            authResult |> Option.iter (fun r -> viewBag.AddValue("UserName", userName r))
            cartId |> Option.iter (fun c -> viewBag.AddValue("CartId", c))
            let ctx = Db.getContext()
            let cartItems =
                match authResult, cartId with
                | Some a, _ ->
                    Db.getCartsDetails (userName a) ctx |> List.sumBy (fun c -> c.Count)
                | _, Some c ->
                    Db.getCartsDetails c ctx |> List.sumBy (fun c -> c.Count)
                | _ -> 
                    0
            viewBag.AddValue("CartItems", cartItems)
            let result =
                RazorEngine.Engine.Razor.RunCompile(contents, view, typeof<'a>, model, viewBag)

            return {
                Data = Encoding.UTF8.GetBytes result
                Description =
                    { Charset = Some Charset.Utf8
                      Encodings = None
                      MediaType = Some MediaType.Html
                      Languages = Some [ LanguageTag.Parse "en" ] } } }