using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.External.B2C2.Settings
{
    public class SettingsModel
    {
        [YamlProperty("ExternalB2C2.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("ExternalB2C2.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("ExternalB2C2.ElkLogs")] public LogElkSettings ElkLogs { get; set; }

        [YamlProperty("ExternalB2C2.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("ExternalB2C2.ApiToken")]
        public string ApiToken { get; set; }

        [YamlProperty("ExternalB2C2.RefreshBalanceIntervalSec")]
        public int RefreshBalanceIntervalSec { get; set; }
    }
}