namespace LocalCacheChecker.ApiModels
{

    internal record ReleaseMemberModel
    {

        public string Nickname { get; set; } = "";

        public StringValueItem Role { get; set; } = new StringValueItem();

    }

}
