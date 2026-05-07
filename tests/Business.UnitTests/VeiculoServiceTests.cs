using Oficina.API.Business;
using Oficina.API.DTOs;
using Oficina.API.Models;

namespace Business.UnitTests
{
    public class VeiculoServiceTests
    {
        [Fact]
        public async Task CriarAsync_DeveCadastrarVeiculo_QuandoClienteEPlacaForemValidos()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);
            var cliente = CriarCliente();
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var resultado = await service.CriarAsync(new CriarVeiculoDto
            {
                ClienteId = cliente.Id,
                Placa = "ABC1D23",
                Marca = "Honda",
                Modelo = "Civic",
                Ano = 2024
            });

            Assert.True(resultado.Sucesso);
            Assert.Null(resultado.Erro);
            Assert.NotNull(resultado.Veiculo);
            Assert.Single(context.Veiculos);
        }

        [Fact]
        public async Task CriarAsync_DeveRetornarErro_QuandoClienteNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);

            var resultado = await service.CriarAsync(new CriarVeiculoDto
            {
                ClienteId = Guid.NewGuid(),
                Placa = "ABC1D23",
                Marca = "Honda",
                Modelo = "Civic",
                Ano = 2024
            });

            Assert.False(resultado.Sucesso);
            Assert.Equal("Cliente não encontrado.", resultado.Erro);
            Assert.Null(resultado.Veiculo);
        }

        [Fact]
        public async Task CriarAsync_DeveRetornarErro_QuandoPlacaForInvalida()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);
            var cliente = CriarCliente();
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var resultado = await service.CriarAsync(new CriarVeiculoDto
            {
                ClienteId = cliente.Id,
                Placa = "123",
                Marca = "Honda",
                Modelo = "Civic",
                Ano = 2024
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains("Placa", resultado.Erro);
            Assert.Empty(context.Veiculos);
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarTodosVeiculos_QuandoClienteIdNaoForInformado()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);
            var cliente1 = CriarCliente("Cliente 1", "cliente1@teste.com");
            var cliente2 = CriarCliente("Cliente 2", "cliente2@teste.com");
            context.Clientes.AddRange(cliente1, cliente2);
            context.Veiculos.AddRange(CriarVeiculo(cliente1.Id, "ABC1D23"), CriarVeiculo(cliente2.Id, "DEF2G34"));
            await context.SaveChangesAsync();

            var veiculos = await service.ListarAsync(null);

            Assert.Equal(2, veiculos.Count);
        }

        [Fact]
        public async Task ListarAsync_DeveFiltrarPorCliente_QuandoClienteIdForInformado()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);
            var cliente1 = CriarCliente("Cliente 1", "cliente1@teste.com");
            var cliente2 = CriarCliente("Cliente 2", "cliente2@teste.com");
            context.Clientes.AddRange(cliente1, cliente2);
            context.Veiculos.AddRange(CriarVeiculo(cliente1.Id, "ABC1D23"), CriarVeiculo(cliente2.Id, "DEF2G34"));
            await context.SaveChangesAsync();

            var veiculos = await service.ListarAsync(cliente1.Id);

            Assert.Single(veiculos);
            Assert.Equal(cliente1.Id, veiculos[0].ClienteId);
        }

        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarVeiculo_QuandoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);
            var cliente = CriarCliente();
            var veiculo = CriarVeiculo(cliente.Id, "ABC1D23");
            context.Clientes.Add(cliente);
            context.Veiculos.Add(veiculo);
            await context.SaveChangesAsync();

            var encontrado = await service.BuscarPorIdAsync(veiculo.Id);

            Assert.NotNull(encontrado);
            Assert.Equal("ABC1D23", encontrado!.Placa);
        }

        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarNull_QuandoNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);

            var encontrado = await service.BuscarPorIdAsync(Guid.NewGuid());

            Assert.Null(encontrado);
        }

        [Fact]
        public async Task AtualizarAsync_DeveAlterarVeiculo_QuandoDadosForemValidos()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);
            var cliente = CriarCliente();
            var veiculo = CriarVeiculo(cliente.Id, "ABC1D23");
            context.Clientes.Add(cliente);
            context.Veiculos.Add(veiculo);
            await context.SaveChangesAsync();

            var resultado = await service.AtualizarAsync(veiculo.Id, new AtualizarVeiculoDto
            {
                Placa = "XYZ9A88",
                Marca = "Toyota",
                Modelo = "Corolla",
                Ano = 2025
            });

            Assert.True(resultado.Sucesso);
            Assert.Equal("XYZ9A88", veiculo.Placa);
            Assert.Equal("Toyota", veiculo.Marca);
        }

        [Fact]
        public async Task AtualizarAsync_DeveRetornarErro_QuandoVeiculoNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);

            var resultado = await service.AtualizarAsync(Guid.NewGuid(), new AtualizarVeiculoDto
            {
                Placa = "ABC1D23",
                Marca = "Honda",
                Modelo = "Civic",
                Ano = 2024
            });

            Assert.False(resultado.Sucesso);
            Assert.Equal("Veículo não encontrado.", resultado.Erro);
        }

        [Fact]
        public async Task AtualizarAsync_DeveRetornarErro_QuandoPlacaForInvalida()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);
            var cliente = CriarCliente();
            var veiculo = CriarVeiculo(cliente.Id, "ABC1D23");
            context.Clientes.Add(cliente);
            context.Veiculos.Add(veiculo);
            await context.SaveChangesAsync();

            var resultado = await service.AtualizarAsync(veiculo.Id, new AtualizarVeiculoDto
            {
                Placa = "123",
                Marca = "Honda",
                Modelo = "Civic",
                Ano = 2024
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains("Placa", resultado.Erro);
        }

        [Fact]
        public async Task ExcluirAsync_DeveRemoverVeiculo_QuandoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);
            var cliente = CriarCliente();
            var veiculo = CriarVeiculo(cliente.Id, "ABC1D23");
            context.Clientes.Add(cliente);
            context.Veiculos.Add(veiculo);
            await context.SaveChangesAsync();

            var resultado = await service.ExcluirAsync(veiculo.Id);

            Assert.True(resultado.Sucesso);
            Assert.Empty(context.Veiculos);
        }

        [Fact]
        public async Task ExcluirAsync_DeveRetornarErro_QuandoVeiculoNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new VeiculoService(context);

            var resultado = await service.ExcluirAsync(Guid.NewGuid());

            Assert.False(resultado.Sucesso);
            Assert.Equal("Veículo não encontrado.", resultado.Erro);
        }

        private static Cliente CriarCliente(string nome = "Cliente", string email = "cliente@teste.com")
        {
            return new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = nome,
                Email = email,
                Telefone = "11999999999",
                CpfCnpj = "12345678901"
            };
        }

        private static Veiculo CriarVeiculo(Guid clienteId, string placa)
        {
            return new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                Placa = placa,
                Marca = "Honda",
                Modelo = "Civic",
                Ano = 2024
            };
        }
    }
}
