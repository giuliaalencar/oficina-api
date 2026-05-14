using Microsoft.EntityFrameworkCore;
using Oficina.API.DAL;
using Oficina.API.DTOs;
using Oficina.API.Models;

namespace Oficina.API.Business
{
    public class OrdemServicoService
    {
        private readonly AppDbContext _context;
        private readonly EstoqueEmailService? _estoqueEmailService;

        public OrdemServicoService(AppDbContext context, EstoqueEmailService? estoqueEmailService = null)
        {
            _context = context;
            _estoqueEmailService = estoqueEmailService;
        }

        public async Task<List<OrdemServicoDto>> ListarAsync()
        {
            var ordens = await _context.OrdensServico
                .AsNoTracking()
                .Include(o => o.Veiculo)
                .ThenInclude(v => v!.Cliente)
                .Include(o => o.Itens)
                .ThenInclude(i => i.Item)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return ordens.Select(MapearOrdem).ToList();
        }

        public async Task<List<OrdemServicoDto>> ListarPorClienteAsync(string email)
        {
            var ordens = await _context.OrdensServico
                .AsNoTracking()
                .Include(o => o.Veiculo)
                .ThenInclude(v => v!.Cliente)
                .Include(o => o.Itens)
                .ThenInclude(i => i.Item)
                .Where(o => o.Veiculo != null &&
                            o.Veiculo.Cliente != null &&
                            o.Veiculo.Cliente.Email == email)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return ordens.Select(MapearOrdem).ToList();
        }

        public async Task<OrdemServicoDto?> BuscarPorIdAsync(int id)
        {
            var ordem = await _context.OrdensServico
                .AsNoTracking()
                .Include(o => o.Veiculo)
                .ThenInclude(v => v!.Cliente)
                .Include(o => o.Itens)
                .ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ordem == null)
                return null;

            return MapearOrdem(ordem);
        }

        public async Task<(bool Sucesso, string? Erro, OrdemServico? OS)> CriarAsync(CriarOrdemServicoDto dto)
        {
            var veiculo = await _context.Veiculos.FirstOrDefaultAsync(v => v.Id == dto.VeiculoId);

            if (veiculo == null)
                return (false, "Veículo não encontrado.", null);

            var os = new OrdemServico
            {
                VeiculoId = dto.VeiculoId,
                DataEntrada = DateTime.Now,
                Status = "Recebida",
                ValorTotal = 0,
                Itens = new List<OrdemServicoItem>()
            };

            _context.OrdensServico.Add(os);
            await _context.SaveChangesAsync();

            return (true, null, os);
        }

