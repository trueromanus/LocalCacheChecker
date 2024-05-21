namespace AnilibriaAPIClient {

    public class ScheduleReleaseIdModel {

        public int Id { get; set; }

        public ReleaseDataModelPublishDay PublishDay { get; init; } = new ReleaseDataModelPublishDay ();

    }

}