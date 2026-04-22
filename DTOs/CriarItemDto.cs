namespace Oficina.API.DTOs
{
    public class CriarItemDto
    {
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int Estoque { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }
}