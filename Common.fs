[<AutoOpen>]
module FreyaMusicStore.Common

open System
open System.IO
open System.Text

open Arachne.Http
open Arachne.Language

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http

open RazorEngine.Templating

let inline write (view : string, model : 'a) =
    freya {
        let contents = File.ReadAllText(view + ".cshtml")
        let result =
            RazorEngine.Engine.Razor.RunCompile(contents, view, typeof<'a>, model)

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