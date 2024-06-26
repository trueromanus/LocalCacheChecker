using AnilibriaAPIClient;
using LocalCacheChecker.SaveModels;
using System.Reflection;
using System.Text.Json;
using static LocalCacheChecker.Helpers.JsonHelpers;
using static LocalCacheChecker.SaveReleasesHelper;

internal class Program {


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

        var jsonContent = SerializeToJson ( result );

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
        if ( File.Exists ( path ) ) {
            var content = await File.ReadAllTextAsync ( path );
            var oldContent = JsonSerializer.Deserialize<Dictionary<string, int>> ( content );
            if ( oldContent != null ) {
                var oldItems = oldContent
                    .Select ( a => a.Key + "-" + a.Value )
                    .OrderBy ( a => a )
                    .ToList ();
                var newItem = result
                    .Select ( a => a.Key + "-" + a.Value )
                    .OrderBy ( a => a )
                    .ToList ();
                if ( oldItems.SequenceEqual ( newItem ) && oldItems.Count == newItem.Count ) {
                    Console.WriteLine ( $"Schedule not contains changes!" );
                    return;
                }
            }
        }

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
            if (releasesItem.FranchiseReleases.Count() <= 1) continue; //franchises with single release not actual

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
        if ( File.Exists ( path ) ) {
            var content = await File.ReadAllTextAsync ( path );
            var oldItems = DeserializeFromJson<List<ReleaseSeriesSaveModel>> ( content );
            if ( oldItems != null ) {
                if ( result.All ( a => oldItems.Any ( b => b.Compare ( a ) ) ) && result.Count () == oldItems.Count () ) {
                    Console.WriteLine ( $"Release series not contains changes!" );
                    return;
                }
            }
        }

        Console.WriteLine ( $"Saving to file {Path.GetFullPath ( path )} items" );

        await File.WriteAllTextAsync ( path, SerializeToJson ( result ) );

        Console.WriteLine ( $"Franchises saved!" );
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