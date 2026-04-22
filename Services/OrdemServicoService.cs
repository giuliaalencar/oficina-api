using Microsoft.EntityFrameworkCore;
using Oficina.API.Context;
using Oficina.API.DTOs;
using Oficina.API.Models;

namespace Oficina.API.Services
{
    public class OrdemServicoService
    {
        private readonly AppDbContext _context;

        public OrdemServicoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrdemServico>> ListarAsync()
        {
            return await _context.OrdensServico
                .Include(o => o.Itens)
                .ToListAsync();
        }

        public async Task<OrdemServico?> BuscarPorIdAsync(int id)
        {
            return await _context.OrdensServico
                .Include(o => o.Itens)
                .FirstOrDefaultAsync(o => o.Id == id);
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

            var fluxoValido = new Dictionary<string, string>
            {
                { "Recebida", "Em Diagnóstico" },
                { "Em Diagnóstico", "Aguardando Aprovação" },
                { "Aguardando Aprovação", "Em Execução" },
                { "Em Execução", "Finalizada" },
                { "Finalizada", "Entregue" }
            };

            if (!fluxoValido.ContainsKey(os.Status) || fluxoValido[os.Status] != dto.Status)
                return (false, "ERR_004 - Não é permitido pular ou voltar status.");

            if (dto.Status == "Aguardando Aprovação")
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

                    if (item.Tipo == "Peca")
                    {
                        var estoqueDisponivel = item.Estoque - item.EstoqueReservado;

                        if (estoqueDisponivel < grupo.QuantidadeTotal)
                            return (false, "ERR_003 - Sem estoque.");

                        item.EstoqueReservado += grupo.QuantidadeTotal;
                    }
                }

                foreach (var itemOs in os.Itens)
                    itemOs.EstoqueReservado = true;
            }

            if (dto.Status == "Em Execução")
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

                    if (item.Tipo == "Peca")
                    {
                        if (item.EstoqueReservado < grupo.QuantidadeTotal)
                            return (false, "ERR_003 - Estoque reservado insuficiente.");

                        item.Estoque -= grupo.QuantidadeTotal;
                        item.EstoqueReservado -= grupo.QuantidadeTotal;
                    }
                }

                foreach (var itemOs in os.Itens)
                    itemOs.EstoqueBaixado = true;
            }

            os.Status = dto.Status;

            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}