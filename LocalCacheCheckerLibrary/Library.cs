using System.Runtime.InteropServices;
using System.Text;

namespace LocalCacheCheckerLibrary {
    public static partial class Library {
        public delegate void RoutineTypesCallBack ( bool completed );

        public delegate void ChangedReleasesCallBack ( bool completed );

        public delegate void LatestReleasesProgress ( int percent, int processesReleases );

        public delegate void FullReleasesProgress ( int percent, int processesReleases, int pagesProcessed );

        [UnmanagedCallersOnly ( EntryPoint = "synchronize_routines" )]
        public static void SynchronizeRoutines ( bool franchises, bool schedule, bool types, nint path, nint callBack ) {
            SynchronizeRoutinesInternal(franchises, schedule, types, Marshal.PtrToStringAnsi(path) ?? "", Marshal.GetDelegateForFunctionPointer<RoutineTypesCallBack>(callBack));
        }

        public static partial void SynchronizeRoutinesInternal ( bool franchises, bool schedule, bool types, string path, RoutineTypesCallBack callBack );

        [UnmanagedCallersOnly ( EntryPoint = "synchronize_changed_releases" )]
        public static void SynchronizeChangedReleases ( int maximumPages, nint callBack ) {
            SynchronizeChangedReleasesInternal(maximumPages, Marshal.GetDelegateForFunctionPointer<ChangedReleasesCallBack>(callBack));
        }

        public static partial void SynchronizeChangedReleasesInternal ( int maximumPages, ChangedReleasesCallBack callBack );

        [UnmanagedCallersOnly ( EntryPoint = "synchronize_latest_releases" )]
        public static void SynchronizeLatestReleases ( int countReleases, int countPages, nint callback ) {
            SynchronizeLatestReleasesInternal(countReleases, countPages, Marshal.GetDelegateForFunctionPointer<LatestReleasesProgress>(callback));
        }

        public static partial void SynchronizeLatestReleasesInternal ( int countReleases, int countPages, LatestReleasesProgress callback );

        [UnmanagedCallersOnly ( EntryPoint = "synchronize_full_releases" )]
        public static void SynchronizeFullReleases ( nint callback ) {
            SynchronizeFullReleasesInternal(Marshal.GetDelegateForFunctionPointer<FullReleasesProgress>(callback));
        }

        public static partial void SynchronizeFullReleasesInternal ( FullReleasesProgress callback );

    }
}