using LocalCacheChecker.ApiModels;

namespace LocalCacheChecker.SaveModels
{

    internal record ReleaseTorrentSaveModel : ReleaseTorrentModel
    {

        public int ReleaseId { get; set; }

    }

}
