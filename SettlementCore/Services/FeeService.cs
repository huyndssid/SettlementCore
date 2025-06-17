using StateMachineCore.Services.Interfaces;

internal class FeeService : IFeeService
{
    public Task<(decimal buyerFee, decimal sellerFee)> CalculateFeesAsync(string symbol, decimal price, decimal quantity, string makerSide)
    {
        decimal buyerFee = 0.001m * price * quantity;
        decimal sellerFee = 0.0015m * price * quantity;
        return Task.FromResult((buyerFee, sellerFee));
        //throw new NotImplementedException();
    }
}
