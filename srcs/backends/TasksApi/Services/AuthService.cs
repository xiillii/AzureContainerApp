using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;

namespace TasksApi.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<User?> ValidateTokenAsync(string token);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly Data.TasksDbContext _context;

    public AuthService(IConfiguration configuration, Data.TasksDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // For demo purposes, using simple validation
        // In production, use proper password hashing (BCrypt, etc.)
        var user = await Task.Run(() => _context.Users.FirstOrDefault(u => u.Username == request.Username));
        
        if (user == null)
            return null;

        // Generate JWT token
        var token = GenerateJwtToken(user);
        
        return new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Role = user.Role
        };
    }

    public async Task<User?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "your-secret-key-min-32-chars-long!");
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var username = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;

            return await Task.Run(() => _context.Users.FirstOrDefault(u => u.Username == username));
        }
        catch
        {
            return null;
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "your-secret-key-min-32-chars-long!");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("userId", user.Id.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
