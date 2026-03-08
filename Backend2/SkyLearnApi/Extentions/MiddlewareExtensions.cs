using Serilog;
using SkyLearnApi.Middleware;
using SkyLearnApi.Hubs;

namespace SkyLearnApi.Extentions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionMiddleware>();
        }
        public static IApplicationBuilder UseSerilogLogging(this IApplicationBuilder app)
        {
            return app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    if (ex != null) return Serilog.Events.LogEventLevel.Error;
                    if (httpContext.Response.StatusCode >= 500) return Serilog.Events.LogEventLevel.Error;
                    if (httpContext.Response.StatusCode >= 400) return Serilog.Events.LogEventLevel.Warning;
                    return Serilog.Events.LogEventLevel.Information;
                };
                
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set("RequestPath", httpContext.Request.Path.ToString());
                    diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                    diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
                    
                    var userId = httpContext.User.FindFirst("UserId")?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        diagnosticContext.Set("UserId", userId);
                    }
                    
                    var roles = httpContext.User.Claims
                        .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList();
                    if (roles.Any())
                    {
                        diagnosticContext.Set("UserRoles", string.Join(",", roles));
                    }
                };
            });
        }

        public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
        {
            var pathBase = app.Configuration.GetValue<string>("PathBase");
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            app.UseSerilogLogging();
            app.UseGlobalExceptionHandler();

            // Swagger should be available in all environments for API documentation
            // But you can restrict it to Development only if needed
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyLearn API v1");
                c.RoutePrefix = string.Empty; // Serve Swagger UI at root
                c.DocumentTitle = "SkyLearn API Documentation";
                c.DefaultModelsExpandDepth(-1); // Hide models section by default
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.EnableFilter();
                c.ShowExtensions();
                c.EnableValidator();
            });

            app.UseCors("AllowAll");

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            // Ensure common media types are present
            if (!provider.Mappings.ContainsKey(".mp4")) provider.Mappings[".mp4"] = "video/mp4";
            if (!provider.Mappings.ContainsKey(".mkv")) provider.Mappings[".mkv"] = "video/x-matroska";
            if (!provider.Mappings.ContainsKey(".webm")) provider.Mappings[".webm"] = "video/webm";
            if (!provider.Mappings.ContainsKey(".mp3")) provider.Mappings[".mp3"] = "audio/mpeg";

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider,
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/octet-stream",
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Accept-Ranges"] = "bytes";
                    ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                }
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<NotificationHub>("/hubs/notifications");
            return app;
        }
    }
}

