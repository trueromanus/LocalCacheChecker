namespace LocalCacheChecker.ApiModels
{

    internal record ReleaseTorrentModel
    {

        public string Hash { get; init; } = "";

        public int Id { get; init; }

        public string Magnet { get; init; } = "";

        public string FileName { get; init; } = "";

        public string Description { get; init; } = "";

        public StringValueItem Quality { get; init; } = new StringValueItem();

        public StringValueItem Codec { get; init; } = new StringValueItem();

        public long Size { get; init; }

    }
}
