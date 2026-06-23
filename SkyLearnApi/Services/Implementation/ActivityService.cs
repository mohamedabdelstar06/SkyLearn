using SkyLearnApi.DTOs.ActivityLog;

namespace SkyLearnApi.Services.Implementation
{
    /// <summary>
    /// Unified activity service that handles both audit logging and analytics tracking.
    /// Uses a separate DI scope to avoid conflicts with the main request scope.
    /// Stores UserFullName denormalized in ActivityLogs for direct data science queries.
    /// </summary>
    public class ActivityService : IActivityService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityService(
            IServiceScopeFactory scopeFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _scopeFactory = scopeFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PagedActivityLogsResponseDto> GetAllAsync(ActivityLogFilterParams filterParams)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = db.ActivityLogs
                .AsNoTracking()
                .AsQueryable();

            // Search filter - uses denormalized UserFullName directly
            if (!string.IsNullOrWhiteSpace(filterParams.Search))
            {
                var search = filterParams.Search.ToLower();
                query = query.Where(a =>
                    a.UserFullName.ToLower().Contains(search) ||
                    a.ActionName.ToLower().Contains(search) ||
                    (a.Description != null && a.Description.ToLower().Contains(search)) ||
                    (a.EntityName != null && a.EntityName.ToLower().Contains(search)));
            }

            // Filter by action name
            if (!string.IsNullOrWhiteSpace(filterParams.ActionName))
            {
                query = query.Where(a => a.ActionName == filterParams.ActionName);
            }

            // Filter by component (EntityName)
            if (!string.IsNullOrWhiteSpace(filterParams.Component))
            {
                query = query.Where(a => a.EntityName == filterParams.Component);
            }

            // Filter by user
            if (filterParams.UserId.HasValue)
            {
                query = query.Where(a => a.UserId == filterParams.UserId.Value);
            }

            // Filter by date range
            if (filterParams.FromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= filterParams.FromDate.Value);
            }

