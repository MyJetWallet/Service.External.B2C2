using System.Collections.Generic;
using System.ServiceModel;
using Service.External.B2C2.Domain.Models.Settings;

namespace Service.External.B2C2.Domain.Settings
{
    [ServiceContract]
    public interface IExternalMarketSettingsAccessor
    {
        [OperationContract]
        ExternalMarketSettings GetExternalMarketSettings(string market);

        [OperationContract]
        List<ExternalMarketSettings> GetExternalMarketSettingsList();
    }
}