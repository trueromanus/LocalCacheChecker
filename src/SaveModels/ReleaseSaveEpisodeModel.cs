using LocalCacheChecker.ApiModels;

namespace LocalCacheChecker.SaveModels
{

    internal record ReleaseSaveEpisodeModel
    {

        public int ReleaseId { get; init; }

        public IEnumerable<ReleaseEpisodeModel> Items { get; init; } = Enumerable.Empty<ReleaseEpisodeModel>();

    }

}
