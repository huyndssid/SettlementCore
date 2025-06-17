using System.Threading.Tasks;

namespace StateMachineCore.Services.Interfaces
{
    public interface IWalletService
    {
        Task<bool> TransferAsync(string fromUserId, string toUserId, string symbol, decimal amount);
        Task<bool> DeductFeeAsync(string userId, string symbol, decimal fee);
        Task<bool> RefundFeeAsync(string userId, string symbol, decimal fee);
    }
} 