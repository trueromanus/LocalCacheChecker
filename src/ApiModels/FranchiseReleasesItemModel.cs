﻿namespace LocalCacheChecker.ApiModels
{

    public class FranchiseReleasesItemModel
    {

        public int SortOrder { get; set; }

        public int ReleaseId { get; set; }

        public ReleaseDataModel Release { get; set; } = new ReleaseDataModel();

    }

}