using System.ComponentModel.DataAnnotations;

namespace Oficina.API.Models
{
    public class Item
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Descricao { get; set; } = string.Empty;

        [Required]
        public decimal Valor { get; set; }

        [Required]
        public int Estoque { get; set; }

        public int EstoqueReservado { get; set; } = 0;

        [Required]
        public string Tipo { get; set; } = string.Empty;
    }
}