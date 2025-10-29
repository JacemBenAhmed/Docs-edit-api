using Docs_edits.Models;
using Microsoft.EntityFrameworkCore; 
namespace Docs_edits.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }


    }
}
