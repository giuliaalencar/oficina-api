using Microsoft.EntityFrameworkCore;
using Oficina.API.Models;

namespace Oficina.API.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Veiculo> Veiculos { get; set; }
        public DbSet<Item> Itens { get; set; }
        public DbSet<OrdemServico> OrdensServico { get; set; }
        public DbSet<OrdemServicoItem> OrdensServicoItens { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
    }
}