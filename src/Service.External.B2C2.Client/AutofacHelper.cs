using Autofac;
using Service.External.B2C2.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.External.B2C2.Client
{
    public static class AutofacHelper
    {
        public static void RegisterExternalB2C2Client(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new ExternalB2C2ClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetMarketMakerSettingsManagerGrpc())
                .As<IExternalMarketSettingsManagerGrpc>().SingleInstance();
        }
    }
}