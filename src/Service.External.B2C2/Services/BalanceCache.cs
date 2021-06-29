using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.B2C2.Rest;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Sdk.Service;
using Service.External.B2C2.Domain.Settings;

namespace Service.External.B2C2.Services
{
    public class BalanceCache : IStartable
    {
        private readonly ILogger<BalanceCache> _logger;
        private readonly B2C2RestApi _restApi;
        private readonly OrderBookManager _orderBookManager;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;
        private readonly SemaphoreSlim _slim = new SemaphoreSlim(1);
        private DateTime _lastUpdate = DateTime.MinValue;

        private GetBalancesResponse _response;


        public BalanceCache(B2C2RestApi restApi, OrderBookManager orderBookManager,
            IExternalMarketSettingsAccessor externalMarketSettingsAccessor, ILogger<BalanceCache> logger)
        {
            _restApi = restApi;
            _orderBookManager = orderBookManager;
            _externalMarketSettingsAccessor = externalMarketSettingsAccessor;
            _logger = logger;
        }

        public void Start()
        {
            RefreshBalancesAsync().GetAwaiter().GetResult();
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            await _slim.WaitAsync();
            try
            {
                if (_response == null || (DateTime.UtcNow - _lastUpdate).TotalSeconds > 1) await RefreshBalancesAsync();

                return _response;
            }
            finally
            {
                _slim.Release();
            }
        }

        private async Task<GetBalancesResponse> RefreshBalancesAsync()
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
                var balances = new List<ExchangeBalance>();
                foreach (var balance in data.Result.balances)
                {
                    if (balance.Key != "USD")
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
                        balances.Add(new ExchangeBalance()
                        {
                            Symbol = balance.Key, Balance = currentBalance,
                            Free = Math.Round(freeBalance, instrument.VolumeAccuracy, MidpointRounding.ToZero)
                        });
                    }
                    else
                    {
                        balances.Add(new ExchangeBalance()
                        {
                            Symbol = balance.Key, Balance = decimal.Parse(balance.Value),
                            Free = Convert.ToDecimal(availableRisk)
                        });
                    }
                }

                _response = new GetBalancesResponse()
                {
                    Balances = balances,
                };
                _lastUpdate = DateTime.UtcNow;
            }
            else
            {
                throw new Exception($"Cannot get balance, error: {data.Error}");
            }

            _response.AddToActivityAsJsonTag("balance");

            _logger.LogDebug("Balance refreshed");

            return _response;
        }
    }
}