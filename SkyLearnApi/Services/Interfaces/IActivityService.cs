using SkyLearnApi.DTOs.ActivityLog;

namespace SkyLearnApi.Services.Interfaces
{
    public interface IActivityService
    {
        // Gets all activity logs with pagination and filtering
        Task<PagedActivityLogsResponseDto> GetAllAsync(ActivityLogFilterParams filterParams);

        // Tracks a general activity/event
        Task TrackAsync(
            string actionName,
            int? userId = null,
            string? entityName = null,
            int? entityId = null,
            string? description = null,
            object? metadata = null,
            long? processingTimeMs = null);

        // Tracks a user login event
        Task TrackLoginAsync(
            int userId,
            DateTime? tokenExpiresAt = null,
            object? metadata = null);

        // Tracks a user logout event
        Task TrackLogoutAsync(
            int? userId,
            object? metadata = null);

        // Tracks a failed login attempt
        Task TrackLoginFailedAsync(
            string attemptedEmail,
            string? reason = null,
            object? metadata = null);

        // Tracks entity-specific actions (CRUD operations)
        Task TrackEntityActionAsync(
            string actionName,
            string entityName,
            int entityId,
            int? userId = null,
            string? description = null,
            object? metadata = null,
            long? processingTimeMs = null);

        // Tracks raw ActivityLog entity (for advanced scenarios)
        Task TrackRawAsync(ActivityLog activityLog);

        // Gets the current request's IP address
        string? GetCurrentIpAddress();
    }
}
