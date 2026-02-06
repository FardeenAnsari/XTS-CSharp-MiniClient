# XTS Market Data Client - C# Mini Implementation

## Overview
C# implementation of core features from the Python `xts-api-client` package, developed as a placement assignment to demonstrate REST API implementation and code comprehension skills.

**Scope:** This is a focused implementation covering the 4 required features, not a complete port of the Python package.

## Assignment Requirements

### 1. Market Data Login
- REST-based authentication with XTS Market Data API
- JWT token management for subsequent API calls
- Implementation: [MarketDataAuthService.cs](Services/MarketDataAuthService.cs)

### 2. Download OHLC for Top 5 NIFTY 50 Stocks
- Fetch 1-minute OHLC data for equity (cash market segment)
- Stocks: RELIANCE, TCS, HDFCBANK, INFY, ICICIBANK
- CSV export for offline analysis
- Implementation: [MarketDataService.cs](Services/MarketDataService.cs)

### 3. Download Near-Month F&O 1-Min Data
- Fetch 1-minute OHLC for HDFCBANK and NIFTY futures
- Dynamic instrument discovery via master data API
- Near-month contract selection (nearest expiry)
- Implementation: [FnoDataService.cs](Services/FnoDataService.cs)

### 4. Stream Data Using Socket
- WebSocket-based real-time market data streaming
- Persistent connection with continuous data reception
- Implementation: [SocketClient.cs](Services/SocketClient.cs)

## Setup Instructions

### Prerequisites
- .NET 10.0
- XTS Market Data API credentials

### Configuration

1. Clone the repository:
```bash
git clone <your-repo-url>
cd XTS-CSharp-MiniClient
```

2. Create `.env` file in project root:
```bash
XTS_API_KEY=your_api_key_here
XTS_API_SECRET=your_secret_key_here
XTS_API_SOURCE=WEBAPI
XTS_API_URL=https://xts.rmoneyindia.co.in:3000
```

3. Set environment variables (on macOS/Linux):
```bash
export $(cat .env | xargs)
```

4. Build and run:
```bash
dotnet build
dotnet run
```

## Project Structure

```
XTS-CSharp-MiniClient/
├── Services/
│   ├── MarketDataAuthService.cs      # Authentication (Task 1)
│   ├── MarketDataService.cs          # Equity OHLC (Task 2)
│   ├── FnoDataService.cs             # F&O OHLC (Task 3)
│   └── SocketClient.cs               # WebSocket streaming (Task 4)
├── Models/
│   ├── AuthResponse.cs               # Login response structure
│   ├── OhlcCandle.cs                 # OHLC data model
│   └── ApiResponse.cs                # Generic API response wrapper
├── Data/                             # Generated at runtime (not in git)
│   ├── OHLC_Equity_*.csv            # Downloaded equity OHLC data
│   └── OHLC_FNO_*.csv               # Downloaded F&O OHLC data
├── Program.cs                        # Main entry point - runs all 4 tasks
├── .env                              # API credentials (not in git)
├── .gitignore
└── README.md
```

## Output

When you run the program, it will:
1. Authenticate with XTS API
2. Download OHLC data for 5 stocks → saves to `Data/OHLC_Equity_*.csv`
3. Fetch near-month F&O contracts and download their OHLC → saves to `Data/OHLC_FNO_*.csv`
4. Stream live market data for 10 seconds → saves to `Data/Streaming_Data_*.txt`

## Key Implementation Decisions

### Dynamic F&O Instrument Discovery
Unlike hardcoded instrument IDs, this implementation:
- Fetches current contracts from master data API
- Filters for FUTIDX (index futures) and FUTSTK (stock futures)
- Selects near-month contracts (nearest expiry date)
- Handles expiry rollovers automatically

### Data Format Parsing
XTS API returns pipe-delimited strings, not JSON arrays:
```
"timestamp|open|high|low|close|volume|oi|,timestamp|..."
```
Custom parsing logic converts this to structured `OhlcCandle` objects.

### WebSocket vs Socket.IO
Python package uses Socket.IO with event codes. This C# implementation uses standard WebSocket for simplicity while demonstrating the same concept of persistent streaming connections.

## Dependencies

- **Newtonsoft.Json** (13.0.3): JSON serialization
- **System.Net.WebSockets**: WebSocket support

## Important Notes

**Security**: Never commit `.env` file or hardcode credentials  
**Scope**: This is a demo project, not production-ready  
**Data Files**: CSV/TXT files in `Data/` folder are git-ignored

## License

Educational/Assignment purpose only.

## Running the Demo

```bash
cd XTS-CSharp-MiniClient
dotnet restore
dotnet build
dotnet run
```

## Output

The program demonstrates:
1. Successful authentication with token receipt
2. OHLC data download for 5 equity stocks
3. OHLC data download for F&O contracts
4. WebSocket connection and streaming data reception

## Code Size

- **Total Lines:** ~450 lines (excluding comments)
- **Services:** ~300 lines
- **Models:** ~80 lines
- **Program:** ~70 lines

This is intentionally concise to demonstrate core concepts without unnecessary complexity.

## Interview Talking Points

1. **REST Architecture:** Token-based auth, header injection, JSON parsing
2. **Async Programming:** All REST calls use `async/await`
3. **Data Modeling:** Strong typing with C# classes
4. **WebSocket:** Persistent connection for streaming
5. **Code Organization:** Clean separation of concerns

## Limitations (Acknowledged)

This is a **demonstration client** with known limitations:
- No instrument discovery (uses hardcoded IDs)
- Minimal error handling
- No connection pooling or rate limiting
- Single-threaded socket client
- No data persistence

These limitations are acceptable for an assignment demonstrating architectural understanding.

## License

Educational/Assignment use only. Based on XTS API by Symphony Fintech Solutions.
