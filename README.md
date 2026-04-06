# FinanceTracker

A personal finance and investment portfolio tracker built with ASP.NET Core 9 MVC. Track Indian equities, mutual funds, US stocks, cash holdings, and day-to-day expenses — all in one place.

---

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
  - [Storage Modes](#storage-modes)
  - [appsettings Overview](#appsettings-overview)
- [Running Locally](#running-locally)
- [Authentication](#authentication)
- [Modules](#modules)
  - [Dashboard](#dashboard)
  - [Equity Holdings](#equity-holdings)
  - [Mutual Fund Holdings](#mutual-fund-holdings)
  - [US Stock Holdings](#us-stock-holdings)
  - [Cash Holdings](#cash-holdings)
  - [Expenses](#expenses)
  - [Tags](#tags)
  - [Categories](#categories)
- [CSV Import Formats](#csv-import-formats)
  - [Equity CSV](#equity-csv)
  - [Mutual Fund CSV](#mutual-fund-csv)
  - [Expense CSV](#expense-csv)
- [API Reference (Swagger)](#api-reference-swagger)
- [Production Deployment](#production-deployment)

---

## Features

| Area | Capability |
|---|---|
| **Dashboard** | Total portfolio value, P&L, asset allocation chart, monthly growth chart |
| **Equity** | Import NSE/BSE holdings via CSV; live price refresh via market data API |
| **Mutual Funds** | Import holdings via CSV; auto-resolve scheme codes via MFAPI; live NAV refresh |
| **US Stocks** | Manual entry; live USD price + INR conversion; P&L in INR |
| **Cash** | Track bank accounts, FDs, savings — by bank and account type |
| **Expenses** | Manual entry + CSV/XLSX bulk import; filter by date, category, tag; paginated AJAX list |
| **Tags & Categories** | User-managed label sets with autocomplete |
| **Authentication** | Cookie-based sessions; BCrypt password hashing; 7-day persistent login |
| **Storage** | File-based JSON (default) or PostgreSQL (configurable) |
| **Logging** | Serilog structured logging to console + rolling daily log files |
| **API Docs** | Swagger UI at `/swagger` |

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Optional) [PostgreSQL 15+](https://www.postgresql.org/download/) — only required when `Storage:UseDatabase` is `true`

---

## Configuration

### Storage Modes

FinanceTracker supports two storage backends, controlled by `Storage:UseDatabase` in `appsettings.json`:

| Mode | Setting | Description |
|---|---|---|
| **JSON files** (default) | `"UseDatabase": false` | Stores all data as JSON files under `Data/`. No database required. |
| **PostgreSQL** | `"UseDatabase": true` | Stores data in a PostgreSQL database. Requires `ConnectionStrings:Postgres`. Migrations are applied automatically on startup. |

### appsettings Overview

**`appsettings.json`** — base configuration (all environments):

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=financetracker;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Storage": {
    "UseDatabase": false
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  },
  "AllowedHosts": "*"
}
```

**`appsettings.Development.json`** — overrides for local development (more verbose logging, dev database):

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=financetracker_dev;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

**`appsettings.Production.json`** — overrides for production (tighter logging, `UseDatabase: true`):

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=YOUR_PROD_HOST;Port=5432;Database=financetracker;Username=YOUR_PROD_USER;Password=YOUR_PROD_PASSWORD"
  },
  "Storage": {
    "UseDatabase": true
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning"
    }
  }
}
```

> ⚠️ Never commit production credentials. Use environment variable overrides or a secrets manager.

---

## Running Locally

```bash
# Clone the repo
git clone <repo-url>
cd FinanceTracker

# Restore packages
dotnet restore

# Run (JSON file storage — no database needed)
dotnet run --project FinanceTracker.Web
```

The app starts on `https://localhost:5001` (or `http://localhost:5000`).

**With PostgreSQL:**

```bash
# 1. Set UseDatabase: true in appsettings.Development.json (or via env var)
# 2. Ensure your Postgres connection string is correct
# 3. Run — EF Core migrations are applied automatically on startup
dotnet run --project FinanceTracker.Web
```

---

## Authentication

FinanceTracker uses **cookie-based authentication**. Sessions last **7 days**.

| Action | URL | Description |
|---|---|---|
| Register | `/Account/Register` | Create a new account (username + display name + password) |
| Login | `/Account/Login` | Sign in; sets a persistent cookie |
| Logout | `POST /Account/Logout` | Clears the session cookie |

> All portfolio and expense pages require an active session. Unauthenticated requests are redirected to `/Account/Login`.

---

## Modules

### Dashboard

**URL:** `/Dashboard`

The home page after login. Shows a real-time summary of your entire portfolio:

- **Total Invested** vs **Current Value** vs **Total P&L** (amount + %)
- **Asset allocation** breakdown: Equity / Mutual Funds / US Stocks / Cash (as %)
- **Per-asset class** cards: invested, current value, P&L
- **Top 5 holdings** for Equity, Mutual Funds, and US Stocks
- **Expense summary**: total all-time + current month
- **Monthly portfolio growth chart** (invested vs current value over time)

**Refresh Prices** button (`POST /Dashboard/RefreshPrices`) fetches live quotes for all holdings from market data APIs.

---

### Equity Holdings

**URL:** `/Equity`

Manage your Indian stock holdings (NSE/BSE).

**Adding holdings — CSV Upload workflow:**

1. Export your holdings CSV from your broker (Zerodha, Groww, Angel, etc.)
2. Go to `/Equity` → select your broker/account name → upload the CSV
3. Review the **preview table** — verify quantities, prices, and totals
4. Click **Confirm** to save. Existing holdings for that account are replaced; live prices are fetched automatically.

**Actions:**

| Action | Description |
|---|---|
| Upload CSV | Import holdings for a named account tag (e.g., `Zerodha`) |
| Delete holding | Remove a single stock entry |
| Delete account | Remove all holdings for a specific broker account |
| Delete all | Clear all equity holdings |

---

### Mutual Fund Holdings

**URL:** `/MutualFund`

Manage your Indian mutual fund holdings.

**Adding holdings — CSV Upload workflow:**

1. Export your MF holdings from Groww, Kuvera, MFCentral, etc.
2. Go to `/MutualFund` → enter account name → upload CSV
3. The app **auto-resolves scheme codes** via [MFAPI](https://mfapi.in/):
   - If a fund name matches exactly → scheme code applied automatically
   - If ambiguous → a mapping UI appears to let you select the correct fund from a dropdown
4. Review the preview → click **Confirm** to save with live NAVs fetched

**Search schemes:** `GET /MutualFund/SearchScheme?q=mirae` — live MFAPI search (used by the autocomplete UI).

---

### US Stock Holdings

**URL:** `/UsStock`

Manually track US equity positions with INR conversion.

**Adding a holding:**

Fill in: Symbol (e.g., `AAPL`), Company Name (optional), Quantity, Average Buy Price (USD).

On save, the app automatically:
- Fetches the current **USD price**
- Fetches the current **USD/INR exchange rate**
- Calculates INR values, P&L in INR, and P&L %

**Refresh Prices** (`POST /UsStock/RefreshPrices`) — updates prices and exchange rate for all holdings.

---

### Cash Holdings

**URL:** `/Cash`

Track bank accounts, fixed deposits, and other cash positions.

**Fields:**

| Field | Description | Example |
|---|---|---|
| Bank Name | Institution name | `HDFC Bank` |
| Account Type | Type of account | `Savings`, `FD`, `Current` |
| Balance | Current balance in ₹ | `75000` |

**Actions:** Add, Edit, Delete individual holdings.

---

### Expenses

**URL:** `/Expense`

Track day-to-day spending. Supports manual entry and bulk CSV/XLSX import.

**Manual entry fields:**

| Field | Required | Description |
|---|---|---|
| Date | Yes | Transaction date |
| Amount | Yes | Amount in ₹ |
| Category | Yes | e.g., `Groceries`, `Transport` |
| Description | No | Free-text note |
| Tags | No | Comma/semicolon-separated labels (e.g., `food, essentials`) |

**CSV/XLSX Import workflow:**

1. Click **Upload CSV** → select your file
2. Preview the parsed rows — verify the data
3. Download preview as CSV if you want to review offline
4. Click **Confirm Import** — expenses are processed asynchronously in the background
5. The list refreshes automatically as imports complete

**Filtering the expense list (AJAX):**

The expense table is loaded via `GET /Expense/List` and supports these query parameters:

| Parameter | Type | Default | Description |
|---|---|---|---|
| `dateFrom` | `yyyy-MM-dd` | 1st of current month | Start date (inclusive) |
| `dateTo` | `yyyy-MM-dd` | Today | End date (inclusive) |
| `category` | string | (all) | Filter by exact category name |
| `tags` | string[] | (all) | Filter by one or more tags |
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |

**Response:** `PagedResult<Expense>` — includes items, total count, total amount, and per-category totals.

---

### Tags

**URL:** `/Tags`

Manage the tag vocabulary for expense labelling.

- **Add** tags manually via the form
- **Delete** individual tags
- Tags are also auto-seeded from existing expense data on first load
- **Autocomplete:** `GET /Tags/Suggestions?q=foo` — returns matching tags as a JSON string array

---

### Categories

**URL:** `/Categories`

Manage expense categories.

- **Add** categories manually via the form
- **Delete** individual categories
- Categories are auto-seeded from existing expenses on first load
- **Autocomplete:** `GET /Categories/Suggestions?q=gro` — returns matching categories as a JSON string array

---

## CSV Import Formats

### Equity CSV

Supported broker exports: **Zerodha**, **Groww** (detected automatically by column headers).

Generic format (fallback):

```csv
Symbol,ISIN,Exchange,CompanyName,Quantity,AverageBuyPrice
RELIANCE,INE002A01018,NSE,Reliance Industries Ltd,10,2400.00
INFY,INE009A01021,NSE,Infosys Ltd,25,1500.00
```

| Column | Required | Description |
|---|---|---|
| `Symbol` | Yes | NSE/BSE ticker symbol |
| `ISIN` | No | ISIN code |
| `Exchange` | No | `NSE` or `BSE` (defaults to `NSE`) |
| `CompanyName` | No | Full company name |
| `Quantity` | Yes | Number of shares |
| `AverageBuyPrice` | Yes | Average purchase price in ₹ |

---

### Mutual Fund CSV

Generic format:

```csv
SchemeName,FolioNumber,Units,AverageNAV,AMC,Category
"Mirae Asset Large Cap Fund - Direct Plan - Growth",12345678,150.456,82.50,Mirae Asset,Equity - Large Cap
"Axis Bluechip Fund - Direct Plan - Growth",87654321,200.000,45.20,Axis AMC,Equity - Large Cap
```

| Column | Required | Description |
|---|---|---|
| `SchemeName` | Yes | Full AMFI scheme name (used to auto-resolve `SchemeCode`) |
| `FolioNumber` | No | Folio number |
| `Units` | Yes | Number of units held |
| `AverageNAV` | Yes | Average purchase NAV |
| `AMC` | No | AMC/fund house name |
| `Category` | No | Fund category |

> If `SchemeCode` is present in the CSV it is used directly; otherwise it is resolved via MFAPI search.

---

### Expense CSV

```csv
Date,Amount,Category,Description,Tags
2025-04-01,850.00,Groceries,Weekly grocery run,"essentials;food"
2025-04-03,1200.00,Utilities,Electricity bill,bills
2025-04-05,450.00,Transport,Cab to office,commute
```

| Column | Required | Description |
|---|---|---|
| `Date` | Yes | `yyyy-MM-dd` format |
| `Amount` | Yes | Amount in ₹ |
| `Category` | Yes | Category name |
| `Description` | No | Free-text description |
| `Tags` | No | Semicolon-separated tag list |

Also accepts `.xlsx` files with the same column structure.

---

## API Reference (Swagger)

Interactive API documentation is available at:

```
http://localhost:5000/swagger
```

The Swagger UI documents all JSON-returning AJAX endpoints. Log in first at `/Account/Login` to obtain a session cookie, then use the **Authorize** button in Swagger UI to indicate cookie auth.

### Key JSON Endpoints

| Method | URL | Description |
|---|---|---|
| `GET` | `/Expense/List` | Paginated, filtered expense list → `PagedResult<Expense>` |
| `GET` | `/Expense/Categories` | All categories for the current user → `string[]` |
| `GET` | `/Expense/CategorySuggestions?q=` | Category autocomplete → `string[]` |
| `GET` | `/Expense/TagSuggestions?q=` | Tag autocomplete → `string[]` |
| `GET` | `/Tags/Suggestions?q=` | Tag autocomplete → `string[]` |
| `GET` | `/Categories/Suggestions?q=` | Category autocomplete → `string[]` |
| `GET` | `/MutualFund/SearchScheme?q=` | Search MF scheme names → `{schemeCode, schemeName}[]` |
| `POST` | `/MutualFund/UpdateSchemes` | Apply user-confirmed scheme mappings to a preview |

---

## Production Deployment

1. **Set environment:**
   ```bash
   export ASPNETCORE_ENVIRONMENT=Production
   ```

2. **Use PostgreSQL** — set `Storage:UseDatabase: true` and configure `ConnectionStrings:Postgres` via environment variables:
   ```bash
   export ConnectionStrings__Postgres="Host=db;Port=5432;Database=financetracker;Username=app;Password=secret"
   ```

3. **Publish:**
   ```bash
   dotnet publish FinanceTracker.Web -c Release -o ./publish
   ```

4. **Run:**
   ```bash
   cd ./publish
   dotnet FinanceTracker.Web.dll
   ```

5. **HTTPS** — configure a reverse proxy (nginx/Caddy/IIS) to handle TLS. The app enforces HTTPS redirect in Production mode.

6. **Logs** — written to `Logs/financetracker-YYYYMMDD.log` (rolling daily, retained 30 days). Point your log aggregator at this path.

### Environment Variable Overrides

Any `appsettings.json` key can be overridden with an environment variable using `__` as the separator:

```bash
Storage__UseDatabase=true
Serilog__MinimumLevel__Default=Warning
```
