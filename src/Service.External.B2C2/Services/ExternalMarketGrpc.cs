using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.B2C2.Rest;
using MyJetWallet.Connector.B2C2.Rest.Models;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;

// ReSharper disable InconsistentLogPropertyNaming

namespace Service.External.B2C2.Services
{
    public class ExternalMarketGrpc : IExternalMarket
    {
        private readonly BalanceCache _balanceCache;
        private readonly ILogger<ExternalMarketGrpc> _logger;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;
        private readonly B2C2RestApi _restApi;

        public ExternalMarketGrpc(ILogger<ExternalMarketGrpc> logger, B2C2RestApi restApi, BalanceCache balanceCache,
            IExternalMarketSettingsAccessor externalMarketSettingsAccessor)
        {
            _logger = logger;
            _restApi = restApi;
            _balanceCache = balanceCache;
            _externalMarketSettingsAccessor = externalMarketSettingsAccessor;
        }

        public Task<GetNameResult> GetNameAsync(GetNameRequest request)
        {
            return Task.FromResult(new GetNameResult {Name = B2C2Const.Name});
        }

        public Task<GetBalancesResponse> GetBalancesAsync(GetBalancesRequest request)
        {
            var list = _balanceCache.GetBalances();
            return Task.FromResult(new GetBalancesResponse {Balances = list});
        }

        public Task<GetMarketInfoResponse> GetMarketInfoAsync(MarketRequest request)
        {
            try
            {
                var data = _externalMarketSettingsAccessor.GetExternalMarketSettings(request.Market);
                if (data == null)
                {
                    return new GetMarketInfoResponse().AsTask();
                }

                return new GetMarketInfoResponse
                {
                    Info = new ExchangeMarketInfo()
                    {
                        Market = data.Market,
                        BaseAsset = data.BaseAsset,
                        QuoteAsset = data.QuoteAsset,
                        MinVolume = data.MinVolume,
                        PriceAccuracy = data.PriceAccuracy,
                        VolumeAccuracy = data.VolumeAccuracy
                    }
                }.AsTask();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get Bitgo GetMarketInfo: {marketText}", request.Market);
                throw;
            }
        }

        public Task<GetMarketInfoListResponse> GetMarketInfoListAsync(GetMarketInfoListRequest request)
        {
            try
            {
                var data = _externalMarketSettingsAccessor.GetExternalMarketSettingsList();
                return new GetMarketInfoListResponse
                {
                    Infos = data.Select(e => new ExchangeMarketInfo()
                    {
                        Market = e.Market,
                        BaseAsset = e.BaseAsset,
                        QuoteAsset = e.QuoteAsset,
                        MinVolume = e.MinVolume,
                        PriceAccuracy = e.PriceAccuracy,
                        VolumeAccuracy = e.VolumeAccuracy
                    }).ToList()
                }.AsTask();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get Bitgo GetMarketInfo");
                throw;
            }
        }

        public async Task<ExchangeTrade> MarketTrade(MarketTradeRequest request)
        {
            try
            {
                using var action = MyTelemetry.StartActivity("B2C2 Market Trade");
                request.AddToActivityAsJsonTag("request");

                var refId = request.ReferenceId ?? Guid.NewGuid().ToString("N");

                refId.AddToActivityAsTag("reference-id");

                var resp = await (request.Side == OrderSide.Buy
                    ? _restApi.PlaceMarketOrder(refId, request.Market,
                        MyJetWallet.Connector.B2C2.Rest.Enums.OrderSide.buy, (decimal) Math.Abs(request.Volume))
                    : _restApi.PlaceMarketOrder(refId, request.Market,
                        MyJetWallet.Connector.B2C2.Rest.Enums.OrderSide.sell, (decimal) Math.Abs(request.Volume)));

                resp.AddToActivityAsJsonTag("marketOrder-response");

                if (!resp.Success)
                {
                    throw new Exception(
                        $"Cannot place marketOrder. Error: {JsonConvert.SerializeObject(resp)}. Request: {JsonConvert.SerializeObject(request)}. Reference: {refId}");
                }

                return ConvertB2C2OrderToExchangeTrade(resp.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot execute trade. Request: {requestJson}",
                    JsonConvert.SerializeObject(request));
                throw;
            }
        }

        private ExchangeTrade ConvertB2C2OrderToExchangeTrade(Order order)
        {
            var size = !string.IsNullOrEmpty(order.Quantity) ? decimal.Parse(order.Quantity) : 0;

            return new ExchangeTrade
            {
                Id = order.OrderId,
                Market = order.Instrument,
                Side = order.Side == "buy" ? OrderSide.Buy : OrderSide.Sell,
                Price = !string.IsNullOrEmpty(order.ExecutedPrice) ? double.Parse(order.ExecutedPrice) : 0,
                ReferenceId = order.ClientOrderId,
                Source = B2C2Const.Name,
                Volume = order.Side == "buy" ? (double) size : (double) -size,
                Timestamp = DateTime.Parse(order.Created)
            };
        }
    }
}