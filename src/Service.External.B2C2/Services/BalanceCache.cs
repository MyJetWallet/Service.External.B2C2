using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.B2C2.Rest;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.Tools;

namespace Service.External.B2C2.Services
{
    public class BalanceCache : IStartable, IDisposable
    {
        private readonly ILogger<BalanceCache> _logger;
        private readonly B2C2RestApi _restApi;
        private readonly OrderBookManager _orderBookManager;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;

        private Dictionary<string, ExchangeBalance> _balances = new();

        private readonly MyTaskTimer _timer;

        public BalanceCache(B2C2RestApi restApi, OrderBookManager orderBookManager,
            IExternalMarketSettingsAccessor externalMarketSettingsAccessor, ILogger<BalanceCache> logger)
        {
            _restApi = restApi;
            _orderBookManager = orderBookManager;
            _externalMarketSettingsAccessor = externalMarketSettingsAccessor;
            _logger = logger;

            _timer = new MyTaskTimer(nameof(BalanceCache), TimeSpan.FromSeconds(1), logger, DoTimer);
        }

        private async Task DoTimer()
        {
            _timer.ChangeInterval(
                TimeSpan.FromSeconds(Program.ReloadedSettings(e => e.RefreshBalanceIntervalSec).Invoke()));

            using var activity = MyTelemetry.StartActivity("Refresh balance data");
            try
            {
                await RefreshData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on refresh balance");
                ex.FailActivity();
            }
        }

        public List<ExchangeBalance> GetBalances()
        {
            return _balances.Values.ToList();
        }

        private async Task RefreshData()
        {
            using var activity = MyTelemetry.StartActivity("Load balance info");

            var accountInfo = await _restApi.GetAccountInfo();
            if (!accountInfo.Success)
            {
                throw new Exception($"Cannot get account info, error: {accountInfo.Error}");
            }

            var availableRisk = decimal.Parse(accountInfo.Result.MaxRiskExposure) -
                                decimal.Parse(accountInfo.Result.RiskExposure);

            var data = await _restApi.GetAccountBalance();

            if (data.Success)
            {
                var dict = new Dictionary<string, ExchangeBalance>();
                foreach (var balance in data.Result.balances)
                {
                    if (balance.Key == "USD")
                    {
                        dict[balance.Key] = new ExchangeBalance()
                        {
                            Symbol = balance.Key, Balance = decimal.Parse(balance.Value),
                            Free = Convert.ToDecimal(availableRisk)
                        };
                    }
                    else
                    {
                        var symbol = $"{balance.Key}USD.SPOT";
                        var orderBook = _orderBookManager.GetOrderBook(symbol);
                        if (orderBook?.Asks.First()?.Price == null || orderBook.Bids.First()?.Price == null) continue;
                        var instrument = _externalMarketSettingsAccessor.GetExternalMarketSettings(symbol);
                        if (instrument == null) continue;
                        var price = Convert.ToDecimal(orderBook.Asks.First().Price + orderBook.Bids.First().Price) / 2;
                        var availableBalance = availableRisk / price;
                        var currentBalance = decimal.Parse(balance.Value);
                        var freeBalance = (currentBalance > 0)
                            ? availableBalance + currentBalance
                            : availableBalance;
                        dict[balance.Key] = new ExchangeBalance()
                        {
                            Symbol = balance.Key, Balance = currentBalance,
                            Free = Math.Round(freeBalance, instrument.VolumeAccuracy, MidpointRounding.ToZero)
                        };
                    }
                }

                _balances = dict;

                _logger.LogDebug("Balance refreshed");
            }
            else
            {
                throw new Exception($"Cannot get balance, error: {data.Error}");
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}