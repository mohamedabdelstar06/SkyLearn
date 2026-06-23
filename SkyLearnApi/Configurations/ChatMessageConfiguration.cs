using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyLearnApi.Entities;

namespace SkyLearnApi.Configurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.HasKey(cm => cm.Id);

            builder.Property(cm => cm.Role)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(cm => cm.Message)
                .IsRequired();
                
            builder.HasIndex(cm => cm.SessionId);
            builder.HasIndex(cm => cm.CreatedAt);
        }
    }
}
