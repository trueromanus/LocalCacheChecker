using AnilibriaAPIClient;
using LocalCacheChecker.ApiModels;
using LocalCacheChecker.SaveModels;
using static LocalCacheChecker.Helpers.JsonHelpers;

namespace LocalCacheChecker {

    internal class SaveReleasesHelper {

        static async Task<int> SaveEpisodesAsFewFiles ( string folderToSaveCacheFiles, List<ReleaseSaveEpisodeModel> allEpisodes ) {
            var countInPart = 200;
            var partsCount = ( allEpisodes.Count () / countInPart ) + 1;
            for ( var i = 0; i < partsCount; i++ ) {
                var episodesPath = Path.Combine ( folderToSaveCacheFiles, $"episodes{i}.json" );
                Console.WriteLine ( $"Saving episodes to file {Path.GetFullPath ( episodesPath )} items" );

                var items = allEpisodes.Skip ( i * countInPart ).Take ( countInPart ).ToList ();
                if ( items.Any () ) await File.WriteAllTextAsync ( episodesPath, SerializeToJson ( items ) );
            }

            return partsCount;
        }

        static async Task<int> SaveReleasesAsFewFiles ( string folderToSaveCacheFiles, List<ReleaseSaveModel> allReleases ) {
            var countInPart = 300;
            var partsCount = ( allReleases.Count () / countInPart ) + 1;
            for ( var i = 0; i < partsCount; i++ ) {
                var episodesPath = Path.Combine ( folderToSaveCacheFiles, $"releases{i}.json" );
                Console.WriteLine ( $"Saving releases to file {Path.GetFullPath ( episodesPath )} items" );

                var items = allReleases.Skip ( i * countInPart ).Take ( countInPart ).ToList ();
                if ( items.Any () ) await File.WriteAllTextAsync ( episodesPath, SerializeToJson ( items ) );
            }

            return partsCount;
        }

        static bool ReleaseIsBlocked ( ReleaseDataModel model ) => model.IsBlockedByGeo || model.IsBlockedByCopyrights;

        static async Task<(IEnumerable<ReleaseSaveEpisodeModel> episodes, IEnumerable<ReleaseSaveModel> releases, IEnumerable<ReleaseTorrentSaveModel> torrents)> ReadCurrentCache ( MetadataModel metadata, string folderToSaveCacheFiles ) {
            var loadedEpisodes = new List<ReleaseSaveEpisodeModel> ();
            var loadedReleases = new List<ReleaseSaveModel> ();
            var loadedTorrents = new List<ReleaseTorrentSaveModel> ();

            var countReleases = metadata.CountReleases;
            for ( var i = 0; i < countReleases; i++ ) {
                var releasesPart = Path.Combine ( folderToSaveCacheFiles, $"releases{i}.json" );
                var releasesPartJson = await File.ReadAllTextAsync ( releasesPart );
                var deserialized = DeserializeFromJson<List<ReleaseSaveModel>> ( releasesPartJson );
                if ( deserialized != null ) loadedReleases.AddRange ( deserialized );
            }

            var countEpisodes = metadata.CountEpisodes;
            for ( var i = 0; i < countEpisodes; i++ ) {
                var episodesPart = Path.Combine ( folderToSaveCacheFiles, $"episodes{i}.json" );
                var partJson = await File.ReadAllTextAsync ( episodesPart );
                var deserialized = DeserializeFromJson<List<ReleaseSaveEpisodeModel>> ( partJson );
                if ( deserialized != null ) loadedEpisodes.AddRange ( deserialized );
            }

            var torrents = Path.Combine ( folderToSaveCacheFiles, $"torrents.json" );
            var fullJson = await File.ReadAllTextAsync ( torrents );
            var deserializedTorrens = DeserializeFromJson<List<ReleaseTorrentSaveModel>> ( fullJson );
            if ( deserializedTorrens != null ) loadedTorrents.AddRange ( deserializedTorrens );

            return (loadedEpisodes, loadedReleases, loadedTorrents);
        }

