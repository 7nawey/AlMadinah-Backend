using AutoMapper;
using AlMadina.Domain.Entities;
using AlMadina.Application.DTOs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Category
        CreateMap<Category, CategoryDto>();

        CreateMap<CreateCategoryDto, Category>()
            .ForMember(d => d.ImageUrl, o => o.Ignore());

        CreateMap<UpdateCategoryDto, Category>()
            .ForMember(d => d.ImageUrl, o => o.Ignore());

        // Product
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName,
                o => o.MapFrom(s => s.Category != null ? s.Category.NameEn : null))
            .ForMember(d => d.FinalPrice, o => o.Ignore()); // computed per-request in ProductService

        CreateMap<CreateProductDto, Product>()
            .ForMember(d => d.ImageUrl, o => o.Ignore());

        CreateMap<UpdateProductDto, Product>()
            .ForMember(d => d.ImageUrl, o => o.Ignore());

        // Order
        CreateMap<Order, OrderDto>();
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductName,
                o => o.MapFrom(s => s.Product.NameEn));

        CreateMap<CreateOrderDto, Order>();
        CreateMap<CreateOrderItemDto, OrderItem>();

        // Stock
        CreateMap<StockMovement, StockMovementDto>();
        CreateMap<CreateStockMovementDto, StockMovement>();

        // Returns
        CreateMap<ReturnRequest, ReturnRequestDto>();
        CreateMap<ReturnItem, ReturnItemDto>();
    }
}