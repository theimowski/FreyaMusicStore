module FreyaMusicStore.StaticFiles

open System
open System.IO

open Arachne.Http

open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Lenses.Http

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

let pipe : FreyaPipeline =
    freyaMachine {
        using http
        methodsSupported (Freya.init [ GET; HEAD ])
        mediaTypesSupported mediaTypesConfiguration
        exists existsDecision
        handleOk fileHandler } |> FreyaMachine.toPipeline