using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SalesforceAccountChangedPublisher.Host;
using SalesforceAccountChangedPublisher.Infrastructure.DependencyInjection;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddInfrastructure(config);

    })
    .Build()
    .Run();
