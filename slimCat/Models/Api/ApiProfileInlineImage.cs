namespace slimCat.Models.Api
{
    using System.Runtime.Serialization;

    [DataContract]
    class ApiProfileInlineImage
    {
        [DataMember(Name = "hash")]
        public string Hash { get; set; }

        [DataMember(Name = "extension")]
        public string Extension { get; set; }

        [DataMember(Name = "nsfw")]
        public bool Nsfw { get; set; }
    }
}