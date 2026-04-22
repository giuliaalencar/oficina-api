using Microsoft.EntityFrameworkCore;
using Oficina.API.Context;
using Oficina.API.DTOs;
using Oficina.API.Models;
using Oficina.API.Utils;

namespace Oficina.API.Services
{
    public class VeiculoService
    {
        private readonly AppDbContext _context;

        public VeiculoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Veiculo>> ListarAsync(Guid? clienteId)
        {
            var query = _context.Veiculos.AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(v => v.ClienteId == clienteId.Value);

            return await query.ToListAsync();
        }

        public async Task<Veiculo?> BuscarPorIdAsync(Guid id)
        {
            return await _context.Veiculos.FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<(bool Sucesso, string? Erro, Veiculo? Veiculo)> CriarAsync(CriarVeiculoDto dto)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId);

            if (cliente == null)
                return (false, "Cliente não encontrado.", null);

            if (!ValidadorPlaca.EhValida(dto.Placa))
                return (false, "ERR_002 - Placa inválida.", null);

            var veiculo = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = dto.ClienteId,
                Placa = dto.Placa,
                Marca = dto.Marca,
                Modelo = dto.Modelo,
                Ano = dto.Ano
            };

            _context.Veiculos.Add(veiculo);
            await _context.SaveChangesAsync();

            return (true, null, veiculo);
        }

        public async Task<(bool Sucesso, string? Erro, Veiculo? Veiculo)> AtualizarAsync(Guid id, AtualizarVeiculoDto dto)
        {
            var veiculo = await _context.Veiculos.FirstOrDefaultAsync(v => v.Id == id);

            if (veiculo == null)
                return (false, "Veículo não encontrado.", null);

            if (!ValidadorPlaca.EhValida(dto.Placa))
                return (false, "ERR_002 - Placa inválida.", null);

            veiculo.Placa = dto.Placa;
            veiculo.Marca = dto.Marca;
            veiculo.Modelo = dto.Modelo;
            veiculo.Ano = dto.Ano;

            await _context.SaveChangesAsync();

            return (true, null, veiculo);
        }

        public async Task<(bool Sucesso, string? Erro)> ExcluirAsync(Guid id)
        {
            var veiculo = await _context.Veiculos.FirstOrDefaultAsync(v => v.Id == id);

            if (veiculo == null)
                return (false, "Veículo não encontrado.");

            _context.Veiculos.Remove(veiculo);
            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}