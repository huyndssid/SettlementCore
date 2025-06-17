using StateMachineCore.Models.Messages;

namespace StateMachineCore.Services.Interfaces
{
    public interface ISettlementProducer
    {
        Task PublishSettlementCompletedAsync(SettlementCompletedMessage message);
        Task PublishBalanceUpdateAsync(BalanceUpdateMessage message);
        Task PublishSettlementFailedAsync(SettlementFailedMessage message);
    }
} 