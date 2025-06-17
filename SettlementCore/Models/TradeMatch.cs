namespace StateMachineCore.Models
{
    public class TradeMatch
    {
        public string TradeId { get; set; }
        public string BuyerId { get; set; }
        public string SellerId { get; set; }
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public DateTime Timestamp { get; set; }
        public string MakerSide { get; set; }
    }
} 