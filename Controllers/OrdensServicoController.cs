using Microsoft.AspNetCore.Mvc;
using Oficina.API.DTOs;
using Oficina.API.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Oficina.API.Context;



namespace Oficina.API.Controllers
{
    [ApiController]
    [Route("api/ordens-servico")]
    [Authorize]
    public class OrdensServicoController : ControllerBase
{
    private readonly OrdemServicoService _service;
    private readonly AppDbContext _context;

    public OrdensServicoController(OrdemServicoService service, AppDbContext context)
    {
        _service = service;
        _context = context;
    }


        [HttpGet]
public async Task<IActionResult> Get()
{
    if (User.IsInRole("CLIENTE"))
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        var ordensCliente = await _context.OrdensServico
            .Include(o => o.Itens)
            .Include(o => o.Veiculo)
            .ThenInclude(v => v!.Cliente)
            .Where(o => o.Veiculo != null &&
                        o.Veiculo.Cliente != null &&
                        o.Veiculo.Cliente.Email == email)
            .ToListAsync();

        return Ok(ordensCliente);
    }

    return Ok(await _service.ListarAsync());
}

[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    var os = await _service.BuscarPorIdAsync(id);

    if (os == null)
        return NotFound();

    if (User.IsInRole("CLIENTE"))
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        var pertenceAoCliente = await _context.OrdensServico
            .Include(o => o.Veiculo)
            .ThenInclude(v => v!.Cliente)
            .AnyAsync(o => o.Id == id &&
                           o.Veiculo != null &&
                           o.Veiculo.Cliente != null &&
                           o.Veiculo.Cliente.Email == email);

        if (!pertenceAoCliente)
            return Forbid("ERR_005 - Não autorizado.");
    }

    return Ok(os);
}

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CriarOrdemServicoDto dto)
        {
            var resultado = await _service.CriarAsync(dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok(resultado.OS);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id}/itens")]
        public async Task<IActionResult> AdicionarItem(int id, [FromBody] AdicionarItemOrdemServicoDto dto)
        {
            var resultado = await _service.AdicionarItemAsync(id, dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok();
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> AtualizarStatus(int id, [FromBody] AtualizarStatusOrdemServicoDto dto)
        {
            var resultado = await _service.AtualizarStatusAsync(id, dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok();
        }
    }
}