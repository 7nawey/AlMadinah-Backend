using AlMadina.Domain.Entities;
using AlMadina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlMadina.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            await context.Database.MigrateAsync();

            // =====================
            // ROLES
            // =====================
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new IdentityRole("User"));

            // =====================
            // ADMIN
            // =====================
            var adminEmail = "Mohamedelhnawey676@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = "7nawey",
                    Email = adminEmail,
                    FullName = "Mohamed Elhnawey",
                    PhoneNumber = "01200289439",
                    Address = "Al Madinah",
                    IsActive = true,
                    Role = "Admin",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin123@M");

                if (!result.Succeeded)
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

                adminUser = await userManager.FindByEmailAsync(adminEmail);
            }

            // Ensure admin has Admin role via direct DbContext (bypass AddToRoleAsync FK bug)
            if (adminUser != null)
            {
                var adminRole = await roleManager.FindByNameAsync("Admin");
                var hasAdminRole = await context.Set<IdentityUserRole<string>>()
                    .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole!.Id);
                if (!hasAdminRole)
                {
                    await context.Set<IdentityUserRole<string>>().AddAsync(new IdentityUserRole<string>
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole!.Id
                    });
                    await context.SaveChangesAsync();
                }
            }

            // =====================
            // USER 1
            // =====================
            var user1Email = "user1@almadina.com";
            var user1 = await userManager.FindByEmailAsync(user1Email);

            if (user1 == null)
            {
                var newUser1 = new ApplicationUser
                {
                    UserName = "user1",
                    Email = user1Email,
                    FullName = "User One",
                    PhoneNumber = "01000000001",
                    Address = "Cairo",
                    IsActive = true,
                    Role = "User",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newUser1, "User123@M");

                if (!result.Succeeded)
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

                user1 = await userManager.FindByEmailAsync(user1Email);
            }

            if (user1 != null)
            {
                var userRole = await roleManager.FindByNameAsync("User");
                var hasUser1Role = await context.Set<IdentityUserRole<string>>()
                    .AnyAsync(ur => ur.UserId == user1.Id && ur.RoleId == userRole!.Id);
                if (!hasUser1Role)
                {
                    await context.Set<IdentityUserRole<string>>().AddAsync(new IdentityUserRole<string>
                    {
                        UserId = user1.Id,
                        RoleId = userRole!.Id
                    });
                    await context.SaveChangesAsync();
                }
            }

            // =====================
            // USER 2
            // =====================
            var user2Email = "user2@almadina.com";
            var user2 = await userManager.FindByEmailAsync(user2Email);

            if (user2 == null)
            {
                var newUser2 = new ApplicationUser
                {
                    UserName = "user2",
                    Email = user2Email,
                    FullName = "User Two",
                    PhoneNumber = "01000000002",
                    Address = "Alexandria",
                    IsActive = true,
                    Role = "User",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newUser2, "User123@M");

                if (!result.Succeeded)
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

                user2 = await userManager.FindByEmailAsync(user2Email);
            }

            if (user2 != null)
            {
                var userRole = await roleManager.FindByNameAsync("User");
                var hasUser2Role = await context.Set<IdentityUserRole<string>>()
                    .AnyAsync(ur => ur.UserId == user2.Id && ur.RoleId == userRole!.Id);
                if (!hasUser2Role)
                {
                    await context.Set<IdentityUserRole<string>>().AddAsync(new IdentityUserRole<string>
                    {
                        UserId = user2.Id,
                        RoleId = userRole!.Id
                    });
                    await context.SaveChangesAsync();
                }
            }

            // =====================
            // CATEGORIES
            // =====================
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new() { Id = Guid.NewGuid(), NameAr = "مشروبات", NameEn = "Beverages", Description = "مشروبات غازية وعصائر و مياه", ImageUrl = "/images/categories/beverages.png", IsActive = true },
                    new() { Id = Guid.NewGuid(), NameAr = "مخبوزات", NameEn = "Bakery", Description = "خبز وكعك ومعجنات", ImageUrl = "/images/categories/bakery.png", IsActive = true },
                    new() { Id = Guid.NewGuid(), NameAr = "حلويات", NameEn = "Sweets", Description = "حلويات وشيكولاتة وبسكويت", ImageUrl = "/images/categories/sweets.png", IsActive = true },
                    new() { Id = Guid.NewGuid(), NameAr = "ألبان وأجبان", NameEn = "Dairy", Description = "حليب و جبن و زبادي", ImageUrl = "/images/categories/dairy.png", IsActive = true },
                    new() { Id = Guid.NewGuid(), NameAr = "لحوم ودواجن", NameEn = "Meat", Description = "لحوم طازجة ودواجن", ImageUrl = "/images/categories/meat.png", IsActive = true },
                    new() { Id = Guid.NewGuid(), NameAr = "منظفات", NameEn = "Cleaning", Description = "منظفات المنزل والمطبخ", ImageUrl = "/images/categories/cleaning.png", IsActive = true },
                    new() { Id = Guid.NewGuid(), NameAr = "معلبات", NameEn = "Canned", Description = "معلبات ومواد غذائية جافة", ImageUrl = "/images/categories/canned.png", IsActive = true },
                    new() { Id = Guid.NewGuid(), NameAr = "خضروات وفاكهة", NameEn = "Vegetables", Description = "خضروات وفاكهة طازجة", ImageUrl = "/images/categories/vegetables.png", IsActive = true }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // =====================
            // PRODUCTS
            // =====================
            if (!await context.Products.AnyAsync())
            {
                var categories = await context.Categories.ToListAsync();

                var beverages = categories.First(c => c.NameEn == "Beverages");
                var bakery = categories.First(c => c.NameEn == "Bakery");
                var sweets = categories.First(c => c.NameEn == "Sweets");
                var dairy = categories.First(c => c.NameEn == "Dairy");
                var meat = categories.First(c => c.NameEn == "Meat");
                var cleaning = categories.First(c => c.NameEn == "Cleaning");
                var canned = categories.First(c => c.NameEn == "Canned");
                var vegetables = categories.First(c => c.NameEn == "Vegetables");

                var products = new List<Product>
                {
                    new() { Id = Guid.NewGuid(), NameAr="بيبسي 1 لتر", NameEn="Pepsi 1L", Barcode="123456789001", Price=15, CostPrice=10, StockQuantity=50, CategoryId=beverages.Id, ImageUrl="/images/products/pepsi.png", IsFeatured=true, DiscountPercentage=10, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="سفن أب 1 لتر", NameEn="7UP 1L", Barcode="123456789002", Price=14, CostPrice=9, StockQuantity=45, CategoryId=beverages.Id, ImageUrl="/images/products/7up.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="خبز بلدي", NameEn="Baladi Bread", Barcode="123456789003", Price=5, CostPrice=2, StockQuantity=200, CategoryId=bakery.Id, ImageUrl="/images/products/bread.png", IsFeatured=true, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="كيك شوكولاتة", NameEn="Chocolate Cake", Barcode="123456789004", Price=35, CostPrice=20, StockQuantity=15, CategoryId=sweets.Id, ImageUrl="/images/products/cake.png", IsFeatured=true, DiscountPercentage=20, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="حليب كامل الدسم 1 لتر", NameEn="Full Milk 1L", Barcode="123456789005", Price=25, CostPrice=18, StockQuantity=60, CategoryId=dairy.Id, ImageUrl="/images/products/milk.png", IsFeatured=true, DiscountPercentage=5, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="جبنة رومي 500جرام", NameEn="Romy Cheese 500g", Barcode="123456789006", Price=45, CostPrice=35, StockQuantity=30, CategoryId=dairy.Id, ImageUrl="/images/products/cheese.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="صدر دجاج 1 كجم", NameEn="Chicken Breast 1kg", Barcode="123456789007", Price=120, CostPrice=90, StockQuantity=25, CategoryId=meat.Id, ImageUrl="/images/products/chicken.png", IsFeatured=true, DiscountPercentage=15, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="لحم بقري مفروم 1 كجم", NameEn="Minced Beef 1kg", Barcode="123456789008", Price=180, CostPrice=140, StockQuantity=20, CategoryId=meat.Id, ImageUrl="/images/products/beef.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="صابون غسيل", NameEn="Laundry Soap", Barcode="123456789009", Price=12, CostPrice=8, StockQuantity=100, CategoryId=cleaning.Id, ImageUrl="/images/products/soap.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="منظف زجاج", NameEn="Glass Cleaner", Barcode="123456789010", Price=18, CostPrice=12, StockQuantity=80, CategoryId=cleaning.Id, ImageUrl="/images/products/cleaner.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="تونة معلبة", NameEn="Canned Tuna", Barcode="123456789011", Price=35, CostPrice=25, StockQuantity=50, CategoryId=canned.Id, ImageUrl="/images/products/tuna.png", IsFeatured=true, DiscountPercentage=10, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="فول معلب", NameEn="Canned Fava Beans", Barcode="123456789012", Price=10, CostPrice=7, StockQuantity=120, CategoryId=canned.Id, ImageUrl="/images/products/fava.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="طماطم طازجة 1 كجم", NameEn="Fresh Tomatoes 1kg", Barcode="123456789013", Price=15, CostPrice=10, StockQuantity=80, CategoryId=vegetables.Id, ImageUrl="/images/products/tomatoes.png", IsFeatured=true, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="بطاطس 1 كجم", NameEn="Potatoes 1kg", Barcode="123456789014", Price=12, CostPrice=8, StockQuantity=100, CategoryId=vegetables.Id, ImageUrl="/images/products/potatoes.png", IsFeatured=false, DiscountPercentage=5, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="شاي ليبتون 20 كيس", NameEn="Lipton Tea 20 bags", Barcode="123456789015", Price=28, CostPrice=20, StockQuantity=70, CategoryId=beverages.Id, ImageUrl="/images/products/tea.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="قهوة نسكافيه 200جرام", NameEn="Nescafe Coffee 200g", Barcode="123456789016", Price=85, CostPrice=65, StockQuantity=40, CategoryId=beverages.Id, ImageUrl="/images/products/coffee.png", IsFeatured=true, DiscountPercentage=10, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="كرواسان", NameEn="Croissant", Barcode="123456789017", Price=12, CostPrice=7, StockQuantity=60, CategoryId=bakery.Id, ImageUrl="/images/products/croissant.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="دونات", NameEn="Donut", Barcode="123456789018", Price=15, CostPrice=9, StockQuantity=50, CategoryId=sweets.Id, ImageUrl="/images/products/donut.png", IsFeatured=true, DiscountPercentage=5, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="زبادي طبيعي", NameEn="Plain Yogurt", Barcode="123456789019", Price=8, CostPrice=5, StockQuantity=90, CategoryId=dairy.Id, ImageUrl="/images/products/yogurt.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="بيض 30 بيضة", NameEn="Eggs 30pcs", Barcode="123456789020", Price=55, CostPrice=42, StockQuantity=45, CategoryId=dairy.Id, ImageUrl="/images/products/eggs.png", IsFeatured=true, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="سجق 1 كجم", NameEn="Sausage 1kg", Barcode="123456789021", Price=95, CostPrice=75, StockQuantity=30, CategoryId=meat.Id, ImageUrl="/images/products/sausage.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="سمن بلدي 500جرام", NameEn="Baladi Ghee 500g", Barcode="123456789022", Price=110, CostPrice=85, StockQuantity=25, CategoryId=dairy.Id, ImageUrl="/images/products/ghee.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="رز مصري 1 كجم", NameEn="Egyptian Rice 1kg", Barcode="123456789023", Price=22, CostPrice=16, StockQuantity=100, CategoryId=canned.Id, ImageUrl="/images/products/rice.png", IsFeatured=true, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="مكرونة 400 جرام", NameEn="Pasta 400g", Barcode="123456789024", Price=14, CostPrice=10, StockQuantity=80, CategoryId=canned.Id, ImageUrl="/images/products/pasta.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="سكر 1 كجم", NameEn="Sugar 1kg", Barcode="123456789025", Price=20, CostPrice=15, StockQuantity=150, CategoryId=canned.Id, ImageUrl="/images/products/sugar.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="دقيق 1 كجم", NameEn="Flour 1kg", Barcode="123456789026", Price=18, CostPrice=13, StockQuantity=120, CategoryId=bakery.Id, ImageUrl="/images/products/flour.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="بسكويت سادة", NameEn="Plain Biscuits", Barcode="123456789027", Price=10, CostPrice=6, StockQuantity=110, CategoryId=sweets.Id, ImageUrl="/images/products/biscuits.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="شيكولاتة كادبوري", NameEn="Cadbury Chocolate", Barcode="123456789028", Price=25, CostPrice=18, StockQuantity=75, CategoryId=sweets.Id, ImageUrl="/images/products/chocolate.png", IsFeatured=true, DiscountPercentage=10, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="عصير برتقال طازج", NameEn="Fresh Orange Juice", Barcode="123456789029", Price=18, CostPrice=12, StockQuantity=40, CategoryId=beverages.Id, ImageUrl="/images/products/juice.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="مياه معدنية 1.5 لتر", NameEn="Mineral Water 1.5L", Barcode="123456789030", Price=7, CostPrice=4, StockQuantity=200, CategoryId=beverages.Id, ImageUrl="/images/products/water.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="بصل 1 كجم", NameEn="Onions 1kg", Barcode="123456789031", Price=10, CostPrice=7, StockQuantity=90, CategoryId=vegetables.Id, ImageUrl="/images/products/onions.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="ثوم 250 جرام", NameEn="Garlic 250g", Barcode="123456789032", Price=12, CostPrice=8, StockQuantity=85, CategoryId=vegetables.Id, ImageUrl="/images/products/garlic.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="ليمون 1 كجم", NameEn="Lemons 1kg", Barcode="123456789033", Price=20, CostPrice=14, StockQuantity=70, CategoryId=vegetables.Id, ImageUrl="/images/products/lemons.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="مانجو 1 كجم", NameEn="Mango 1kg", Barcode="123456789034", Price=35, CostPrice=25, StockQuantity=50, CategoryId=vegetables.Id, ImageUrl="/images/products/mango.png", IsFeatured=true, DiscountPercentage=15, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="تفاح 1 كجم", NameEn="Apples 1kg", Barcode="123456789035", Price=30, CostPrice=22, StockQuantity=60, CategoryId=vegetables.Id, ImageUrl="/images/products/apples.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="موز 1 كجم", NameEn="Bananas 1kg", Barcode="123456789036", Price=18, CostPrice=13, StockQuantity=80, CategoryId=vegetables.Id, ImageUrl="/images/products/bananas.png", IsFeatured=false, DiscountPercentage=5, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="جزر 1 كجم", NameEn="Carrots 1kg", Barcode="123456789037", Price=12, CostPrice=9, StockQuantity=95, CategoryId=vegetables.Id, ImageUrl="/images/products/carrots.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="خيار 1 كجم", NameEn="Cucumbers 1kg", Barcode="123456789038", Price=14, CostPrice=10, StockQuantity=85, CategoryId=vegetables.Id, ImageUrl="/images/products/cucumbers.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="فلفل أخضر 1 كجم", NameEn="Green Pepper 1kg", Barcode="123456789039", Price=16, CostPrice=12, StockQuantity=75, CategoryId=vegetables.Id, ImageUrl="/images/products/pepper.png", IsFeatured=false, DiscountPercentage=0, IsActive=true },
                    new() { Id = Guid.NewGuid(), NameAr="سبانخ 250 جرام", NameEn="Spinach 250g", Barcode="123456789040", Price=8, CostPrice=5, StockQuantity=100, CategoryId=vegetables.Id, ImageUrl="/images/products/spinach.png", IsFeatured=false, DiscountPercentage=0, IsActive=true }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            // =====================
            // BACKFILL NameArNormalized (Arabic search)
            // =====================
            List<Product> needsNormalization;

            try
            {
                needsNormalization = await context.Products
                    .Where(p => p.NameArNormalized == null)
                    .ToListAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 207)
            {
                // 207 = Invalid column name. The migration is recorded in
                // __EFMigrationsHistory but its SQL never actually ran.
                Console.Error.WriteLine(
                    "[SeedData] Column 'NameArNormalized' is missing. The migration " +
                    "'20260916230000_AddProductSearchIndexes' is recorded as applied but " +
                    "the column does not exist. Fix: run in SSMS:" +
                    " DELETE FROM __EFMigrationsHistory WHERE MigrationId = " +
                    "'20260916230000_AddProductSearchIndexes';" +
                    " then run 'update-database' again.");
                throw;
            }

            foreach (var p in needsNormalization)
            {
                p.NameArNormalized =
                    AlMadina.Application.Common.ArabicTextNormalizer.Normalize(p.NameAr);
            }

            if (needsNormalization.Count > 0)
                await context.SaveChangesAsync();
        }
    }
}