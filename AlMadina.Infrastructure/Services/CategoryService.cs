using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces;
using AlMadina.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileService = fileService;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _unitOfWork.Categories
            .GetAllQueryable()
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);

        if (category == null)
            return null;

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var category = _mapper.Map<Category>(dto);

        if (dto.Image != null)
        {
            category.ImageUrl = await _fileService.UploadImageAsync(dto.Image);
        }

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<bool> UpdateAsync(UpdateCategoryDto dto)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(dto.Id);

        if (category == null)
            return false;

        _mapper.Map(dto, category);

        if (dto.Image != null)
        {
            category.ImageUrl = await _fileService.UploadImageAsync(dto.Image);
        }

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);

        if (category == null)
            return false;

        _unitOfWork.Categories.Delete(category);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
