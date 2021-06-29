using System.Threading.Tasks;
using Service.External.B2C2.Domain.Models.Settings;
using Service.External.B2C2.Domain.Settings;
using Service.External.B2C2.Grpc;
using Service.External.B2C2.Grpc.Models;

namespace Service.External.B2C2.GrpcServices
{
    public class ExternalMarketSettingsManagerGrpc : IExternalMarketSettingsManagerGrpc
    {
        private readonly IExternalMarketSettingsAccessor _accessor;
        private readonly IExternalMarketSettingsManager _manager;

        public ExternalMarketSettingsManagerGrpc(IExternalMarketSettingsAccessor accessor,
            IExternalMarketSettingsManager manager)
        {
            _accessor = accessor;
            _manager = manager;
        }

        public Task GetExternalMarketSettings(GetMarketRequest request)
        {
            return Task.FromResult(_accessor.GetExternalMarketSettings(request.Symbol));
        }

        public Task<GrpcList<ExternalMarketSettings>> GetExternalMarketSettingsList()
        {
            return Task.FromResult(GrpcList<ExternalMarketSettings>.Create(_accessor.GetExternalMarketSettingsList()));
        }

        public Task AddExternalMarketSettings(ExternalMarketSettings settings)
        {
            return _manager.AddExternalMarketSettings(settings);
        }

        public Task UpdateExternalMarketSettings(ExternalMarketSettings settings)
        {
            return _manager.UpdateExternalMarketSettings(settings);
        }

        public Task RemoveExternalMarketSettings(RemoveMarketRequest request)
        {
            return _manager.RemoveExternalMarketSettings(request.Symbol);
        }
    }
}