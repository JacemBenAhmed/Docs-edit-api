using Docs_edits.DTOs;
using Docs_edits.Data;
using Docs_edits.Models;

public interface IAuthService
{
    Task<(string token, User user)> GoogleLoginAsync(GoogleLoginDto dto);
}
