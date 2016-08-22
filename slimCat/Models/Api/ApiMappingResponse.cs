namespace slimCat.Models.Api
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Services;

    [DataContract]
    class ApiMappingResponse : IHaveAnErrorMaybe
    {
        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "kinks")]
        public IList<ApiKink> Kinks { get; set; }

        [DataMember(Name = "kink_groups")]
        public IList<ApiGroup> KinkGroups { get; set; }

        [DataMember(Name = "infotags")]
        public IList<ApiInfoTag> InfoTags { get; set; }

        [DataMember(Name = "infotag_groups")]
        public IList<ApiGroup> InfoTagGroups { get; set; }

        [DataMember(Name = "list_items")]
        public IList<ApiListItem> ListItems { get; set; }
    }

    [DataContract]
    public class ApiKink
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "group_id")]
        public int GroupId { get; set; }
    }

    [DataContract]
    public class ApiInfoTag
    {

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "list")]
        public string List { get; set; }

        [DataMember(Name = "group_id")]
        public int GroupId { get; set; }
    }

    [DataContract]
    public class ApiGroup
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }
    }

    [DataContract]
    public class ApiListItem
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "value")]
        public string Value { get; set; }
    }
}
