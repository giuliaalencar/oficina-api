using Oficina.API.Models;

namespace DAL.UnitTests
{
    public class AppDbContextTests
    {
        [Fact]
        public async Task AppDbContext_DeveSalvarEConsultarCliente()
        {
            using var context = TestHelpers.CriarContexto();

            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Cliente Teste",
                Email = "cliente@teste.com",
                Telefone = "11999999999",
                CpfCnpj = "12345678901"
            };

            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var clienteSalvo = context.Clientes.FirstOrDefault(c => c.Email == "cliente@teste.com");

            Assert.NotNull(clienteSalvo);
            Assert.Equal("Cliente Teste", clienteSalvo!.Nome);
            Assert.Equal("12345678901", clienteSalvo.CpfCnpj);
        }

        [Fact]
        public async Task AppDbContext_DeveSalvarEConsultarUsuario()
        {
            using var context = TestHelpers.CriarContexto();

            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = "Admin Teste",
                Email = "admin@teste.com",
                Senha = "123456",
                Perfil = "ADMIN"
            };

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var usuarioSalvo = context.Usuarios.FirstOrDefault(u => u.Email == "admin@teste.com");

            Assert.NotNull(usuarioSalvo);
            Assert.Equal("Admin Teste", usuarioSalvo!.Nome);
            Assert.Equal("ADMIN", usuarioSalvo.Perfil);
        }
    }
}

