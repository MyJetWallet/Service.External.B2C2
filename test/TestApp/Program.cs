using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Sdk.GrpcMetrics;
using ProtoBuf.Grpc.Client;
using Service.External.B2C2.Client;
using Service.External.B2C2.Domain.Models.Settings;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await TestSettings();
        }

        private static async Task TestSettings()
        {
            var factory = new ExternalB2C2ClientFactory("http://localhost:99");
            var client = factory.GetMarketMakerSettingsManagerGrpc();

            Console.WriteLine(JsonSerializer.Serialize(await client.GetExternalMarketSettingsList(),
                new JsonSerializerOptions {WriteIndented = true}));

            await client.AddExternalMarketSettings(new ExternalMarketSettings()
            {
                Market = "BTCUST.SPOT",
                BaseAsset = "BTC",
                QuoteAsset = "USD",
                MinVolume = 0.0001,
                PriceAccuracy = 4,
                VolumeAccuracy = 8,
                Active = true,
                Levels = "1;3"
            });

            Console.WriteLine(JsonSerializer.Serialize(await client.GetExternalMarketSettingsList(),
                new JsonSerializerOptions {WriteIndented = true}));
        }

        private async Task TestOrderBooks()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress("http://localhost:99");
            var _channel = channel.Intercept(new PrometheusMetricsInterceptor());

            var orderBookClient = _channel.CreateGrpcService<IOrderBookSource>();

            var orderBook = await orderBookClient.GetOrderBookAsync(new MarketRequest {Market = "BTCUSD.SPOT"});

            Console.WriteLine(JsonSerializer.Serialize(orderBook, new JsonSerializerOptions {WriteIndented = true}));

            Console.WriteLine("***********************************************");

            var externalMarketClient = _channel.CreateGrpcService<IExternalMarket>();
            Console.WriteLine(JsonSerializer.Serialize(await externalMarketClient.GetBalancesAsync(),
                new JsonSerializerOptions {WriteIndented = true}));

            Console.WriteLine("***********************************************");
            // var order = await externalMarketClient.MarketTrade(new MarketTradeRequest
            // {
            //     ReferenceId = "Order8", Market = "TBTC-TUSD*", Side = OrderSide.Sell, Volume = 0.1,
            //     OppositeVolume = 4000.0
            // });
            //
            // Console.WriteLine(JsonSerializer.Serialize(order, new JsonSerializerOptions {WriteIndented = true}));

            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}