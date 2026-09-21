using AlMadina.Application.DTOs;
using AlMadina.Application.Exceptions;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlMadina.Infrastructure.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AutoMapper.IMapper _mapper;

        public AdminUserService(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, AutoMapper.IMapper mapper)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            return users.Select(MapToDto);
        }

        public async Task<UserDto?> GetByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<bool> ToggleBlockAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return false;

            user.IsBlocked = !user.IsBlocked;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> ToggleFlagAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return false;

            user.IsFlagged = !user.IsFlagged;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
        {
            var orders = await _unitOfWork.Orders.GetAllQueryable()
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<OrderDto>>(orders);
        }

        private UserDto MapToDto(ApplicationUser user)
        {
            return _mapper.Map<UserDto>(user);
        }
    }
}
