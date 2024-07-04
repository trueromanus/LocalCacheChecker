namespace LocalCacheChecker.SaveModels
{

    public record ReleaseSeriesSaveModel
    {

        public int CountReleases { get; init; } = 0;

        public string Poster { get; init; } = "";

        public IEnumerable<string> Posters { get; init; } = [];

        public IEnumerable<int> ReleasesIds { get; init; } = [];

        public IEnumerable<string> Titles { get; init; } = [];

        public string Title { get; init; } = "";

        public bool Compare(ReleaseSeriesSaveModel model)
        {
            if (CountReleases != model.CountReleases) return false;
            if (Poster != model.Poster) return false;
            if (Title != model.Title) return false;
            if (!Posters.SequenceEqual(model.Posters)) return false;
            if (!ReleasesIds.SequenceEqual(model.ReleasesIds)) return false;
            if (!Titles.SequenceEqual(model.Titles)) return false;

            return true;
        }

    }

}
