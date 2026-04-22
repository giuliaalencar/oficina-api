using Microsoft.AspNetCore.Mvc;
using Oficina.API.DTOs;
using Oficina.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace Oficina.API.Controllers
{
    [ApiController]
    [Route("api/veiculos")]
    [Authorize]
    public class VeiculosController : ControllerBase
    {
        private readonly VeiculoService _veiculoService;

        public VeiculosController(VeiculoService veiculoService)
        {
            _veiculoService = veiculoService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] Guid? clienteId)
        {
            var veiculos = await _veiculoService.ListarAsync(clienteId);
            return Ok(veiculos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var veiculo = await _veiculoService.BuscarPorIdAsync(id);

            if (veiculo == null)
                return NotFound();

            return Ok(veiculo);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CriarVeiculoDto dto)
        {
            var resultado = await _veiculoService.CriarAsync(dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return CreatedAtAction(nameof(GetById), new { id = resultado.Veiculo!.Id }, resultado.Veiculo);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] AtualizarVeiculoDto dto)
        {
            var resultado = await _veiculoService.AtualizarAsync(id, dto);

            if (!resultado.Sucesso)
            {
                if (resultado.Erro == "Veículo não encontrado.")
                    return NotFound(resultado.Erro);

                return BadRequest(resultado.Erro);
            }

            return Ok(resultado.Veiculo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var resultado = await _veiculoService.ExcluirAsync(id);

            if (!resultado.Sucesso)
                return NotFound(resultado.Erro);

            return NoContent();
        }
    }
}