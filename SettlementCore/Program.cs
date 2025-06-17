using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StateMachineCore.Consumers;
using StateMachineCore.Services;
using StateMachineCore.Services.Interfaces;
using StateMachineCore.Models;
using StateMachineCore.Services.Kafka;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        // Configure Kafka consumer settings
        services.Configure<KafkaConsumerSettings>(
            hostContext.Configuration.GetSection("Kafka:Consumer"));

        // Register services
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IFeeService, FeeService>();
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<ISettlementProducer, SettlementProducer>();
        // Register Kafka consumer as hosted service
        services.AddHostedService<SettlementConsumer>();

    })
    .Build();

await host.RunAsync();
