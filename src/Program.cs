using AnilibriaAPIClient;
using LocalCacheChecker;
using System.Reflection;
using System.Text.Json;

var version = Assembly.GetEntryAssembly ()?.GetName ()?.Version;

Console.WriteLine ( $"Local cache checker version {( version?.Major ?? 0 )}.{( version?.Minor ?? 0 )}.{( version?.Build ?? 0 )} \n" );

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

string folderToSaveCacheFiles = "";

var httpClient = new HttpClient ();

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

    var path = Path.Combine ( folderToSaveCacheFiles, "types.cache" );
    Console.WriteLine ( $"Saving to file {Path.GetFullPath ( path )} items" );

    await File.WriteAllTextAsync ( path, jsonContent );

    Console.WriteLine ( $"Types saved!" );
}
if ( synchronizeTypes ) await SaveTypes ( httpClient, folderToSaveCacheFiles );

static async Task SaveReleases ( HttpClient httpClient, bool synchronizeFullReleases, string folderToSaveCacheFiles ) {
    Console.WriteLine ( "Start synchronized releases..." );

    if ( synchronizeFullReleases ) {
        Console.WriteLine ( "Try to get first page" );

        var result = new List<ReleaseSaveModel> ();
        var resultTorrents = new List<ReleaseTorrentSaveModel> ();

        var firstPage = await RequestMaker.GetPage ( 1, httpClient );

        var totalPages = firstPage.Meta.Pagination.TotalPages;
        Console.WriteLine ( "Total pages: " + totalPages );

        var pathToTypes = Path.Combine ( folderToSaveCacheFiles, "types.cache" );
        if ( !File.Exists ( pathToTypes ) ) {
            Console.WriteLine ( $"File types.cache not found by path {Path.GetFullPath ( pathToTypes )}. You need synchronize types, please add -types or -all parameters to command!" );
            return;
        }
        var types = JsonSerializer.Deserialize<TypesResultModel> (
            await File.ReadAllTextAsync ( pathToTypes ),
            options: new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            }
        );
        if ( types == null ) {
            Console.WriteLine ( $"Content of types.cache is corrupt. You need synchronize types, please add -types or -all parameters to command!" );
            return;
        }

        await MapPageReleases ( httpClient, firstPage, result, resultTorrents, types );

        for ( var i = 2; i < totalPages; i++ ) {
            Console.WriteLine ( "Load page: " + i );

            var pageData = await RequestMaker.GetPage ( i, httpClient );
            await MapPageReleases ( httpClient, firstPage, result, resultTorrents, types );
        }

        var jsonContent = JsonSerializer.Serialize<IEnumerable<ReleaseSaveModel>> (
            result,
            new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }
        );

        var path = Path.Combine ( folderToSaveCacheFiles, "releases.cache" );
        Console.WriteLine ( $"Saving to file {Path.GetFullPath ( path )} items" );

        await File.WriteAllTextAsync ( path, jsonContent );
    } else {
        Console.WriteLine ( "Sorry synchronize releases partly no implement yet :(" );
        return;
    }

    static async Task MapPageReleases ( HttpClient httpClient, ReleasesModel model, List<ReleaseSaveModel> result, List<ReleaseTorrentSaveModel> torrents, TypesResultModel types ) {
        var relatedStuff = await GetRelatedStuffForReleases ( httpClient, model.Data.Select ( a => a.Id ).ToList () );
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
        result.AddRange ( MapForSave ( model.Data, relatedStuff, types ) );
    }

    static IEnumerable<ReleaseSaveModel> MapForSave ( IEnumerable<ReleaseDataModel> items, IEnumerable<(int releaseId, IEnumerable<ReleaseTorrentModel> torrents, IEnumerable<ReleaseMemberModel> members)> relatedStuff, TypesResultModel types ) {
        var result = new List<ReleaseSaveModel> ();
        foreach ( var item in items ) {
            var (releaseId, torrents, members) = relatedStuff.FirstOrDefault ( a => a.releaseId == item.Id );
            result.Add (
                new ReleaseSaveModel {
                    Id = item.Id,
                    Announce = item.Notification ?? "",
                    Code = item.Alias,
                    CountVideos = 0, //TODO: videos
                    CountTorrents = torrents?.Count () ?? 0,
                    Description = item.Description,
                    Timestamp = DateTimeOffset.Parse ( item.FreshAt ).ToUnixTimeSeconds (),
                    OriginalName = item.Name.English,
                    Title = item.Name.Main,
                    Rating = item.AddedInUsersFavorites ?? 0,
                    YearInt = item.Year,
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

    static async Task<IEnumerable<(int releaseId, IEnumerable<ReleaseTorrentModel> torrents, IEnumerable<ReleaseMemberModel> members)>> GetRelatedStuffForReleases ( HttpClient httpClient, IEnumerable<int> ids ) {
        var result = new List<(int, IEnumerable<ReleaseTorrentModel>, IEnumerable<ReleaseMemberModel>)> ();
        foreach ( int releaseId in ids ) {
            var torrents = await RequestMaker.GetTorrentsForRelease ( httpClient, releaseId );
            var members = await RequestMaker.GetTeamForRelease ( httpClient, releaseId );

            result.Add ( (releaseId, torrents, members) );
        }

        return result;
    }
}
if ( synchronizeReleases ) await SaveReleases ( httpClient, synchronizeFullReleases, folderToSaveCacheFiles );

static async Task SaveSchedule ( HttpClient httpClient ) {
    Console.WriteLine ( "Start synchronized schedule..." );

    var scheduleData = await RequestMaker.GetFullSchedule ( httpClient );

    var result = new Dictionary<string, int> ();
    if ( !scheduleData.Any () ) return;

    Console.WriteLine ( $"Received {scheduleData.Count ()} items" );

    foreach ( var schedule in scheduleData ) {
        if ( schedule.Release == null ) continue;

        result.Add ( schedule.Release.Id.ToString (), schedule.Release.PublishDay.Value );
    }

    var jsonContent = JsonSerializer.Serialize<Dictionary<string, int>> ( result );

    Console.WriteLine ( $"Saving to file {Path.GetFullPath ( "schedule.cache" )} items" );

    await File.WriteAllTextAsync ( "schedule.cache", jsonContent );

    Console.WriteLine ( $"Schedule saved!" );
}
if ( synchronizeSchedule ) await SaveSchedule ( httpClient );

static async Task SaveReleaseSeries ( HttpClient httpClient ) {
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

    var jsonContent = JsonSerializer.Serialize (
        result,
        new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }
    );

    Console.WriteLine ( $"Saving to file {Path.GetFullPath ( "releaseseries.cache" )} items" );

    await File.WriteAllTextAsync ( "releaseseries.cache", jsonContent );

    Console.WriteLine ( $"Franchises saved!" );
}
if ( synchronizeFranchises ) await SaveReleaseSeries ( httpClient );

//collections
//FAVORITES
//PLANNED
//WATCHED
//WATCHING
/*
const PLANNED = 'PLANNED';
    const WATCHED = 'WATCHED';
    const WATCHING = 'WATCHING';
    const FAVORITES = 'FAVORITES';
    const POSTPONED = 'POSTPONED';
    const ABANDONED = 'ABANDONED';
*/

