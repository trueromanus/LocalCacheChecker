using LocalCacheChecker;
using System.Net;
using System.Reflection;
using static LocalCacheChecker.SaveReleasesHelper;

internal class Program {

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
        bool isSavePosters = args.Any ( a => a.ToLowerInvariant () == "-saveposters" );
        if ( isSavePosters ) Console.WriteLine ( "Posters will be saved" );

        string folderToSaveCacheFiles = "";

        var environmentCachePath = Environment.GetEnvironmentVariable ( "CACHE_PATH" );
        if ( !string.IsNullOrEmpty ( environmentCachePath ) ) folderToSaveCacheFiles = environmentCachePath;

        HttpClientHandler httpClientHandler = new () {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true
        };

        var httpClient = new HttpClient ( httpClientHandler );
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd ( "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0 LocalCacheChecker/1.0" );

        if ( synchronizeTypes ) await SaveRoutineHelpers.SaveTypes ( httpClient, folderToSaveCacheFiles );
        if ( synchronizeReleases ) await SaveReleases ( httpClient, synchronizeFullReleases, folderToSaveCacheFiles, isSaveBlocked );
        if ( synchronizeSchedule ) await SaveRoutineHelpers.SaveSchedule ( httpClient, folderToSaveCacheFiles );
        if ( synchronizeFranchises ) await SaveRoutineHelpers.SaveReleaseSeries ( httpClient, folderToSaveCacheFiles );
        if ( isSavePosters ) await SynchronizeAllPosters ( httpClient, folderToSaveCacheFiles, ( percent, count ) => { } );
    }

}