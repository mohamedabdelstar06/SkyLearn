namespace SkyLearnApi.Configurations
{
    public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
    {
        public void Configure(EntityTypeBuilder<QuestionOption> builder)
        {
            builder.ToTable("QuestionOptions");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.OptionText)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(o => o.OptionTextAr)
                .HasMaxLength(1000);

            builder.HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(o => o.QuestionId);
        }
    }
}
