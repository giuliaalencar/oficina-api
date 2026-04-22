namespace Oficina.API.DTOs
{
    public class CriarVeiculoDto
    {
        public Guid ClienteId { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Ano { get; set; }
    }
}