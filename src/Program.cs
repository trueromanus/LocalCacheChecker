using AnilibriaAPIClient;
using System.Text.Json;

Console.WriteLine ( "Local cache checker version 1.0.0\n" );

bool synchronizeSchedule = true;
bool synchronizeFranchises = true;
bool synchronizeReleases = true;
bool synchronizeFullReleases = true;

var httpClient = new HttpClient ();

static async Task SaveReleases ( HttpClient httpClient, bool synchronizeFullReleases ) {
    Console.WriteLine ( "Start synchronized releases..." );

    if ( synchronizeFullReleases ) {
        Console.WriteLine ( "Try to get first page" );

        var result = new List<ReleaseDataModel> ();

        var firstPage = await RequestMaker.GetPage ( 1, httpClient );

        var totalPages = firstPage.Meta.Pagination.TotalPages;
        Console.WriteLine ( "Total pages: " + totalPages );

        result.AddRange ( firstPage.Data );

        for ( var i = 2; i < totalPages; i++ ) {
            Console.WriteLine ( "Load page: " + i );

            var pageData = await RequestMaker.GetPage ( i, httpClient );
            result.AddRange ( pageData.Data );
        }
    } else {

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

