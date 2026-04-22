using System.ComponentModel.DataAnnotations;

namespace Oficina.API.Models
{
    public class Cliente
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Telefone { get; set; } = string.Empty;

        [Required]
        public string CpfCnpj { get; set; } = string.Empty;
    }
}