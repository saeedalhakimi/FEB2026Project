using FEB2026Project.RUSTApi.Data.ContextModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEB2026Project.RUSTApi.Data.Configurations
{
    internal class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // Configure the primary key
            builder.HasKey(rt => rt.Id);

            // Configure properties
            builder.Property(rt => rt.Token).IsRequired();
            builder.Property(rt => rt.ExpiryDate).IsRequired();
            builder.Property(rt => rt.IdentityId).IsRequired();
            builder.Property(rt => rt.IsUsed).IsRequired();
            builder.Property(rt => rt.IsRevoked).IsRequired();
        }
    }
}
