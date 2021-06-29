using System.ServiceModel;
using System.Threading.Tasks;
using Service.External.B2C2.Domain.Models.Settings;

namespace Service.External.B2C2.Domain.Settings
{
    [ServiceContract]
    public interface IExternalMarketSettingsManager
    {
        [OperationContract]
        Task AddExternalMarketSettings(ExternalMarketSettings settings);

        [OperationContract]
        Task UpdateExternalMarketSettings(ExternalMarketSettings settings);

        [OperationContract]
        Task RemoveExternalMarketSettings(string symbol);
    }
}