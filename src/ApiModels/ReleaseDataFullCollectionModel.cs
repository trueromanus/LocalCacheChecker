namespace LocalCacheChecker.ApiModels
{

    internal class ReleaseDataFullCollectionModel
    {

        public IEnumerable<ReleaseDataFullModel> Data { get; set; } = Enumerable.Empty<ReleaseDataFullModel>();

    }

}
