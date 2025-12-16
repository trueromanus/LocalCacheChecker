using LocalCacheChecker;
using static LocalCacheChecker.Helpers.HttpHelper;

namespace LocalCacheCheckerLibrary {

    public static partial class Library {

        public static partial void SynchronizeRoutinesInternal ( bool franchises, bool schedule, bool types, string path, RoutineTypesCallBack callBack ) {
            Task.Run (
                async () => {
                    try {
                        if ( franchises ) await SaveRoutineHelpers.SaveReleaseSeries ( GetHttpClient (), path );
                        if ( schedule ) await SaveRoutineHelpers.SaveSchedule ( GetHttpClient (), path );
                        if ( types ) await SaveRoutineHelpers.SaveTypes ( GetHttpClient (), path );
                        callBack ( true );
                    } catch {
                        callBack ( false );
                    }
                }
            );
        }

        public static partial void SynchronizeChangedReleasesInternal ( int maximumPages, string path, ChangedReleasesCallBack callBack ) {
            Task.Run (
                async () => {
                    try {
                        await SaveReleasesHelper.SaveReleases ( GetHttpClient (), false, "", false );
                        callBack ( true );
                    } catch {
                        callBack ( false );
                    }
                }
            );
        }

        public static partial void SynchronizeLatestReleasesInternal ( int countReleases, int countPages, string path, LatestReleasesProgress callback ) {
            Task.Run (
                async () => {
                    try {
                        await SaveReleasesHelper.SaveReleases ( GetHttpClient (), false, "", false );
                        // callBack ( true );
                    } catch {
                        // callBack ( false );
                    }
                }
            );
        }

        public static partial void SynchronizeFullReleasesInternal ( string path, FullReleasesProgress callback ) {
            Task.Run (
                async () => {
                    try {
                        await SaveReleasesHelper.SaveReleases ( GetHttpClient (), false, "", false );
                        // callBack ( true );
                    } catch {
                        // callBack ( false );
                    }
                }
            );
        }

        public static partial void ShareCacheInternal ( bool posters, bool torrents, bool releaseCache, string cachePath, string resultPath, ShareCacheCallBack callBack ) {

        }

        public static partial void LoadCacheInternal ( string cacheFile, string cachePath, ShareCacheCallBack callBack ) {

        }

    }

}
