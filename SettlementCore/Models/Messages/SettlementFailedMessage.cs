namespace StateMachineCore.Models.Messages
{
    public class SettlementFailedMessage
    {
        public string TradeId { get; set; }
        public string SettlementId { get; set; }
        public string ErrorMessage { get; set; }
        public SettlementState FailedAtState { get; set; }
        public DateTime FailedAt { get; set; }
        public int RetryCount { get; set; }
    }
} 