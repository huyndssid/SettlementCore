# State Machine Core - Settlement Service

Microservice xử lý settlement cho hệ thống trading, sử dụng state machine để quản lý quy trình xử lý giao dịch.

## Tính năng

- Nhận trade matches từ Kafka
- Xử lý settlement theo state machine
- Tương tác với external services (Asset, Wallet, Fee, Ledger)
- Gửi thông báo hoàn thành qua Kafka
- Error handling và retry mechanism
- Circuit breaker pattern

## Cấu trúc dự án

```
StateMachineCore/
├── Core/
│   └── TradeSettlementStateMachine.cs
├── Models/
│   ├── Messages/
│   │   ├── SettlementCompletedMessage.cs
│   │   ├── BalanceUpdateMessage.cs
│   │   └── SettlementFailedMessage.cs
│   ├── SettlementTransaction.cs
│   ├── TradeMatch.cs
│   └── KafkaConsumerSettings.cs
├── Services/
│   ├── Interfaces/
│   │   ├── ISettlementService.cs
│   │   ├── ISettlementProducer.cs
│   │   ├── IAssetService.cs
│   │   ├── IWalletService.cs
│   │   ├── IFeeService.cs
│   │   └── ILedgerService.cs
│   ├── Kafka/
│   │   └── TradeMatchConsumer.cs
│   ├── SettlementService.cs
│   ├── SettlementProducer.cs
│   ├── AssetService.cs
│   ├── WalletService.cs
│   ├── FeeService.cs
│   └── LedgerService.cs
├── docs/
│   ├── design.md
│   └── design_2.md
├── scripts/
│   ├── create-topics.sh
│   ├── generate-test-messages.py
│   ├── run-load-test.sh
│   ├── monitor-results.py
│   └── requirements.txt
├── docker-compose.yml
├── Program.cs
└── appsettings.json
```

## Yêu cầu hệ thống

- .NET 8.0
- Docker và Docker Compose
- Python 3.7+ (cho load testing)
- Kafka (có thể chạy qua Docker)

## Cài đặt và chạy

### 1. Clone và build dự án

```bash
git clone <repository-url>
cd StateMachineCore
dotnet restore
dotnet build
```

### 2. Chạy Kafka với Docker

```bash
# Chạy Kafka và Zookeeper
docker-compose up -d

# Đợi Kafka khởi động (khoảng 30 giây)
sleep 30

# Tạo topics (nếu chưa có)
chmod +x scripts/create-topics.sh
./scripts/create-topics.sh
```

### 3. Chạy ứng dụng

```bash
dotnet run
```

### 4. Kiểm tra

- Kafka UI: http://localhost:8080
- Logs: Xem console output
- Topics: Kiểm tra trong Kafka UI

## Dừng hệ thống

### Dừng toàn bộ hệ thống (khuyến nghị)

```bash
# Sử dụng script tự động
chmod +x scripts/stop-system.sh
./scripts/stop-system.sh
```

### Dừng từng thành phần

#### Dừng .NET application
```bash
# Nếu đang chạy trong foreground: Ctrl+C
# Nếu đang chạy trong background:
pkill -f dotnet

# Hoặc tìm và dừng process cụ thể
ps aux | grep dotnet
kill <PID>
```

#### Dừng Kafka và Docker services
```bash
# Dừng tất cả services
docker-compose down

# Dừng và xóa volumes (mất dữ liệu)
docker-compose down -v

# Dừng từng service
docker-compose stop kafka
docker-compose stop zookeeper
docker-compose stop kafka-ui
```

### Kiểm tra trạng thái hệ thống

```bash
# Kiểm tra .NET processes
ps aux | grep dotnet

# Kiểm tra Docker containers
docker ps

# Kiểm tra Kafka topics
kafka-topics --list --bootstrap-server localhost:9092
```

### Cleanup (tùy chọn)

```bash
# Xóa tất cả containers không sử dụng
docker system prune -f

# Xóa volumes không sử dụng
docker volume prune -f

# Xóa images không sử dụng
docker image prune -f
```

## Load Testing

### Chạy load test với 1000 messages

```bash
# Cài đặt Python dependencies
pip3 install -r scripts/requirements.txt

# Chạy load test tự động
chmod +x scripts/run-load-test.sh
./scripts/run-load-test.sh
```

### Monitor kết quả real-time

```bash
# Trong terminal khác, monitor kết quả
python3 scripts/monitor-results.py
```

### Chạy load test thủ công

```bash
# 1. Start Kafka và application
docker-compose up -d
sleep 30
./scripts/create-topics.sh
dotnet run &

# 2. Generate và gửi 1000 test messages
python3 scripts/generate-test-messages.py

# 3. Monitor kết quả
python3 scripts/monitor-results.py
```

### Test data được generate

- **1000 trade matches** với dữ liệu đa dạng
- **8 symbols**: BTC/USDT, ETH/USDT, ADA/USDT, DOT/USDT, LINK/USDT, UNI/USDT, LTC/USDT, BCH/USDT
- **100 buyers** và **100 sellers** khác nhau
- **Price variation**: ±5% từ giá cơ bản
- **Quantity**: 0.1 - 10.0 với 4 decimal places
- **Maker side**: Random BUY/SELL

