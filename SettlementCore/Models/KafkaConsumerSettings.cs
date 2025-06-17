namespace StateMachineCore.Models
{
    public class KafkaConsumerSettings
    {
        public string GroupId { get; set; } = "settlement-service";
        public string AutoOffsetReset { get; set; } = "Earliest";
        public bool EnableAutoCommit { get; set; } = false;
        public int SessionTimeoutMs { get; set; } = 30000;
        public int HeartbeatIntervalMs { get; set; } = 3000;
        public int MaxPollIntervalMs { get; set; } = 300000;
    }
} 