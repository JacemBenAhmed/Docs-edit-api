using Docs_edits.Data;
using Docs_edits.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }


    [HttpGet("/users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();
        if (users == null || users.Count == 0)
        {
            return Ok("No users found !");
        }

        return Ok(users);

    }

    }
