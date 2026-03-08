using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkyLearnApi.Configuration;
using SkyLearnApi.Configurations;
using SkyLearnApi.Entities;

namespace SkyLearnApi.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Year> Years { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Squadron> Squadrons { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        // New - Activity system
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; }
        public DbSet<StudentActivityProgress> StudentActivityProgress { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());
            modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
            modelBuilder.ApplyConfiguration(new YearConfiguration());
            modelBuilder.ApplyConfiguration(new CourseConfiguration());
            modelBuilder.ApplyConfiguration(new SquadronConfiguration());
            modelBuilder.ApplyConfiguration(new StudentProfileConfiguration());
            modelBuilder.ApplyConfiguration(new EnrollmentConfiguration());
            modelBuilder.ApplyConfiguration(new ActivityLogConfiguration());

            // New configurations
            modelBuilder.ApplyConfiguration(new ActivityConfiguration());
            modelBuilder.ApplyConfiguration(new QuestionConfiguration());
            modelBuilder.ApplyConfiguration(new QuestionOptionConfiguration());
            modelBuilder.ApplyConfiguration(new QuizAttemptConfiguration());
            modelBuilder.ApplyConfiguration(new StudentAnswerConfiguration());
            modelBuilder.ApplyConfiguration(new AssignmentSubmissionConfiguration());
            modelBuilder.ApplyConfiguration(new CommentConfiguration());
            modelBuilder.ApplyConfiguration(new CommentLikeConfiguration());
            modelBuilder.ApplyConfiguration(new StudentActivityProgressConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationConfiguration());

            // Derived entity configurations (TPH)
            modelBuilder.Entity<Lecture>().Property(l => l.ContentType).HasMaxLength(20);
            modelBuilder.Entity<Lecture>().Property(l => l.FileUrl).HasMaxLength(500);
            modelBuilder.Entity<Lecture>().Property(l => l.ThumbnailUrl).HasMaxLength(500);

            modelBuilder.Entity<Quiz>().Property(q => q.TotalMarks).HasColumnType("decimal(7,2)");
            modelBuilder.Entity<Quiz>().Property(q => q.PassingScore).HasColumnType("decimal(5,2)");
            modelBuilder.Entity<Quiz>().Property(q => q.GradingMode).HasMaxLength(10);
            modelBuilder.Entity<Quiz>().Property(q => q.QuizScope).HasMaxLength(10);
            modelBuilder.Entity<Quiz>().Property(q => q.SourceLectureIds).HasMaxLength(500);
            modelBuilder.Entity<Quiz>().Property(q => q.DifficultyLevel).HasMaxLength(10);

            modelBuilder.Entity<Assignment>().Property(a => a.MaxGrade).HasColumnType("decimal(7,2)");
            modelBuilder.Entity<Assignment>().Property(a => a.AssignmentFileUrls).HasColumnType("nvarchar(max)");
        }
    }
}
