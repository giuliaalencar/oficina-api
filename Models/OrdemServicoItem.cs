using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oficina.API.Models
{
    public class OrdemServicoItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrdemServicoId { get; set; }

        [ForeignKey("OrdemServicoId")]
        public OrdemServico? OrdemServico { get; set; }

        [Required]
        public int ItemId { get; set; }

        [ForeignKey("ItemId")]
        public Item? Item { get; set; }

        [Required]
        public int Quantidade { get; set; }

        [Required]
        public decimal Valor { get; set; }

        public bool EstoqueReservado { get; set; } = false;
        public bool EstoqueBaixado { get; set; } = false;
    }
}