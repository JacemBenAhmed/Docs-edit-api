using Docs_edits.DTOs;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        if (dto == null)
            return BadRequest(new { message = "Request body is missing." });

        try
        {
            var result = await _authService.GoogleLoginAsync(dto);

            return Ok(new
            {
                token = result.token,
                user = new
                {
                    result.user.Id,
                    result.user.Name,
                    result.user.Email,
                    result.user.Avatar
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    
           
}
