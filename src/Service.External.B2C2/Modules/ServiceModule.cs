using Autofac;
using MyJetWallet.Connector.B2C2.Rest;
using MyNoSqlServer.Abstractions;
using Service.External.B2C2.Domain.NoSql;
using Service.External.B2C2.Domain.Settings;
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
                    new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<TEntity>(
                        Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), table, true))
                .As<IMyNoSqlServerDataWriter<TEntity>>()
                .SingleInstance();
        }
    }
}