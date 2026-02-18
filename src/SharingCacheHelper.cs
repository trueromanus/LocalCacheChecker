using System.IO.Compression;

namespace LocalCacheChecker
{

    public static class SharingCacheHelper
    {

        public static void SaveChacheToFolder(string cachePath, string resultFile, bool includePosters, bool includeTorrents, bool includeReleaseCache)
        {
            if (!Directory.Exists(cachePath)) throw new Exception($"Folder {cachePath} not exists!");
            if (!includePosters && !includeTorrents && !includeReleaseCache) return;

            Console.WriteLine("Start create cache for folder: " + cachePath);
            Console.WriteLine("Cache options: " + cachePath);

            var timeStamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var shareFileName = $"share-{timeStamp}.zip";

            using var zipToOpen = new FileStream(Path.Combine(resultFile, shareFileName), FileMode.Create);
            using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);

            if (includePosters)
            {
                var postersDirectoryName = Path.Combine(cachePath, "imagecache");
                if (Directory.Exists(postersDirectoryName))
                {
                    var posters = Directory.EnumerateFiles(postersDirectoryName);
                    foreach (var poster in posters)
                    {
                        var posterFileName = Path.GetFileName(poster);
                        archive.CreateEntryFromFile(poster, "imagecache/" + posterFileName);
                    }
                }
            }
            if (includeReleaseCache)
            {
                var cacheFiles = Directory.EnumerateFiles(cachePath);
                foreach (var cacheFile in cacheFiles)
                {
                    var onlyFileName = Path.GetFileName(cacheFile);
                    if (onlyFileName.StartsWith("metadata") ||
                        onlyFileName.StartsWith("episodes") ||
                        onlyFileName.StartsWith("releases") ||
                        onlyFileName.StartsWith("releaseseries") ||
                        onlyFileName.StartsWith("schedule") ||
                        onlyFileName.StartsWith("torrents") ||
                        onlyFileName.StartsWith("types"))
                    {
                        archive.CreateEntryFromFile(cacheFile, onlyFileName);
                    }
                }
            }
        }

        public static void LoadFromFolder(string cacheFile, string cachePath)
        {
            if (!Directory.Exists(cachePath)) throw new Exception($"Folder {cachePath} not exists!");
            if (!File.Exists(cacheFile)) throw new Exception($"File {cacheFile} not exists!");

            using var zipToOpen = new FileStream(cacheFile, FileMode.Open);
            using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                var isPoster = entry.FullName.StartsWith("imagecache");
                var isReleaseCache = entry.Name.Contains(".cache") || entry.Name.Contains("metadata");
                if (isPoster)
                {
                    var path = Path.Combine(cachePath, "imagecache");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    var cacheFileName = Path.Combine(cachePath, "imagecache", entry.Name);

                    entry.ExtractToFile(cacheFileName, overwrite: true);
                }
                if (isReleaseCache)
                {
                    entry.ExtractToFile(Path.Combine(cachePath, entry.Name), overwrite: true);
                }
            }
        }

    }

}