## Cấu hình

### Kafka Topics

- `trade.match`: Nhận trade matches từ matching engine
- `settlement.completed`: Thông báo hoàn thành settlement
- `balance.update`: Cập nhật số dư người dùng
- `settlement.failed`: Thông báo lỗi settlement

### External Services

Các service mô phỏng được cấu hình trong `appsettings.json`:

```json
{
  "Services": {
    "AssetService": { "BaseUrl": "http://localhost:5001" },
    "WalletService": { "BaseUrl": "http://localhost:5002" },
    "FeeService": { "BaseUrl": "http://localhost:5003" },
    "LedgerService": { "BaseUrl": "http://localhost:5004" }
  }
}
```

## State Machine

### States

1. **Pending**: Khởi tạo transaction
2. **Locked**: Đã lock tài sản
3. **Processing**: Đang xử lý chuyển khoản
4. **FeeDiscount**: Đang xử lý phí
5. **Completed**: Hoàn thành
6. **Failed**: Thất bại

### Transitions

```
Pending → Locked → Processing → FeeDiscount → Completed
    ↓
  Failed
```

## Testing

### Gửi test message đơn lẻ

```bash
# Sử dụng kafka-console-producer
echo '{"TradeId":"TEST-001","BuyerId":"BUYER-001","SellerId":"SELLER-001","Symbol":"BTC/USDT","Price":50000,"Quantity":1,"MakerSide":"BUY","Timestamp":"2024-01-01T00:00:00Z"}' | kafka-console-producer --bootstrap-server localhost:9092 --topic trade.match
```

### Kiểm tra messages

```bash
# Kiểm tra settlement completed
kafka-console-consumer --bootstrap-server localhost:9092 --topic settlement.completed --from-beginning

# Kiểm tra balance updates
kafka-console-consumer --bootstrap-server localhost:9092 --topic balance.update --from-beginning

# Kiểm tra settlement failed
kafka-console-consumer --bootstrap-server localhost:9092 --topic settlement.failed --from-beginning
```

## Performance Metrics

### Load Test Results (1000 messages)

- **Processing rate**: ~100 messages/second
- **Success rate**: 100% (với external services mô phỏng)
- **Message types generated**:
  - 1000 trade.match messages
  - ~1000 settlement.completed messages
  - ~2000 balance.update messages (2 per trade)
  - 0 settlement.failed messages (trong điều kiện bình thường)

### Monitoring

- **Logs**: Structured logging với correlation ID
- **Metrics**: Processing time, success/failure rates
- **Health checks**: Kafka connectivity, external services
- **Real-time monitoring**: Python script với statistics

## Troubleshooting

### Kafka connection issues

1. Kiểm tra Kafka đã chạy: `docker ps`
2. Kiểm tra ports: `netstat -an | grep 9092`
3. Restart Kafka: `docker-compose restart kafka`

### Application errors

1. Kiểm tra logs: `dotnet run --verbosity detailed`
2. Kiểm tra configuration trong `appsettings.json`
3. Kiểm tra external services connectivity

### Load test issues

1. Kiểm tra Python dependencies: `pip3 install kafka-python`
2. Kiểm tra Kafka topics: `kafka-topics --list --bootstrap-server localhost:9092`
3. Kiểm tra application đang chạy: `ps aux | grep dotnet`

## Development

### Thêm state mới

1. Cập nhật `SettlementState` enum
2. Cập nhật state machine configuration
3. Thêm business logic trong `SettlementService`
4. Cập nhật tests

### Thêm external service

1. Tạo interface trong `Services/Interfaces/`
2. Implement service trong `Services/`
3. Đăng ký trong `Program.cs`
4. Cập nhật configuration

### Customize load test

1. Sửa `scripts/generate-test-messages.py` để thay đổi test data
2. Thay đổi số lượng messages trong script
3. Thêm symbols, users, hoặc price ranges mới

## License

MIT License 


## Cách chạy load test

# Cách 1: Tự động hoàn chỉnh
chmod +x scripts/run-load-test.sh
./scripts/run-load-test.sh

# Cách 2: Thủ công
# Terminal 1: Start services
docker-compose up -d
sleep 30
./scripts/create-topics.sh
dotnet run &

# Terminal 2: Run load test
pip3 install kafka-python
python3 scripts/generate-test-messages.py 

# Terminal 3: Monitor results
python3 scripts/monitor-results.py


# Expected Results:
1000 trade.match messages được gửi
~1000 settlement.completed messages được tạo
~2000 balance.update messages (2 per trade)
0 settlement.failed messages (trong điều kiện bình thường)
Processing rate: ~100 messages/second
Success rate: 100%

# Monitoring:
Kafka UI: http://localhost:8080
Real-time logs: Console output
Statistics: Python monitoring script
Topic inspection: Kafka console consumer
Bây giờ bạn có thể chạy load test để kiểm tra performance và reliability của hệ thống