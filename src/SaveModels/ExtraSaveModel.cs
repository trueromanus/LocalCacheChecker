namespace LocalCacheChecker.SaveModels
{

    internal record ExtraSaveModel
    {

        public List<ReleaseSaveModel> Releases { get; set; }

        public List<ReleaseTorrentSaveModel> Torrents { get; set; }

        public List<ReleaseSaveEpisodeModel> Videos { get; set; }

    }

}
