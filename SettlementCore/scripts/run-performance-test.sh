#!/bin/bash

# Performance Test Script for SettlementCore
# Tests system with large volumes of data

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
MESSAGES=${1:-1000000}  # Default 1M messages
BATCH_SIZE=${2:-1000}   # Default batch size 1000
THREADS=${3:-4}         # Default 4 threads
MONITOR_DURATION=${4:-10} # Default 10 minutes monitoring

echo -e "${BLUE}🚀 SettlementCore Performance Test${NC}"
echo -e "${BLUE}================================${NC}"
echo -e "Messages: ${GREEN}${MESSAGES:,}${NC}"
echo -e "Batch size: ${GREEN}${BATCH_SIZE}${NC}"
echo -e "Threads: ${GREEN}${THREADS}${NC}"
echo -e "Monitor duration: ${GREEN}${MONITOR_DURATION} minutes${NC}"
echo ""

# Check if Kafka is running
echo -e "${YELLOW}🔍 Checking Kafka status...${NC}"
if ! docker ps | grep -q kafka; then
    echo -e "${RED}❌ Kafka is not running. Starting Kafka...${NC}"
    docker-compose up -d
    echo -e "${YELLOW}⏳ Waiting for Kafka to start...${NC}"
    sleep 30
else
    echo -e "${GREEN}✅ Kafka is running${NC}"
fi

# Check if topics exist
echo -e "${YELLOW}🔍 Checking Kafka topics...${NC}"
if ! docker exec kafka kafka-topics --list --bootstrap-server localhost:9092 | grep -q trade.match; then
    echo -e "${YELLOW}📝 Creating Kafka topics...${NC}"
    chmod +x scripts/create-topics.sh
    ./scripts/create-topics.sh
else
    echo -e "${GREEN}✅ Kafka topics exist${NC}"
fi

# Check if .NET application is running
echo -e "${YELLOW}🔍 Checking .NET application...${NC}"
if ! pgrep -f "dotnet.*SettlementCore" > /dev/null; then
    echo -e "${YELLOW}🚀 Starting .NET application...${NC}"
    cd SettlementCore
    dotnet run > ../app.log 2>&1 &
    APP_PID=$!
    echo -e "${GREEN}✅ .NET application started (PID: $APP_PID)${NC}"
    cd ..
    
    # Wait for application to start
    echo -e "${YELLOW}⏳ Waiting for application to start...${NC}"
    sleep 10
else
    echo -e "${GREEN}✅ .NET application is running${NC}"
fi

# Install Python dependencies if needed
echo -e "${YELLOW}🔍 Checking Python dependencies...${NC}"
if ! python3 -c "import kafka" 2>/dev/null; then
    echo -e "${YELLOW}📦 Installing Python dependencies...${NC}"
    pip3 install -r scripts/requirements.txt
fi

# Start monitoring in background
echo -e "${YELLOW}📊 Starting monitoring in background...${NC}"
python3 scripts/performance-test.py --monitor-only --monitor-duration $MONITOR_DURATION > monitoring.log 2>&1 &
MONITOR_PID=$!

# Wait a moment for monitoring to start
sleep 5

# Run performance test
echo -e "${YELLOW}🚀 Starting performance test...${NC}"
echo -e "${BLUE}This may take a while depending on the number of messages...${NC}"
echo ""

python3 scripts/performance-test.py --messages $MESSAGES --batch-size $BATCH_SIZE --threads $THREADS

# Wait for monitoring to complete
echo -e "${YELLOW}⏳ Waiting for monitoring to complete...${NC}"
wait $MONITOR_PID

# Display monitoring results
echo ""
echo -e "${BLUE}📊 MONITORING RESULTS${NC}"
echo -e "${BLUE}====================${NC}"
if [ -f monitoring.log ]; then
    tail -20 monitoring.log
fi

# Display application logs
echo ""
echo -e "${BLUE}📋 APPLICATION LOGS (last 20 lines)${NC}"
echo -e "${BLUE}==================================${NC}"
if [ -f app.log ]; then
    tail -20 app.log
fi

# Cleanup
echo ""
echo -e "${YELLOW}🧹 Cleaning up...${NC}"
if [ ! -z "$APP_PID" ]; then
    kill $APP_PID 2>/dev/null || true
    echo -e "${GREEN}✅ Stopped .NET application${NC}"
fi

# Summary
echo ""
echo -e "${BLUE}📈 PERFORMANCE TEST SUMMARY${NC}"
echo -e "${BLUE}==========================${NC}"
echo -e "Test completed with:"
echo -e "  - Messages sent: ${GREEN}${MESSAGES:,}${NC}"
echo -e "  - Batch size: ${GREEN}${BATCH_SIZE}${NC}"
echo -e "  - Threads: ${GREEN}${THREADS}${NC}"
echo -e "  - Monitor duration: ${GREEN}${MONITOR_DURATION} minutes${NC}"
echo ""
echo -e "${GREEN}✅ Performance test completed successfully!${NC}"
echo -e "${YELLOW}📁 Check the logs above for detailed results${NC}"
echo -e "${YELLOW}📊 For real-time monitoring, use: python3 scripts/monitor-results.py${NC}" 