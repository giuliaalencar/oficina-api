using Microsoft.AspNetCore.Mvc;
using Oficina.API.DTOs;
using Oficina.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace Oficina.API.Controllers
{
    [ApiController]
    [Route("api/ordens-servico")]
    [Authorize]
    public class OrdensServicoController : ControllerBase
    {
        private readonly OrdemServicoService _service;

        public OrdensServicoController(OrdemServicoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.ListarAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var os = await _service.BuscarPorIdAsync(id);

            if (os == null)
                return NotFound();

            return Ok(os);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CriarOrdemServicoDto dto)
        {
            var resultado = await _service.CriarAsync(dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok(resultado.OS);
        }

        [HttpPost("{id}/itens")]
        public async Task<IActionResult> AdicionarItem(int id, [FromBody] AdicionarItemOrdemServicoDto dto)
        {
            var resultado = await _service.AdicionarItemAsync(id, dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok();
        }

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