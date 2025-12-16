using AnilibriaAPIClient;
using LocalCacheChecker.SaveModels;
using static LocalCacheChecker.Helpers.JsonHelpers;

namespace LocalCacheChecker {

    public static class SaveRoutineHelpers {

        public static async Task SaveTypes ( HttpClient httpClient, string folderToSaveCacheFiles ) {
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

        public static async Task SaveSchedule ( HttpClient httpClient, string folderToSaveCacheFiles ) {
            Console.WriteLine ( "Start synchronized schedule..." );

            var scheduleData = await RequestMaker.GetFullSchedule ( httpClient );

            var result = new Dictionary<string, int> ();
            if ( !scheduleData.Any () ) return;

            Console.WriteLine ( $"Received {scheduleData.Count ()} items" );

            foreach ( var schedule in scheduleData ) {
                if ( schedule.Release == null ) continue;

                result.Add ( schedule.Release.Id.ToString (), schedule.Release.PublishDay.Value );
            }

            var jsonContent = SerializeToJson ( result );

            var path = Path.Combine ( folderToSaveCacheFiles, "schedule.json" );
            if ( File.Exists ( path ) ) {
                var content = await File.ReadAllTextAsync ( path );
                var oldContent = DeserializeFromJson<Dictionary<string, int>> ( content );
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

        public static async Task SaveReleaseSeries ( HttpClient httpClient, string folderToSaveCacheFiles ) {
            Console.WriteLine ( "Start synchronized franchises..." );
            var franchises = await RequestMaker.GetAllFranchises ( httpClient );

            var result = new List<ReleaseSeriesSaveModel> ();
            if ( !franchises.Any () ) return;

            Console.WriteLine ( $"Received {franchises.Count ()} franchises" );

            foreach ( var franchise in franchises ) {
                var releasesItem = await RequestMaker.GetFranchisesReleases ( httpClient, franchise.Id );
                if ( releasesItem.FranchiseReleases.Count () <= 1 ) continue; //franchises with single release not actual

                var model = new ReleaseSeriesSaveModel {
                    CountReleases = releasesItem.FranchiseReleases.Count (),
                    ReleasesIds = releasesItem.FranchiseReleases.Select ( a => a.ReleaseId ).ToList (),
                    Poster = franchise.Image.Preview,
                    Titles = releasesItem.FranchiseReleases.Select ( a => a.Release.Name.Main ).ToList (),
                    Posters = releasesItem.FranchiseReleases.Select ( a => a.Release.Poster.Src ).ToList (),
                    Title = franchise.Name,
                    Sec = franchise.TotalDurationInSeconds,
                    Eps = franchise.TotalEpisodes,
                    Rat = franchise.Rating ?? 0
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

    }

}
