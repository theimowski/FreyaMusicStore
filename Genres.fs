module FreyaMusicStore.Genres

open Arachne.Http

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http

type Genre = 
    { Name : string }

    static member fromDb (g: Db.Genre) =
        { Name = g.Name }

type Genres = 
    { Genres : Genre [] }

let get =
    freya {
        let ctx = Db.getContext()
        let genres = Db.getGenres ctx |> Array.map Genre.fromDb
        return! write ("genres", { Genres = genres } )
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk (fun _ -> get) } |> FreyaMachine.toPipeline