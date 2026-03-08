using Hangfire;
using Microsoft.EntityFrameworkCore;
using SkyLearnApi.Data;
using SkyLearnApi.Services.Interfaces;

namespace SkyLearnApi.Services.Implementation
{
    public class EmailJobs
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailJobs> _logger;

        public EmailJobs(AppDbContext context, IEmailService emailService, ILogger<EmailJobs> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // Logo hosted on the deployed server – update this URL if your domain changes
        private const string LogoUrl = "https://skylearn.runasp.net/images/skylearn-logo.png";

        private string GetEmailHeader()
        {
            return $@"
                <div style='text-align: center; margin-bottom: 10px; padding: 20px 20px 0 20px;'>
                    <img src='{LogoUrl}' alt='SkyLearn - Egypt Air Force College' style='max-width: 320px; height: auto;' />
                </div>";
        }

        private static string GetEmailFooter()
        {
            return @"
                <div style='text-align: center; padding: 20px 30px; background-color: #f8fafc;'>
                    <p style='margin: 0 0 8px 0; color: #94a3b8; font-size: 12px;'>
                        © 2026 SkyLearn Platform — Egypt Air Force College
                    </p>
                    <p style='margin: 0; color: #cbd5e1; font-size: 11px;'>
                        This is an automated message. Please do not reply directly to this email.
                    </p>
                </div>";
        }

        public async Task SendDelayedEmailNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
            {
                _logger.LogWarning("Email Job: Notification {NotificationId} not found.", notificationId);
                return;
            }

            if (notification.IsRead || notification.EmailSent)
            {
                _logger.LogInformation("Email Job: Notification {NotificationId} already read or email already sent. Skipping...", notificationId);
                return;
            }

            var userEmail = notification.User?.Email;
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Email Job: User {UserId} has no email address. Skipping email for notification {NotificationId}.", notification.UserId, notificationId);
                return;
            }

