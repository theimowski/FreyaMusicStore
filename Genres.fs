module FreyaMusicStore.Genres

open Freya.Core
open Freya.Machine

let fetch = Db.getGenres >> Array.map (fun g -> g.Name) |> Freya.init

let pipe = res fetch "genres" |> FreyaMachine.toPipeline