        static public async Task SaveReleases ( HttpClient httpClient, bool synchronizeFullReleases, string folderToSaveCacheFiles, bool isSaveBlocked ) {
            Console.WriteLine ( "Start synchronized releases..." );

            if ( synchronizeFullReleases ) {
                var types = await ReadTypes ( folderToSaveCacheFiles );
                var metadata = await ReadMetadata ( folderToSaveCacheFiles );
                if ( metadata == null ) return;

                Console.WriteLine ( "Try to read relases, torrents and episodes" );

                var allUpdatedReleases = new List<ReleaseDataModel> ();

                for ( var i = 1; i < 3; i++ ) {
                    Console.WriteLine ( $"Try to get page {i}" );
                    var page = await RequestMaker.GetPage ( i, httpClient );
                    var totalPages = page.Meta.Pagination.TotalPages;
                    Console.WriteLine ( "Total pages: " + totalPages );

                    allUpdatedReleases.AddRange ( page.Data.ToList () );
                }
                if ( !allUpdatedReleases.Any () ) return;

                Console.WriteLine ( $"Releases {allUpdatedReleases.Count} will be updated!" );

                var lastTimestamp = DateTimeOffset.Parse ( allUpdatedReleases.First ().FreshAt ).ToUnixTimeSeconds ();

                var ignoredIds = await ReadIgnoreIds ( folderToSaveCacheFiles );

                var result = new List<ReleaseSaveModel> ();
                var resultTorrents = new List<ReleaseTorrentSaveModel> ();
                var resultVideos = new List<ReleaseSaveEpisodeModel> ();
                await MapPageReleases ( httpClient, allUpdatedReleases, result, resultTorrents, types, resultVideos, ignoredIds );

                var (currentEpisodes, currentReleases, currentTorrents) = await ReadCurrentCache ( metadata, folderToSaveCacheFiles );

                var updatedIds = result.Select ( a => a.Id ).ToHashSet ();

                var editedReleases = currentReleases
                    .Where ( a => !updatedIds.Contains ( a.Id ) )
                    .ToList ()
                    .Concat ( result )
                    .ToList ();
                var updatedEpisodes = currentEpisodes
                    .Where ( a => !updatedIds.Contains ( a.ReleaseId ) )
                    .ToList ()
                    .Concat ( resultVideos )
                    .ToList ();
                var updatedTorrents = currentTorrents
                    .Where ( a => !updatedIds.Contains ( a.ReleaseId ) )
                    .ToList ()
                    .Concat ( resultTorrents )
                    .ToList ();

                await SaveLoadedItemsToFiles ( folderToSaveCacheFiles, editedReleases, updatedTorrents, updatedEpisodes, lastTimestamp );
            } else {
                var types = await ReadTypes ( folderToSaveCacheFiles );
                var metadata = await ReadMetadata ( folderToSaveCacheFiles );
                if ( metadata == null ) return;

                Console.WriteLine ( "Try to read current relases, torrents and episodes" );

                var allUpdatedReleases = new List<ReleaseDataModel> ();

                for ( var i = 1; i < 10; i++ ) {
                    Console.WriteLine ( $"Try to get page {i}" );
                    var page = await RequestMaker.GetPage ( i, httpClient );
                    var totalPages = page.Meta.Pagination.TotalPages;
                    Console.WriteLine ( "Total pages: " + totalPages );

                    var updatedReleases = page.Data
                        .Where ( a => DateTimeOffset.Parse ( a.FreshAt ).ToUnixTimeSeconds () > metadata.LastReleaseTimeStamp )
                        .ToList ();
                    if ( !updatedReleases.Any () ) {
                        Console.WriteLine ( $"No changes in releases on page {i}!" );
                        continue;
                    }

                    allUpdatedReleases.AddRange ( updatedReleases );

                    if ( updatedReleases.Count () != page.Data.Count () ) break;
                }


                if ( !allUpdatedReleases.Any () ) return;

                var lastTimestamp = DateTimeOffset.Parse ( allUpdatedReleases.First ().FreshAt ).ToUnixTimeSeconds ();

                var ignoredIds = await ReadIgnoreIds ( folderToSaveCacheFiles );

                var result = new List<ReleaseSaveModel> ();
                var resultTorrents = new List<ReleaseTorrentSaveModel> ();
                var resultVideos = new List<ReleaseSaveEpisodeModel> ();
                await MapPageReleases ( httpClient, allUpdatedReleases, result, resultTorrents, types, resultVideos, ignoredIds );

                var (currentEpisodes, currentReleases, currentTorrents) = await ReadCurrentCache ( metadata, folderToSaveCacheFiles );

                var updatedIds = result.Select ( a => a.Id ).ToHashSet ();

                var editedReleases = currentReleases
                    .Where ( a => !updatedIds.Contains ( a.Id ) )
                    .ToList ()
                    .Concat ( result )
                    .ToList ();
                var updatedEpisodes = currentEpisodes
                    .Where ( a => !updatedIds.Contains ( a.ReleaseId ) )
                    .ToList ()
                    .Concat ( resultVideos )
                    .ToList ();
                var updatedTorrents = currentTorrents
                    .Where ( a => !updatedIds.Contains ( a.ReleaseId ) )
                    .ToList ()
                    .Concat ( resultTorrents )
                    .ToList ();

                await SaveLoadedItemsToFiles ( folderToSaveCacheFiles, editedReleases, updatedTorrents, updatedEpisodes, lastTimestamp );

                return;
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
                                        Filename = torrent.Filename,
                                        Hash = torrent.Hash,
                                        Magnet = torrent.Magnet,
                                        Quality = torrent.Quality,
                                        Type = torrent.Type,
                                        Size = torrent.Size,
                                        ReleaseId = a.releaseId,
                                        Seeders = torrent.Seeders,
                                        Time = ParseDateTimeOffset ( torrent.UpdatedAt )
                                    }
                                );
                        }
                    )
                );

                episodes.AddRange (
                    relatedStuff
                        .Where ( a => a.episodes.Any () ) // if no episodes no need to save it
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
                            Season = types.Seasons.FirstOrDefault ( a => a.Value == item.Season.Value )?.Description ?? "Не указано",
                            Status = item.IsInProduction ? "Сейчас в озвучке" : "Озвучка завершена",
                            Series = item.EpisodesAreUnknown ? "?" : $"({item.EpisodesTotal ?? 0})",
                            Poster = item.Poster.Src,
                            Type = types.Types.FirstOrDefault ( a => a.Value == item.Type.Value )?.Description ?? item.Type.Value,
                            Genres = string.Join ( ", ", item.Genres.Select ( a => types.Genres.FirstOrDefault ( b => b.Id == a.Id )?.Name ?? "" ).Where ( a => !string.IsNullOrEmpty ( a ) ) ),
                            IsOngoing = item.IsOngoing,
                            AgeRating = types.AgeRatings.FirstOrDefault ( a => a.Value == item.AgeRating.Value )?.Description ?? item.AgeRating.Value,
                            Voices = members != null ? string.Join ( ", ", members.Where ( a => a.Role.Value == "voicing" ).Select ( a => a.Nickname ) ) : "",
                            Team = members != null ? string.Join ( ", ", members.OrderByDescending ( a => a.Role.Value ).Select ( a => a.Nickname ) ) : ""
                        }
                    ); ;
                }

                return result;
            }

            //fix domain and language issues
            static string RemakeDomain ( string url ) => url.Replace ( "cache.libria.fun", "cache-rfn.libria.fun" ).Replace ( "countryIso=US", "countryIso=RU" );

            static async Task<IEnumerable<(int releaseId, IEnumerable<ReleaseTorrentModel> torrents, IEnumerable<ReleaseMemberModel> members, IEnumerable<ReleaseEpisodeModel> episodes)>> GetRelatedStuffForReleases ( HttpClient httpClient, IEnumerable<int> ids ) {
                var result = new List<(int, IEnumerable<ReleaseTorrentModel>, IEnumerable<ReleaseMemberModel>, IEnumerable<ReleaseEpisodeModel>)> ();
                foreach ( int releaseId in ids ) {
                    ReleaseOnlyCollectionsModel collections;
                    try {
                        collections = await RequestMaker.GetReleaseInnerCollections ( httpClient, releaseId );
                    } catch ( Exception ex ) {
                        Console.WriteLine ( $"Error while try get release info for {releaseId} {ex.Message}" );
                        continue;
                    }

                    foreach ( var collection in collections.Episodes ) {
                        if ( collection.Preview?.Thumbnail?.Any () == true ) {
                            collection.Preview = collection.Preview with { Thumbnail = "" };
                        }

                        if ( !string.IsNullOrEmpty ( collection.Hls720 ) ) collection.Hls720 = RemakeDomain ( collection.Hls720 );
                        if ( !string.IsNullOrEmpty ( collection.Hls1080 ) ) collection.Hls1080 = RemakeDomain ( collection.Hls1080 );
                        if ( !string.IsNullOrEmpty ( collection.Hls480 ) ) collection.Hls480 = RemakeDomain ( collection.Hls480 );
                    }

                    //reorder episodes from zero
                    var orderedEpisodes = collections.Episodes.OrderBy ( a => a.SortOrder );
                    var iterator = 0;
                    foreach ( var orderedEpisode in orderedEpisodes ) {
                        orderedEpisode.SortOrder = iterator;
                        iterator++;
                    }

                    result.Add ( (releaseId, collections.Torrents, collections.Members, collections.Episodes) );
                    await Task.Delay ( 1000 ); // make half secound delay for avoid `too much requests` issue
                }

                return result;
            }
        }

        private static async Task SaveLoadedItemsToFiles (
            string folderToSaveCacheFiles,
            List<ReleaseSaveModel> result,
            List<ReleaseTorrentSaveModel> resultTorrents,
            List<ReleaseSaveEpisodeModel> resultVideos,
            long lastTimestamp ) {
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
                        LastReleaseTimeStamp = lastTimestamp,
                        CountEpisodes = countEpisodeFiles,
                        CountReleases = countReleaseFiles
                    }
                )
            );
        }

    }

}
