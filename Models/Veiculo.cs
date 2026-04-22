using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oficina.API.Models
{
    public class Veiculo
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente? Cliente { get; set; }

        [Required]
        public string Placa { get; set; } = string.Empty;

        [Required]
        public string Marca { get; set; } = string.Empty;

        [Required]
        public string Modelo { get; set; } = string.Empty;

        [Required]
        public int Ano { get; set; }
    }
}