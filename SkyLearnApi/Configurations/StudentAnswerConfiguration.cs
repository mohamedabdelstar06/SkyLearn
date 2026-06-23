namespace SkyLearnApi.Configurations
{
    public class StudentAnswerConfiguration : IEntityTypeConfiguration<StudentAnswer>
    {
        public void Configure(EntityTypeBuilder<StudentAnswer> builder)
        {
            builder.ToTable("StudentAnswers");

            builder.HasKey(sa => sa.Id);

            builder.Property(sa => sa.WrittenAnswer)
                .HasColumnType("nvarchar(max)");

            builder.Property(sa => sa.MarksAwarded)
                .HasColumnType("decimal(5,2)");

            builder.Property(sa => sa.InstructorFeedback)
                .HasMaxLength(1000);

            builder.HasOne(sa => sa.QuizAttempt)
                .WithMany(qa => qa.Answers)
                .HasForeignKey(sa => sa.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sa => sa.Question)
                .WithMany(q => q.StudentAnswers)
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(sa => sa.SelectedOption)
                .WithMany()
                .HasForeignKey(sa => sa.SelectedOptionId)
                .OnDelete(DeleteBehavior.NoAction);

            // One answer per question per attempt
            builder.HasIndex(sa => new { sa.QuizAttemptId, sa.QuestionId })
                .IsUnique()
                .HasDatabaseName("IX_StudentAnswer_Attempt_Question");

            builder.HasIndex(sa => sa.QuizAttemptId);
        }
    }
}
