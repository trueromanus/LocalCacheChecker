using System.IO.Compression;

namespace LocalCacheChecker {

    public static class SharingCacheHelper {

        public static void SaveChacheToFolder ( string cachePath, string resultFile, bool includePosters, bool includeTorrents, bool includeReleaseCache ) {
            if ( !Directory.Exists ( cachePath ) ) throw new Exception ( $"Folder {cachePath} not exists!" );
            if ( !includePosters && !includeTorrents && !includeReleaseCache ) return;

            using var zipToOpen = new FileStream ( resultFile, FileMode.Create );
            using var archive = new ZipArchive ( zipToOpen, ZipArchiveMode.Create );

            if ( includePosters ) {
                var postersDirectoryName = Path.Combine ( cachePath, "imagecache" );
                if ( Directory.Exists ( postersDirectoryName ) ) {
                    var posters = Directory.EnumerateFiles ( postersDirectoryName );
                    foreach ( var poster in posters ) {
                        archive.CreateEntryFromFile ( Path.Combine ( postersDirectoryName, poster ), "imagecache/" + poster );
                    }
                }
            }
            if ( includeReleaseCache ) {
                var cacheFiles = Directory.EnumerateFiles ( cachePath );
                foreach ( var cacheFile in cacheFiles ) {
                    if ( cacheFile.StartsWith ( "metadata" ) || cacheFile.Contains ( ".cache" ) ) {
                        archive.CreateEntryFromFile ( Path.Combine ( cachePath, cacheFile ), cacheFile );
                    }
                }
            }
        }

    }

}
