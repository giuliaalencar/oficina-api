using Microsoft.EntityFrameworkCore;
using Oficina.API.Context;
using Oficina.API.DTOs;
using Oficina.API.Models;
using Oficina.API.Utils;

namespace Oficina.API.Services
{
    public class ClienteService
    {
        private readonly AppDbContext _context;

        public ClienteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cliente>> ListarAsync()
        {
            return await _context.Clientes.ToListAsync();
        }

        public async Task<Cliente?> BuscarPorIdAsync(Guid id)
        {
            return await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<(bool Sucesso, string? Erro, Cliente? Cliente)> CriarAsync(CriarClienteDto dto)
        {
            if (!ValidadorDocumento.EhValido(dto.CpfCnpj))
                return (false, "ERR_001 - CPF/CNPJ inválido.", null);

            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = dto.Nome,
                Email = dto.Email,
                Telefone = dto.Telefone,
                CpfCnpj = dto.CpfCnpj
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return (true, null, cliente);
        }

        public async Task<(bool Sucesso, string? Erro, Cliente? Cliente)> AtualizarAsync(Guid id, AtualizarClienteDto dto)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
                return (false, "Cliente não encontrado.", null);

            if (!ValidadorDocumento.EhValido(dto.CpfCnpj))
                return (false, "ERR_001 - CPF/CNPJ inválido.", null);

            cliente.Nome = dto.Nome;
            cliente.Email = dto.Email;
            cliente.Telefone = dto.Telefone;
            cliente.CpfCnpj = dto.CpfCnpj;

            await _context.SaveChangesAsync();

            return (true, null, cliente);
        }

        public async Task<(bool Sucesso, string? Erro)> ExcluirAsync(Guid id)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
                return (false, "Cliente não encontrado.");

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}