using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StateMachineCore.Models.Messages;
using StateMachineCore.Services.Interfaces;

namespace StateMachineCore.Services.Kafka
{
    public class SettlementProducer : ISettlementProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<SettlementProducer> _logger;
        private readonly string _settlementCompletedTopic;
        private readonly string _balanceUpdateTopic;
        private readonly string _settlementFailedTopic;

        public SettlementProducer(IConfiguration configuration, ILogger<SettlementProducer> logger)
        {
            _logger = logger;
            
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All,
                EnableIdempotence = true,
                MaxInFlight = 5,
                RetryBackoffMs = 1000,
                RequestTimeoutMs = 30000,
                BatchSize = 16384,
                LingerMs = 5
            };

            _producer = new ProducerBuilder<string, string>(producerConfig)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Error}", e.Reason))
                .SetLogHandler((_, e) => _logger.LogInformation("Kafka producer log: {Message}", e.Message))
                .Build();
            
            _settlementCompletedTopic = configuration["Kafka:Topics:SettlementCompleted"];
            _balanceUpdateTopic = configuration["Kafka:Topics:BalanceUpdate"];
            _settlementFailedTopic = configuration["Kafka:Topics:SettlementFailed"];
        }

        public async Task PublishSettlementCompletedAsync(SettlementCompletedMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var result = await _producer.ProduceAsync(_settlementCompletedTopic, 
                    new Message<string, string> { Key = message.TradeId, Value = json });
                
                _logger.LogInformation(
                    "Published settlement completed message for trade {TradeId} to partition {Partition}", 
                    message.TradeId, 
                    result.Partition);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, 
                    "Failed to publish settlement completed message for trade {TradeId}. Error: {Error}", 
                    message.TradeId, 
                    ex.Error.Reason);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to publish settlement completed message for trade {TradeId}", 
                    message.TradeId);
                throw;
            }
        }

        public async Task PublishBalanceUpdateAsync(BalanceUpdateMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var result = await _producer.ProduceAsync(_balanceUpdateTopic, 
                    new Message<string, string> { Key = message.UserId, Value = json });
                
                _logger.LogInformation(
                    "Published balance update message for user {UserId} to partition {Partition}", 
                    message.UserId, 
                    result.Partition);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, 
                    "Failed to publish balance update message for user {UserId}. Error: {Error}", 
                    message.UserId, 
                    ex.Error.Reason);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to publish balance update message for user {UserId}", 
                    message.UserId);
                throw;
            }
        }

        public async Task PublishSettlementFailedAsync(SettlementFailedMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var result = await _producer.ProduceAsync(_settlementFailedTopic, 
                    new Message<string, string> { Key = message.TradeId, Value = json });
                
                _logger.LogInformation(
                    "Published settlement failed message for trade {TradeId} to partition {Partition}", 
                    message.TradeId, 
                    result.Partition);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, 
                    "Failed to publish settlement failed message for trade {TradeId}. Error: {Error}", 
                    message.TradeId, 
                    ex.Error.Reason);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to publish settlement failed message for trade {TradeId}", 
                    message.TradeId);
                throw;
            }
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
} 