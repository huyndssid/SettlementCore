using StateMachineCore.Services.Interfaces;

public class AssetService : IAssetService
{
    public Task<decimal> GetBalanceAsync(string userId, string symbol)
    {
        return Task.FromResult(1000m);
        //throw new NotImplementedException();
    }

    public Task<bool> LockAssetsAsync(string userId, string symbol, decimal amount)
    {
        return Task.FromResult(true);
        //throw new NotImplementedException();
    }

    public Task<bool> UnlockAssetsAsync(string userId, string symbol, decimal amount)
    {
        return Task.FromResult(true);
        //throw new NotImplementedException();
    }
}