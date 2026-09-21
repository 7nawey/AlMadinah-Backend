using System.Text;
using AlMadina.Domain.Settings;
using AlMadina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using AlMadina.Application.Interfaces;
using AlMadina.Infrastructure.Repositories;
using AlMadina.Infrastructure.Services;
using AlMadina.Domain.Entities;
using AlMadina.Application.Interfaces.Services;
using AlMadina.API.Middleware;

namespace AlMadina.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================
            // Database
            // =========================
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // =========================
            // CORS
            // =========================
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            });

            // =========================
            // Identity
            // =========================
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // =========================
            // JWT Authentication
            // =========================
            var jwtKey = builder.Configuration["Jwt:Key"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
                };
            });

            // =========================
            // Authorization
            // =========================
            builder.Services.AddAuthorization();

            // =========================
            // Configuration Sections
            // =========================
            builder.Services.Configure<PaymobSettings>(
                builder.Configuration.GetSection(PaymobSettings.SectionName));

            // =========================
            // HttpClient
            // =========================
            builder.Services.AddHttpClient<PaymobService>();

            // =========================
            // AutoMapper
            // =========================
            builder.Services.AddAutoMapper(
                typeof(MappingProfile).Assembly,
                typeof(IdentityMappingProfile).Assembly
            );

            // =========================
            // Repositories & Unit of Work
            // =========================
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // =========================
            // Application Services
            // =========================
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IPaymobService, PaymobService>();
            builder.Services.AddScoped<IReturnService, ReturnService>();
            builder.Services.AddScoped<IAdminUserService, AdminUserService>();
            builder.Services.AddScoped<IStockMovementService, StockMovementService>();
            builder.Services.AddScoped<IHomePageService, HomePageService>();
            builder.Services.AddScoped<IDealService, DealService>();

            builder.Services.AddMemoryCache();

            // =========================
            // Output Cache (public catalog endpoints)
            // =========================
            builder.Services.AddOutputCache();

            // =========================
            // Controllers + Swagger
            // =========================
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // =========================
            // Middleware pipeline
            // =========================
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");

            // Output cache must run before authentication so anonymous
            // catalog responses are served without hitting the database.
            app.UseOutputCache();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.MapControllers();

            app.UseStaticFiles();

            // Serve external images folder
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "images")),
                RequestPath = "/images"
            });

            // Seed data on startup
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                await AlMadina.Infrastructure.Data.SeedData.SeedAsync(context, userManager, roleManager);
            }

            app.MapGet("/", context =>
            {
                context.Response.Redirect("/swagger");
                return Task.CompletedTask;
            });

            app.Run();
        }
    }
}
