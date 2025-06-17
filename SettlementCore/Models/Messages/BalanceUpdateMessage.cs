namespace StateMachineCore.Models.Messages
{
    public class BalanceUpdateMessage
    {
        public string UserId { get; set; }
        public string Symbol { get; set; }
        public decimal Balance { get; set; }
        public decimal LockedBalance { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string TradeId { get; set; }
    }
} 