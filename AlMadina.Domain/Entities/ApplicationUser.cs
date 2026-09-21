using Microsoft.AspNetCore.Identity;

namespace AlMadina.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public bool IsBlocked { get; set; }

        public string Role { get; set; } = "User";

        public int ReturnCount { get; set; }

        public bool IsFlagged { get; set; }
    }
}
