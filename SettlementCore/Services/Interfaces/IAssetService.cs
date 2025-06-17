using System.Threading.Tasks;

namespace StateMachineCore.Services.Interfaces
{
    public interface IAssetService
    {
        Task<bool> LockAssetsAsync(string userId, string symbol, decimal amount);
        Task<bool> UnlockAssetsAsync(string userId, string symbol, decimal amount);
        Task<decimal> GetBalanceAsync(string userId, string symbol);
    }
} 