            var htmlBody = $@"
                <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 650px; margin: 0 auto; background-color: #f4f7f6; padding: 20px;'>
                    {GetEmailHeader()}
                    <div style='background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                        <div style='background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%); padding: 20px; color: white;'>
                            <h2 style='margin: 0; font-size: 22px;'>🔔 SkyLearn Notification</h2>
                        </div>
                        <div style='padding: 30px;'>
                            <h3 style='color: #1f2937; font-size: 18px; margin-top: 0;'>{notification.Title}</h3>
                            <p style='color: #4b5563; line-height: 1.6; font-size: 16px;'>{notification.Body}</p>
                            
                            <div style='background: #eff6ff; padding: 15px; border-radius: 8px; margin-top: 20px; border-left: 4px solid #3b82f6;'>
                                <p style='margin: 0; color: #1e3a8a; font-size: 14px;'>
                                    <strong>📌 Category:</strong> {notification.Type}<br/>
                                    <strong>⏰ Time:</strong> {notification.CreatedAt:g} UTC
                                </p>
                            </div>
                            
                            <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 25px 0;'/>
                            <p style='color: #6b7280; font-size: 13px; text-align: center; margin: 0;'>
                                This email was sent because you have an unread notification on SkyLearn
                                that has been waiting for over 10 minutes. <br/>✨ Log in to view and respond! ✨
                            </p>
                        </div>
                        {GetEmailFooter()}
                    </div>
                </div>";

            try
            {
                await _emailService.SendEmailAsync(userEmail, $"SkyLearn: {notification.Title}", htmlBody);

                notification.EmailSent = true;
                notification.EmailSentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Email sent successfully for notification {NotificationId} to {Email}.", notificationId, userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email for notification {NotificationId} to {Email}.", notificationId, userEmail);
                throw; // Retry via Hangfire
            }
        }

        public async Task SendWelcomeEmailAsync(string email, string fullName, string role)
        {
            if (string.IsNullOrEmpty(email)) return;

            var firstName = fullName.Split(' ').FirstOrDefault() ?? fullName;

            // Role-specific emoji, greeting, and message
            var (roleEmoji, roleTitle, roleMessage) = role switch
            {
                "Student" => (
                    "🎓",
                    "Future Leader",
                    "You now have access to all your courses, lectures, quizzes, and assignments. " +
                    "Your academic journey starts here — make the most of every moment!"
                ),
                "Instructor" => (
                    "👨‍🏫",
                    "Valued Instructor",
                    "You can now manage courses, create lectures, build quizzes, and guide students " +
                    "toward excellence. Your knowledge shapes the future!"
                ),
                _ => (
                    "⭐",
                    "Administrator",
                    "You have full access to manage the platform, users, and educational content. " +
                    "Your leadership keeps SkyLearn running smoothly!"
                )
            };

            var htmlBody = $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            </head>
            <body style='margin: 0; padding: 0; background-color: #f0f4f8;'>
                <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 650px; margin: 0 auto; background-color: #f0f4f8; padding: 20px;'>
                    
                    <!-- Logo Section -->
                    <div style='text-align: center; margin-bottom: 5px; padding: 20px;'>
                        <img src='{LogoUrl}' alt='SkyLearn - Egypt Air Force College' style='max-width: 320px; height: auto;' />
                    </div>
                    
                    <!-- Main Card -->
                    <div style='background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 40px rgba(0,0,0,0.08);'>
                        
                        <!-- Hero Banner -->
                        <div style='background: linear-gradient(135deg, #1a2744 0%, #2c3e6b 50%, #c9a84c 100%); padding: 40px 30px; text-align: center;'>
                            <div style='font-size: 48px; margin-bottom: 15px;'>🎉✨🚀</div>
                            <h1 style='margin: 0; font-size: 32px; color: #ffffff; font-weight: 700; letter-spacing: 0.5px;'>
                                Welcome to SkyLearn!
                            </h1>
                            <p style='margin: 10px 0 0 0; font-size: 16px; color: #e2d5a0; font-weight: 300;'>
                                Egypt Air Force College Learning Platform
                            </p>
                        </div>
                        
                        <!-- Greeting Section -->
                        <div style='padding: 35px 30px 10px 30px;'>
                            <h2 style='color: #1a2744; font-size: 22px; margin: 0 0 5px 0;'>
                                Hello {firstName}! 👋
                            </h2>
                            <p style='color: #c9a84c; font-size: 14px; margin: 0 0 20px 0; font-weight: 600; text-transform: uppercase; letter-spacing: 1px;'>
                                {roleEmoji} {roleTitle}
                            </p>
                            <p style='color: #4b5563; line-height: 1.7; font-size: 16px; margin: 0;'>
                                We are <strong>thrilled</strong> to have you on board! 🎊 Your account on the 
                                <strong style='color: #1a2744;'>SkyLearn</strong> platform has been successfully created.
                            </p>
                        </div>
                        
                        <!-- Role Message Box -->
                        <div style='padding: 0 30px;'>
                            <div style='background: linear-gradient(135deg, #f8f6f0 0%, #faf7ed 100%); padding: 20px 25px; border-radius: 12px; margin-top: 20px; border-left: 5px solid #c9a84c;'>
                                <p style='margin: 0; color: #1a2744; font-size: 15px; line-height: 1.6;'>
                                    {roleEmoji} {roleMessage}
                                </p>
                            </div>
                        </div>
                        
                        <!-- Motivational Quote Section -->
                        <div style='padding: 25px 30px;'>
                            <div style='text-align: center; padding: 25px; background: linear-gradient(135deg, #1a2744 0%, #2c3e6b 100%); border-radius: 12px;'>
                                <p style='margin: 0; color: #e2d5a0; font-size: 15px; font-style: italic; line-height: 1.6;'>
                                    ""The sky is not the limit, it's just the beginning."" ✈️
                                </p>
                                <p style='margin: 10px 0 0 0; color: #8b9dc3; font-size: 13px;'>
                                    — Your journey to excellence starts NOW! 💪🌟
                                </p>
                            </div>
                        </div>
                        
                        <!-- Action Required Section -->
                        <div style='padding: 0 30px 30px 30px;'>
                            <div style='background: linear-gradient(135deg, #ecfdf5 0%, #f0fdf9 100%); padding: 25px; border-radius: 12px; text-align: center; border: 2px dashed #10b981;'>
                                <div style='font-size: 36px; margin-bottom: 10px;'>🔑</div>
                                <h3 style='margin: 0 0 10px 0; color: #065f46; font-size: 18px;'>
                                    Activate Your Account
                                </h3>
                                <p style='margin: 0; color: #047857; font-size: 14px; line-height: 1.6;'>
                                    Log in to the platform using your email address:<br/>
                                    <strong style='color: #065f46; font-size: 15px;'>{email}</strong><br/><br/>
                                    You will be prompted to set your <strong>secure password</strong> 🔒<br/>
                                    for the first time to activate your account.
                                </p>
                            </div>
                        </div>
                        
                        <!-- Divider -->
                        <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 0 30px;'/>
                        
                        <!-- Support Section -->
                        <div style='padding: 25px 30px; text-align: center;'>
                            <p style='color: #6b7280; font-size: 14px; margin: 0 0 8px 0;'>
                                Need help? Contact your system administrator 📩
                            </p>
                            <p style='color: #1a2744; font-size: 16px; margin: 0; font-weight: 600;'>
                                🌟 We wish you a successful and inspiring journey! 🌟
                            </p>
                            <p style='color: #c9a84c; font-size: 20px; margin: 10px 0 0 0;'>
                                ✈️ Aim High, Fly Higher! ✈️
                            </p>
                        </div>
                        
                        <!-- Footer -->
                        <div style='text-align: center; padding: 20px 30px; background-color: #1a2744;'>
                            <p style='margin: 0 0 5px 0; color: #e2d5a0; font-size: 13px; font-weight: 600;'>
                                SkyLearn Platform — Egypt Air Force College
                            </p>
                            <p style='margin: 0; color: #8b9dc3; font-size: 11px;'>
                                © 2026 SkyLearn. All Rights Reserved.
                            </p>
                        </div>
                        
                    </div>
                </div>
            </body>
            </html>";

            try
            {
                await _emailService.SendEmailAsync(email, "🎉🚀 Welcome to SkyLearn — Your Journey Starts Now! ✈️", htmlBody);
                _logger.LogInformation("Welcome email sent successfully to {Email}.", email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Silently handled error sending welcome email to {Email}. Email might be fake or invalid.", email);
                // Intentionally swallowing the exception so fake/invalid emails don't crash the background job or cause endless retries.
            }
        }
    }
}
