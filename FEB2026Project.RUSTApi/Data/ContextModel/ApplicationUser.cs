using Microsoft.AspNetCore.Identity;

namespace FEB2026Project.RUSTApi.Data.ContextModel
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
    }
}
