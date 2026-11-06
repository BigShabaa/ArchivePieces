# ArchivePieces

An online clothing storefront built with ASP.NET Core MVC (.NET 8). Customers can browse products by category, pick sizes, manage a cart and pay through [Yoco](https://www.yoco.com/). Admins manage the catalogue, stock and orders from a built-in dashboard.

## Features

**Storefront**
- Product catalogue with categories, multiple images per product, and featured / "new" badges
- Size variants (XS–XXL) with per-size stock, low-stock and sold-out indicators
- Session-based shopping cart
- Checkout with Yoco online payments
- Customer accounts (register / login) with order history

**Admin dashboard** (`/admin`)
- Manage products, images and categories
- Stock management per size
- Order management with status tracking (Pending → Processing → Shipped → Delivered / Cancelled) and status history

## Tech stack

- ASP.NET Core 8 MVC with Razor views
- Entity Framework Core 8 with SQL Server (LocalDB locally, Azure SQL in production)
- ASP.NET Core Identity for authentication and roles
- Bootstrap 5 and jQuery
- Yoco Payments API

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (installed with Visual Studio) or another SQL Server instance
- A Yoco account for payment keys (test keys work for development)

### Setup

1. Clone the repo:
   ```bash
   git clone https://github.com/BigShabaa/ArchivePieces.git
   cd ArchivePieces/ArchivePieces
   ```

2. Configure secrets with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets). They're kept out of source control:
   ```bash
   dotnet user-secrets set "Yoco:SecretKey" "sk_test_your_key_here"
   dotnet user-secrets set "AdminSeed:Password" "YourAdminPassword1!"
   ```
   The admin password must have at least 8 characters, including uppercase, lowercase, a digit and a symbol.

3. Run the app:
   ```bash
   dotnet run
   ```
   On startup the app applies database migrations and seeds an admin account (`admin@archivepieces.com` by default, or whatever `AdminSeed:Email` is set to).

### Configuration

| Key | Description |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string. Defaults to LocalDB. |
| `Yoco:SecretKey` | Yoco secret API key |
| `Yoco:BaseUrl` | Yoco API base URL |
| `AdminSeed:Email` | Email for the seeded admin account |
| `AdminSeed:Password` | Password for the seeded admin account |

In production (for example Azure App Service), set these as application settings using `__` as the separator, e.g. `Yoco__SecretKey`.

## Project structure

```
ArchivePieces/
├── Controllers/        Storefront controllers (Shop, Cart, Checkout, Account)
│   └── Admin/          Admin dashboard controllers
├── Data/               EF Core DbContext and database seeder
├── Migrations/         EF Core migrations
├── Models/             Domain entities (Product, Category, Order, Cart, ...)
├── Services/           Cart, image upload, order numbering, Yoco payments
├── ViewModels/         View models for storefront and admin
├── Views/              Razor views
└── wwwroot/            Static assets (CSS, JS, libraries)
```
