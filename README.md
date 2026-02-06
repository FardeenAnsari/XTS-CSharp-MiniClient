# XTS Market Data Client - C# Mini Implementation

## Overview
C# implementation of core features from the Python `xts-api-client` package for XTS Market Data API integration.

**Scope:** This is a focused implementation covering 4 core features.

## Features

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
git clone https://github.com/FardeenAnsari/XTS-CSharp-MiniClient.git
cd XTS-CSharp-MiniClient
```

2. Create `.env` file in project root with your XTS API credentials:
```bash
XTS_API_KEY=<your_api_key>
XTS_API_SECRET=<your_secret_key>
XTS_API_SOURCE=<source>
XTS_API_URL=<api_url>
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
│   ├── MarketDataAuthService.cs      # Authentication
│   ├── MarketDataService.cs          # Equity OHLC
│   ├── FnoDataService.cs             # F&O OHLC
│   └── SocketClient.cs               # WebSocket streaming
├── Models/
│   ├── AuthResponse.cs               # Login response structure
│   ├── OhlcCandle.cs                 # OHLC data model
│   └── ApiResponse.cs                # Generic API response wrapper
├── Data/                             # Generated at runtime (not in git)
│   ├── OHLC_Equity_*.csv            # Downloaded equity OHLC data
│   └── OHLC_FNO_*.csv               # Downloaded F&O OHLC data
├── Program.cs                        # Main entry point
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
- **DotNetEnv** (3.1.1): Environment variable management from .env file
- **System.Net.WebSockets**: WebSocket support (built-in)

## Important Notes

**Security**: Never commit `.env` file or hardcode credentials  
**Data Files**: CSV/TXT files in `Data/` folder are git-ignored  
**Environment**: Ensure proper environment configuration before running

## Code Statistics

- **Total Lines:** 1,227 lines
- **Services:** 977 lines
  - FnoDataService.cs: 406 lines
  - SocketClient.cs: 240 lines
  - MarketDataService.cs: 204 lines
  - MarketDataAuthService.cs: 127 lines
- **Program:** 135 lines
- **Models:** 115 lines
  - OhlcCandle.cs: 65 lines
  - AuthResponse.cs: 30 lines
  - ApiResponse.cs: 20 lines

This implementation provides comprehensive functionality with clean, maintainable code.

## Technical Highlights

1. **REST Architecture:** Token-based authentication, header injection, JSON parsing
2. **Async Programming:** All REST calls use `async/await`
3. **Data Modeling:** Strong typing with C# classes
4. **WebSocket:** Persistent connection for real-time streaming
5. **Code Organization:** Clean separation of concerns

## Known Limitations

- Basic error handling
- No connection pooling or rate limiting
- Single-threaded socket client
- WebSocket implementation instead of full Socket.IO protocol
- Limited to specified date ranges

## License

Based on XTS API by Symphony Fintech Solutions.