            if (filterParams.ToDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= filterParams.ToDate.Value);
            }

            // Filter by origin (web, mobile, cli, other)
            if (!string.IsNullOrWhiteSpace(filterParams.Origin))
            {
                var originFilter = filterParams.Origin.ToLower();
                query = originFilter switch
                {
                    "web" => query.Where(a => a.UserAgent != null && (
                        a.UserAgent.ToLower().Contains("mozilla") ||
                        a.UserAgent.ToLower().Contains("chrome") ||
                        a.UserAgent.ToLower().Contains("safari") ||
                        a.UserAgent.ToLower().Contains("edge"))),
                    "mobile" => query.Where(a => a.UserAgent != null && (
                        a.UserAgent.ToLower().Contains("mobile") ||
                        a.UserAgent.ToLower().Contains("android") ||
                        a.UserAgent.ToLower().Contains("iphone"))),
                    "cli" => query.Where(a => a.UserAgent != null &&
                        a.UserAgent.ToLower().Contains("postman")),
                    _ => query
                };
            }

            var totalCount = await query.CountAsync();

            // Sorting - uses denormalized UserFullName column directly
            query = filterParams.SortBy?.ToLower() switch
            {
                "userfullname" => filterParams.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(a => a.UserFullName)
                    : query.OrderByDescending(a => a.UserFullName),
                "eventname" => filterParams.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(a => a.ActionName)
                    : query.OrderByDescending(a => a.ActionName),
                "component" => filterParams.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(a => a.EntityName)
                    : query.OrderByDescending(a => a.EntityName),
                "ipaddress" => filterParams.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(a => a.IpAddress)
                    : query.OrderByDescending(a => a.IpAddress),
                _ => filterParams.SortDirection?.ToLower() == "asc"
                    ? query.OrderBy(a => a.Timestamp)
                    : query.OrderByDescending(a => a.Timestamp)
            };

            // Pagination - materialize first, then project in memory
            var rawLogs = await query
                .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                .Take(filterParams.PageSize)
                .ToListAsync();

            var logs = rawLogs.Select(a => new ActivityLogResponseDto
            {
                Id = a.Id,
                Time = a.Timestamp,
                UserFullName = a.UserFullName,
                AffectedUser = null,
                EventContext = BuildEventContext(a.EntityName, a.EntityId),
                Component = a.EntityName ?? "System",
                EventName = FormatActionName(a.ActionName),
                Description = a.Description,
                Origin = ParseOrigin(a.UserAgent),
                IpAddress = a.IpAddress
            }).ToList();

            var response = new PagedActivityLogsResponseDto
            {
                Logs = logs,
                TotalCount = totalCount,
                PageNumber = filterParams.PageNumber,
                PageSize = filterParams.PageSize
            };

            // Populate filter options only when requested (first page load)
            if (filterParams.IncludeFilterOptions)
            {
                var allLogs = db.ActivityLogs.AsNoTracking();

                var actionNames = await allLogs
                    .Select(a => a.ActionName)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToListAsync();

                var components = await allLogs
                    .Where(a => a.EntityName != null)
                    .Select(a => a.EntityName!)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToListAsync();

                var users = await allLogs
                    .Where(a => a.UserId != null)
                    .Select(a => new { a.UserId, a.UserFullName })
                    .Distinct()
                    .OrderBy(u => u.UserFullName)
                    .Take(500) // Limit to prevent huge payloads
                    .ToListAsync();

                response.FilterOptions = new ActivityLogFilterOptions
                {
                    ActionNames = actionNames,
                    Components = components,
                    Users = users.Select(u => new ActivityLogUserOption
                    {
                        Id = u.UserId!.Value,
                        FullName = u.UserFullName
                    }).ToList()
                };
            }

            return response;
        }

        public async Task TrackAsync(
            string actionName,
            int? userId = null,
            string? entityName = null,
            int? entityId = null,
            string? description = null,
            object? metadata = null,
            long? processingTimeMs = null)
        {
            var activity = new ActivityLog
            {
                ActionName = actionName,
                UserId = userId,
                UserFullName = await ResolveUserFullNameAsync(userId),
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                Metadata = SerializeMetadata(metadata),
                ProcessingTimeMs = processingTimeMs,
                IpAddress = GetCurrentIpAddress(),
                UserAgent = GetCurrentUserAgent(),
                Timestamp = DateTime.UtcNow
            };

            await TrackRawAsync(activity);
        }

        public async Task TrackLoginAsync(
            int userId,
            DateTime? tokenExpiresAt = null,
            object? metadata = null)
        {
            var now = DateTime.UtcNow;

            var activity = new ActivityLog
            {
                ActionName = ActivityActions.UserLoggedIn,
                UserId = userId,
                UserFullName = await ResolveUserFullNameAsync(userId),
                EntityName = "Auth",
                TokenExpiresAt = tokenExpiresAt,
                Description = $"User logged in successfully",
                Metadata = SerializeMetadata(metadata),
                IpAddress = GetCurrentIpAddress(),
                UserAgent = GetCurrentUserAgent(),
                Timestamp = now
            };

            await TrackRawAsync(activity);
        }

        public async Task TrackLogoutAsync(
            int? userId,
            object? metadata = null)
        {
            var now = DateTime.UtcNow;

            var activity = new ActivityLog
            {
                ActionName = ActivityActions.UserLoggedOut,
                UserId = userId,
                UserFullName = await ResolveUserFullNameAsync(userId),
                EntityName = "Auth",
                Description = "User logged out",
                Metadata = SerializeMetadata(metadata),
                IpAddress = GetCurrentIpAddress(),
                UserAgent = GetCurrentUserAgent(),
                Timestamp = now
            };

            await TrackRawAsync(activity);
        }

        public async Task TrackLoginFailedAsync(
            string attemptedEmail,
            string? reason = null,
            object? metadata = null)
        {
            var combinedMetadata = new
            {
                attemptedEmail,
                reason,
                additionalData = metadata
            };

            var activity = new ActivityLog
            {
                ActionName = ActivityActions.LoginFailed,
                EntityName = "Auth",
                UserFullName = "Guest user",
                Description = $"Login failed for {attemptedEmail}: {reason ?? "Invalid credentials"}",
                Metadata = SerializeMetadata(combinedMetadata),
                IpAddress = GetCurrentIpAddress(),
                UserAgent = GetCurrentUserAgent(),
                Timestamp = DateTime.UtcNow
            };

            await TrackRawAsync(activity);
        }

        public async Task TrackEntityActionAsync(
            string actionName,
            string entityName,
            int entityId,
            int? userId = null,
            string? description = null,
            object? metadata = null,
            long? processingTimeMs = null)
        {
            var activity = new ActivityLog
            {
                ActionName = actionName,
                EntityName = entityName,
                EntityId = entityId,
                UserId = userId,
                UserFullName = await ResolveUserFullNameAsync(userId),
                Description = description,
                Metadata = SerializeMetadata(metadata),
                ProcessingTimeMs = processingTimeMs,
                IpAddress = GetCurrentIpAddress(),
                UserAgent = GetCurrentUserAgent(),
                Timestamp = DateTime.UtcNow
            };

            await TrackRawAsync(activity);
        }

        public async Task TrackRawAsync(ActivityLog activityLog)
        {
            try
            {
                // If UserFullName wasn't set, resolve it now
                if (string.IsNullOrEmpty(activityLog.UserFullName) && activityLog.UserId.HasValue)
                {
                    activityLog.UserFullName = await ResolveUserFullNameAsync(activityLog.UserId);
                }

                if (string.IsNullOrEmpty(activityLog.UserFullName))
                {
                    activityLog.UserFullName = "Guest user";
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.ActivityLogs.Add(activityLog);
                await db.SaveChangesAsync();

                Log.Debug("Activity tracked: {ActionName} for User {UserFullName}, Entity: {EntityName}",
                    activityLog.ActionName, activityLog.UserFullName, activityLog.EntityName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, 
                    "Failed to track activity: {ActionName}, UserId: {UserId}, EntityName: {EntityName}, EntityId: {EntityId}",
                    activityLog.ActionName, 
                    activityLog.UserId, 
                    activityLog.EntityName,
                    activityLog.EntityId);
            }
        }

        public string? GetCurrentIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetCurrentUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();
        }

        /// <summary>
        /// Resolves the user's full name from the database for denormalized storage.
        /// Returns "Guest user" if no userId is provided or user is not found.
        /// </summary>
        private async Task<string> ResolveUserFullNameAsync(int? userId)
        {
            if (!userId.HasValue) return "Guest user";

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var fullName = await db.Users
                    .Where(u => u.Id == userId.Value)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync();

                return fullName ?? "Unknown user";
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to resolve UserFullName for UserId: {UserId}", userId);
                return "Unknown user";
            }
        }

        private static string? SerializeMetadata(object? metadata)
        {
            if (metadata == null) return null;

            try
            {
                return JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to serialize activity metadata");
                return null;
            }
        }

        /// <summary>
        /// Builds the event context string like "Course: Course id '5'"
        /// </summary>
        private static string? BuildEventContext(string? entityName, int? entityId)
        {
            if (string.IsNullOrWhiteSpace(entityName)) return "System";
            if (entityId.HasValue)
                return $"{entityName}: {entityName} id '{entityId}'";
            return entityName;
        }

        /// <summary>
        /// Formats PascalCase action name to a readable event name.
        /// e.g., "UserLoggedIn" → "User logged in"
        /// </summary>
        private static string FormatActionName(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName)) return actionName;

            var result = new StringBuilder();
            for (int i = 0; i < actionName.Length; i++)
            {
                if (i > 0 && char.IsUpper(actionName[i]))
                    result.Append(' ');

                result.Append(i == 0 ? char.ToUpper(actionName[i]) : char.ToLower(actionName[i]));
            }
            return result.ToString();
        }

        /// <summary>
        /// Determines origin (web, mobile, cli, etc.) from the User-Agent header.
        /// </summary>
        private static string? ParseOrigin(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return null;

            var ua = userAgent.ToLower();
            if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
                return "mobile";
            if (ua.Contains("postman"))
                return "cli";
            if (ua.Contains("mozilla") || ua.Contains("chrome") || ua.Contains("safari") || ua.Contains("edge"))
                return "web";

            return "other";
        }
    }
}
