using Microsoft.EntityFrameworkCore.Migrations;
using Oficina.API.Models;

namespace Oficina.API.Data
{
    public static class MemoriaBanco
    {
        public static List<Cliente> Clientes { get; } = new();
        public static List<Veiculo> Veiculos { get; } = new();
        public static List<Item> Itens { get; } = new();
        public static List<OrdemServico> OrdensServico { get; } = new();
    }
}