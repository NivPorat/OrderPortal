using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OrderPortal.OrderService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/test-auth")]
public class AuthTestController : ControllerBase
{
    private readonly JwtSettingsConfig settings;

    public AuthTestController(IOptions<JwtSettingsConfig> configuration)
    {
        settings = configuration.Value;
    }

    [HttpGet("generate-token")]
    public IActionResult GenerateTestToken()
    {
        var secretKey = settings.SecretKey;
        var issuer = settings.SecretKey;
        var audience = settings.SecretKey;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "test-user-id"),
            new Claim(JwtRegisteredClaimNames.Name, "Niv"), 
            new Claim(ClaimTypes.Role, "Admin")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var stringToken = tokenHandler.WriteToken(token);

        return Ok(new { token = stringToken });
    }
}