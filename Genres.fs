module FreyaMusicStore.Genres

open Arachne.Http

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http

type Genre = {
    Name : string
}

type Genres = {
    Genres : Genre []
}

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk (fun _ -> freya { return!  write ("home", {Genres =  [||] } ) } ) } |> FreyaMachine.toPipeline