using MyNoSqlServer.Abstractions;
using Service.External.B2C2.Domain.Models.Settings;

namespace Service.External.B2C2.Domain.NoSql
{
    public class ExternalMarketSettingsNoSql : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-external-market-settings";

        public static string GeneratePartitionKey(string externalSystem) => externalSystem;
        public static string GenerateRowKey(string market) => market;

        public ExternalMarketSettings Settings { get; set; }

        public static ExternalMarketSettingsNoSql Create(string externalSystem, ExternalMarketSettings settings)
        {
            return new ExternalMarketSettingsNoSql()
            {
                PartitionKey = GeneratePartitionKey(externalSystem),
                RowKey = GenerateRowKey(settings.Market),
                Settings = settings
            };
        }
    }
}