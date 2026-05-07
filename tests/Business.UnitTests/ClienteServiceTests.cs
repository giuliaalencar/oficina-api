using Oficina.API.Business;
using Oficina.API.DTOs;
using Oficina.API.Models;

namespace Business.UnitTests
{
    public class ClienteServiceTests
    {
        [Fact]
        public async Task CriarAsync_DeveCadastrarCliente_QuandoDocumentoForValido()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);

            var resultado = await service.CriarAsync(new CriarClienteDto
            {
                Nome = "Maria Cliente",
                Email = "maria@teste.com",
                Telefone = "11999999999",
                CpfCnpj = "12345678901"
            });

            Assert.True(resultado.Sucesso);
            Assert.Null(resultado.Erro);
            Assert.NotNull(resultado.Cliente);
            Assert.Single(context.Clientes);
        }

        [Fact]
        public async Task CriarAsync_DeveRetornarErro_QuandoDocumentoForInvalido()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);

            var resultado = await service.CriarAsync(new CriarClienteDto
            {
                Nome = "Maria Cliente",
                Email = "maria@teste.com",
                Telefone = "11999999999",
                CpfCnpj = "123"
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains("CPF/CNPJ", resultado.Erro);
            Assert.Empty(context.Clientes);
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarClientesCadastrados()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);
            context.Clientes.Add(CriarCliente("Cliente 1", "cliente1@teste.com"));
            context.Clientes.Add(CriarCliente("Cliente 2", "cliente2@teste.com", "12345678901234"));
            await context.SaveChangesAsync();

            var clientes = await service.ListarAsync();

            Assert.Equal(2, clientes.Count);
        }

        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarCliente_QuandoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);
            var cliente = CriarCliente();
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var encontrado = await service.BuscarPorIdAsync(cliente.Id);

            Assert.NotNull(encontrado);
            Assert.Equal(cliente.Email, encontrado!.Email);
        }

        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarNull_QuandoNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);

            var encontrado = await service.BuscarPorIdAsync(Guid.NewGuid());

            Assert.Null(encontrado);
        }

        [Fact]
        public async Task AtualizarAsync_DeveAlterarDados_QuandoClienteExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);
            var cliente = CriarCliente("Nome Antigo", "antigo@teste.com");
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var resultado = await service.AtualizarAsync(cliente.Id, new AtualizarClienteDto
            {
                Nome = "Nome Novo",
                Email = "novo@teste.com",
                Telefone = "22222222222",
                CpfCnpj = "12345678901234"
            });

            Assert.True(resultado.Sucesso);
            Assert.Equal("Nome Novo", cliente.Nome);
            Assert.Equal("novo@teste.com", cliente.Email);
        }

        [Fact]
        public async Task AtualizarAsync_DeveRetornarErro_QuandoClienteNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);

            var resultado = await service.AtualizarAsync(Guid.NewGuid(), new AtualizarClienteDto
            {
                Nome = "Nome",
                Email = "email@teste.com",
                Telefone = "11111111111",
                CpfCnpj = "12345678901"
            });

            Assert.False(resultado.Sucesso);
            Assert.Equal("Cliente não encontrado.", resultado.Erro);
        }

        [Fact]
        public async Task AtualizarAsync_DeveRetornarErro_QuandoDocumentoForInvalido()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);
            var cliente = CriarCliente();
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var resultado = await service.AtualizarAsync(cliente.Id, new AtualizarClienteDto
            {
                Nome = "Nome",
                Email = "email@teste.com",
                Telefone = "11111111111",
                CpfCnpj = "123"
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains("CPF/CNPJ", resultado.Erro);
        }

        [Fact]
        public async Task ExcluirAsync_DeveRemoverCliente_QuandoClienteExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);
            var cliente = CriarCliente();
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var resultado = await service.ExcluirAsync(cliente.Id);

            Assert.True(resultado.Sucesso);
            Assert.Empty(context.Clientes);
        }

        [Fact]
        public async Task ExcluirAsync_DeveRetornarErro_QuandoClienteNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new ClienteService(context);

            var resultado = await service.ExcluirAsync(Guid.NewGuid());

            Assert.False(resultado.Sucesso);
            Assert.Equal("Cliente não encontrado.", resultado.Erro);
        }

        private static Cliente CriarCliente(
            string nome = "Cliente",
            string email = "cliente@teste.com",
            string cpfCnpj = "12345678901")
        {
            return new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = nome,
                Email = email,
                Telefone = "11999999999",
                CpfCnpj = cpfCnpj
            };
        }
    }
}
