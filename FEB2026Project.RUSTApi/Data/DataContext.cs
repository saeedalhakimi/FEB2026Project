using FEB2026Project.RUSTApi.Data.Configurations;
using FEB2026Project.RUSTApi.Data.ContextModel;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FEB2026Project.RUSTApi.Data
{
    public class DataContext : IdentityDbContext<ApplicationUser>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Apply the RefreshToken configuration
            builder.ApplyConfiguration(new RefreshTokenConfig());

            builder.Entity<ApplicationUser>(b =>
            {
                b.Property(u => u.IsActive)
                 .IsRequired()
                 .HasDefaultValue(true);   // SQL default = 1
            });

            base.OnModelCreating(builder);
            // Additional model configuration can go here
        }
    }
}
