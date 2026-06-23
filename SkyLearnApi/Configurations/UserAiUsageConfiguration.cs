using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyLearnApi.Entities;

namespace SkyLearnApi.Configurations
{
    public class UserAiUsageConfiguration : IEntityTypeConfiguration<UserAiUsage>
    {
        public void Configure(EntityTypeBuilder<UserAiUsage> builder)
        {
            builder.HasKey(u => u.Id);

            builder.HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(u => new { u.UserId, u.Date }).IsUnique();
        }
    }
}
