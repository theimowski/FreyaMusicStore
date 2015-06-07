module Db

open FSharp.Data.Sql

type Sql = 
    SqlDataProvider< 
        "Server=(LocalDb)\\v11.0;Database=FreyaMusicStore;Trusted_Connection=True;MultipleActiveResultSets=true", 
        DatabaseVendor=Common.DatabaseProviderTypes.MSSQLSERVER >

type DbContext = Sql.dataContext
type Album = DbContext.``[dbo].[Albums]Entity``
type Artist = DbContext.``[dbo].[Artists]Entity``
type Genre = DbContext.``[dbo].[Genres]Entity``
type AlbumDetails = DbContext.``[dbo].[AlbumDetails]Entity``

let getContext() = Sql.GetDataContext()

let firstOrNone s = s |> Seq.tryFind (fun _ -> true)

let getGenres (ctx : DbContext) : Genre [] = 
    ctx.``[dbo].[Genres]`` |> Seq.toArray

let getArtists (ctx : DbContext) : Artist [] = 
    ctx.``[dbo].[Artists]`` |> Seq.toArray

let getAlbum id (ctx : DbContext) : Album option = 
    query { 
        for album in ctx.``[dbo].[Albums]`` do
            where (album.AlbumId = id)
            select album
    } |> firstOrNone

let getAlbumsForGenre genreName (ctx : DbContext) : Album [] = 
    query { 
        for album in ctx.``[dbo].[Albums]`` do
            join genre in ctx.``[dbo].[Genres]`` on (album.GenreId = genre.GenreId)
            where (genre.Name = genreName)
            select album
    }
    |> Seq.toArray

let getAlbumDetails id (ctx : DbContext) : AlbumDetails option = 
    query { 
        for album in ctx.``[dbo].[AlbumDetails]`` do
            where (album.AlbumId = id)
            select album
    } |> firstOrNone

let getAlbumsDetails (ctx : DbContext) : AlbumDetails [] = 
    ctx.``[dbo].[AlbumDetails]`` |> Seq.toArray

let createAlbum (artistId, genreId, price, title, albumArtUrl) (ctx : DbContext) =
    let album = ctx.``[dbo].[Albums]``.Create(artistId, genreId, price, title)
    album.AlbumArtUrl <- albumArtUrl
    ctx.SubmitUpdates()
    album