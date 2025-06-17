using StateMachineCore.Services.Interfaces;

internal class WalletService : IWalletService
{
    public Task<bool> DeductFeeAsync(string userId, string symbol, decimal fee)
    {
        return Task.FromResult(true);
        //throw new NotImplementedException();
    }

    public Task<bool> RefundFeeAsync(string userId, string symbol, decimal fee)
    {
        return Task.FromResult(true);
        //throw new NotImplementedException();
    }

    public Task<bool> TransferAsync(string fromUserId, string toUserId, string symbol, decimal amount)
    {
        return Task.FromResult(true);
        //throw new NotImplementedException();
    }
}
