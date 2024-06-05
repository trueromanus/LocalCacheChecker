using AnilibriaAPIClient;
using LocalCacheChecker;
using System.Reflection;
using System.Text.Json;

internal class Program {

    private static string SerializeToJson<T> ( T model ) {
        return JsonSerializer.Serialize<T> (
            model,
            new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            }
        );
    }

    private static T? DeserializeFromJson<T> ( string json ) {
        return JsonSerializer.Deserialize<T> (
            json,
            options: new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            }
        );
    }

    static async Task SaveTypes ( HttpClient httpClient, string folderToSaveCacheFiles ) {
        Console.WriteLine ( "Start synchronized types..." );

        var ageRatings = await RequestMaker.GetAgeRatings ( httpClient );
        var genres = await RequestMaker.GetGenres ( httpClient );
        var seasons = await RequestMaker.GetSeasons ( httpClient );
        var types = await RequestMaker.GetTypes ( httpClient );

        Console.WriteLine ( $"Received {ageRatings.Count ()} ratings items" );
        Console.WriteLine ( $"Received {genres.Count ()} genres items" );
        Console.WriteLine ( $"Received {seasons.Count ()} seasons items" );
        Console.WriteLine ( $"Received {types.Count ()} types items" );

        var result = new TypesResultModel {
            AgeRatings = ageRatings,
            Genres = genres,
            Seasons = seasons,
            Types = types
        };

        var jsonContent = JsonSerializer.Serialize (
            result,
            new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            }
        );

        var path = Path.Combine ( folderToSaveCacheFiles, "types.json" );
        Console.WriteLine ( $"Saving to file {Path.GetFullPath ( path )} items" );

        await File.WriteAllTextAsync ( path, jsonContent );

        Console.WriteLine ( $"Types saved!" );
    }

    static async Task SaveSchedule ( HttpClient httpClient, string folderToSaveCacheFiles ) {
        Console.WriteLine ( "Start synchronized schedule..." );

        var scheduleData = await RequestMaker.GetFullSchedule ( httpClient );

        var result = new Dictionary<string, int> ();
        if ( !scheduleData.Any () ) return;

        Console.WriteLine ( $"Received {scheduleData.Count ()} items" );

        foreach ( var schedule in scheduleData ) {
            if ( schedule.Release == null ) continue;

            result.Add ( schedule.Release.Id.ToString (), schedule.Release.PublishDay.Value );
        }

        var jsonContent = JsonSerializer.Serialize ( result );

        var path = Path.Combine ( folderToSaveCacheFiles, "schedule.json" );
        Console.WriteLine ( $"Saving to file {Path.GetFullPath ( path )} items" );

        await File.WriteAllTextAsync ( path, jsonContent );

        Console.WriteLine ( $"Schedule saved!" );
    }

    static async Task SaveReleaseSeries ( HttpClient httpClient, string folderToSaveCacheFiles ) {
        Console.WriteLine ( "Start synchronized franchises..." );
        var franchises = await RequestMaker.GetAllFranchises ( httpClient );

        var result = new List<ReleaseSeriesSaveModel> ();
        if ( !franchises.Any () ) return;

        Console.WriteLine ( $"Received {franchises.Count ()} franchises" );

        foreach ( var franchise in franchises ) {
            var releasesItem = await RequestMaker.GetFranchisesReleases ( httpClient, franchise.Id );

            var model = new ReleaseSeriesSaveModel {
                CountReleases = releasesItem.FranchiseReleases.Count (),
                ReleasesIds = releasesItem.FranchiseReleases.Select ( a => a.ReleaseId ).ToList (),
                Poster = franchise.Image.Preview,
                Titles = releasesItem.FranchiseReleases.Select ( a => a.Release.Name.Main ).ToList (),
                Posters = releasesItem.FranchiseReleases.Select ( a => a.Release.Poster.Src ).ToList (),
                Genres = releasesItem.FranchiseReleases.SelectMany ( a => a.Release.Genres ).Select ( a => a.Id ).ToList (),
                Title = franchise.Name
            };
            result.Add ( model );
        }

        var path = Path.Combine ( folderToSaveCacheFiles, "releaseseries.json" );
        Console.WriteLine ( $"Saving to file {Path.GetFullPath ( path )} items" );

        await File.WriteAllTextAsync ( path, SerializeToJson ( result ) );

        Console.WriteLine ( $"Franchises saved!" );
    }

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

    static async Task SaveReleases ( HttpClient httpClient, bool synchronizeFullReleases, string folderToSaveCacheFiles, bool isSaveBlocked ) {
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

            var pathToTypes = Path.Combine ( folderToSaveCacheFiles, "types.json" );
            if ( !File.Exists ( pathToTypes ) ) {
                Console.WriteLine ( $"File types.json not found by path {Path.GetFullPath ( pathToTypes )}. You need synchronize types, please add -types or -all parameters to command!" );
                return;
            }
            var types = DeserializeFromJson<TypesResultModel> ( await File.ReadAllTextAsync ( pathToTypes ) );
            if ( types == null ) {
                Console.WriteLine ( $"Content of types.json is corrupt. You need synchronize types, please add -types or -all parameters to command!" );
                return;
            }

            var pathToIgnored = Path.Combine ( folderToSaveCacheFiles, "ignored.json" );
            var ignoredIds = new List<int> ();
            if ( File.Exists ( pathToIgnored ) ) {
                ignoredIds = ( DeserializeFromJson<IEnumerable<int>> ( await File.ReadAllTextAsync ( pathToIgnored ) ) )?.ToList () ?? Enumerable.Empty<int> ().ToList ();
            }

            await MapPageReleases ( httpClient, firstPage, result, resultTorrents, types, resultVideos, ignoredIds );

            for ( var i = 2; i <= totalPages; i++ ) {
                Console.WriteLine ( "Load page: " + i );

                var pageData = await RequestMaker.GetPage ( i, httpClient );
                if ( isSaveBlocked ) blockedByGeoOrCopyrights.AddRange ( pageData.Data.Where ( ReleaseIsBlocked ).Select ( a => a.Id ) );

                await MapPageReleases ( httpClient, pageData, result, resultTorrents, types, resultVideos, ignoredIds );
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
            Console.WriteLine ( "Sorry synchronize releases partly not implement yet :(" );
            return;
        }

        static async Task MapPageReleases (
            HttpClient httpClient,
            ReleasesModel model,
            List<ReleaseSaveModel> result,
            List<ReleaseTorrentSaveModel> torrents,
            TypesResultModel types,
            List<ReleaseSaveEpisodeModel> episodes,
            IEnumerable<int> ignoredIds ) {

            var actualReleases = model.Data
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

    private static async Task Main ( string[] args ) {
        var version = Assembly.GetEntryAssembly ()?.GetName ()?.Version;

        Console.WriteLine ( $"Local cache checker version {version?.Major ?? 0}.{version?.Minor ?? 0}.{version?.Build ?? 0} \n" );

        bool isAll = args.Any ( a => a.ToLowerInvariant () == "-all" );
        bool synchronizeSchedule = args.Any ( a => a.ToLowerInvariant () == "-schedule" ) || isAll;
        if ( synchronizeSchedule ) Console.WriteLine ( "Schedule: enabled" );
        bool synchronizeFranchises = args.Any ( a => a.ToLowerInvariant () == "-franchises" ) || isAll;
        if ( synchronizeFranchises ) Console.WriteLine ( "Franchises: enabled" );
        bool synchronizeReleases = args.Any ( a => a.ToLowerInvariant () == "-releases" ) || isAll;
        bool synchronizeFullReleases = args.Any ( a => a.ToLowerInvariant () == "-fullreleases" ) || isAll;
        if ( synchronizeReleases ) Console.WriteLine ( $"Releases {( synchronizeFullReleases ? "fully" : "only latest" )}: enabled" );
        bool synchronizeTypes = args.Any ( a => a.ToLowerInvariant () == "-types" ) || isAll;
        if ( synchronizeTypes ) Console.WriteLine ( "Types: enabled" );
        bool isSaveBlocked = args.Any ( a => a.ToLowerInvariant () == "-saveblocked" );
        if ( isSaveBlocked ) Console.WriteLine ( "Blocked list will be saved" );

        string folderToSaveCacheFiles = "";

        var environmentCachePath = Environment.GetEnvironmentVariable ( "CACHE_PATH" );
        if ( !string.IsNullOrEmpty ( environmentCachePath ) ) folderToSaveCacheFiles = environmentCachePath;

        var httpClient = new HttpClient ();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd ( "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0 LocalCacheChecker/1.0" );

        if ( synchronizeTypes ) await SaveTypes ( httpClient, folderToSaveCacheFiles );
        if ( synchronizeReleases ) await SaveReleases ( httpClient, synchronizeFullReleases, folderToSaveCacheFiles, isSaveBlocked );
        if ( synchronizeSchedule ) await SaveSchedule ( httpClient, folderToSaveCacheFiles );
        if ( synchronizeFranchises ) await SaveReleaseSeries ( httpClient, folderToSaveCacheFiles );
    }

}