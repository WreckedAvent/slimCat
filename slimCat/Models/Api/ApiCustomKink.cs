namespace slimCat.Models.Api
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    class ApiCustomKink
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "choice")]
        public string Choice { get; set; }

        [DataMember(Name = "children")]
        public IList<int> Children { get; set; }
    }
}