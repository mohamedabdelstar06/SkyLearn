

namespace SkyLearnApi.Services.Implementation
{
     
    /// Centralized JWT token generation service.
    /// Single source of truth for all JWT operations.
     
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtSettings _jwtSettings;

        public JwtService(IConfiguration config, UserManager<ApplicationUser> userManager)
        {
            _config = config;
            _userManager = userManager;
            _jwtSettings = config.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        }

        public async Task<JwtTokenResult> GenerateTokenAsync(ApplicationUser user)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException(nameof(user), "User cannot be null");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var jti = Guid.NewGuid().ToString();
                var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes);

                Log.Information("Generating JWT for UserId: {UserId}, Roles: {Roles}",
                    user.Id, string.Join(",", roles));

                var claims = new List<Claim>
                {
                    new Claim("UserId", user.Id.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(JwtRegisteredClaimNames.Jti, jti),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
                };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var secretKey = _jwtSettings.Key;

                if (string.IsNullOrEmpty(secretKey))
                {
                    Log.Error("JWT Secret Key is missing from configuration");
                    throw new InvalidOperationException("JWT Secret Key is missing from configuration");
                }

                if (secretKey.Length < 32)
                {
                    Log.Warning("JWT Secret Key is too short (minimum 32 characters recommended). Current length: {Length}", secretKey.Length);
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires: expiresAt,
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                if (string.IsNullOrEmpty(tokenString))
                {
                    Log.Error("JWT token string is null or empty after generation for UserId: {UserId}", user.Id);
                    throw new InvalidOperationException("Failed to generate JWT token string");
                }

                Log.Debug("JWT generated successfully for UserId: {UserId}, Jti: {Jti}, TokenLength: {TokenLength}", 
                    user.Id, jti, tokenString.Length);

                return new JwtTokenResult
                {
                    Token = tokenString,
                    Jti = jti,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating JWT token for UserId: {UserId}", user?.Id);
                throw;
            }
        }

        public JwtTokenInfo? ParseToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                var jti = jwt.Id;

                return new JwtTokenInfo
                {
                    UserId = int.TryParse(userIdClaim, out var userId) ? userId : null,
                    Jti = jti,
                    ExpiresAt = jwt.ValidTo,
                    Roles = jwt.Claims
                        .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                        .Select(c => c.Value)
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to parse JWT token");
                return null;
            }
        }
    }
}
