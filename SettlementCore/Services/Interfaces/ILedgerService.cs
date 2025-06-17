using System.Threading.Tasks;
using StateMachineCore.Models;

namespace StateMachineCore.Services.Interfaces
{
    public interface ILedgerService
    {
        Task<bool> RecordTransactionAsync(SettlementTransaction transaction);
        Task<SettlementTransaction> GetTransactionAsync(string tradeId);
    }
} 