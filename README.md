# QuickCart API

QuickCart API is an ASP.NET Core Web API for an online shopping system. It provides authentication, product and category management, shopping carts, orders, addresses, shipping calculations, reviews, deals, vouchers, administration, and sales reporting.

## Purpose

The API provides the backend for a QuickCart storefront. It exposes versioned HTTP endpoints for customers and administrators, stores application data in SQL Server through Entity Framework Core, and supports JWT authentication with refresh-token cookies.

## Features

- Customer registration, login, logout, token refresh, and profile management
- JWT access tokens and refresh-token cookies
- Product and category management
- Product images, featured products, and stock management
- Shopping cart operations
- Address management
- Order preview, shipping quotes, placement, cancellation, and status updates
- Shipping zones and tiered shipping discounts
- Product reviews with image uploads
- Administrator review moderation and replies
- Deals and vouchers
- Administrator dashboard and customer management
- Sales reports and PDF report generation
- API versioning and OpenAPI documentation
- CORS and rate limiting

## Technology Stack

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core 10
- SQL Server
- JWT Bearer authentication
- ASP.NET Core password hashing
- OpenAPI and Scalar API Reference
- QuestPDF
- xUnit and Moq
- GitHub Actions
- Azure Web App deployment

## Architecture

```text
HTTP request
    |
Controllers
    |
Application services
    |
Repositories
    |
Entity Framework Core / AppDbContext
    |
SQL Server
```

Controllers define HTTP endpoints and authorization rules. Services contain application and business logic. Repositories handle data access through `AppDbContext`. Entity Framework Core migrations define the database schema.

## Project Structure

```text
.
├── QuickCart.slnx
├── QuickCartApi
│   ├── Common
│   ├── Controllers
│   ├── Data
│   ├── DTOs
│   ├── Entities
│   ├── Enums
│   ├── Extensions
│   ├── Migrations
│   ├── Options
│   ├── Repositories
│   ├── Reports
│   ├── Services
│   ├── Validators
│   ├── Program.cs
│   ├── QuickCartApi.csproj
│   ├── appsettings.json
│   └── Properties
│       └── launchSettings.json
└── QuickCartApi.Test
    └── QuickCartApi.Test.csproj
```

## Database

The API uses SQL Server through Entity Framework Core. `AppDbContext` defines entity sets for:

- Users and refresh tokens
- Categories and products
- Product images
- Cart items
- Addresses
- Orders, order items, and transactions
- Product reviews and review images
- Deals, deal products, and vouchers

Database migrations are stored in `QuickCartApi/Migrations`.

## Authentication and Authorization

Authentication uses JWT Bearer tokens. Access tokens contain the user ID, username, and role.

Refresh tokens are generated using secure random bytes, stored as SHA-256 hashes, and sent through an HTTP-only `refresh_token` cookie. Tokens are revoked during logout and rotation, and expired or reused tokens are rejected.

Customer registration assigns the `Customer` role. Administrative endpoints require the `Admin` role.

## API

All endpoints use version 1 by default:

```text
/api/v1/{resource}
```

### Authentication

