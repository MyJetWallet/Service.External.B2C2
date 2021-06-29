using System.Runtime.Serialization;

namespace Service.External.B2C2.Grpc.Models
{
    [DataContract]
    public class GetMarketRequest
    {
        [DataMember(Order = 1)] public string Symbol { get; set; }
    }
}