using Docs_edits.Data;
using Docs_edits.Models;

public interface IUserRepository
{
    Task<User> GetByEmailAsync(string email);
    Task<User> AddAsync(User user);
}
