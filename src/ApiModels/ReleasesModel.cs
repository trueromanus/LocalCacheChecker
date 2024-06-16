namespace LocalCacheChecker.ApiModels
{

    /// <summary>
    /// Releases model.
    /// </summary>
    internal sealed class ReleasesModel
    {

        public ReleaseMetaModel Meta { get; set; } = new ReleaseMetaModel();

        public IEnumerable<ReleaseDataModel> Data { get; set; } = Enumerable.Empty<ReleaseDataModel>();

    }

}
