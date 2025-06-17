using StateMachineCore.Models;

namespace StateMachineCore.Services
{
    public interface ISettlementService
    {
        Task<bool> ProcessSettlementAsync(SettlementTransaction transaction);
        Task<bool> RollbackSettlementAsync(SettlementTransaction transaction);
    }
} 