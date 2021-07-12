using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Sdk.ExternalMarketsSettings.Autofac;
using MyJetWallet.Sdk.GrpcMetrics;
using ProtoBuf.Grpc.Client;

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
            var factory = new ExternalMarketSettingsClientFactory("http://localhost:99");
            var client = factory.GetMarketMakerSettingsManagerGrpc();

            Console.WriteLine(JsonSerializer.Serialize(await client.GetExternalMarketSettingsList(),
                new JsonSerializerOptions {WriteIndented = true}));

            // await client.UpdateExternalMarketSettings(new ExternalMarketSettings()
            // {
            //     Market = "XLMUSD.SPOT",
            //     BaseAsset = "XLM",
            //     QuoteAsset = "USD",
            //     MinVolume = 0.01,
            //     PriceAccuracy = 5,
            //     VolumeAccuracy = 8,
            //     Active = true,
            //     Levels = "100;3000"
            // });

            // await client.RemoveExternalMarketSettings(new RemoveMarketRequest() {Symbol = "BTCUST.SPOT"});

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
            Console.WriteLine(JsonSerializer.Serialize(await externalMarketClient.GetBalancesAsync(null),
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