using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.B2C2.WebSocket;
using MyJetWallet.Connector.B2C2.WebSocket.Models;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using Service.External.B2C2.Domain.Settings;

namespace Service.External.B2C2.Services
{
    public class OrderBookManager : IDisposable
    {
        private readonly B2C2WsOrderBooks _wsB2C2;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;

        public OrderBookManager(IExternalMarketSettingsAccessor externalMarketSettingsAccessor,
            ILoggerFactory loggerFactory)
        {
            _externalMarketSettingsAccessor = externalMarketSettingsAccessor;

            _wsB2C2 = new B2C2WsOrderBooks(loggerFactory.CreateLogger<B2C2WsOrderBooks>(), Program.Settings.ApiToken,
                _externalMarketSettingsAccessor.GetExternalMarketSettingsList().Select(e => new MarketProfile()
                    {name = e.Market, levels = e.GetDoubleLevels()}).ToArray());
            _wsB2C2.ReceiveUpdates += _ => Task.CompletedTask;
        }


        public void Dispose()
        {
            _wsB2C2?.Dispose();
        }

        public List<string> GetSymbols()
        {
            return _externalMarketSettingsAccessor.GetExternalMarketSettingsList().Select(e => e.Market).ToList();
        }

        public bool HasSymbol(string symbol)
        {
            return _externalMarketSettingsAccessor.GetExternalMarketSettingsList().Find(e => e.Market == symbol) !=
                   null;
        }

        public async Task Resubscribe(string symbol, double[] levels)
        {
            await _wsB2C2.Reset(symbol, levels);
        }

        public LeOrderBook GetOrderBook(string symbol)
        {
            var data = _wsB2C2.GetOrderBookById(symbol);

            if (data == null)
                return null;

            var book = new LeOrderBook
            {
                Symbol = symbol,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(data.Timestamp).LocalDateTime,
                Asks = data.Levels.Sell.Select(ConvertPriceLevel).Select(LeOrderBookLevel.Create).Where(e => e != null)
                    .ToList(),
                Bids = data.Levels.Buy.Select(ConvertPriceLevel).Select(LeOrderBookLevel.Create).Where(e => e != null)
                    .ToList(),
                Source = B2C2Const.Name
            };

            return book;
        }

        public void Start()
        {
            _wsB2C2.Start();
        }

        public void Stop()
        {
            _wsB2C2.Stop();
        }

        private static double?[] ConvertPriceLevel(Level level)
        {
            return new double?[]
            {
                double.Parse(level.Price),
                double.Parse(level.Quantity)
            };
        }
    }
}