using StateMachineCore.Models;
using StateMachineCore.Services.Interfaces;

internal class LedgerService : ILedgerService
{
    public Task<SettlementTransaction> GetTransactionAsync(string tradeId)
    {
        return Task.FromResult(new SettlementTransaction());
        //throw new NotImplementedException();
    }

    public Task<bool> RecordTransactionAsync(SettlementTransaction transaction)
    {
        return Task.FromResult(true);
        //throw new NotImplementedException();
    }
}