namespace slimCat.Models.Api
{
    using System.Runtime.Serialization;
    using Services;

    [DataContract]
    class ApiGenericResponse : IHaveAnErrorMaybe
    {
        [DataMember(Name = "error")]
        public string Error { get; set; }
    }
}
