[<AutoOpen>]
module FreyaMusicStore.Common

open System
open System.Globalization
open System.IO
open System.Security.Claims
open System.Text

open Arachne.Http
open Arachne.Language

open Freya.Core
open Freya.Lenses.Http
open Freya.Machine
open Freya.Machine.Extensions.Http

open Microsoft.AspNet.Identity
open Microsoft.Owin.Security

open RazorEngine.Templating

let fromNullable = function | null -> None | x -> Some x

let getAuthResult: Freya<_> =
    (fun freyaState -> 
            let ctx = Microsoft.Owin.OwinContext(freyaState.Environment)
            let result = ctx.Authentication.AuthenticateAsync(DefaultAuthenticationTypes.ApplicationCookie) |> Async.AwaitTask |> Async.RunSynchronously
            async.Return (fromNullable result, freyaState)) |> Freya.memo

let isLoggedOn =
    freya {
        let! cookie = getAuthResult
        return cookie.IsSome
    }

let isAdmin = 
    freya {
        let! cookie = getAuthResult
        match cookie with
        | Some c when c.Identity.HasClaim(Predicate(fun claim -> claim.Type = ClaimTypes.Role && claim.Value = "admin")) ->
            return true
        | _ ->
            return false
    }

let setSessionCartId cartId : Freya<unit> = (fun freyaState ->
        let ctx = Microsoft.Owin.OwinContext(freyaState.Environment)
        ctx.Response.Cookies.Append("cartId", cartId)
        async.Return ((), freyaState)
    )

let getSessionCartId : Freya<string option> = (fun freyaState ->
        let ctx = Microsoft.Owin.OwinContext(freyaState.Environment)
        let cartId = 
            match ctx.Request.Cookies.["cartId"] with
            | null -> None
            | x -> Some x
        async.Return (cartId, freyaState)
    )


let userName (result: AuthenticateResult) =
    result.Identity.Claims |> Seq.find (fun c -> c.Type = ClaimTypes.Name) |> fun x -> x.Value

let inline writeHtml (view : string, model : 'a) =
    freya {
        let contents = File.ReadAllText(view + ".cshtml")
        let! authResult = getAuthResult
        let! cartId = getSessionCartId
        let viewBag = DynamicViewBag()
        authResult |> Option.iter (fun r -> viewBag.AddValue("UserName", userName r))
        cartId |> Option.iter (fun c -> viewBag.AddValue("CartId", c))
        let result =
            RazorEngine.Engine.Razor.RunCompile(contents, view, typeof<'a>, model, viewBag)

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

let kv (s: string) =
    match s.Split([| '=' |]) with
    | [|k;v|] -> Some(k,v)
    | _ -> None

let both f (x,y) = f x, f y
let decode = System.Net.WebUtility.UrlDecode

let query =
    freya {
        let! query = Freya.getLens Request.query
        return query.Split([| '&' |]) |> Array.choose kv |> Array.map (both decode) |> Map.ofArray } |> Freya.memo


let readStream (x: Stream) =
    use reader = new StreamReader (x)
    reader.ReadToEndAsync()
    |> Async.AwaitTask

let readBody () =
    freya {
        let! body = Freya.getLens Request.body
        return! Freya.fromAsync readStream body } |> Freya.memo

let form () =
    freya {
        let! body = readBody ()
        return body.Split([| '&' |]) |> Array.choose kv |> Array.map (both decode) |> Map.ofArray } |> Freya.memo


type MaybeBuilder() =
    member __.Bind(m, f) = Option.bind f m
    member __.Return(x) = Some x
    member __.ReturnFrom(x) = x

let maybe = MaybeBuilder()

let mInt (s: string) = 
    match Int32.TryParse s with
    | true, x -> Some x
    | _ -> None

let mDec (s: string) = 
    match Decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) with
    | true, x -> Some x
    | _ -> None





type AlbumForm =
    { Title : string
      ArtistId : int
      GenreId : int
      Price : decimal
      AlbumArtUrl : string }

let readAlbum =
    freya {
        let! form = form ()
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

let passHash (pass: string) =
    use sha = Security.Cryptography.SHA256.Create()
    Text.Encoding.UTF8.GetBytes(pass)
    |> sha.ComputeHash
    |> Array.map (fun b -> b.ToString("x2"))
    |> String.concat ""