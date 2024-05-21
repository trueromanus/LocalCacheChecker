namespace AnilibriaAPIClient {

    public record ReleaseSeriesSaveModel {

        public int CountReleases { get; init; } = 0;

        public string Poster { get; set; } = "";

        public IEnumerable<int> Genres { get; init; } = Enumerable.Empty<int> ();

        public IEnumerable<string> Posters { get; init; } = Enumerable.Empty<string> ();

        public IEnumerable<int> ReleasesIds { get; init; } = Enumerable.Empty<int> ();

        public IEnumerable<string> Titles { get; init; } = Enumerable.Empty<string> ();

        public string Title { get; init; } = "";

    }

}
