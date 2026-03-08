using SkyLearnApi.Filters;
using SkyLearnApi.Services;
using SkyLearnApi.Services.Implementation;
using SkyLearnApi.Services.Interfaces;
using Hangfire;
using Hangfire.SqlServer;

namespace SkyLearnApi.Extentions
{
    public static class ApplicationServicesExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            // Existing services
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IYearService, YearService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<ISquadronService, SquadronService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
            services.AddScoped<IStudentImportService, StudentImportService>();

            // New services - Activity system
            services.AddScoped<ILectureService, LectureService>();
            services.AddScoped<IQuizService, QuizService>();
            services.AddScoped<IAssignmentService, AssignmentService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IStudentActivityService, StudentActivityService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEmailService, EmailService>();

            // Gemini AI service
            services.Configure<GeminiSettings>(configuration.GetSection("Gemini"));
            services.AddHttpClient<IGeminiService, GeminiService>();
            
            // Local AI Transcription service (Whisper)
            services.AddScoped<ILocalTranscriptionService, LocalTranscriptionService>();

            // Universal Text Processing Pipeline
            services.AddScoped<SkyLearnApi.Services.TextPipeline.ITextCleaner, SkyLearnApi.Services.TextPipeline.TextCleaner>();
            services.AddScoped<SkyLearnApi.Services.TextPipeline.ILocalSummarizer, SkyLearnApi.Services.TextPipeline.LocalSummarizer>();
            
            services.AddScoped<SkyLearnApi.Services.TextPipeline.ITextExtractor, SkyLearnApi.Services.TextPipeline.PdfTextExtractor>();
            services.AddScoped<SkyLearnApi.Services.TextPipeline.ITextExtractor, SkyLearnApi.Services.TextPipeline.OpenXmlTextExtractor>();
            services.AddScoped<SkyLearnApi.Services.TextPipeline.ITextExtractor, SkyLearnApi.Services.TextPipeline.VideoAudioTextExtractor>();
            services.AddScoped<SkyLearnApi.Services.TextPipeline.ITextExtractor, SkyLearnApi.Services.TextPipeline.ImageTextExtractor>();

            // Email settings
            services.Configure<EmailSettings>(configuration.GetSection("Email"));

            // Hangfire and Background Jobs
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true
                }));
            
            services.AddHangfireServer();

            // Background email service for unread notification emails
            services.AddHostedService<EmailBackgroundService>();

            // Filters & infrastructure
            services.AddScoped<ActivityTrackingFilter>();
            services.AddProblemDetails();
            services.AddHttpContextAccessor();
            return services;
        }
    }
}
