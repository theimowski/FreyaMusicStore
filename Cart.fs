module FreyaMusicStore.Cart

open Arachne.Http

open Freya.Core
open Freya.Lenses.Http
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Router

type Item = 
    { AlbumId : int
      Title : string
      Price : decimal
      Count : int }

    static member fromDb (details : Db.CartDetails) =
        { AlbumId = details.AlbumId
          Title = details.AlbumTitle
          Price = details.Price
          Count = details.Count }


type Cart = {
    CartItems : Item []
    CartTotal : decimal
}

let id =
    freya {
        let! id = Freya.getLensPartial (Route.atom "0")
        return id
    } |> Freya.memo

let ok _=
    freya {
        let! cartId = id
        let ctx = Db.getContext()
        let cartDetails = Db.getCartsDetails cartId.Value ctx |> List.toArray
        return! writeHtml ("cart", { CartItems = cartDetails |> Array.map Item.fromDb; CartTotal = cartDetails |> Array.sumBy (fun d -> d.Price * (decimal) d.Count) } )
    }

let albumId = 
    freya {
        let! form = form()
        let albumId = form |> Map.tryFind "AlbumId"
        return albumId |> Option.bind mInt
    } |> Freya.memo

let isMalformed = 
    freya {
        let! meth = Freya.getLens Request.meth
        match meth with
        | GET ->
            return false
        | _ ->
            let! albumId = albumId
            return albumId.IsNone
        
    }


let post = 
    freya {
        let! albumId = albumId
        let! cartId = id

        match cartId.Value |> System.Guid.TryParse with
        | true, _ ->
            do! setSessionCartId cartId.Value
        | _ ->
            ()

        let ctx = Db.getContext()
        Db.addToCart cartId.Value albumId.Value ctx
        return ()
    }

let delete =
    freya {
        let! albumId = albumId
        let! cartId = id

        let ctx = Db.getContext()
        let cart = Db.getCart cartId.Value albumId.Value ctx
        Db.removeFromCart cart.Value albumId ctx
        return ()
    }

let pipe = 
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET; POST; DELETE ] } ) 
        handleOk ok
        respondWithEntity (Freya.init true)
        malformed isMalformed
        created (Freya.init false)
        doPost post 
        doDelete delete} |> FreyaMachine.toPipeline