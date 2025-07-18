# Real-Time Cryptocurrency Chart System

## Overview
Real-time cryptocurrency chart system with updates every 5 seconds using SignalR WebSocket technology.

## Architecture
- **Background Service**: Fetches crypto data every 5 seconds
- **SignalR Hub**: Broadcasts real-time updates to connected clients
- **REST API**: Provides historical chart data

## API Endpoints

### 1. Chart Data
```
GET /api/crypto/chart/{symbol}?limit=100
```
**Description**: Returns historical data for chart visualization
**Parameters**: 
- `symbol`: Cryptocurrency symbol (BTC, ETH, etc.)
- `limit`: Number of data points (default: 100)

**Response:**
```json
{
  "symbol": "BTC",
  "dataPoints": [
    {
      "timestamp": "2025-07-18T10:00:00Z",
      "price": 45000.50,
      "volume": 1234567.89
    }
  ],
  "currentPrice": 45000.50,
  "change24h": 2.5,
  "high24h": 46000.00,
  "low24h": 44000.00,
  "volume24h": 987654321.0,
  "lastUpdated": "2025-07-18T10:05:00Z"
}
```

## SignalR Real-Time Connection

### Hub Endpoint
```
ws://localhost:5000/cryptohub
```

### Authentication
SignalR connections require JWT authentication token.

### Client Implementation

#### 1. Connection Setup
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/cryptohub", {
    accessTokenFactory: () => localStorage.getItem('token')
  })
  .build();

// Start connection
await connection.start();
```

#### 2. Join Symbol Group
```javascript
// Join BTC price updates
await connection.invoke("JoinSymbolGroup", "BTC");
```

#### 3. Listen for Real-Time Updates
```javascript
connection.on("PriceUpdate", (update) => {
  console.log("Real-time update:", update);
  // Update chart with new data point
  updateChart(update);
});
```

#### 4. Leave Symbol Group
```javascript
// Stop receiving BTC updates
await connection.invoke("LeaveSymbolGroup", "BTC");
```

### Real-Time Update Data Structure
```json
{
  "symbol": "BTC",
  "price": 45123.45,
  "volume": 1234567.89,
  "change24h": 2.5,
  "high24h": 46000.00,
  "low24h": 44000.00,
  "timestamp": "2025-07-18T10:05:00Z"
}
```

## Supported Cryptocurrencies
Currently tracking: **BTC, ETH, BNB, ADA, DOT**

For complete list:
```
GET /api/crypto/supported-symbols
```

## Data Flow

1. **Background Service** (`CryptoUpdateService`):
   - Runs every 5 seconds
   - Fetches data from Binance API
   - Sends updates via SignalR Hub

2. **SignalR Hub** (`CryptoHub`):
   - Manages client connections
   - Groups clients by cryptocurrency symbol
   - Broadcasts price updates to relevant groups

3. **Frontend Integration**:
   - Connects to SignalR hub
   - Receives real-time updates
   - Updates chart visualization

## Implementation Example

### Complete Frontend Integration
```javascript
class CryptoChart {
  constructor(symbol) {
    this.symbol = symbol;
    this.connection = null;
    this.chart = null;
  }

  async initialize() {
    // Setup SignalR connection
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl("/cryptohub", {
        accessTokenFactory: () => localStorage.getItem('token')
      })
      .build();

    // Listen for price updates
    this.connection.on("PriceUpdate", (update) => {
      if (update.symbol === this.symbol) {
        this.addDataPoint(update);
      }
    });

    // Start connection
    await this.connection.start();
    
    // Join symbol group
    await this.connection.invoke("JoinSymbolGroup", this.symbol);

    // Load initial chart data
    await this.loadInitialData();
  }

  async loadInitialData() {
    const response = await fetch(`/api/crypto/chart/${this.symbol}`);
    const chartData = await response.json();
    this.initializeChart(chartData);
  }

  addDataPoint(update) {
    // Add new point to chart
    this.chart.addPoint({
      x: new Date(update.timestamp).getTime(),
      y: update.price
    });
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.invoke("LeaveSymbolGroup", this.symbol);
      await this.connection.stop();
    }
  }
}

// Usage
const btcChart = new CryptoChart("BTC");
await btcChart.initialize();
```

## CORS Configuration
SignalR is configured to accept connections from:
- `http://localhost:5173` (Vite dev server)
- `http://localhost:5000` (Backend)
- Other configured frontend URLs

## Error Handling
- Connection failures: Automatic reconnection
- Missing data: Fallback to REST API
- Authentication errors: Redirect to login

## Performance Notes
- Updates every 5 seconds (configurable)
- Only tracked symbols receive updates
- Clients can join/leave symbol groups dynamically
- Minimal bandwidth usage with targeted updates