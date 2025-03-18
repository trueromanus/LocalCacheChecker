namespace LocalCacheChecker.SaveModels {

    public record ReleaseSeriesSaveModel {

        public int CountReleases { get; init; } = 0;

        public string Poster { get; init; } = "";

        public IEnumerable<string> Posters { get; init; } = [];

        public IEnumerable<int> ReleasesIds { get; init; } = [];

        public IEnumerable<string> Titles { get; init; } = [];

        public string Title { get; init; } = "";

        public long Sec { get; set; }

        public int Eps { get; set; }

        public decimal Rat { get; set; }

        public bool Compare ( ReleaseSeriesSaveModel model ) {
            if ( CountReleases != model.CountReleases ) return false;
            if ( Poster != model.Poster ) return false;
            if ( Title != model.Title ) return false;
            if ( !Posters.SequenceEqual ( model.Posters ) ) return false;
            if ( !ReleasesIds.SequenceEqual ( model.ReleasesIds ) ) return false;
            if ( !Titles.SequenceEqual ( model.Titles ) ) return false;
            if ( Sec != model.Sec ) return false;
            if ( Eps != model.Eps ) return false;
            if ( Rat != model.Rat ) return false;

            return true;
        }

    }

}
