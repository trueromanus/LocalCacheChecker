using System.Runtime.InteropServices;
using System.Text;

namespace LocalCacheCheckerLibrary {
    public static partial class Library {
        public delegate void RoutineTypesCallBack ( bool completed );

        public delegate void ChangedReleasesCallBack ( bool completed );

        public delegate void LatestReleasesProgress ( int percent, int processesReleases );

        public delegate void FullReleasesProgress ( int percent, int processesReleases, int pagesProcessed );

        public delegate void ShareCacheCallBack ( bool completed, nint message );

        [UnmanagedCallersOnly ( EntryPoint = "synchronize_routines" )]
        public static void SynchronizeRoutines ( bool franchises, bool schedule, bool types, nint path, nint callBack ) {
            SynchronizeRoutinesInternal(franchises, schedule, types, Marshal.PtrToStringAnsi(path) ?? "", Marshal.GetDelegateForFunctionPointer<RoutineTypesCallBack>(callBack));
        }

        public static partial void SynchronizeRoutinesInternal ( bool franchises, bool schedule, bool types, string path, RoutineTypesCallBack callBack );

        [UnmanagedCallersOnly ( EntryPoint = "synchronize_changed_releases" )]
        public static void SynchronizeChangedReleases ( int maximumPages, nint path, nint callBack ) {
            SynchronizeChangedReleasesInternal(maximumPages, Marshal.PtrToStringAnsi(path) ?? "", Marshal.GetDelegateForFunctionPointer<ChangedReleasesCallBack>(callBack));
        }

        public static partial void SynchronizeChangedReleasesInternal ( int maximumPages, string path, ChangedReleasesCallBack callBack );

        [UnmanagedCallersOnly ( EntryPoint = "synchronize_latest_releases" )]
        public static void SynchronizeLatestReleases ( int countReleases, int countPages, nint path, nint callback ) {
            SynchronizeLatestReleasesInternal(countReleases, countPages, Marshal.PtrToStringAnsi(path) ?? "", Marshal.GetDelegateForFunctionPointer<LatestReleasesProgress>(callback));
        }

        public static partial void SynchronizeLatestReleasesInternal ( int countReleases, int countPages, string path, LatestReleasesProgress callback );

        [UnmanagedCallersOnly ( EntryPoint = "synchronize_full_releases" )]
        public static void SynchronizeFullReleases ( nint path, nint callback ) {
            SynchronizeFullReleasesInternal(Marshal.PtrToStringAnsi(path) ?? "", Marshal.GetDelegateForFunctionPointer<FullReleasesProgress>(callback));
        }

        public static partial void SynchronizeFullReleasesInternal ( string path, FullReleasesProgress callback );

        [UnmanagedCallersOnly ( EntryPoint = "share_cache" )]
        public static void ShareCache ( bool posters, bool torrents, bool releaseCache, nint cachePath, nint resultPath, nint callBack ) {
            ShareCacheInternal(posters, torrents, releaseCache, Marshal.PtrToStringAnsi(cachePath) ?? "", Marshal.PtrToStringAnsi(resultPath) ?? "", Marshal.GetDelegateForFunctionPointer<ShareCacheCallBack>(callBack));
        }

        public static partial void ShareCacheInternal ( bool posters, bool torrents, bool releaseCache, string cachePath, string resultPath, ShareCacheCallBack callBack );

        [UnmanagedCallersOnly ( EntryPoint = "load_cache" )]
        public static void LoadCache ( nint cacheFile, nint cachePath, nint callBack ) {
            LoadCacheInternal(Marshal.PtrToStringAnsi(cacheFile) ?? "", Marshal.PtrToStringAnsi(cachePath) ?? "", Marshal.GetDelegateForFunctionPointer<ShareCacheCallBack>(callBack));
        }

        public static partial void LoadCacheInternal ( string cacheFile, string cachePath, ShareCacheCallBack callBack );

    }
}