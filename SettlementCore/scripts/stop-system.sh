#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    Stopping State Machine Core${NC}"
echo -e "${BLUE}========================================${NC}"

# Stop .NET application
echo -e "${YELLOW}Stopping .NET application...${NC}"

# Find and stop dotnet processes
DOTNET_PIDS=$(pgrep -f "dotnet.*StateMachineCore" 2>/dev/null)
if [ ! -z "$DOTNET_PIDS" ]; then
    echo -e "${YELLOW}Found .NET processes: $DOTNET_PIDS${NC}"
    for pid in $DOTNET_PIDS; do
        echo -e "${YELLOW}Stopping process $pid...${NC}"
        kill $pid 2>/dev/null
        sleep 2
        
        # Check if process is still running
        if kill -0 $pid 2>/dev/null; then
            echo -e "${YELLOW}Force killing process $pid...${NC}"
            kill -9 $pid 2>/dev/null
        fi
    done
    echo -e "${GREEN}✅ .NET application stopped${NC}"
else
    echo -e "${GREEN}✅ No .NET application running${NC}"
fi

# Stop Kafka and related services
echo -e "${YELLOW}Stopping Kafka and related services...${NC}"

# Check if docker-compose is running
if docker-compose ps | grep -q "Up"; then
    echo -e "${YELLOW}Stopping Docker Compose services...${NC}"
    docker-compose down
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Docker Compose services stopped${NC}"
    else
        echo -e "${RED}❌ Error stopping Docker Compose services${NC}"
    fi
else
    echo -e "${GREEN}✅ No Docker Compose services running${NC}"
fi

# Clean up any remaining containers
echo -e "${YELLOW}Cleaning up any remaining containers...${NC}"
REMAINING_CONTAINERS=$(docker ps -q --filter "name=statemachinecore" 2>/dev/null)
if [ ! -z "$REMAINING_CONTAINERS" ]; then
    echo -e "${YELLOW}Stopping remaining containers: $REMAINING_CONTAINERS${NC}"
    docker stop $REMAINING_CONTAINERS 2>/dev/null
    docker rm $REMAINING_CONTAINERS 2>/dev/null
    echo -e "${GREEN}✅ Remaining containers cleaned up${NC}"
else
    echo -e "${GREEN}✅ No remaining containers found${NC}"
fi

# Check if anything is still running
echo -e "${YELLOW}Checking if anything is still running...${NC}"

# Check for dotnet processes
if pgrep -f "dotnet.*StateMachineCore" >/dev/null; then
    echo -e "${RED}❌ Some .NET processes are still running${NC}"
    pgrep -f "dotnet.*StateMachineCore" | xargs ps -p
else
    echo -e "${GREEN}✅ No .NET processes running${NC}"
fi

# Check for Kafka containers
if docker ps | grep -q "kafka\|zookeeper"; then
    echo -e "${RED}❌ Some Kafka containers are still running${NC}"
    docker ps | grep -E "kafka|zookeeper"
else
    echo -e "${GREEN}✅ No Kafka containers running${NC}"
fi

echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}🎉 System stopped successfully!${NC}"
echo -e "${BLUE}========================================${NC}"

# Optional: Show cleanup commands
echo -e "${YELLOW}Optional cleanup commands:${NC}"
echo -e "  Remove all containers: docker system prune -f"
echo -e "  Remove volumes: docker volume prune -f"
echo -e "  Remove images: docker image prune -f" 