namespace SkyLearnApi.Configurations
{
    public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
    {
        public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
        {
            builder.ToTable("AssignmentSubmissions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.FileUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(s => s.FileName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Grade)
                .HasColumnType("decimal(7,2)");

            builder.Property(s => s.Feedback)
                .HasColumnType("nvarchar(max)");

            builder.Property(s => s.Status)
                .IsRequired()
                .HasMaxLength(15);

            builder.Property(s => s.SubmittedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.GradedBy)
                .WithMany()
                .HasForeignKey(s => s.GradedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.AssignmentId);
            builder.HasIndex(s => s.StudentId);
        }
    }
}
