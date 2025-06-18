#!/usr/bin/env python3
"""
Performance Test Script for SettlementCore
Tests system with large volumes of data (millions of messages)
"""

import json
import random
import time
import threading
from datetime import datetime, timedelta
from kafka import KafkaProducer, KafkaConsumer
from collections import defaultdict
import argparse
import statistics

# Configuration
KAFKA_BOOTSTRAP_SERVERS = ['localhost:9092']
INPUT_TOPIC = 'trade.match'
OUTPUT_TOPICS = ['settlement.completed', 'balance.update', 'settlement.failed']

# Test data
SYMBOLS = ['BTC/USDT', 'ETH/USDT', 'ADA/USDT', 'DOT/USDT', 'LINK/USDT', 'UNI/USDT', 'LTC/USDT', 'BCH/USDT']
BUYER_IDS = [f'BUYER-{i:03d}' for i in range(1, 1001)]  # 1000 buyers
SELLER_IDS = [f'SELLER-{i:03d}' for i in range(1, 1001)]  # 1000 sellers
MAKER_SIDES = ['BUY', 'SELL']

class PerformanceTest:
    def __init__(self, num_messages=1000000, batch_size=1000, num_threads=4):
        self.num_messages = num_messages
        self.batch_size = batch_size
        self.num_threads = num_threads
        self.results = defaultdict(list)
        self.start_time = None
        self.end_time = None
        
    def generate_trade_match(self, trade_id):
        """Generate a single trade match message"""
        symbol = random.choice(SYMBOLS)
        base_price = {
            'BTC/USDT': 50000,
            'ETH/USDT': 3000,
            'ADA/USDT': 0.5,
            'DOT/USDT': 7,
            'LINK/USDT': 15,
            'UNI/USDT': 8,
            'LTC/USDT': 100,
            'BCH/USDT': 250
        }
        
        base = base_price.get(symbol, 100)
        price = round(base * random.uniform(0.95, 1.05), 2)
        quantity = round(random.uniform(0.1, 10.0), 4)
        
        return {
            "TradeId": f"PERF-{trade_id:08d}",
            "BuyerId": random.choice(BUYER_IDS),
            "SellerId": random.choice(SELLER_IDS),
            "Symbol": symbol,
            "Price": price,
            "Quantity": quantity,
            "MakerSide": random.choice(MAKER_SIDES),
            "Timestamp": datetime.utcnow().isoformat() + 'Z'
        }
    
    def send_batch(self, producer, start_id, end_id, thread_id):
        """Send a batch of messages"""
        messages = []
        for i in range(start_id, end_id):
            message = self.generate_trade_match(i)
            messages.append((message["TradeId"], message))
        
        batch_start = time.time()
        futures = []
        
        for key, message in messages:
            future = producer.send(INPUT_TOPIC, key=key, value=message)
            futures.append((future, key))
        
        # Wait for batch to complete
        successful = 0
        failed = 0
        for future, key in futures:
            try:
                record_metadata = future.get(timeout=30)
                successful += 1
            except Exception as e:
                failed += 1
                print(f"Thread {thread_id}: Failed to send message {key}: {e}")
        
        batch_time = time.time() - batch_start
        rate = len(messages) / batch_time if batch_time > 0 else 0
        
        self.results[f'thread_{thread_id}'].append({
            'batch_size': len(messages),
            'successful': successful,
            'failed': failed,
            'time': batch_time,
            'rate': rate
        })
        
        print(f"Thread {thread_id}: Batch {start_id}-{end_id-1} completed in {batch_time:.2f}s "
              f"({rate:.1f} msg/s, {successful} success, {failed} failed)")
    
    def send_messages_parallel(self):
        """Send messages using multiple threads"""
        producer = KafkaProducer(
            bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
            value_serializer=lambda v: json.dumps(v).encode('utf-8'),
            key_serializer=lambda k: k.encode('utf-8') if k else None,
            acks='all',
            retries=3,
            batch_size=16384,
            linger_ms=5
        )
        
        print(f"Starting parallel performance test...")
        print(f"Total messages: {self.num_messages:,}")
        print(f"Batch size: {self.batch_size}")
        print(f"Number of threads: {self.num_threads}")
        print("-" * 80)
        
        self.start_time = time.time()
        threads = []
        
        # Calculate batch distribution
        messages_per_thread = self.num_messages // self.num_threads
        remaining = self.num_messages % self.num_threads
        
        for thread_id in range(self.num_threads):
            start_id = thread_id * messages_per_thread + 1
            end_id = start_id + messages_per_thread
            if thread_id == self.num_threads - 1:
                end_id += remaining
            
            # Create batches for this thread
            batches = []
            for i in range(start_id, end_id, self.batch_size):
                batch_end = min(i + self.batch_size, end_id)
                batches.append((i, batch_end))
            
            # Create thread for this range
            thread = threading.Thread(
                target=self._send_batches_for_thread,
                args=(producer, batches, thread_id)
            )
            threads.append(thread)
            thread.start()
        
        # Wait for all threads to complete
        for thread in threads:
            thread.join()
        
        self.end_time = time.time()
        producer.flush()
        producer.close()
        
        return self.calculate_statistics()
    
    def _send_batches_for_thread(self, producer, batches, thread_id):
        """Send all batches for a specific thread"""
        for start_id, end_id in batches:
            self.send_batch(producer, start_id, end_id, thread_id)
    
    def calculate_statistics(self):
        """Calculate performance statistics"""
        total_time = self.end_time - self.start_time
        total_messages = sum(
            sum(batch['batch_size'] for batch in thread_results)
            for thread_results in self.results.values()
        )
        total_successful = sum(
            sum(batch['successful'] for batch in thread_results)
            for thread_results in self.results.values()
        )
        total_failed = sum(
            sum(batch['failed'] for batch in thread_results)
            for thread_results in self.results.values()
        )
        
        # Calculate rates
        overall_rate = total_messages / total_time if total_time > 0 else 0
        success_rate = (total_successful / total_messages * 100) if total_messages > 0 else 0
        
        # Calculate per-thread statistics
        thread_rates = []
        for thread_results in self.results.values():
            thread_total = sum(batch['batch_size'] for batch in thread_results)
            thread_time = sum(batch['time'] for batch in thread_results)
            if thread_time > 0:
                thread_rates.append(thread_total / thread_time)
        
        return {
            'total_messages': total_messages,
            'total_successful': total_successful,
            'total_failed': total_failed,
            'total_time': total_time,
            'overall_rate': overall_rate,
            'success_rate': success_rate,
            'thread_rates': thread_rates,
            'avg_thread_rate': statistics.mean(thread_rates) if thread_rates else 0,
            'min_thread_rate': min(thread_rates) if thread_rates else 0,
            'max_thread_rate': max(thread_rates) if thread_rates else 0
        }

