using System.ServiceModel;
using System.Threading.Tasks;
using Service.External.B2C2.Domain.Models.Settings;
using Service.External.B2C2.Grpc.Models;

namespace Service.External.B2C2.Grpc
{
    [ServiceContract]
    public interface IExternalMarketSettingsManagerGrpc
    {
        [OperationContract]
        Task GetExternalMarketSettings(GetMarketRequest request);

        [OperationContract]
        Task<GrpcList<ExternalMarketSettings>> GetExternalMarketSettingsList();

        [OperationContract]
        Task AddExternalMarketSettings(ExternalMarketSettings settings);

        [OperationContract]
        Task UpdateExternalMarketSettings(ExternalMarketSettings settings);

        [OperationContract]
        Task RemoveExternalMarketSettings(RemoveMarketRequest request);
    }
}