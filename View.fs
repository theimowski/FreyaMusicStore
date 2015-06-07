module View

type AlbumDetails = {
    Title : string
    AlbumArtUrl : string
    Price : decimal
    Artist : string
    Genre : string
}

let toAlbum (a: Db.AlbumDetails) = {
    Title = a.Title
    AlbumArtUrl = a.AlbumArtUrl
    Price = a.Price
    Artist = a.Artist
    Genre = a.Genre
}