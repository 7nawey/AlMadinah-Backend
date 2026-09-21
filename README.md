# Al-Madinah Al-Munawwarah — Backend API

<div dir="rtl">

منصة تجارة إلكترونية متكاملة — هذا المستودع يحتوي على الـ **Backend API** المبني بـ **ASP.NET Core 8** بمعمارية نظيفة متعددة الطبقات (Clean Architecture).

</div>

RESTful API for the Al-Madinah e-commerce platform, built with **ASP.NET Core 8**, **Entity Framework Core**, and **SQL Server**, following a layered Clean Architecture.

---

## 🏗️ Architecture

The solution is split into 4 layers:

```
Al-Madinah-Al-Munawwarah/
├── AlMadina.API/              # Presentation layer — Controllers, Middleware, Filters
│   ├── Controllers/           # REST endpoints (Auth, Products, Orders, Payment, ...)
│   ├── Middleware/            # Global exception handling & request pipeline
│   ├── Filters/               # Action/validation filters
│   └── Program.cs             # DI, JWT, Swagger, CORS setup
├── AlMadina.Application/      # Application layer — business logic
│   ├── DTOs/                  # Data Transfer Objects
│   ├── Interfaces/            # Service & repository contracts (IUnitOfWork, ...)
│   ├── Services/              # Business services
│   └── Mapping/               # AutoMapper profiles
├── AlMadina.Domain/           # Domain layer — core entities, no dependencies
│   └── Entities/              # Product, Order, Category, Deal, PaymentRecord, ...
└── AlMadina.Infrastructure/   # Infrastructure layer — data access
    ├── Data/                  # DbContext, configurations, seed data
    ├── Migrations/            # EF Core migrations
    └── Repositories/          # Repository & Unit of Work implementations
```

## ✨ Features

- 🔐 **Authentication & Authorization** — JWT-based auth with ASP.NET Core Identity roles (Admin / Customer)
- 🛍️ **Products & Categories** — full CRUD with stock tracking
- 🧾 **Orders** — order placement, status lifecycle, order history
- 💳 **Payments** — [Paymob](https://paymob.com/) integration (card + mobile wallets) with payment records
- ↩️ **Returns** — return-request workflow
- 📦 **Stock Movements** — inventory in/out audit trail
- 🏷️ **Deals & Offers** — promotional deals management
- 📊 **Admin Dashboard** — sales & inventory statistics endpoints
- ✉️ **Email Notifications** — Gmail SMTP integration
- 📖 **Swagger / OpenAPI** — interactive API documentation out of the box

## 🛠️ Tech Stack

| Technology | Usage |
|---|---|
| .NET 8 / ASP.NET Core | Web API framework |
| Entity Framework Core 9 | ORM & migrations |
| SQL Server | Database |
| ASP.NET Core Identity + JWT | Authentication |
| AutoMapper | DTO mapping |
| Swashbuckle | Swagger docs |
| Paymob API | Payment gateway |

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (or SQL Server Express)

### Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/7nawey/AlMadinah-Backend.git
   cd AlMadinah-Backend
   ```

2. **Configure the connection string & secrets**

   Edit `AlMadina.API/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=AlMadinaDB;Trusted_Connection=True;TrustServerCertificate=True;"
   },
   "Jwt": {
     "Key": "YOUR_SUPER_SECRET_KEY",
     "Issuer": "AlMadinaAPI",
     "Audience": "AlMadinaClient"
   }
   ```

   > ⚠️ Never commit real secrets (JWT keys, email passwords, Paymob keys) to a public repository. Use environment variables or [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) in development.

3. **Apply database migrations**

   ```bash
   cd AlMadina.API
   dotnet ef database update
   ```

4. **Run the API**

   ```bash
   dotnet run
   ```

5. **Open Swagger UI** — navigate to `https://localhost:<port>/swagger`

## 📡 Main API Endpoints

| Area | Endpoint | Description |
|---|---|---|
| Auth | `POST /api/auth/register` · `POST /api/auth/login` | Register & login (returns JWT) |
| Products | `GET/POST/PUT/DELETE /api/products` | Product management |
| Categories | `GET/POST/PUT/DELETE /api/categories` | Category management |
| Orders | `GET/POST /api/orders` | Place & track orders |
| Payment | `POST /api/payment/...` | Paymob payment initiation & callbacks |
| Returns | `POST /api/returns` | Return requests |
| Stock | `GET /api/stockmovements` | Inventory movement log |
| Deals | `GET /api/deals` | Active offers |
| Dashboard | `GET /api/dashboard/...` | Admin statistics |
| Admin | `/api/admin/...` | Admin-only operations |

## 🔗 Related Repositories

- **Frontend (Angular 19):** [7nawey/AlMadinah-Frontend](https://github.com/7nawey/AlMadinah-Frontend)

## 👤 Author

**Mohamed Emad Elhnawey** — [GitHub @7nawey](https://github.com/7nawey)
