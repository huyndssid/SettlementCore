#!/usr/bin/env python3
import json
import random
import time
from datetime import datetime, timedelta
from kafka import KafkaProducer
import uuid

# Kafka configuration
KAFKA_BOOTSTRAP_SERVERS = ['localhost:9092']
TOPIC = 'trade.match'

# Test data
SYMBOLS = ['BTC/USDT', 'ETH/USDT', 'ADA/USDT', 'DOT/USDT', 'LINK/USDT', 'UNI/USDT', 'LTC/USDT', 'BCH/USDT']
BUYER_IDS = [f'BUYER-{i:03d}' for i in range(1, 101)]  # 100 buyers
SELLER_IDS = [f'SELLER-{i:03d}' for i in range(1, 101)]  # 100 sellers
MAKER_SIDES = ['BUY', 'SELL']

def generate_trade_match(trade_id):
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
    price = round(base * random.uniform(0.95, 1.05), 2)  # ±5% variation
    quantity = round(random.uniform(0.1, 10.0), 4)
    
    return {
        "TradeId": f"TRADE-{trade_id:06d}",
        "BuyerId": random.choice(BUYER_IDS),
        "SellerId": random.choice(SELLER_IDS),
        "Symbol": symbol,
        "Price": price,
        "Quantity": quantity,
        "MakerSide": random.choice(MAKER_SIDES),
        "Timestamp": datetime.utcnow().isoformat() + 'Z'
    }

def send_messages(num_messages=1000, batch_size=100):
    """Send messages to Kafka in batches"""
    producer = KafkaProducer(
        bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
        value_serializer=lambda v: json.dumps(v).encode('utf-8'),
        key_serializer=lambda k: k.encode('utf-8') if k else None,
        acks='all',
        retries=3
    )
    
    print(f"Starting to send {num_messages} messages to topic '{TOPIC}'...")
    print(f"Batch size: {batch_size}")
    print("-" * 50)
    
    start_time = time.time()
    successful_sends = 0
    failed_sends = 0
    
    for i in range(0, num_messages, batch_size):
        batch_end = min(i + batch_size, num_messages)
        batch_size_actual = batch_end - i
        
        print(f"Processing batch {i//batch_size + 1}/{(num_messages + batch_size - 1)//batch_size} "
              f"({i+1}-{batch_end}/{num_messages})")
        
        # Generate batch of messages
        messages = []
        for j in range(batch_size_actual):
            trade_id = i + j + 1
            message = generate_trade_match(trade_id)
            messages.append((message["TradeId"], message))
        
        # Send batch
        batch_start = time.time()
        futures = []
        
        for key, message in messages:
            future = producer.send(TOPIC, key=key, value=message)
            futures.append((future, key))
        
        # Wait for batch to complete
        for future, key in futures:
            try:
                record_metadata = future.get(timeout=10)
                successful_sends += 1
                if successful_sends % 100 == 0:
                    print(f"  ✓ Sent {successful_sends} messages successfully")
            except Exception as e:
                failed_sends += 1
                print(f"  ✗ Failed to send message {key}: {e}")
        
        batch_time = time.time() - batch_start
        print(f"  Batch completed in {batch_time:.2f}s")
        
        # Small delay between batches to avoid overwhelming Kafka
        if i + batch_size < num_messages:
            time.sleep(0.1)
    
    total_time = time.time() - start_time
    
    print("-" * 50)
    print(f"Summary:")
    print(f"  Total messages: {num_messages}")
    print(f"  Successful: {successful_sends}")
    print(f"  Failed: {failed_sends}")
    print(f"  Success rate: {(successful_sends/num_messages)*100:.1f}%")
    print(f"  Total time: {total_time:.2f}s")
    print(f"  Average rate: {num_messages/total_time:.1f} messages/second")
    
    producer.flush()
    producer.close()
    
    return successful_sends, failed_sends

if __name__ == "__main__":
    try:
        successful, failed = send_messages(1000, 100)
        if failed == 0:
            print("\n🎉 All messages sent successfully!")
        else:
            print(f"\n⚠️  {failed} messages failed to send")
    except Exception as e:
        print(f"❌ Error: {e}")
        print("Make sure Kafka is running: docker-compose up -d") 