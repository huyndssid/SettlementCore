using System;

namespace StateMachineCore.Models.Messages
{
    public class SettlementCompletedMessage
    {
        public string TradeId { get; set; }
        public string SettlementId { get; set; }
        public string BuyerId { get; set; }
        public string SellerId { get; set; }
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal BuyerFee { get; set; }
        public decimal SellerFee { get; set; }
        public DateTime CompletedAt { get; set; }
        public string MakerSide { get; set; }
    }
} 