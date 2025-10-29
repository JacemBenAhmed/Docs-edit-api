using Docs_edits.DTOs;

using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Docs_edits.Models;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository userRepository, IConfiguration config)
    {
        _userRepository = userRepository;
        _config = config;
    }

    public async Task<(string token, User user)> GoogleLoginAsync(GoogleLoginDto dto)
    {
        if (dto == null || string.IsNullOrEmpty(dto.Email))
            throw new ArgumentException("Google user info is required.", nameof(dto));

       
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            user = new User
            {
                Name = dto.Name ?? "No Name",
                Email = dto.Email,
                Avatar = dto.Picture ?? string.Empty
            };
            user = await _userRepository.AddAsync(user);
        }


        var token = GenerateJwtToken(user);
        return (token, user);
    }


    private string GenerateJwtToken(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var keyString = _config["Jwt:Key"];
        if (string.IsNullOrEmpty(keyString))
            throw new Exception("JWT Key is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), 
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Name, user.Name ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) 
    };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
