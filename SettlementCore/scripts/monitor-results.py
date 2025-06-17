#!/usr/bin/env python3
import json
import time
from datetime import datetime
from kafka import KafkaConsumer
from collections import defaultdict

# Kafka configuration
KAFKA_BOOTSTRAP_SERVERS = ['localhost:9092']
TOPICS = ['settlement.completed', 'balance.update', 'settlement.failed']

def monitor_topics():
    """Monitor all output topics and display real-time statistics"""
    
    # Create consumers for each topic
    consumers = {}
    for topic in TOPICS:
        consumer = KafkaConsumer(
            topic,
            bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
            auto_offset_reset='earliest',
            enable_auto_commit=False,
            value_deserializer=lambda x: json.loads(x.decode('utf-8'))
        )
        consumers[topic] = consumer
    
    print("🔍 Monitoring Kafka topics for settlement results...")
    print("Press Ctrl+C to stop monitoring")
    print("-" * 80)
    
    # Statistics
    stats = defaultdict(int)
    start_time = time.time()
    
    try:
        while True:
            # Check each topic for new messages
            for topic, consumer in consumers.items():
                messages = consumer.poll(timeout_ms=1000)
                
                for partition, records in messages.items():
                    for record in records:
                        stats[topic] += 1
                        
                        # Display message details
                        timestamp = datetime.fromtimestamp(record.timestamp / 1000).strftime('%H:%M:%S')
                        
                        if topic == 'settlement.completed':
                            trade_id = record.value.get('TradeId', 'N/A')
                            symbol = record.value.get('Symbol', 'N/A')
                            print(f"[{timestamp}] ✅ {topic}: Trade {trade_id} ({symbol}) completed")
                            
                        elif topic == 'balance.update':
                            user_id = record.value.get('UserId', 'N/A')
                            symbol = record.value.get('Symbol', 'N/A')
                            balance = record.value.get('Balance', 0)
                            print(f"[{timestamp}] 💰 {topic}: User {user_id} balance updated ({symbol}: {balance})")
                            
                        elif topic == 'settlement.failed':
                            trade_id = record.value.get('TradeId', 'N/A')
                            error = record.value.get('ErrorMessage', 'N/A')
                            print(f"[{timestamp}] ❌ {topic}: Trade {trade_id} failed - {error}")
            
            # Display summary every 10 seconds
            elapsed = time.time() - start_time
            if int(elapsed) % 10 == 0 and elapsed > 0:
                print("-" * 80)
                print(f"📊 Summary at {datetime.now().strftime('%H:%M:%S')} (elapsed: {elapsed:.0f}s):")
                print(f"  Settlement completed: {stats['settlement.completed']}")
                print(f"  Balance updates: {stats['balance.update']}")
                print(f"  Settlement failed: {stats['settlement.failed']}")
                
                total_processed = stats['settlement.completed'] + stats['settlement.failed']
                if total_processed > 0:
                    success_rate = (stats['settlement.completed'] / total_processed) * 100
                    print(f"  Success rate: {success_rate:.1f}%")
                
                print("-" * 80)
                
    except KeyboardInterrupt:
        print("\n🛑 Monitoring stopped by user")
        
        # Final summary
        print("\n📈 Final Statistics:")
        print(f"  Settlement completed: {stats['settlement.completed']}")
        print(f"  Balance updates: {stats['balance.update']}")
        print(f"  Settlement failed: {stats['settlement.failed']}")
        
        total_processed = stats['settlement.completed'] + stats['settlement.failed']
        if total_processed > 0:
            success_rate = (stats['settlement.completed'] / total_processed) * 100
            print(f"  Success rate: {success_rate:.1f}%")
        
        print(f"  Total monitoring time: {time.time() - start_time:.1f}s")
    
    finally:
        # Close all consumers
        for consumer in consumers.values():
            consumer.close()

if __name__ == "__main__":
    try:
        monitor_topics()
    except Exception as e:
        print(f"❌ Error: {e}")
        print("Make sure Kafka is running and topics exist") 