namespace SkyLearnApi.Configurations
{
    public class StudentActivityProgressConfiguration : IEntityTypeConfiguration<StudentActivityProgress>
    {
        public void Configure(EntityTypeBuilder<StudentActivityProgress> builder)
        {
            builder.ToTable("StudentActivityProgress");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(15)
                .HasDefaultValue("NotStarted");

            builder.Property(p => p.ProgressPercent)
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(0);

            builder.Property(p => p.TotalTimeSpentSeconds)
                .HasDefaultValue(0L);

            builder.HasOne(p => p.Activity)
                .WithMany(a => a.StudentProgress)
                .HasForeignKey(p => p.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // One progress record per student per activity
            builder.HasIndex(p => new { p.ActivityId, p.StudentId })
                .IsUnique()
                .HasDatabaseName("IX_StudentActivityProgress_Activity_Student");

            builder.HasIndex(p => p.StudentId);
        }
    }
}
