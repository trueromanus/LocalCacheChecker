using System.Net;

namespace LocalCacheChecker.Helpers {

    public static class HttpHelper {

        private static HttpClient? currentClient = null;

        public static HttpClient GetHttpClient () {
            if ( currentClient == null ) {
                HttpClientHandler httpClientHandler = new () {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                    AllowAutoRedirect = true
                };

                var httpClient = new HttpClient ( httpClientHandler );
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd ( "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0 LocalCacheChecker/1.0" );
                currentClient = httpClient;
            }

            return currentClient;
        }

    }

}
