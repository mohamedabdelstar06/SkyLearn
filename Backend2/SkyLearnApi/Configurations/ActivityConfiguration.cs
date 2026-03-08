namespace SkyLearnApi.Configurations
{
    public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
    {
        public void Configure(EntityTypeBuilder<Activity> builder)
        {
            builder.ToTable("Activities");

            builder.HasKey(a => a.Id);

            // TPH Discriminator
            builder.HasDiscriminator<string>("ActivityType")
                .HasValue<Lecture>("Lecture")
                .HasValue<Quiz>("Quiz")
                .HasValue<Assignment>("Assignment");

            builder.Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(a => a.Description)
                .HasColumnType("nvarchar(max)");

            builder.Property(a => a.SortOrder)
                .HasDefaultValue(0);

            builder.Property(a => a.IsVisible)
                .HasDefaultValue(true);

            builder.Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(a => a.Course)
                .WithMany(c => c.Activities)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.TargetSquadron)
                .WithMany()
                .HasForeignKey(a => a.TargetSquadronId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(a => a.CourseId);
            builder.HasIndex(a => a.CreatedById);
            builder.HasIndex(a => a.TargetSquadronId);
        }
    }
}
