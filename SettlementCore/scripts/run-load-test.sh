#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    State Machine Core Load Test${NC}"
echo -e "${BLUE}========================================${NC}"

# Check if Kafka is running
echo -e "${YELLOW}Checking Kafka status...${NC}"
if ! docker ps | grep -q kafka; then
    echo -e "${RED}❌ Kafka is not running!${NC}"
    echo -e "${YELLOW}Starting Kafka with docker-compose...${NC}"
    docker-compose up -d
    echo -e "${YELLOW}Waiting for Kafka to be ready...${NC}"
    sleep 30
else
    echo -e "${GREEN}✅ Kafka is running${NC}"
fi

# Check if topics exist
echo -e "${YELLOW}Checking Kafka topics...${NC}"
if ! kafka-topics --bootstrap-server localhost:9092 --list | grep -q trade.match; then
    echo -e "${YELLOW}Creating Kafka topics...${NC}"
    chmod +x scripts/create-topics.sh
    ./scripts/create-topics.sh
else
    echo -e "${GREEN}✅ Topics already exist${NC}"
fi

# Check Python and kafka-python
echo -e "${YELLOW}Checking Python dependencies...${NC}"
if ! python3 -c "import kafka" 2>/dev/null; then
    echo -e "${YELLOW}Installing kafka-python...${NC}"
    pip3 install kafka-python
else
    echo -e "${GREEN}✅ kafka-python is installed${NC}"
fi

# Start the application in background
echo -e "${YELLOW}Starting State Machine Core application...${NC}"
dotnet run &
APP_PID=$!

# Wait for application to start
echo -e "${YELLOW}Waiting for application to start...${NC}"
sleep 10

# Check if application is running
if ! kill -0 $APP_PID 2>/dev/null; then
    echo -e "${RED}❌ Application failed to start${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Application is running (PID: $APP_PID)${NC}"

# Run the load test
echo -e "${YELLOW}Starting load test with 1000 messages...${NC}"
echo -e "${BLUE}========================================${NC}"

python3 scripts/generate-test-messages.py

# Wait a bit for processing
echo -e "${YELLOW}Waiting for messages to be processed...${NC}"
sleep 30

# Check results
echo -e "${BLUE}========================================${NC}"
echo -e "${YELLOW}Checking results...${NC}"

# Count messages in output topics
echo -e "${YELLOW}Checking settlement.completed messages:${NC}"
COMPLETED_COUNT=$(kafka-console-consumer --bootstrap-server localhost:9092 --topic settlement.completed --from-beginning --timeout-ms 5000 2>/dev/null | wc -l)
echo -e "${GREEN}✅ Settlement completed: $COMPLETED_COUNT messages${NC}"

echo -e "${YELLOW}Checking balance.update messages:${NC}"
BALANCE_COUNT=$(kafka-console-consumer --bootstrap-server localhost:9092 --topic balance.update --from-beginning --timeout-ms 5000 2>/dev/null | wc -l)
echo -e "${GREEN}✅ Balance updates: $BALANCE_COUNT messages${NC}"

echo -e "${YELLOW}Checking settlement.failed messages:${NC}"
FAILED_COUNT=$(kafka-console-consumer --bootstrap-server localhost:9092 --topic settlement.failed --from-beginning --timeout-ms 5000 2>/dev/null | wc -l)
echo -e "${GREEN}✅ Settlement failed: $FAILED_COUNT messages${NC}"

# Calculate success rate
TOTAL_PROCESSED=$((COMPLETED_COUNT + FAILED_COUNT))
if [ $TOTAL_PROCESSED -gt 0 ]; then
    SUCCESS_RATE=$((COMPLETED_COUNT * 100 / TOTAL_PROCESSED))
    echo -e "${BLUE}========================================${NC}"
    echo -e "${GREEN}📊 Load Test Results:${NC}"
    echo -e "  Total processed: $TOTAL_PROCESSED"
    echo -e "  Successfully completed: $COMPLETED_COUNT"
    echo -e "  Failed: $FAILED_COUNT"
    echo -e "  Success rate: ${SUCCESS_RATE}%"
    echo -e "  Balance updates sent: $BALANCE_COUNT"
else
    echo -e "${RED}❌ No messages were processed${NC}"
fi

# Stop the application
echo -e "${YELLOW}Stopping application...${NC}"
kill $APP_PID 2>/dev/null
wait $APP_PID 2>/dev/null

echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}🎉 Load test completed!${NC}"
echo -e "${BLUE}========================================${NC}"

# Optional: Show Kafka UI info
echo -e "${YELLOW}You can view detailed results in Kafka UI:${NC}"
echo -e "${BLUE}http://localhost:8080${NC}" 