        public async Task<(bool Sucesso, string? Erro)> AdicionarItemAsync(int osId, AdicionarItemOrdemServicoDto dto)
        {
            var os = await _context.OrdensServico
                .Include(o => o.Itens)
                .FirstOrDefaultAsync(o => o.Id == osId);

            if (os == null)
                return (false, "OS não encontrada.");

            if (os.Status != "Recebida")
                return (false, "ERR_004 - Não é permitido adicionar itens neste status.");

            var item = await _context.Itens.FirstOrDefaultAsync(i => i.Id == dto.ItemId);

            if (item == null)
                return (false, "Item não encontrado.");

            var osItem = new OrdemServicoItem
            {
                OrdemServicoId = os.Id,
                ItemId = item.Id,
                Quantidade = dto.Quantidade,
                Valor = item.Valor,
                EstoqueReservado = false,
                EstoqueBaixado = false
            };

            os.Itens.Add(osItem);
            os.ValorTotal = os.Itens.Sum(i => i.Quantidade * i.Valor);

            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> AtualizarStatusAsync(int id, AtualizarStatusOrdemServicoDto dto)
        {
            var os = await _context.OrdensServico
                .Include(o => o.Itens)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (os == null)
                return (false, "OS não encontrada.");

            var statusAtual = NormalizarStatus(os.Status);
            var novoStatus = NormalizarStatus(dto.Status);

            if (statusAtual == null || novoStatus == null)
                return (false, "ERR_004 - Status inválido.");

            var proximoStatus = ObterProximoStatus(statusAtual);

            if (proximoStatus != novoStatus)
                return (false, "ERR_004 - Não é permitido pular ou voltar status.");

            if (novoStatus == "Aguardando Aprovação")
            {
                var itensAgrupados = os.Itens
                    .GroupBy(i => i.ItemId)
                    .Select(g => new
                    {
                        ItemId = g.Key,
                        QuantidadeTotal = g.Sum(x => x.Quantidade)
                    });

                foreach (var grupo in itensAgrupados)
                {
                    var item = await _context.Itens.FirstOrDefaultAsync(i => i.Id == grupo.ItemId);

                    if (item == null)
                        return (false, "Item não encontrado.");

                    if (EhPeca(item.Tipo))
                    {
                        var estoqueDisponivel = item.Estoque - item.EstoqueReservado;

                        if (estoqueDisponivel < grupo.QuantidadeTotal)
                        {
                            await NotificarEstoqueBaixoAsync($"Tentativa de reservar estoque sem quantidade suficiente na OS #{os.Id}");
                            return (false, "ERR_003 - Sem estoque.");
                        }

                        item.EstoqueReservado += grupo.QuantidadeTotal;
                    }
                }

                foreach (var itemOs in os.Itens)
                    itemOs.EstoqueReservado = true;
            }

            if (novoStatus == "Em Execução")
            {
                var itensAgrupados = os.Itens
                    .GroupBy(i => i.ItemId)
                    .Select(g => new
                    {
                        ItemId = g.Key,
                        QuantidadeTotal = g.Sum(x => x.Quantidade)
                    });

                foreach (var grupo in itensAgrupados)
                {
                    var item = await _context.Itens.FirstOrDefaultAsync(i => i.Id == grupo.ItemId);

                    if (item == null)
                        return (false, "Item não encontrado.");

                    if (EhPeca(item.Tipo))
                    {
                        if (item.EstoqueReservado < grupo.QuantidadeTotal)
                        {
                            await NotificarEstoqueBaixoAsync($"Tentativa de baixar estoque sem reserva suficiente na OS #{os.Id}");
                            return (false, "ERR_003 - Estoque reservado insuficiente.");
                        }

                        item.Estoque -= grupo.QuantidadeTotal;
                        item.EstoqueReservado -= grupo.QuantidadeTotal;
                    }
                }

                foreach (var itemOs in os.Itens)
                    itemOs.EstoqueBaixado = true;
            }

            os.Status = novoStatus;

            await _context.SaveChangesAsync();

            if (novoStatus == "Em Execução" && _estoqueEmailService != null)
            {
                await NotificarEstoqueBaixoAsync($"Baixa de estoque realizada pela OS #{os.Id}");
            }

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> AvancarStatusAsync(int id)
        {
            var os = await _context.OrdensServico
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (os == null)
                return (false, "OS não encontrada.");

            var statusAtual = NormalizarStatus(os.Status);

            if (statusAtual == null)
                return (false, "ERR_004 - Status inválido.");

            var proximoStatus = ObterProximoStatus(statusAtual);

            if (proximoStatus == null)
                return (false, "ERR_004 - Não existe próximo status para esta ordem.");

            return await AtualizarStatusAsync(id, new AtualizarStatusOrdemServicoDto
            {
                Status = proximoStatus
            });
        }

        private static string? NormalizarStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            var valor = status.Trim();

            var statusNormalizados = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Recebida", "Recebida" },
                { "Em Diagnóstico", "Em Diagnóstico" },
                { "Em Diagnostico", "Em Diagnóstico" },
                { "Em DiagnÃ³stico", "Em Diagnóstico" },
                { "Aguardando Aprovação", "Aguardando Aprovação" },
                { "Aguardando Aprovacao", "Aguardando Aprovação" },
                { "Aguardando AprovaÃ§Ã£o", "Aguardando Aprovação" },
                { "Em Execução", "Em Execução" },
                { "Em Execucao", "Em Execução" },
                { "Em ExecuÃ§Ã£o", "Em Execução" },
                { "Finalizada", "Finalizada" },
                { "Entregue", "Entregue" }
            };

            return statusNormalizados.TryGetValue(valor, out var statusNormalizado)
                ? statusNormalizado
                : null;
        }

        private static string? ObterProximoStatus(string statusAtual)
        {
            return statusAtual switch
            {
                "Recebida" => "Em Diagnóstico",
                "Em Diagnóstico" => "Aguardando Aprovação",
                "Aguardando Aprovação" => "Em Execução",
                "Em Execução" => "Finalizada",
                "Finalizada" => "Entregue",
                _ => null
            };
        }

        private static bool EhPeca(string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return false;

            var valor = tipo.Trim();

            return valor.Equals("Peca", StringComparison.OrdinalIgnoreCase) ||
                   valor.Equals("Pe\u00e7a", StringComparison.OrdinalIgnoreCase) ||
                   valor.Equals("Pe\u00c3\u00a7a", StringComparison.OrdinalIgnoreCase);
        }

        private async Task NotificarEstoqueBaixoAsync(string motivo)
        {
            if (_estoqueEmailService != null)
            {
                await _estoqueEmailService.NotificarItensComBaixoEstoqueAsync(motivo);
            }
        }

        private static OrdemServicoDto MapearOrdem(OrdemServico ordem)
        {
            return new OrdemServicoDto
            {
                Id = ordem.Id,
                VeiculoId = ordem.VeiculoId,
                DataEntrada = ordem.DataEntrada,
                Status = ordem.Status,
                ValorTotal = ordem.ValorTotal,
                Veiculo = ordem.Veiculo == null ? null : new OrdemServicoVeiculoDto
                {
                    Id = ordem.Veiculo.Id,
                    Placa = ordem.Veiculo.Placa,
                    Marca = ordem.Veiculo.Marca,
                    Modelo = ordem.Veiculo.Modelo,
                    Ano = ordem.Veiculo.Ano,
                    NomeCliente = ordem.Veiculo.Cliente?.Nome,
                    EmailCliente = ordem.Veiculo.Cliente?.Email
                },
                Itens = ordem.Itens.Select(item => new OrdemServicoItemDto
                {
                    Id = item.Id,
                    ItemId = item.ItemId,
                    Descricao = item.Item?.Descricao ?? string.Empty,
                    Tipo = item.Item?.Tipo ?? string.Empty,
                    Quantidade = item.Quantidade,
                    Valor = item.Valor,
                    EstoqueReservado = item.EstoqueReservado,
                    EstoqueBaixado = item.EstoqueBaixado
                }).ToList()
            };
        }
    }
}

