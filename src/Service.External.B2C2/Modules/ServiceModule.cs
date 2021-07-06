using Autofac;
using MyJetWallet.Connector.B2C2.Rest;
using MyJetWallet.Sdk.ExternalMarketsSettings.NoSql;
using MyJetWallet.Sdk.ExternalMarketsSettings.Services;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataWriter;
using Service.External.B2C2.Services;

namespace Service.External.B2C2.Modules
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var b2C2RestClient = B2C2RestApiFactory.CreateClient(Program.Settings.ApiToken);
            builder.RegisterInstance(b2C2RestClient).AsSelf().SingleInstance();

            builder.RegisterType<BalanceCache>().As<IStartable>().AutoActivate().AsSelf().SingleInstance();
            builder.RegisterType<OrderBookManager>().AsSelf().SingleInstance();

            builder
                .RegisterType<ExternalMarketSettingsManager>()
                .WithParameter("name", B2C2Const.Name)
                .As<IExternalMarketSettingsManager>()
                .As<IExternalMarketSettingsAccessor>()
                .AsSelf()
                .SingleInstance();


            RegisterMyNoSqlWriter<ExternalMarketSettingsNoSql>(builder, ExternalMarketSettingsNoSql.TableName);
        }

        private void RegisterMyNoSqlWriter<TEntity>(ContainerBuilder builder, string table)
            where TEntity : IMyNoSqlDbEntity, new()
        {
            builder.Register(ctx =>
                    new MyNoSqlServerDataWriter<TEntity>(
                        Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), table, true))
                .As<IMyNoSqlServerDataWriter<TEntity>>()
                .SingleInstance();
        }
    }
}