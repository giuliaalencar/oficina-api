using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Oficina.API.Models
{
    public class OrdemServico
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid VeiculoId { get; set; }

        [ForeignKey("VeiculoId")]
        public Veiculo? Veiculo { get; set; }

        [Required]
        public DateTime DataEntrada { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        [Required]
        [Precision(18, 2)]
        public decimal ValorTotal { get; set; }

        public List<OrdemServicoItem> Itens { get; set; } = new();
    }
}