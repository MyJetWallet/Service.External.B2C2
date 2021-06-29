using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using MyJetWallet.Sdk.GrpcMetrics;
using ProtoBuf.Grpc.Client;
using Service.External.B2C2.Grpc;

namespace Service.External.B2C2.Client
{
    [UsedImplicitly]
    public class ExternalB2C2ClientFactory : MyGrpcClientFactory
    {
        private readonly CallInvoker _channel;

        public ExternalB2C2ClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(grpcServiceUrl);
            _channel = channel.Intercept(new PrometheusMetricsInterceptor());
        }

        public IExternalMarketSettingsManagerGrpc GetMarketMakerSettingsManagerGrpc() =>
            _channel.CreateGrpcService<IExternalMarketSettingsManagerGrpc>();
    }
}