```text
POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

### Users

```text
PUT  /api/v1/users/me
POST /api/v1/users/me/password
```

### Products

```text
GET    /api/v1/products
GET    /api/v1/products/{id}
POST   /api/v1/products
PUT    /api/v1/products/{id}
DELETE /api/v1/products/{id}
DELETE /api/v1/products/{id}/images/{imageId}
PUT    /api/v1/products/{id}/images/{imageId}/default
PATCH  /api/v1/products/{id}/stock
GET    /api/v1/products/stock-summary
```

Product creation and updates use multipart form requests.

### Categories

```text
GET    /api/v1/categories
GET    /api/v1/categories/{id}
POST   /api/v1/categories
PUT    /api/v1/categories/{id}
DELETE /api/v1/categories/{id}
```

Write operations require the `Admin` role.

### Cart

```text
GET    /api/v1/cart
POST   /api/v1/cart/items
PATCH  /api/v1/cart/items/{id}
DELETE /api/v1/cart/items/{id}
DELETE /api/v1/cart/items
```

### Addresses

```text
GET    /api/v1/address
GET    /api/v1/address/{id}
POST   /api/v1/address
PUT    /api/v1/address/{id}
PATCH  /api/v1/address/{id}/default
DELETE /api/v1/address/{id}
```

### Orders

```text
POST /api/v1/orders
GET  /api/v1/orders
GET  /api/v1/orders/{id}
POST /api/v1/orders/{id}/cancel
GET  /api/v1/orders/shipping-quote
POST /api/v1/orders/preview
```

### Reviews

```text
GET    /api/v1/products/{productId}/reviews/summary
GET    /api/v1/products/{productId}/reviews
GET    /api/v1/products/{productId}/reviews/mine
POST   /api/v1/products/{productId}/reviews
PUT    /api/v1/products/{productId}/reviews/mine
DELETE /api/v1/products/{productId}/reviews/mine
DELETE /api/v1/reviews/{id}
GET    /api/v1/admin/reviews
PUT    /api/v1/admin/reviews/{id}/reply
DELETE /api/v1/admin/reviews/{id}/reply
```

### Deals

```text
GET  /api/v1/deals/active
GET  /api/v1/deals
POST /api/v1/deals
POST /api/v1/deals/{id}/end
```

### Vouchers

```text
GET  /api/v1/vouchers
POST /api/v1/vouchers
POST /api/v1/vouchers/{id}/active
```

### Administration

```text
GET /api/v1/admin/dashboard
GET /api/v1/admin/reports/sales
GET /api/v1/admin/reports/sales/pdf
GET /api/v1/admin/customers
GET /api/v1/admin/customers/{id}
GET /api/v1/admin/orders
GET /api/v1/admin/orders/{id}
PUT /api/v1/admin/orders/{id}/status
```

Administration endpoints require the `Admin` role.

## API Documentation

The OpenAPI document is available at:

```text
/openapi/v1.json
```

The application also maps a Scalar API reference using the version 1 OpenAPI document.

## Rate Limiting

The API uses fixed-window rate limiting:

| Policy | Limit | Usage |
|---|---:|---|
| `auth` | 5 requests per minute | Login and registration |
| `refresh` | 20 requests per minute | Token refresh |
| `write` | 30 requests per minute | Write operations |
| `read` | 120 requests per minute | Read operations |

Authenticated requests are partitioned by user ID. Anonymous requests are partitioned by IP address. Rejected requests return `429 Too Many Requests`.

## Shipping

Shipping configuration is stored in the `Shipping` section. Supported zones include Mandaue, Metro Cebu, Cebu Province, Visayas, Mindanao, and Luzon.

The calculator supports configured shipping rates, a maximum shipping fee, a free-shipping threshold, and tiered shipping discounts.

## Prerequisites

- .NET SDK 10.0
- SQL Server
- A configured SQL Server connection string
- A JWT signing secret

The repository includes a local `dotnet-ef` tool manifest using version `10.0.12`.

## Configuration

Default configuration is stored in `QuickCartApi/appsettings.json`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=QuickCart;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "Jwt": {
    "Issuer": "QuickCartApi",
    "Audience": "QuickCartClient",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7,
    "SecretKey": "..."
  }
}
```

The JWT secret must be supplied through local user secrets, environment-specific configuration, or another supported ASP.NET Core configuration provider.

Environment-variable equivalents include:

```text
ConnectionStrings__DefaultConnection
Jwt__SecretKey
Jwt__Issuer
Jwt__Audience
Jwt__AccessTokenMinutes
Jwt__RefreshTokenDays
Shipping__FreeShippingThreshold
Shipping__MaxShippingFee
```

Do not commit credentials, signing keys, or production connection strings.

## Running Locally

Restore dependencies:

```bash
dotnet restore QuickCart.slnx
```

Configure the SQL Server connection string and JWT settings, then run the API:

```bash
dotnet run --project QuickCartApi/QuickCartApi.csproj
```

The configured local URLs are:

```text
http://localhost:5278
https://localhost:7265
```

## Testing

The test project uses xUnit, Moq, ASP.NET Core MVC testing packages, Entity Framework Core SQL Server, and Coverlet.

Run tests with:

```bash
dotnet test QuickCart.slnx -c Release
```

The GitHub Actions workflow restores, builds, tests, and publishes the solution on pushes to `main` and on manual workflow dispatches.
