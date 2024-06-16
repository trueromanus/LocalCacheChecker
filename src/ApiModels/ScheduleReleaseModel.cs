namespace LocalCacheChecker.ApiModels
{

    internal record ScheduleReleaseModel
    {

        public ScheduleReleaseIdModel Release { get; init; } = new ScheduleReleaseIdModel();

    }

}
