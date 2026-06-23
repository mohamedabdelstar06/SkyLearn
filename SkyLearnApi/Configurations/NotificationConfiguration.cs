namespace SkyLearnApi.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Body)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(n => n.Type)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(n => n.IsRead)
                .HasDefaultValue(false);

            builder.Property(n => n.EmailSent)
                .HasDefaultValue(false);

            builder.Property(n => n.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.ReferenceActivity)
                .WithMany()
                .HasForeignKey(n => n.ReferenceActivityId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(n => n.UserId);
            builder.HasIndex(n => new { n.IsRead, n.EmailSent })
                .HasDatabaseName("IX_Notification_IsRead_EmailSent");
        }
    }
}
