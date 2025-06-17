namespace StateMachineCore.Services.Interfaces
{
    public interface IFeeService
    {
        Task<(decimal buyerFee, decimal sellerFee)> CalculateFeesAsync(
            string symbol,
            decimal price,
            decimal quantity,
            string makerSide);
    }
} 