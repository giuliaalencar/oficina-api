using System.ComponentModel.DataAnnotations;

namespace Oficina.API.Models
{
    public class Usuario
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Senha { get; set; } = string.Empty;

        [Required]
        public string Perfil { get; set; } = "ADMIN";
    }
}