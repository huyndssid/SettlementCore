using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StateMachineCore.Models;
using StateMachineCore.Services;

namespace StateMachineCore.Consumers
{
    public class SettlementConsumer : BackgroundService
    {
        private readonly ILogger<SettlementConsumer> _logger;
        private readonly ISettlementService _settlementService;
        private readonly IConsumer<string, string> _consumer;
        private readonly string _topic;

        public SettlementConsumer(
            ILogger<SettlementConsumer> logger,
            ISettlementService settlementService,
            IOptions<KafkaConsumerSettings> consumerSettings,
            IConfiguration configuration)
        {
            _logger = logger;
            _settlementService = settlementService;
            _topic = configuration["Kafka:Topics:TradeMatch"];

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = consumerSettings.Value.GroupId,
                AutoOffsetReset = Enum.Parse<AutoOffsetReset>(consumerSettings.Value.AutoOffsetReset),
                EnableAutoCommit = consumerSettings.Value.EnableAutoCommit,
                SessionTimeoutMs = consumerSettings.Value.SessionTimeoutMs,
                HeartbeatIntervalMs = consumerSettings.Value.HeartbeatIntervalMs,
                MaxPollIntervalMs = consumerSettings.Value.MaxPollIntervalMs,
                EnablePartitionEof = true
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Error}", e.Reason))
                .SetLogHandler((_, e) => _logger.LogInformation("Kafka log: {Message}", e.Message))
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_topic);
            _logger.LogInformation("Started consuming from topic: {Topic}", _topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = _consumer.Consume(stoppingToken);
                        
                        if (result?.Message?.Value == null)
                        {
                            _logger.LogWarning("Received null message");
                            continue;
                        }

                        await ConsumeAsync(result);
                        _consumer.Commit(result);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Consumer operation cancelled");
                        break;
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error: {Error}", ex.Error.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error consuming message");
                    }
                }
            }
            finally
            {
                _consumer.Close();
                _logger.LogInformation("Consumer stopped");
            }
        }

        public async Task ConsumeAsync(ConsumeResult<string, string> result)
        {
            try
            {
                var tradeMatch = JsonSerializer.Deserialize<TradeMatch>(result.Message.Value);
                
                if (tradeMatch == null)
                {
                    _logger.LogError("Failed to deserialize trade match message");
                    return;
                }

                _logger.LogInformation("Received trade match: {TradeId}", tradeMatch.TradeId);

                var transaction = new SettlementTransaction(tradeMatch);
                var success = await _settlementService.ProcessSettlementAsync(transaction);

                if (success)
                {
                    _logger.LogInformation("Successfully processed trade: {TradeId}", tradeMatch.TradeId);
                }
                else
                {
                    _logger.LogError("Failed to process trade: {TradeId}", tradeMatch.TradeId);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message: {Message}", result.Message.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing trade match");
                throw;
            }
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }
} 