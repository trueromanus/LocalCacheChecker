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

        public static void LoadFromFolder ( string cacheFile, string cachePath ) {
            if ( !Directory.Exists ( cachePath ) ) throw new Exception ( $"Folder {cachePath} not exists!" );
            if ( !File.Exists ( cacheFile ) ) throw new Exception ( $"File {cacheFile} not exists!" );

            using var zipToOpen = new FileStream ( cacheFile, FileMode.Open );
            using var archive = new ZipArchive ( zipToOpen, ZipArchiveMode.Read );

            foreach ( var entry in archive.Entries ) {
                var isPoster = entry.FullName.StartsWith ( "imagecache" );
                var isReleaseCache = entry.Name.Contains ( ".cache" ) || entry.Name.Contains ( "metadata" );
                if ( isPoster ) {
                    var cacheFileName = Path.Combine ( cachePath, "imagecache", entry.Name );
                    var imageExists = File.Exists ( cacheFileName );
                    if ( !imageExists ) entry.ExtractToFile ( cacheFileName );
                }
                if ( isReleaseCache ) {
                    entry.ExtractToFile ( Path.Combine ( cachePath, entry.Name ) );
                }
            }
        }

    }

}
