using Oficina.API.Business;
using Oficina.API.DTOs;
using Oficina.API.Models;

namespace Business.UnitTests
{
    public class OrdemServicoServiceTests
    {
        [Fact]
        public async Task CriarAsync_DeveCriarOrdem_QuandoVeiculoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, _) = await CriarClienteEVeiculoAsync(context);

            var resultado = await service.CriarAsync(new CriarOrdemServicoDto
            {
                VeiculoId = veiculo.Id
            });

            Assert.True(resultado.Sucesso);
            Assert.NotNull(resultado.OS);
            Assert.Equal("Recebida", resultado.OS!.Status);
            Assert.Equal(0, resultado.OS.ValorTotal);
        }

        [Fact]
        public async Task CriarAsync_DeveRetornarErro_QuandoVeiculoNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);

            var resultado = await service.CriarAsync(new CriarOrdemServicoDto
            {
                VeiculoId = Guid.NewGuid()
            });

            Assert.False(resultado.Sucesso);
            Assert.Equal("Veículo não encontrado.", resultado.Erro);
            Assert.Null(resultado.OS);
        }

        [Fact]
        public async Task ListarAsync_DeveMapearOrdemComVeiculoClienteEItens()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (cliente, veiculo, item) = await CriarClienteVeiculoItemAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Recebida");

            ordem.Itens.Add(new OrdemServicoItem
            {
                OrdemServicoId = ordem.Id,
                ItemId = item.Id,
                Item = item,
                Quantidade = 2,
                Valor = item.Valor
            });
            await context.SaveChangesAsync();

            var ordens = await service.ListarAsync();

            Assert.Single(ordens);
            Assert.Equal(veiculo.Placa, ordens[0].Veiculo!.Placa);
            Assert.Equal(cliente.Nome, ordens[0].Veiculo!.NomeCliente);
            Assert.Single(ordens[0].Itens);
            Assert.Equal(item.Descricao, ordens[0].Itens[0].Descricao);
        }

        [Fact]
        public async Task ListarPorClienteAsync_DeveRetornarSomenteOrdensDoEmailInformado()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculoCliente1, _) = await CriarClienteEVeiculoAsync(context, "Cliente 1", "cliente1@teste.com", "ABC1D23");
            var (_, veiculoCliente2, _) = await CriarClienteEVeiculoAsync(context, "Cliente 2", "cliente2@teste.com", "DEF2G34");

            await CriarOrdemAsync(context, veiculoCliente1.Id, "Recebida");
            await CriarOrdemAsync(context, veiculoCliente2.Id, "Recebida");

            var ordens = await service.ListarPorClienteAsync("cliente1@teste.com");

            Assert.Single(ordens);
            Assert.Equal("cliente1@teste.com", ordens[0].Veiculo!.EmailCliente);
        }

        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarNull_QuandoNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);

            var ordem = await service.BuscarPorIdAsync(999);

            Assert.Null(ordem);
        }

        [Fact]
        public async Task AdicionarItemAsync_DeveAdicionarItemEAtualizarValorTotal()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, item) = await CriarClienteVeiculoItemAsync(context, valorItem: 50);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Recebida");

            var resultado = await service.AdicionarItemAsync(ordem.Id, new AdicionarItemOrdemServicoDto
            {
                ItemId = item.Id,
                Quantidade = 3
            });

            Assert.True(resultado.Sucesso);
            Assert.Single(ordem.Itens);
            Assert.Equal(150, ordem.ValorTotal);
        }

        [Fact]
        public async Task AdicionarItemAsync_DeveRetornarErro_QuandoOrdemNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);

            var resultado = await service.AdicionarItemAsync(999, new AdicionarItemOrdemServicoDto
            {
                ItemId = 1,
                Quantidade = 1
            });

            Assert.False(resultado.Sucesso);
            Assert.Equal("OS não encontrada.", resultado.Erro);
        }

        [Fact]
        public async Task AdicionarItemAsync_DeveRetornarErro_QuandoStatusNaoForRecebida()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, item) = await CriarClienteVeiculoItemAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Em Diagnóstico");

            var resultado = await service.AdicionarItemAsync(ordem.Id, new AdicionarItemOrdemServicoDto
            {
                ItemId = item.Id,
                Quantidade = 1
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains("status", resultado.Erro);
        }

        [Fact]
        public async Task AdicionarItemAsync_DeveRetornarErro_QuandoItemNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, _) = await CriarClienteEVeiculoAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Recebida");

            var resultado = await service.AdicionarItemAsync(ordem.Id, new AdicionarItemOrdemServicoDto
            {
                ItemId = 999,
                Quantidade = 1
            });

            Assert.False(resultado.Sucesso);
            Assert.Equal("Item não encontrado.", resultado.Erro);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveAvancarStatus_QuandoFluxoForValido()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, _) = await CriarClienteEVeiculoAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Recebida");

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Em Diagnóstico"
            });

            Assert.True(resultado.Sucesso);
            Assert.Equal("Em Diagnóstico", ordem.Status);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveRetornarErro_QuandoFluxoForInvalido()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, _) = await CriarClienteEVeiculoAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Recebida");

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Finalizada"
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains("pular", resultado.Erro);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveReservarEstoque_QuandoStatusForAguardandoAprovacao()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, item) = await CriarClienteVeiculoItemAsync(context, estoque: 10);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Em Diagnóstico");
            await AdicionarItemNaOrdemAsync(context, ordem, item, 4);

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Aguardando Aprovação"
            });

            Assert.True(resultado.Sucesso);
            Assert.Equal("Aguardando Aprovação", ordem.Status);
            Assert.Equal(4, item.EstoqueReservado);
            Assert.All(ordem.Itens, i => Assert.True(i.EstoqueReservado));
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveRetornarErro_QuandoEstoqueForInsuficiente()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, item) = await CriarClienteVeiculoItemAsync(context, estoque: 2);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Em Diagnóstico");
            await AdicionarItemNaOrdemAsync(context, ordem, item, 4);

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Aguardando Aprovação"
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains("Sem estoque", resultado.Erro);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveBaixarEstoque_QuandoStatusForEmExecucao()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, item) = await CriarClienteVeiculoItemAsync(context, estoque: 10, estoqueReservado: 4);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Aguardando Aprovação");
            await AdicionarItemNaOrdemAsync(context, ordem, item, 4, reservado: true);

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Em Execução"
            });

            Assert.True(resultado.Sucesso);
            Assert.Equal("Em Execução", ordem.Status);
            Assert.Equal(6, item.Estoque);
            Assert.Equal(0, item.EstoqueReservado);
            Assert.All(ordem.Itens, i => Assert.True(i.EstoqueBaixado));
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveRetornarErro_QuandoEstoqueReservadoForInsuficiente()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, item) = await CriarClienteVeiculoItemAsync(context, estoque: 10, estoqueReservado: 1);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Aguardando Aprovação");
            await AdicionarItemNaOrdemAsync(context, ordem, item, 4, reservado: true);

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Em Execução"
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains("reservado insuficiente", resultado.Erro);
        }

        private static async Task<(Cliente Cliente, Veiculo Veiculo, Item? Item)> CriarClienteEVeiculoAsync(
            Oficina.API.DAL.AppDbContext context,
            string nome = "Cliente",
            string email = "cliente@teste.com",
            string placa = "ABC1D23")
        {
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = nome,
                Email = email,
                Telefone = "11999999999",
                CpfCnpj = "12345678901"
            };

            var veiculo = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = cliente.Id,
                Cliente = cliente,
                Placa = placa,
                Marca = "Honda",
                Modelo = "Civic",
                Ano = 2024
            };

            context.Clientes.Add(cliente);
            context.Veiculos.Add(veiculo);
            await context.SaveChangesAsync();

            return (cliente, veiculo, null);
        }

        private static async Task<(Cliente Cliente, Veiculo Veiculo, Item Item)> CriarClienteVeiculoItemAsync(
            Oficina.API.DAL.AppDbContext context,
            decimal valorItem = 100,
            int estoque = 10,
            int estoqueReservado = 0)
        {
            var (cliente, veiculo, _) = await CriarClienteEVeiculoAsync(context);
            var item = new Item
            {
                Descricao = "Filtro de óleo",
                Tipo = "Peca",
                Valor = valorItem,
                Estoque = estoque,
                EstoqueReservado = estoqueReservado
            };

            context.Itens.Add(item);
            await context.SaveChangesAsync();

            return (cliente, veiculo, item);
        }

        private static async Task<OrdemServico> CriarOrdemAsync(Oficina.API.DAL.AppDbContext context, Guid veiculoId, string status)
        {
            var ordem = new OrdemServico
            {
                VeiculoId = veiculoId,
                DataEntrada = DateTime.Now,
                Status = status,
                ValorTotal = 0,
                Itens = new List<OrdemServicoItem>()
            };

            context.OrdensServico.Add(ordem);
            await context.SaveChangesAsync();
            return ordem;
        }

        private static async Task AdicionarItemNaOrdemAsync(
            Oficina.API.DAL.AppDbContext context,
            OrdemServico ordem,
            Item item,
            int quantidade,
            bool reservado = false)
        {
            ordem.Itens.Add(new OrdemServicoItem
            {
                OrdemServicoId = ordem.Id,
                ItemId = item.Id,
                Item = item,
                Quantidade = quantidade,
                Valor = item.Valor,
                EstoqueReservado = reservado
            });

            ordem.ValorTotal = ordem.Itens.Sum(i => i.Quantidade * i.Valor);
            await context.SaveChangesAsync();
        }
    }
}
