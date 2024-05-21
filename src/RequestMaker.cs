using System.Text.Json;

namespace AnilibriaAPIClient {

    static class RequestMaker {

        private static string m_apiDomain = "https://anilibria.top";

        static public async Task<ReleasesModel> GetPage ( int page, HttpClient httpClient ) {
            var dictionary = new Dictionary<string, string> ();
            dictionary["page"] = "1";
            dictionary["limit"] = "50";

            var serializeOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            HttpResponseMessage pageContent;
            try {
                pageContent = await httpClient.GetAsync ( $"{m_apiDomain}/api/v1/anime/catalog/releases?" + string.Join ( "&", dictionary.Select ( a => $"{a.Key}={a.Value}" ) ) );
            } catch ( Exception ex ) {
                throw new Exception ( $"Can't make HTTP request for page {page}", ex );
            }
            if ( pageContent == null ) throw new Exception ( $"Can't read content for page {page}" );

            string jsonContent = "";
            try {
                jsonContent = await pageContent.Content.ReadAsStringAsync ();
            } catch ( Exception ex ) {
                throw new Exception ( $"Can't read content for page {page}", ex );
            }
            if ( jsonContent == null ) throw new Exception ( $"Can't read content for page {page}" );

            var content = JsonSerializer.Deserialize<ReleasesModel> ( jsonContent, serializeOptions );
            if ( content == null ) throw new Exception ( $"Can't serialize response for page {page}" );

            return content;
        }

        static public async Task<IEnumerable<ScheduleReleaseModel>> GetFullSchedule ( HttpClient httpClient ) {
            var serializeOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            HttpResponseMessage pageContent;
            try {
                pageContent = await httpClient.GetAsync ( $"{m_apiDomain}/api/v1/anime/schedule/week" );
            } catch ( Exception ex ) {
                throw new Exception ( $"Can't make HTTP request for get schedule", ex );
            }
            if ( pageContent == null ) throw new Exception ( $"Can't read content for get schedule" );

            string jsonContent;
            try {
                jsonContent = await pageContent.Content.ReadAsStringAsync ();
            } catch ( Exception ex ) {
                throw new Exception ( $"Can't read content for get schedule", ex );
            }
            if ( string.IsNullOrEmpty ( jsonContent ) ) throw new Exception ( $"JSON content for schedule is empty!" );

            var content = JsonSerializer.Deserialize<IEnumerable<ScheduleReleaseModel>> ( jsonContent, serializeOptions );
            return content == null ? throw new Exception ( $"Can't serialize response for schedule" ) : content;
        }

        static public async Task<IEnumerable<FranchiseModel>> GetAllFranchises ( HttpClient httpClient ) {
            return await PerformRequest<IEnumerable<FranchiseModel>> ( httpClient, $"{m_apiDomain}/api/v1/anime/franchises", "franchise releases" );
        }

        static public async Task<FranchiseReleasesModel> GetFranchisesReleases ( HttpClient httpClient, string id ) {
            return await PerformRequest<FranchiseReleasesModel> ( httpClient, $"{m_apiDomain}/api/v1/anime/franchises/" + id, "franchise releases" );
        }

        private static async Task<T> PerformRequest<T> ( HttpClient httpClient, string url, string requestName ) {
            var serializeOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            HttpResponseMessage pageContent;
            try {
                pageContent = await httpClient.GetAsync ( url );
            } catch ( Exception ex ) {
                throw new Exception ( $"Can't make HTTP request for {requestName}", ex );
            }
            if ( pageContent == null ) throw new Exception ( $"Can't read content for {requestName}" );

            string jsonContent;
            try {
                jsonContent = await pageContent.Content.ReadAsStringAsync ();
            } catch ( Exception ex ) {
                throw new Exception ( $"Can't read content for {requestName}", ex );
            }
            if ( string.IsNullOrEmpty ( jsonContent ) ) throw new Exception ( $"JSON content for {requestName} is empty!" );

            var content = JsonSerializer.Deserialize<T> ( jsonContent, serializeOptions );
            return content == null ? throw new Exception ( $"Can't serialize response for {requestName}" ) : content;
        }

    }

}
