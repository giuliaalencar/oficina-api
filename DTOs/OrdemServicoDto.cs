namespace Oficina.API.DTOs
{
    public class OrdemServicoDto
    {
        public int Id { get; set; }

        public Guid VeiculoId { get; set; }

        public DateTime DataEntrada { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal ValorTotal { get; set; }

        public OrdemServicoVeiculoDto? Veiculo { get; set; }

        public List<OrdemServicoItemDto> Itens { get; set; } = new();
    }

    public class OrdemServicoVeiculoDto
    {
        public Guid Id { get; set; }

        public string Placa { get; set; } = string.Empty;

        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public int Ano { get; set; }

        public string? NomeCliente { get; set; }

        public string? EmailCliente { get; set; }
    }

    public class OrdemServicoItemDto
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        public string Descricao { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public int Quantidade { get; set; }

        public decimal Valor { get; set; }

        public bool EstoqueReservado { get; set; }

        public bool EstoqueBaixado { get; set; }
    }
}
