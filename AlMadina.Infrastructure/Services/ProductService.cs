using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Domain.Entities;

namespace AlMadina.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public ProductService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileService = fileService;
        }

        // ================= GET ALL (PAGINATION) =================
        public async Task<PagedResult<ProductDto>> GetAllAsync(int pageNumber = 1, int pageSize = 20)
        {
            var query = _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = await MapWithPricingAsync(products)
            };
        }

        // ================= SEARCH (PRODUCT + CATEGORY + BARCODE) =================
        public async Task<PagedResult<ProductDto>> SearchAsync(string keyword, int pageNumber = 1, int pageSize = 20)
        {
            keyword = keyword?.Trim() ?? "";

            if (keyword.Length == 0)
            {
                return new PagedResult<ProductDto>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0,
                    Data = new List<ProductDto>()
                };
            }

            // Arabic-normalized form of the keyword matches the
            // pre-normalized NameArNormalized column (أ/إ/آ→ا, ة→ه, ى→ي,
            // diacritics stripped), so "الألبان" finds "الالبان" and vice versa.
            var normalizedKeyword = ArabicTextNormalizer.Normalize(keyword) ?? keyword.ToLowerInvariant();

            // Split into tokens so near / partially-typed names still match
            // ("جبنه رومي" finds "جبنة رومي قديم", word order does not matter).
            var tokens = normalizedKeyword
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToArray();

            var baseQuery = _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category);

            // Full-keyword match (original behavior)
            IQueryable<Product> query = baseQuery.Where(x =>
                x.NameAr.Contains(keyword) ||
                (x.NameArNormalized != null && x.NameArNormalized.Contains(normalizedKeyword)) ||
                x.NameEn.Contains(keyword) ||
                (x.Barcode != null && x.Barcode.Contains(keyword)) ||
                (x.Category != null &&
                 (x.Category.NameAr.Contains(keyword) ||
                  x.Category.NameEn.Contains(keyword)))
            );

            // Broaden with per-token matching so near names are found too
            foreach (var token in tokens)
            {
                if (token == normalizedKeyword) continue;
                var t = token;
                query = query.Union(baseQuery.Where(x =>
                    x.NameAr.Contains(t) ||
                    (x.NameArNormalized != null && x.NameArNormalized.Contains(t)) ||
                    x.NameEn.Contains(t)));
            }

            var candidates = await query.ToListAsync();

            // Rank by closeness to the keyword (nearest first):
            // exact barcode > barcode prefix > exact name > name starts-with >
            // name contains > all tokens matched > some tokens > category only.
            var lowerKeyword = keyword.ToLowerInvariant();

            int Score(Product x)
            {
                var nameNorm = x.NameArNormalized ?? ArabicTextNormalizer.Normalize(x.NameAr) ?? "";
                var nameEn = (x.NameEn ?? "").ToLowerInvariant();

                if (x.Barcode != null && x.Barcode == keyword) return 1000;

                var score = 0;
                if (x.Barcode != null && x.Barcode.StartsWith(keyword)) score = Math.Max(score, 500);
                if (nameNorm == normalizedKeyword || nameEn == lowerKeyword) score = Math.Max(score, 400);
                if (nameNorm.StartsWith(normalizedKeyword) || nameEn.StartsWith(lowerKeyword)) score = Math.Max(score, 300);
                if (x.NameAr.Contains(keyword) || nameNorm.Contains(normalizedKeyword) || nameEn.Contains(lowerKeyword))
                    score = Math.Max(score, 250);

                var matchedTokens = tokens.Count(t => nameNorm.Contains(t) || nameEn.Contains(t));
                if (matchedTokens == tokens.Length) score = Math.Max(score, 200);
                else if (matchedTokens > 0) score = Math.Max(score, 100 + matchedTokens * 20);

                if (x.Category != null &&
                    (x.Category.NameAr.Contains(keyword) || x.Category.NameEn.Contains(keyword)))
                    score = Math.Max(score, 50);

                return score;
            }

            var ordered = candidates
                .OrderByDescending(Score)
                .ThenBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

            var totalCount = ordered.Count;

            var products = ordered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = await MapWithPricingAsync(products)
            };
        }

        // ================= GET BY ID =================
        public async Task<ProductDto?> GetByIdAsync(Guid id)
        {
            var product = await _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null) return null;

            var dto = await MapWithPricingAsync(new List<Product> { product });
            return dto.First();
        }

        // ================= GET FOR UPDATE (SEARCH BY NAME OR BARCODE) =================
        public async Task<ProductDto?> GetForUpdateAsync(string value)
        {
            value = value?.Trim() ?? "";

            var product = await _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x =>
                    x.NameAr == value ||
                    x.NameEn == value ||
                    x.Barcode == value);

            if (product == null) return null;

            var dto = await MapWithPricingAsync(new List<Product> { product });
            return dto.First();
        }

        // ================= FEATURED =================
        public async Task<PagedResult<ProductDto>> GetFeaturedAsync(int pageNumber = 1, int pageSize = 20)
        {
            var query = _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category)
                .Where(x => x.IsFeatured)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = await MapWithPricingAsync(products)
            };
        }

        // ================= DISCOUNTED (own discount columns OR an active deal) =================
        public async Task<PagedResult<ProductDto>> GetDiscountedAsync(int pageNumber = 1, int pageSize = 20)
        {
            var now = DateTime.UtcNow;

            var activeDeals = _unitOfWork.Deals
                .GetAllQueryable()
                .Where(d => d.IsActive
                    && (d.StartDate == default || d.StartDate <= now)
                    && (d.EndDate == default || d.EndDate >= now));

            var query = _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category)
                .Where(x => x.DiscountPercentage > 0
                    || activeDeals.Any(d => d.ProductId == x.Id))
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = await MapWithPricingAsync(products)
            };
        }

        // ================= BEST SELLING (REAL SALES LOGIC) =================
        // Only counts items from actually paid/delivered orders —
        // cancelled or unpaid orders must not inflate best-seller numbers.
        public async Task<PagedResult<ProductDto>> GetBestSellingAsync(int pageNumber = 1, int pageSize = 20)
        {
            var query = _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category)
                .Select(p => new
                {
                    Product = p,
                    SoldQty = p.OrderItems
                        .Where(o => o.Order.Status == OrderStatus.Paid ||
                                    o.Order.Status == OrderStatus.Delivered)
                        .Sum(o => (int?)o.Quantity) ?? 0
                })
                .OrderByDescending(x => x.SoldQty)
                .ThenBy(x => x.Product.DisplayOrder);

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Product)
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = await MapWithPricingAsync(products)
            };
        }

        // ================= OTHERS =================
        public async Task<PagedResult<ProductDto>> GetOthersAsync(int pageNumber = 1, int pageSize = 20)
        {
            var query = _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category)
                .Where(x => x.Category != null && x.Category.NameEn == "Others")
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = await MapWithPricingAsync(products)
            };
        }

        // ================= HOME REGULAR (non-featured, non-pinned) =================
        public async Task<PagedResult<ProductDto>> GetHomeRegularProductsAsync(int pageNumber = 1, int pageSize = 20)
        {
            var query = _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category)
                .Where(x => !x.IsFeatured && !x.IsPinned)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = await MapWithPricingAsync(products)
            };
        }

        // ================= CREATE =================
        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            var exists = await _unitOfWork.Products
                .GetAllQueryable()
                .AnyAsync(x => x.NameAr == dto.NameAr);

            if (exists)
                throw new Exception("Product already exists");

            var product = new Product
            {
                Id = Guid.NewGuid(),
                NameAr = dto.NameAr,
                NameEn = string.IsNullOrWhiteSpace(dto.NameEn) ? dto.NameAr : dto.NameEn,
                NameArNormalized = ArabicTextNormalizer.Normalize(dto.NameAr),
                Barcode = dto.Barcode,
                Price = dto.Price,
                CostPrice = dto.CostPrice,
                StockQuantity = dto.StockQuantity
            };

            Category category;

            if (!string.IsNullOrWhiteSpace(dto.CategoryName))
            {
                category = await _unitOfWork.Categories
                    .GetAllQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.NameAr == dto.CategoryName ||
                        x.NameEn == dto.CategoryName);

                if (category == null)
                {
                    category = new Category
                    {
                        Id = Guid.NewGuid(),
                        NameAr = dto.CategoryName,
                        NameEn = dto.CategoryName,
                        IsActive = true
                    };

                    await _unitOfWork.Categories.AddAsync(category);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            else
            {
                category = await _unitOfWork.Categories
                    .GetAllQueryable()
                    .FirstOrDefaultAsync(x => x.NameEn == "Others");
            }

            product.CategoryId = category.Id;

            if (dto.Image != null)
                product.ImageUrl = await _fileService.UploadImageAsync(dto.Image);

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductDto>(product);
        }

        // ================= UPDATE =================
        public async Task<bool> UpdateAsync(UpdateProductDto dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(dto.Id);

            if (product == null)
                return false;

            product.NameAr = dto.NameAr;
            product.NameEn = dto.NameEn;
            product.NameArNormalized = ArabicTextNormalizer.Normalize(dto.NameAr);
            product.Barcode = dto.Barcode;
            product.Price = dto.Price;
            product.CostPrice = dto.CostPrice;
            product.StockQuantity = dto.StockQuantity;
            product.IsActive = dto.IsActive;

            if (!string.IsNullOrWhiteSpace(dto.CategoryName))
            {
                var category = await _unitOfWork.Categories
                    .GetAllQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.NameAr == dto.CategoryName ||
                        x.NameEn == dto.CategoryName);

                if (category != null)
                    product.CategoryId = category.Id;
            }

            if (dto.Image != null)
                product.ImageUrl = await _fileService.UploadImageAsync(dto.Image);

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // ================= DELETE =================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);

            if (product == null)
                return false;

            _unitOfWork.Products.Delete(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // ================= GET BY CATEGORY =================
        public async Task<PagedResult<ProductDto>> GetProductsByCategoryAsync(
            Guid categoryId,
            int pageNumber = 1,
            int pageSize = 40)
        {
            var query = _unitOfWork.Products
                .GetAllQueryable()
                .Include(x => x.Category)
                .Where(x => x.CategoryId == categoryId)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = await MapWithPricingAsync(products)
            };
        }

        // ================= UPLOAD IMAGE =================
        public async Task<string> UploadImageAsync(Guid id, IFormFile file)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                throw new Exception("Product not found");

            var imageUrl = await _fileService.UploadImageAsync(file);
            product.ImageUrl = imageUrl;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return imageUrl;
        }

        // ================= EFFECTIVE PRICING (active deal first, then product's own discount) =================
        private async Task<List<ProductDto>> MapWithPricingAsync(List<Product> products)
        {
            var dtos = _mapper.Map<List<ProductDto>>(products);

            var ids = products.Select(p => p.Id).ToList();
            if (ids.Count == 0)
                return dtos;

            var now = DateTime.UtcNow;

            var activeDeals = await _unitOfWork.Deals
                .GetAllQueryable()
                .Where(d => ids.Contains(d.ProductId)
                    && d.IsActive
                    && (d.StartDate == default || d.StartDate <= now)
                    && (d.EndDate == default || d.EndDate >= now))
                .ToListAsync();

            var bestDealByProduct = activeDeals
                .GroupBy(d => d.ProductId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.DiscountPercentage ?? 0).First());

            for (var i = 0; i < products.Count; i++)
            {
                var product = products[i];
                var dto = dtos[i];

                if (bestDealByProduct.TryGetValue(product.Id, out var deal))
                {
                    dto.FinalPrice = deal.DiscountedPrice;
                    dto.DiscountPercentage = deal.DiscountPercentage
                        ?? (product.Price > 0 ? Math.Round((1 - deal.DiscountedPrice / product.Price) * 100m, 2) : null);
                }
                else if (product.DiscountPercentage is > 0
                    && product.DiscountStartDate.HasValue
                    && product.DiscountEndDate.HasValue
                    && now >= product.DiscountStartDate.Value
                    && now <= product.DiscountEndDate.Value)
                {
                    dto.FinalPrice = Math.Round(product.Price * (1 - product.DiscountPercentage.Value / 100m), 2);
                }
            }

            return dtos;
        }
    }
}
