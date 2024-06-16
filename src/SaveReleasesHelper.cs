using AnilibriaAPIClient;
using LocalCacheChecker.ApiModels;
using LocalCacheChecker.SaveModels;
using System.Text.Json;
using static LocalCacheChecker.Helpers.JsonHelpers;

namespace LocalCacheChecker {

    internal class SaveReleasesHelper {

        static async Task<int> SaveEpisodesAsFewFiles ( string folderToSaveCacheFiles, List<ReleaseSaveEpisodeModel> allEpisodes ) {
            var countInPart = 200;
            var partsCount = allEpisodes.Count () / countInPart;
            for ( var i = 0; i < partsCount; i++ ) {
                var episodesPath = Path.Combine ( folderToSaveCacheFiles, $"episodes{i}.json" );
                Console.WriteLine ( $"Saving episodes to file {Path.GetFullPath ( episodesPath )} items" );

                await File.WriteAllTextAsync ( episodesPath, SerializeToJson ( allEpisodes.Skip ( i * countInPart ).Take ( countInPart ).ToList () ) );
            }

            return partsCount;
        }

        static async Task<int> SaveReleasesAsFewFiles ( string folderToSaveCacheFiles, List<ReleaseSaveModel> allReleases ) {
            var countInPart = 300;
            var partsCount = allReleases.Count () / countInPart;
            for ( var i = 0; i < partsCount; i++ ) {
                var episodesPath = Path.Combine ( folderToSaveCacheFiles, $"releases{i}.json" );
                Console.WriteLine ( $"Saving episodes to file {Path.GetFullPath ( episodesPath )} items" );

                await File.WriteAllTextAsync ( episodesPath, SerializeToJson ( allReleases.Skip ( i * countInPart ).Take ( countInPart ).ToList () ) );
            }

            return partsCount;
        }

        static bool ReleaseIsBlocked ( ReleaseDataModel model ) => model.IsBlockedByGeo || model.IsBlockedByCopyrights;

