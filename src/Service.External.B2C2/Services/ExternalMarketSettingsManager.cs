using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json;
using Service.External.B2C2.Domain.Models.Settings;
using Service.External.B2C2.Domain.NoSql;
using Service.External.B2C2.Domain.Settings;

namespace Service.External.B2C2.Services
{
    public class ExternalMarketSettingsManager : IExternalMarketSettingsAccessor, IExternalMarketSettingsManager
    {
        private readonly ILogger<ExternalMarketSettingsManager> _logger;

        private readonly IMyNoSqlServerDataWriter<ExternalMarketSettingsNoSql> _writer;
        // private readonly OrderBookManager _orderBookManager;

        private Dictionary<string, ExternalMarketSettings> _externalMarketSettings = new();

        private readonly object _sync = new();
        private readonly string _system = Program.Settings.Name;

        public ExternalMarketSettingsManager(ILogger<ExternalMarketSettingsManager> logger,
            IMyNoSqlServerDataWriter<ExternalMarketSettingsNoSql> writer)
        {
            _logger = logger;
            _writer = writer;
            // _orderBookManager = orderBookManager;

            ReloadSettings().GetAwaiter().GetResult();
        }

        public ExternalMarketSettings GetExternalMarketSettings(string market)
        {
            lock (_sync)
            {
                return _externalMarketSettings[market];
            }
        }

        public List<ExternalMarketSettings> GetExternalMarketSettingsList()
        {
            lock (_sync)
            {
                return _externalMarketSettings.Values.ToList();
            }
        }

        public async Task AddExternalMarketSettings(ExternalMarketSettings settings)
        {
            using var action = MyTelemetry.StartActivity("Add External Market Settings");
            settings.AddToActivityAsJsonTag("settings");
            try
            {
                ValidateSettings(settings);

                var entity = ExternalMarketSettingsNoSql.Create(_system, settings);

                var exist = await _writer.GetAsync(entity.PartitionKey, entity.RowKey);

                if (exist != null)
                {
                    _logger.LogError(
                        "Cannot add new External Market Settings, because already exist settings for market. Request: {jsonText}",
                        JsonConvert.SerializeObject(settings));
                    throw new Exception(
                        $"Cannot add new External Market Settings, because already exist settings for market {settings.Market}");
                }

                await _writer.InsertOrReplaceAsync(entity);

                await ReloadSettings();

                _logger.LogInformation("Added External Market Settings: {jsonText}",
                    JsonConvert.SerializeObject(settings));

                // await _orderBookManager.Resubscribe(settings.Market, settings.GetDoubleLevels());
                //
                // _logger.LogInformation("Resubscribed External Market Settings: {jsonText}",
                //     JsonConvert.SerializeObject(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot add ExternalMarketSettings: {requestJson}",
                    JsonConvert.SerializeObject(settings));
                ex.FailActivity();
                throw;
            }
        }

        public async Task UpdateExternalMarketSettings(ExternalMarketSettings settings)
        {
            using var action = MyTelemetry.StartActivity("Update External Market Settings");
            settings.AddToActivityAsJsonTag("settings");
            try
            {
                ValidateSettings(settings);

                var entity = ExternalMarketSettingsNoSql.Create(_system, settings);

                await _writer.InsertOrReplaceAsync(entity);

                await ReloadSettings();

                _logger.LogInformation("Updated External Market Settings: {jsonText}",
                    JsonConvert.SerializeObject(settings));

                // await _orderBookManager.Resubscribe(settings.Market, settings.GetDoubleLevels());
                //
                // _logger.LogInformation("Resubscribed External Market Settings: {jsonText}",
                //     JsonConvert.SerializeObject(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot update ExternalMarketSettings: {requestJson}",
                    JsonConvert.SerializeObject(settings));
                ex.FailActivity();
                throw;
            }
        }

        public async Task RemoveExternalMarketSettings(string symbol)
        {
            using var action = MyTelemetry.StartActivity("Remove External Market Settings");
            new {symbol}.AddToActivityAsJsonTag("settings");

            try
            {
                var entity = await _writer.DeleteAsync(ExternalMarketSettingsNoSql.GeneratePartitionKey(_system),
                    ExternalMarketSettingsNoSql.GenerateRowKey(symbol));

                if (entity != null)
                    _logger.LogInformation("Removed External Market Settings: {jsonText}",
                        JsonConvert.SerializeObject(entity.Settings));

                await ReloadSettings();

                _logger.LogInformation("Removed External Market Settings: {symbol}", symbol);
            }
            catch (Exception ex)
            {
                ex.FailActivity();
                throw;
            }
        }

        private async Task ReloadSettings()
        {
            var markets = (await _writer.GetAsync(ExternalMarketSettingsNoSql.GeneratePartitionKey(_system)))
                .ToList().Select(e => new ExternalMarketSettings()
                {
                    Market = e.Settings.Market,
                    BaseAsset = e.Settings.BaseAsset,
                    QuoteAsset = e.Settings.QuoteAsset,
                    MinVolume = e.Settings.MinVolume,
                    PriceAccuracy = e.Settings.PriceAccuracy,
                    VolumeAccuracy = e.Settings.VolumeAccuracy,
                    Active = e.Settings.Active,
                    Levels = e.Settings.Levels
                }).ToDictionary(e => e.Market, e => e);

            lock (_sync)
            {
                _externalMarketSettings = markets;
            }
        }

        private static void ValidateSettings(ExternalMarketSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Market)) throw new Exception("Cannot add settings with empty market");
            if (settings.PriceAccuracy < 0) throw new Exception("Cannot add settings with negative price accuracy");
            if (settings.VolumeAccuracy < 0) throw new Exception("Cannot add settings with negative volume accuracy");
            if (settings.MinVolume < 0) throw new Exception("Cannot add settings with negative min volume");
            if (string.IsNullOrEmpty(settings.BaseAsset))
                throw new Exception("Cannot add settings with empty base asset");
            if (string.IsNullOrEmpty(settings.QuoteAsset))
                throw new Exception("Cannot add settings with empty quote asset");
            if (string.IsNullOrEmpty(settings.Levels)) throw new Exception("Cannot add settings with empty levels");
        }
    }
}