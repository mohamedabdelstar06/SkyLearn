namespace SkyLearnApi.Configurations
{
    public class CommentLikeConfiguration : IEntityTypeConfiguration<CommentLike>
    {
        public void Configure(EntityTypeBuilder<CommentLike> builder)
        {
            builder.ToTable("CommentLikes");

            builder.HasKey(cl => cl.Id);

            builder.Property(cl => cl.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(cl => cl.Comment)
                .WithMany(c => c.Likes)
                .HasForeignKey(cl => cl.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cl => cl.User)
                .WithMany()
                .HasForeignKey(cl => cl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One like per user per comment
            builder.HasIndex(cl => new { cl.CommentId, cl.UserId })
                .IsUnique()
                .HasDatabaseName("IX_CommentLike_Comment_User");
        }
    }
}
