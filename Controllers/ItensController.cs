using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oficina.API.Context;
using Oficina.API.DTOs;
using Oficina.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace Oficina.API.Controllers
{
    [ApiController]
    [Route("api/itens")]
    [Authorize(Roles = "ADMIN")]
    public class ItensController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItensController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var itens = await _context.Itens.ToListAsync();
            return Ok(itens);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Itens.FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CriarItemDto dto)
        {
            var item = new Item
            {
                Descricao = dto.Descricao,
                Valor = dto.Valor,
                Estoque = dto.Estoque,
                EstoqueReservado = 0,
                Tipo = dto.Tipo
            };

            _context.Itens.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] AtualizarItemDto dto)
        {
            var item = await _context.Itens.FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound();

            item.Descricao = dto.Descricao;
            item.Valor = dto.Valor;
            item.Estoque = dto.Estoque;
            item.Tipo = dto.Tipo;

            await _context.SaveChangesAsync();

            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Itens.FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound();

            _context.Itens.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}