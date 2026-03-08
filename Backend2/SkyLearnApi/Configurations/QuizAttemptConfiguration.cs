namespace SkyLearnApi.Configurations
{
    public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
    {
        public void Configure(EntityTypeBuilder<QuizAttempt> builder)
        {
            builder.ToTable("QuizAttempts");

            builder.HasKey(qa => qa.Id);

            builder.Property(qa => qa.Score)
                .HasColumnType("decimal(7,2)");

            builder.Property(qa => qa.MaxScore)
                .HasColumnType("decimal(7,2)");

            builder.Property(qa => qa.ScorePercent)
                .HasColumnType("decimal(5,2)");

            builder.Property(qa => qa.Status)
                .IsRequired()
                .HasMaxLength(15);

            builder.Property(qa => qa.StartedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(qa => qa.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(qa => qa.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(qa => qa.Student)
                .WithMany()
                .HasForeignKey(qa => qa.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(qa => qa.GradedBy)
                .WithMany()
                .HasForeignKey(qa => qa.GradedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: one attempt number per student per quiz
            builder.HasIndex(qa => new { qa.QuizId, qa.StudentId, qa.AttemptNumber })
                .IsUnique()
                .HasDatabaseName("IX_QuizAttempt_Quiz_Student_Attempt");

            builder.HasIndex(qa => qa.QuizId);
            builder.HasIndex(qa => qa.StudentId);
        }
    }
}
