using Contracts.Salesforce.InternalAccountChanged;
using LLMSessionGateway.Infrastructure.Observability;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Observability.Shared.Contracts;
using Observability.Shared.DefaultImplementations;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SalesforceAccountChangedPublisher.Infrastructure.Messaging;
using SalesforceAccountChangedPublisher.Infrastructure.Salesforce;
using Serilog;

namespace SalesforceAccountChangedPublisher.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ISalesforceAccountChangedMapper, SalesforceAccountChangedMapper>();
            services.AddScoped<IAccountChangedEventPublisher, NServiceBusAccountChangedPublisher>();

            services.AddNServiceBus(configuration);
            services.AddOpenTelemetryTracing(configuration);
            services.AddSerilogToFileLogging(configuration);

            return services;
        }

        private static IServiceCollection AddNServiceBus(this IServiceCollection services, IConfiguration config)
        {
            // Internal helper to hold MessageSession
            services.AddSingleton<NServiceBusSingletonHolder>();
            services.AddSingleton<IMessageSession>(sp =>
            {
                var holder = sp.GetRequiredService<NServiceBusSingletonHolder>();
                return holder.MessageSession ?? throw new InvalidOperationException("NServiceBus endpoint not initialized yet.");
            });

            // Register startup as hosted service
            services.AddSingleton<IHostedService>(sp =>
                new NServiceBusHostedService(config, sp.GetRequiredService<NServiceBusSingletonHolder>())
            );

            return services;
        }

        public static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<ITracingService, OpenTelemetryTracingService>();

            var jaegerConfig = config.GetSection("OpenTelemetry:Jaeger").Get<JaegerConfigs>();

            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .AddSource("SalesforceAccountChangedPublisher")
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SalesforceAccountChangedPublisher"));

                    builder.AddJaegerExporter(o =>
                    {
                        o.AgentHost = jaegerConfig!.AgentHost;
                        o.AgentPort = jaegerConfig!.AgentPort;
                    });
                });

            return services;
        }

        public static IServiceCollection AddSerilogToFileLogging(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<Serilog.ILogger>(Log.Logger);
            services.AddScoped<IStructuredLogger, SerilogStructuredLogger>();

            var fileConfig = config
                .GetSection("Logging:File")
                .Get<FileLoggingConfig>();

            Enum.TryParse<RollingInterval>(fileConfig!.RollingInterval, out var rollingInterval);

            LoggingBootstrapper.ConfigureToFileSerilog(
                fileConfig.BasePath,
                fileConfig.FileNamePattern,
                rollingInterval
            );

            return services;
        }

        private class NServiceBusSingletonHolder
        {
            public IMessageSession? MessageSession { get; private set; }
            public void Set(IMessageSession session) => MessageSession = session;
        }

        // Manages the lifecycle of the NServiceBus endpoint
        private class NServiceBusHostedService : IHostedService
        {
            private readonly IConfiguration _config;
            private readonly NServiceBusSingletonHolder _holder;
            private IEndpointInstance? _endpointInstance;

            public NServiceBusHostedService(IConfiguration config, NServiceBusSingletonHolder holder)
            {
                _config = config;
                _holder = holder;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                // Endpoint's identity in logs/monitoring
                var endpointConfiguration = new EndpointConfiguration("SalesforceAccountChangedPublisher");

                // Configure Azure Service Bus as the transport mechanism
                var connectionString = _config.GetConnectionString("AzureServiceBus");
                var transport = new AzureServiceBusTransport(connectionString!, TopicTopology.Default);
                // Define routing — publish InternalAccountChangedMessage to the "account.changed" logical endpoint
                var routing = endpointConfiguration.UseTransport(transport);
                routing.RouteToEndpoint(typeof(InternalAccountChangedMessage), "account.changed");

                // Enable SQL persistence for Outbox and message durability
                var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
                persistence.ConnectionBuilder(() => new SqlConnection(_config.GetConnectionString("OutboxDb")));
                persistence.SqlDialect<SqlDialect.MsSqlServer>();

                // Enable Outbox pattern — ensures exactly-once message dispatch
                endpointConfiguration.EnableOutbox();

                //TODO: Modify to add structured logging
                // Configure error queue — failed messages after all retries go here
                endpointConfiguration.SendFailedMessagesTo("error");

                //TODO: Modify to add structured logging
                // Retry configuration
                endpointConfiguration.Recoverability()
                    .Immediate(i => i.NumberOfRetries(3))
                    .Delayed(d => d.NumberOfRetries(5).TimeIncrease(TimeSpan.FromSeconds(10)));

                // Allow automatic creation of queues and DB schema during development
                // In production use ARM templates to setup queues, topics, subscriptions, outbox tables
                endpointConfiguration.EnableInstallers();

                // Start the NServiceBus endpoint instance
                _endpointInstance = await Endpoint.Start(endpointConfiguration, cancellationToken);
                // Store the IMessageSession for publishing messages from outside NServiceBus (send-only use)
                _holder.Set(_endpointInstance);
            }

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                if (_endpointInstance != null)
                {
                    // Gracefully shut down the NServiceBus endpoint
                    await _endpointInstance.Stop(cancellationToken);
                }
            }
        }
    }
}
