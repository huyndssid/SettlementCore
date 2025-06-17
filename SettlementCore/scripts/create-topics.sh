#!/bin/bash

# Wait for Kafka to be ready
echo "Waiting for Kafka to be ready..."
sleep 30

# Create topics
echo "Creating Kafka topics..."

# Trade match topic
kafka-topics --create \
  --bootstrap-server localhost:9092 \
  --topic trade.match \
  --partitions 3 \
  --replication-factor 1 \
  --config retention.ms=604800000 \
  --config cleanup.policy=delete

# Settlement completed topic
kafka-topics --create \
  --bootstrap-server localhost:9092 \
  --topic settlement.completed \
  --partitions 3 \
  --replication-factor 1 \
  --config retention.ms=604800000 \
  --config cleanup.policy=delete

# Balance update topic
kafka-topics --create \
  --bootstrap-server localhost:9092 \
  --topic balance.update \
  --partitions 3 \
  --replication-factor 1 \
  --config retention.ms=604800000 \
  --config cleanup.policy=delete

# Settlement failed topic
kafka-topics --create \
  --bootstrap-server localhost:9092 \
  --topic settlement.failed \
  --partitions 3 \
  --replication-factor 1 \
  --config retention.ms=604800000 \
  --config cleanup.policy=delete

echo "Topics created successfully!"
echo "Available topics:"
kafka-topics --list --bootstrap-server localhost:9092 