        static public async Task SaveReleases ( HttpClient httpClient, bool synchronizeFullReleases, string folderToSaveCacheFiles, bool isSaveBlocked ) {
            Console.WriteLine ( "Start synchronized releases..." );

            if ( synchronizeFullReleases ) {
                Console.WriteLine ( "Try to get first page" );

                var result = new List<ReleaseSaveModel> ();
                var resultTorrents = new List<ReleaseTorrentSaveModel> ();
                var resultVideos = new List<ReleaseSaveEpisodeModel> ();
                var blockedByGeoOrCopyrights = new List<int> ();

                var firstPage = await RequestMaker.GetPage ( 1, httpClient );

                var totalPages = firstPage.Meta.Pagination.TotalPages;
                Console.WriteLine ( "Total pages: " + totalPages );

                if ( isSaveBlocked ) blockedByGeoOrCopyrights.AddRange ( firstPage.Data.Where ( ReleaseIsBlocked ).Select ( a => a.Id ) );

                var types = await ReadTypes ( folderToSaveCacheFiles );
                var ignoredIds = await ReadIgnoreIds ( folderToSaveCacheFiles );

                await MapPageReleases ( httpClient, firstPage.Data, result, resultTorrents, types, resultVideos, ignoredIds );

                for ( var i = 2; i <= totalPages; i++ ) {
                    Console.WriteLine ( "Load page: " + i );

                    var pageData = await RequestMaker.GetPage ( i, httpClient );
                    if ( isSaveBlocked ) blockedByGeoOrCopyrights.AddRange ( pageData.Data.Where ( ReleaseIsBlocked ).Select ( a => a.Id ) );

                    await MapPageReleases ( httpClient, pageData.Data, result, resultTorrents, types, resultVideos, ignoredIds );
                }

                var countReleaseFiles = await SaveReleasesAsFewFiles ( folderToSaveCacheFiles, result );

                var torrentPath = Path.Combine ( folderToSaveCacheFiles, "torrents.json" );
                Console.WriteLine ( $"Saving torrents to file {Path.GetFullPath ( torrentPath )} items" );
                await File.WriteAllTextAsync ( torrentPath, SerializeToJson ( resultTorrents ) );

                var countEpisodeFiles = await SaveEpisodesAsFewFiles ( folderToSaveCacheFiles, resultVideos );

                var metadataPath = Path.Combine ( folderToSaveCacheFiles, "metadata" );
                Console.WriteLine ( $"Saving metadata to file {Path.GetFullPath ( metadataPath )} items" );
                await File.WriteAllTextAsync (
                    metadataPath,
                    SerializeToJson (
                        new MetadataModel {
                            LastReleaseTimeStamp = DateTimeOffset.Parse ( firstPage.Data.First ().FreshAt ).ToUnixTimeSeconds (),
                            CountEpisodes = countEpisodeFiles,
                            CountReleases = countReleaseFiles
                        }
                    )
                );

                if ( isSaveBlocked && blockedByGeoOrCopyrights.Any () ) {
                    var blockedPath = Path.Combine ( folderToSaveCacheFiles, "blockedreleases.json" );
                    Console.WriteLine ( $"Saving blocked to file {Path.GetFullPath ( blockedPath )} items" );
                    await File.WriteAllTextAsync ( blockedPath, SerializeToJson ( blockedByGeoOrCopyrights ) );
                }
            } else {
                var types = await ReadTypes ( folderToSaveCacheFiles );
                var metadata = await ReadMetadata ( folderToSaveCacheFiles );
                if ( metadata == null ) return;

                Console.WriteLine ( "Try to get first page" );
                var firstPage = await RequestMaker.GetPage ( 1, httpClient );
                var totalPages = firstPage.Meta.Pagination.TotalPages;
                Console.WriteLine ( "Total pages: " + totalPages );

                var updatedReleases = firstPage.Data
                    .Where ( a => DateTimeOffset.Parse ( a.FreshAt ).ToUnixTimeSeconds () > metadata.LastReleaseTimeStamp )
                    .ToList ();
                if ( !updatedReleases.Any () ) {
                    Console.WriteLine ( "No changes in releases!" );
                    return;
                }

                var allUpdatedReleases = updatedReleases;

                if ( updatedReleases.Count () == firstPage.Data.Count () ) {

                    var currentPage = 2;
                    while ( true ) {
                        if ( currentPage > totalPages ) break;

                        var (pageUpdatedReleases, isFullPage) = await GetUpdatedReleaseFromPage ( httpClient, currentPage, metadata.LastReleaseTimeStamp );
                        if ( !pageUpdatedReleases.Any () ) break;

                        allUpdatedReleases.AddRange ( pageUpdatedReleases );

                        if ( !isFullPage ) break;

                        currentPage++;
                    }
                }

                var ignoredIds = await ReadIgnoreIds ( folderToSaveCacheFiles );

                var result = new List<ReleaseSaveModel> ();
                var resultTorrents = new List<ReleaseTorrentSaveModel> ();
                var resultVideos = new List<ReleaseSaveEpisodeModel> ();

                await MapPageReleases ( httpClient, allUpdatedReleases, result, resultTorrents, types, resultVideos, ignoredIds );

                await SaveUpdatedReleases ( result, resultTorrents, resultVideos, metadata, folderToSaveCacheFiles );

                return;
            }

            static async Task SaveUpdatedReleases ( List<ReleaseSaveModel> result, List<ReleaseTorrentSaveModel> resultTorrents, List<ReleaseSaveEpisodeModel> resultVideos, MetadataModel metadata, string folderToSaveCacheFiles ) {
                var countReleases = metadata.CountReleases;
                var needProcessReleased = result.ToList ();

                for ( var i = 0; i < countReleases; i++ ) {
                    var path = Path.Combine ( folderToSaveCacheFiles, $"releases{i}.json" );
                    if ( !File.Exists ( path ) ) continue;

                    var content = await File.ReadAllTextAsync ( path );
                    var pageItems = DeserializeFromJson<IEnumerable<ReleaseSaveModel>> ( content );
                    if ( pageItems == null ) continue;

                    var savedItems = pageItems.ToList ();

                    var processedItems = new HashSet<int> ();
                    foreach ( var item in needProcessReleased ) {
                        var savedItem = savedItems.FirstOrDefault ( a => a.Id == item.Id );
                        if ( savedItem == null ) continue;
                        if ( savedItem == item ) continue;

                        savedItems.Remove ( savedItem );
                        savedItems.Add ( item );
                        processedItems.Add ( item.Id );
                    }
                    if ( !processedItems.Any () ) continue;

                    needProcessReleased = needProcessReleased
                        .Where ( a => processedItems.Contains ( a.Id ) )
                        .ToList ();
                    await File.WriteAllTextAsync ( path, SerializeToJson ( savedItems ) );
                }

                var countEpisodes = metadata.CountEpisodes;
                var needProcessEpisodes = resultVideos.ToList ();

                for ( var i = 0; i < countEpisodes; i++ ) {
                    var path = Path.Combine ( folderToSaveCacheFiles, $"episodes{i}.json" );
                    if ( !File.Exists ( path ) ) continue;

                    var content = await File.ReadAllTextAsync ( path );
                    var episodePageItems = DeserializeFromJson<IEnumerable<ReleaseSaveEpisodeModel>> ( content );
                    if ( episodePageItems == null ) continue;

                    var savedItems = episodePageItems.ToList ();

                    var processedItems = new HashSet<int> ();
                    foreach ( var item in needProcessEpisodes ) {
                        var savedItem = savedItems.FirstOrDefault ( a => a.ReleaseId == item.ReleaseId );
                        if ( savedItem == null ) continue;
                        if ( savedItem == item ) continue;

                        savedItems.Remove ( savedItem );
                        savedItems.Add ( item );
                        processedItems.Add ( item.ReleaseId );
                    }
                    if ( !processedItems.Any () ) continue;

                    needProcessEpisodes = needProcessEpisodes
                        .Where ( a => processedItems.Contains ( a.ReleaseId ) )
                        .ToList ();
                    await File.WriteAllTextAsync ( path, SerializeToJson ( savedItems ) );
                }
            }

            static async Task<MetadataModel?> ReadMetadata ( string folderToSaveCacheFiles ) {
                var metadataPath = Path.Combine ( folderToSaveCacheFiles, "metadata" );

                if ( !File.Exists ( metadataPath ) ) {
                    Console.WriteLine ( "Sorry but you need to synchronize all releases first. Use `-releases -fullreleases` options!" );
                    return null;
                }
                var metadata = DeserializeFromJson<MetadataModel> ( await File.ReadAllTextAsync ( metadataPath ) );
                if ( metadata == null ) {
                    Console.WriteLine ( "Can't read metadata file, please check if it file is correct! May be need to make recreate it with `-releases -fullreleases` options!" );
                    return null;
                }

                return metadata;
            }

            static async Task<TypesResultModel> ReadTypes ( string folderToSaveCacheFiles ) {
                var pathToTypes = Path.Combine ( folderToSaveCacheFiles, "types.json" );
                if ( !File.Exists ( pathToTypes ) ) {
                    Console.WriteLine ( $"File types.json not found by path {Path.GetFullPath ( pathToTypes )}. You need synchronize types, please add -types or -all parameters to command!" );
                    Environment.Exit ( 1 );
                }
                var types = DeserializeFromJson<TypesResultModel> ( await File.ReadAllTextAsync ( pathToTypes ) );
                if ( types == null ) {
                    Console.WriteLine ( $"Content of types.json is corrupt. You need synchronize types, please add -types or -all parameters to command!" );
                    Environment.Exit ( 1 );
                }

                return types;
            }

            static async Task<IEnumerable<int>> ReadIgnoreIds ( string folderToSaveCacheFiles ) {
                var pathToIgnored = Path.Combine ( folderToSaveCacheFiles, "ignored.json" );
                var ignoredIds = new List<int> ();
                if ( File.Exists ( pathToIgnored ) ) {
                    ignoredIds = ( DeserializeFromJson<IEnumerable<int>> ( await File.ReadAllTextAsync ( pathToIgnored ) ) )?.ToList () ?? Enumerable.Empty<int> ().ToList ();
                }

                return ignoredIds;
            }

            static async Task<(IEnumerable<ReleaseDataModel>, bool)> GetUpdatedReleaseFromPage ( HttpClient httpClient, int page, long lastTimestamp ) {
                Console.WriteLine ( $"Load Page {page}" );
                var pageData = await RequestMaker.GetPage ( page, httpClient );

                var updatedReleases = pageData.Data
                    .Where ( a => DateTimeOffset.Parse ( a.FreshAt ).ToUnixTimeSeconds () > lastTimestamp )
                    .ToList ();
                return (updatedReleases, updatedReleases.Count == pageData.Data.Count ());
            }

            static async Task MapPageReleases (
                HttpClient httpClient,
                IEnumerable<ReleaseDataModel> releases,
                List<ReleaseSaveModel> result,
                List<ReleaseTorrentSaveModel> torrents,
                TypesResultModel types,
                List<ReleaseSaveEpisodeModel> episodes,
                IEnumerable<int> ignoredIds ) {

                var actualReleases = releases
                    .Where ( a => !ignoredIds.Contains ( a.Id ) )
                    .ToList ();

                var relatedStuff = await GetRelatedStuffForReleases ( httpClient, actualReleases.Select ( a => a.Id ).ToList () );
                torrents.AddRange (
                    relatedStuff.SelectMany (
                        a => {
                            return a.torrents
                                .Select (
                                    torrent => new ReleaseTorrentSaveModel {
                                        Id = torrent.Id,
                                        Codec = torrent.Codec,
                                        Description = torrent.Description,
                                        FileName = torrent.FileName,
                                        Hash = torrent.Hash,
                                        Magnet = torrent.Magnet,
                                        Quality = torrent.Quality,
                                        Size = torrent.Size,
                                        ReleaseId = a.releaseId
                                    }
                                );
                        }
                    )
                );

                episodes.AddRange (
                    relatedStuff
                        .Select ( a => new ReleaseSaveEpisodeModel { ReleaseId = a.releaseId, Items = a.episodes } )
                        .ToList ()
                );
                result.AddRange ( MapForSave ( actualReleases, relatedStuff, types ) );
            }

            static long ParseDateTimeOffset ( string value ) {
                if ( string.IsNullOrEmpty ( value ) ) return 0;

                try {
                    return DateTimeOffset.Parse ( value ).ToUnixTimeSeconds ();
                } catch {
                    return 0;
                }
            }

            static IEnumerable<ReleaseSaveModel> MapForSave ( IEnumerable<ReleaseDataModel> items, IEnumerable<(int releaseId, IEnumerable<ReleaseTorrentModel> torrents, IEnumerable<ReleaseMemberModel> members, IEnumerable<ReleaseEpisodeModel> episodes)> relatedStuff, TypesResultModel types ) {
                var result = new List<ReleaseSaveModel> ();
                foreach ( var item in items ) {
                    var (releaseId, torrents, members, episodes) = relatedStuff.FirstOrDefault ( a => a.releaseId == item.Id );
                    result.Add (
                        new ReleaseSaveModel {
                            Id = item.Id,
                            Announce = item.Notification ?? "",
                            Code = item.Alias,
                            CountVideos = episodes?.Count () ?? 0,
                            CountTorrents = torrents?.Count () ?? 0,
                            Description = item.Description,
                            Timestamp = ParseDateTimeOffset ( item.FreshAt ),
                            OriginalName = item.Name.English,
                            Title = item.Name.Main,
                            Rating = item.AddedInUsersFavorites ?? 0,
                            Year = item.Year.ToString (),
                            Season = types.Seasons.FirstOrDefault ( a => a.Value == item.Season.Value )?.Description ?? item.Season.Value,
                            Status = item.IsInProduction ? "Сейчас в озвучке" : "Озвучка завершена",
                            Series = item.EpisodesAreUnknown ? "?" : ( item.EpisodesTotal ?? 0 ).ToString (),
                            Poster = item.Poster.Src,
                            Type = types.Types.FirstOrDefault ( a => a.Value == item.Type.Value )?.Description ?? item.Type.Value,
                            Genres = string.Join ( ", ", item.Genres.Select ( a => types.Genres.FirstOrDefault ( b => b.Id == a.Id )?.Name ?? "" ).Where ( a => !string.IsNullOrEmpty ( a ) ) ),
                            IsOngoing = item.IsOngoing,
                            AgeRating = types.AgeRatings.FirstOrDefault ( a => a.Value == item.AgeRating.Value )?.Description ?? item.AgeRating.Value,
                            Voices = string.Join ( ", ", members.Where ( a => a.Role.Value == "voicing" ).Select ( a => a.Nickname ) )
                        }
                    ); ;
                }

                return result;
            }

            static async Task<IEnumerable<(int releaseId, IEnumerable<ReleaseTorrentModel> torrents, IEnumerable<ReleaseMemberModel> members, IEnumerable<ReleaseEpisodeModel> episodes)>> GetRelatedStuffForReleases ( HttpClient httpClient, IEnumerable<int> ids ) {
                var result = new List<(int, IEnumerable<ReleaseTorrentModel>, IEnumerable<ReleaseMemberModel>, IEnumerable<ReleaseEpisodeModel>)> ();
                foreach ( int releaseId in ids ) {
                    var collections = await RequestMaker.GetReleaseInnerCollections ( httpClient, releaseId );

                    foreach ( var collection in collections.Episodes ) {
                        if ( collection.Preview?.Thumbnail?.Any () == true ) {
                            collection.Preview = collection.Preview with { Thumbnail = "" };
                        }
                    }

                    result.Add ( (releaseId, collections.Torrents, collections.Members, collections.Episodes) );
                }

                return result;
            }
        }

    }

}
