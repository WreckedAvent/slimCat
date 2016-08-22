namespace slimCat.Models.Api
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Web.ModelBinding;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Services;

    [DataContract]
    class ApiProfileResponse : IHaveAnErrorMaybe
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "views")]
        public int Views { get; set; }

        [DataMember(Name = "customs_first")]
        public bool CustomsFirst { get; set; }

        [DataMember(Name = "custom_title")]
        public string CustomTitle { get; set; }

        [DataMember(Name = "created_at")]
        public int CreatedAt { get; set; }

        [DataMember(Name = "updated_at")]
        public int UpdatedAt { get; set; }

        [DataMember(Name = "kinks")]
        public IDictionary<string, string> Kinks { get; set; }

        [DataMember(Name = "custom_kinks")]
        public IDictionary<string, ApiCustomKink> CustomKinks { get; set; }

        [DataMember(Name = "images")]
        public IList<ApiProfileImage> Images { get; set; }

        [DataMember(Name = "inlines")]
        public IDictionary<string, ApiProfileInlineImage> Inlines { get; set; }

        [DataMember(Name = "infotags")]
        public IDictionary<string, string> InfoTags { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }
    }
}
