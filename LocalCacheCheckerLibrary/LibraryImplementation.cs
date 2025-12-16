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

        public static partial void SynchronizeChangedReleasesInternal ( int maximumPages, ChangedReleasesCallBack callBack ) {

        }

        public static partial void SynchronizeLatestReleasesInternal ( int countReleases, int countPages, LatestReleasesProgress callback ) {

        }

        public static partial void SynchronizeFullReleasesInternal ( FullReleasesProgress callback ) {

        }

    }

}
