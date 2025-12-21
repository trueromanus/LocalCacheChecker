using LocalCacheChecker;
using System.Runtime.InteropServices;
using static LocalCacheChecker.Helpers.HttpHelper;

namespace LocalCacheCheckerLibrary {

    public static partial class Library {

        public static partial void SynchronizeRoutinesInternal ( bool franchises, bool schedule, bool types, string path, RoutineTypesCallBack callBack ) {
            Console.WriteLine ( $"SynchronizeRoutinesInternal:franchises={franchises},schedule={schedule},types={types},path={path}" );
            Task.Run (
                async () => {
                    try {

                        if ( types ) await SaveRoutineHelpers.SaveTypes ( GetHttpClient (), path );
                        if ( schedule ) await SaveRoutineHelpers.SaveSchedule ( GetHttpClient (), path );
                        if ( franchises ) await SaveRoutineHelpers.SaveReleaseSeries ( GetHttpClient (), path );
                        callBack ( true );
                    } catch ( Exception ex ) {
                        Console.WriteLine ( $"SynchronizeRoutinesInternal:failed " + ex.Message );
                        callBack ( false );
                    }
                }
            );
        }

        public static partial void SynchronizeChangedReleasesInternal ( int maximumPages, string path, ChangedReleasesCallBack callBack ) {
            Console.WriteLine ( $"SynchronizeChangedReleasesInternal:maximumPages={maximumPages},path={path}" );
            Task.Run (
                async () => {
                    try {
                        await SaveReleasesHelper.SaveReleases ( GetHttpClient (), false, path, false );
                        callBack ( true );
                    } catch ( Exception ex ) {
                        Console.WriteLine ( $"SynchronizeChangedReleasesInternal:failed " + ex.Message );
                        callBack ( false );
                    }
                }
            );
        }

        public static partial void SynchronizeLatestReleasesInternal ( int countReleases, int countPages, string path, LatestReleasesProgress callback ) {
            Task.Run (
                async () => {
                    try {
                        await SaveReleasesHelper.SaveReleases ( GetHttpClient (), false, path, false );
                        //callback ( 1, );
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
                        //callback ( true );
                    } catch {
                        // callBack ( false );
                    }
                }
            );
        }

        public static partial void ShareCacheInternal ( bool posters, bool torrents, bool releaseCache, string cachePath, string resultPath, ShareCacheCallBack callBack ) {
            Task.Run (
                () => {
                    try {
                        SharingCacheHelper.SaveChacheToFolder ( cachePath, resultPath, posters, torrents, releaseCache );

                        var message = Marshal.StringToHGlobalAnsi ( "" );
                        callBack ( false, message );
                        Marshal.FreeHGlobal ( message );

                    } catch ( Exception ex ) {
                        var message = Marshal.StringToHGlobalAnsi ( ex.Message );
                        callBack ( false, message );
                        Marshal.FreeHGlobal ( message );
                    }
                }
            );
        }

        public static partial void LoadCacheInternal ( string cacheFile, string cachePath, ShareCacheCallBack callBack ) {
            Task.Run (
                () => {
                    try {
                        SharingCacheHelper.LoadFromFolder ( cacheFile, cachePath );

                        var message = Marshal.StringToHGlobalAnsi ( "" );
                        callBack ( false, message );
                        Marshal.FreeHGlobal ( message );

                    } catch ( Exception ex ) {
                        var message = Marshal.StringToHGlobalAnsi ( ex.Message );
                        callBack ( false, message );
                        Marshal.FreeHGlobal ( message );
                    }
                }
            );
        }

    }

}
