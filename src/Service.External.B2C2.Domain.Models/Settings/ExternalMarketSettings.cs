using System.Linq;
using System.Runtime.Serialization;

namespace Service.External.B2C2.Domain.Models.Settings
{
    [DataContract]
    public class ExternalMarketSettings
    {
        [DataMember(Order = 1)] public string Market { get; set; }
        [DataMember(Order = 2)] public int PriceAccuracy { get; set; }
        [DataMember(Order = 3)] public double MinVolume { get; set; }
        [DataMember(Order = 4)] public string BaseAsset { get; set; }
        [DataMember(Order = 5)] public string QuoteAsset { get; set; }
        [DataMember(Order = 6)] public int VolumeAccuracy { get; set; }
        [DataMember(Order = 7)] public string Levels { get; set; }
        [DataMember(Order = 8)] public bool Active { get; set; }

        public double[] GetDoubleLevels()
        {
            return Levels.Split(";").Select(double.Parse).ToArray();
        }
    }
}