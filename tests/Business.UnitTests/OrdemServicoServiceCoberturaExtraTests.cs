using Oficina.API.Business;
using Oficina.API.DTOs;
using Oficina.API.Models;

namespace Business.UnitTests
{
    public class OrdemServicoServiceCoberturaExtraTests
    {
        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarOrdemMapeada_QuandoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (cliente, veiculo, item) = await CriarClienteVeiculoItemAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Recebida");
            await AdicionarItemNaOrdemAsync(context, ordem, item, 2);

            var encontrada = await service.BuscarPorIdAsync(ordem.Id);

            Assert.NotNull(encontrada);
            Assert.Equal(ordem.Id, encontrada!.Id);
            Assert.Equal(cliente.Nome, encontrada.Veiculo!.NomeCliente);
            Assert.Single(encontrada.Itens);
        }
[Fact]
        public async Task AtualizarStatusAsync_DeveRetornarErro_QuandoOrdemNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);

            var resultado = await service.AtualizarStatusAsync(999, new AtualizarStatusOrdemServicoDto
            {
                Status = "Em Diagnóstico"
            });

            Assert.False(resultado.Sucesso);
            Assert.Equal("OS não encontrada.", resultado.Erro);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveFinalizarOrdem_QuandoFluxoForEmExecucaoParaFinalizada()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, _) = await CriarClienteEVeiculoAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Em Execução");

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Finalizada"
            });

            Assert.True(resultado.Sucesso);
            Assert.Equal("Finalizada", ordem.Status);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveEntregarOrdem_QuandoFluxoForFinalizadaParaEntregue()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, _) = await CriarClienteEVeiculoAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Finalizada");

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Entregue"
            });

            Assert.True(resultado.Sucesso);
            Assert.Equal("Entregue", ordem.Status);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveIgnorarEstoque_QuandoItemForServicoNaAprovacao()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, item) = await CriarClienteVeiculoItemAsync(context, tipo: "Servico", estoque: 0);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Em Diagnóstico");
            await AdicionarItemNaOrdemAsync(context, ordem, item, 3);

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Aguardando Aprovação"
            });

            Assert.True(resultado.Sucesso);
            Assert.Equal(0, item.EstoqueReservado);
            Assert.All(ordem.Itens, itemOs => Assert.True(itemOs.EstoqueReservado));
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveIgnorarEstoque_QuandoItemForServicoNaExecucao()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, item) = await CriarClienteVeiculoItemAsync(context, tipo: "Servico", estoque: 0);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Aguardando Aprovação");
            await AdicionarItemNaOrdemAsync(context, ordem, item, 3, reservado: true);

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Em Execução"
            });

            Assert.True(resultado.Sucesso);
            Assert.Equal(0, item.Estoque);
            Assert.All(ordem.Itens, itemOs => Assert.True(itemOs.EstoqueBaixado));
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveRetornarErroNaAprovacao_QuandoItemNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, _) = await CriarClienteEVeiculoAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Em Diagnóstico");

            ordem.Itens.Add(new OrdemServicoItem
            {
                OrdemServicoId = ordem.Id,
                ItemId = 999,
                Quantidade = 1,
                Valor = 10
            });
            await context.SaveChangesAsync();

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Aguardando Aprovação"
            });

            Assert.False(resultado.Sucesso);
            Assert.Equal("Item não encontrado.", resultado.Erro);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveRetornarErroNaExecucao_QuandoItemNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new OrdemServicoService(context);
            var (_, veiculo, _) = await CriarClienteEVeiculoAsync(context);
            var ordem = await CriarOrdemAsync(context, veiculo.Id, "Aguardando Aprovação");

            ordem.Itens.Add(new OrdemServicoItem
            {
                OrdemServicoId = ordem.Id,
                ItemId = 999,
                Quantidade = 1,
                Valor = 10,
                EstoqueReservado = true
            });
            await context.SaveChangesAsync();

            var resultado = await service.AtualizarStatusAsync(ordem.Id, new AtualizarStatusOrdemServicoDto
            {
                Status = "Em Execução"
            });

            Assert.False(resultado.Sucesso);
            Assert.Equal("Item não encontrado.", resultado.Erro);
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
            int estoqueReservado = 0,
            string tipo = "Peca")
        {
            var (cliente, veiculo, _) = await CriarClienteEVeiculoAsync(context);
            var item = new Item
            {
                Descricao = "Filtro de oleo",
                Tipo = tipo,
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

