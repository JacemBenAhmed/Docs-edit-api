using System.ComponentModel.DataAnnotations;

namespace Docs_edits.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;
    }
}