def monitor_output_topics(duration_minutes=10):
    """Monitor output topics for processing results"""
    consumers = {}
    for topic in OUTPUT_TOPICS:
        consumer = KafkaConsumer(
            topic,
            bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
            auto_offset_reset='earliest',
            enable_auto_commit=False,
            value_deserializer=lambda x: json.loads(x.decode('utf-8'))
        )
        consumers[topic] = consumer
    
    print(f"🔍 Monitoring output topics for {duration_minutes} minutes...")
    print("-" * 80)
    
    stats = defaultdict(int)
    start_time = time.time()
    end_time = start_time + (duration_minutes * 60)
    
    try:
        while time.time() < end_time:
            for topic, consumer in consumers.items():
                messages = consumer.poll(timeout_ms=1000)
                
                for partition, records in messages.items():
                    for record in records:
                        stats[topic] += 1
            
            # Display summary every 30 seconds
            elapsed = time.time() - start_time
            if int(elapsed) % 30 == 0 and elapsed > 0:
                print(f"📊 Summary at {datetime.now().strftime('%H:%M:%S')} (elapsed: {elapsed:.0f}s):")
                for topic, count in stats.items():
                    rate = count / elapsed if elapsed > 0 else 0
                    print(f"  {topic}: {count:,} messages ({rate:.1f} msg/s)")
                print("-" * 80)
    
    except KeyboardInterrupt:
        print("\n🛑 Monitoring stopped by user")
    
    finally:
        for consumer in consumers.values():
            consumer.close()
    
    return stats

def main():
    parser = argparse.ArgumentParser(description='Performance test for SettlementCore')
    parser.add_argument('--messages', type=int, default=1000000, help='Number of messages to send')
    parser.add_argument('--batch-size', type=int, default=1000, help='Batch size for sending')
    parser.add_argument('--threads', type=int, default=4, help='Number of threads')
    parser.add_argument('--monitor-only', action='store_true', help='Only monitor output topics')
    parser.add_argument('--monitor-duration', type=int, default=10, help='Monitor duration in minutes')
    
    args = parser.parse_args()
    
    if args.monitor_only:
        stats = monitor_output_topics(args.monitor_duration)
        print("\n📈 Final Monitoring Statistics:")
        for topic, count in stats.items():
            print(f"  {topic}: {count:,} messages")
        return
    
    # Run performance test
    test = PerformanceTest(args.messages, args.batch_size, args.threads)
    stats = test.send_messages_parallel()
    
    # Display results
    print("\n" + "=" * 80)
    print("📊 PERFORMANCE TEST RESULTS")
    print("=" * 80)
    print(f"Total messages sent: {stats['total_messages']:,}")
    print(f"Successful: {stats['total_successful']:,}")
    print(f"Failed: {stats['total_failed']:,}")
    print(f"Success rate: {stats['success_rate']:.1f}%")
    print(f"Total time: {stats['total_time']:.2f}s")
    print(f"Overall rate: {stats['overall_rate']:.1f} messages/second")
    print(f"Average thread rate: {stats['avg_thread_rate']:.1f} messages/second")
    print(f"Thread rate range: {stats['min_thread_rate']:.1f} - {stats['max_thread_rate']:.1f} messages/second")
    
    # Calculate throughput
    throughput_tps = stats['overall_rate']
    daily_capacity = throughput_tps * 86400
    monthly_capacity = daily_capacity * 30
    
    print(f"\n🚀 THROUGHPUT ANALYSIS")
    print(f"Current TPS: {throughput_tps:,.0f}")
    print(f"Daily capacity: {daily_capacity:,.0f} transactions")
    print(f"Monthly capacity: {monthly_capacity:,.0f} transactions")
    
    # Comparison with industry standards
    print(f"\n📈 INDUSTRY COMPARISON")
    binance_ratio = (throughput_tps / 1400000) * 100
    coinbase_ratio = (throughput_tps / 500000) * 100
    kraken_ratio = (throughput_tps / 200000) * 100
    
    print(f"vs Binance (1.4M TPS): {binance_ratio:.1f}%")
    print(f"vs Coinbase (500K TPS): {coinbase_ratio:.1f}%")
    print(f"vs Kraken (200K TPS): {kraken_ratio:.1f}%")
    
    if throughput_tps >= 100000:
        print("✅ Excellent performance - suitable for large exchanges")
    elif throughput_tps >= 50000:
        print("✅ Good performance - suitable for medium exchanges")
    elif throughput_tps >= 10000:
        print("⚠️  Moderate performance - needs optimization for production")
    else:
        print("❌ Low performance - significant optimization required")

if __name__ == "__main__":
    main() 