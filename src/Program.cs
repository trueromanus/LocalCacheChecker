using AnilibriaAPIClient;
using LocalCacheChecker;
using System.Reflection;
using System.Text.Json;

var version = Assembly.GetEntryAssembly ()?.GetName ()?.Version;

Console.WriteLine ( $"Local cache checker version {( version?.Major ?? 0 )}.{( version?.Minor ?? 0 )}.{( version?.Build ?? 0 )} \n" );

bool synchronizeSchedule = false;
bool synchronizeFranchises = false;
bool synchronizeReleases = false;
bool synchronizeFullReleases = false;
bool synchronizeTypes = true;

var httpClient = new HttpClient ();

static async Task SaveTypes ( HttpClient httpClient ) {
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

    Console.WriteLine ( $"Saving to file {Path.GetFullPath ( "types.cache" )} items" );

    await File.WriteAllTextAsync ( "types.cache", jsonContent );

    Console.WriteLine ( $"Types saved!" );
}
if ( synchronizeTypes ) await SaveTypes ( httpClient );

static async Task SaveReleases ( HttpClient httpClient, bool synchronizeFullReleases ) {
    Console.WriteLine ( "Start synchronized releases..." );

    if ( synchronizeFullReleases ) {
        Console.WriteLine ( "Try to get first page" );

        var result = new List<ReleaseSaveModel> ();

        var firstPage = await RequestMaker.GetPage ( 1, httpClient );

        var totalPages = firstPage.Meta.Pagination.TotalPages;
        Console.WriteLine ( "Total pages: " + totalPages );

        result.AddRange ( MapForSave ( firstPage.Data ) );

        for ( var i = 2; i < totalPages; i++ ) {
            Console.WriteLine ( "Load page: " + i );

            var pageData = await RequestMaker.GetPage ( i, httpClient );
            result.AddRange ( MapForSave ( pageData.Data ) );
        }

        var jsonContent = JsonSerializer.Serialize<IEnumerable<ReleaseSaveModel>> (
            result,
            new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }
        );

        Console.WriteLine ( $"Saving to file {Path.GetFullPath ( "releases.cache" )} items" );

        await File.WriteAllTextAsync ( "releases.cache", jsonContent );
    } else {

    }

    static IEnumerable<ReleaseSaveModel> MapForSave ( IEnumerable<ReleaseDataModel> items ) {
        var result = new List<ReleaseSaveModel> ();
        foreach ( var item in items ) {
            result.Add (
                new ReleaseSaveModel {
                    Id = item.Id,
                    Announce = item.Notification ?? "",
                    Code = item.Alias,
                    CountVideos = 0,
                    CountTorrents = 0,
                    Description = item.Description,
                    Timestamp = DateTimeOffset.Parse ( item.FreshAt ).ToUnixTimeSeconds (),
                    OriginalName = item.Name.English,
                    Title = item.Name.Main,
                    Rating = 0,
                    Year = item.Year,
                    Season = item.Season.Value,
                    Status = item.IsInProduction ? "Сейчас в озвучке" : "Озвучка завершена",
                    Series = "",
                    Poster = item.Poster.Src,
                    Type = item.Type.Value,
                    Genres = string.Join ( ", ", item.Genres.Select ( a => a.Id ) ),
                    IsOngoing = item.IsOngoing,
                    AgeRating = item.AgeRating.Value
                }
            ); ;
        }

        return result;
    }
}
if ( synchronizeReleases ) await SaveReleases ( httpClient, synchronizeFullReleases );

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

