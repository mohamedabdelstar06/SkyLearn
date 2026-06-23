namespace SkyLearnApi.Configurations
{
    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.ToTable("Questions");

            builder.HasKey(q => q.Id);

            builder.Property(q => q.QuestionText)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(q => q.QuestionTextAr)
                .HasMaxLength(2000);

            builder.Property(q => q.QuestionType)
                .IsRequired()
                .HasMaxLength(15);

            builder.Property(q => q.Marks)
                .HasColumnType("decimal(5,2)");

            builder.Property(q => q.DifficultyLevel)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(q => q.Explanation)
                .HasColumnType("nvarchar(max)");

            builder.Property(q => q.ExplanationAr)
                .HasColumnType("nvarchar(max)");

            builder.Property(q => q.SourceReference)
                .HasMaxLength(200);

            builder.Property(q => q.ImageUrl)
                .HasMaxLength(500);

            builder.HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(q => q.QuizId);
        }
    }
}
