using System;

namespace StateMachineCore.Models
{
    public enum SettlementState
    {
        Pending,
        Locked,
        Processing,
        FeeDiscount,
        Completed,
        Failed
    }

    public class SettlementTransaction
    {
        public SettlementTransaction()
        {
        }

        public SettlementTransaction(TradeMatch tradeMatch)
        {
            Id = Guid.NewGuid().ToString();
            TradeId = tradeMatch.TradeId;
            State = SettlementState.Pending;
            CreatedAt = DateTime.UtcNow;
            BuyerId = tradeMatch.BuyerId;
            SellerId = tradeMatch.SellerId;
            Symbol = tradeMatch.Symbol;
            Price = tradeMatch.Price;
            Quantity = tradeMatch.Quantity;
            MakerSide = tradeMatch.MakerSide;
        }

        public string Id { get; set; }
        public string TradeId { get; set; }
        public SettlementState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public string IdempotencyKey { get; set; }
        
        // Trade details
        public string BuyerId { get; set; }
        public string SellerId { get; set; }
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public string MakerSide { get; set; }
        
        // Settlement details
        public decimal BuyerFee { get; set; }
        public decimal SellerFee { get; set; }
        public bool IsBuyerLocked { get; set; }
        public bool IsSellerLocked { get; set; }
        public bool IsTransferCompleted { get; set; }
        public bool IsFeeProcessed { get; set; }
    }
} 