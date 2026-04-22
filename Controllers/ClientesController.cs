using Microsoft.AspNetCore.Mvc;
using Oficina.API.DTOs;
using Oficina.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace Oficina.API.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly ClienteService _clienteService;

        public ClientesController(ClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var clientes = await _clienteService.ListarAsync();
            return Ok(clientes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var cliente = await _clienteService.BuscarPorIdAsync(id);

            if (cliente == null)
                return NotFound();

            return Ok(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CriarClienteDto dto)
        {
            var resultado = await _clienteService.CriarAsync(dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return CreatedAtAction(nameof(GetById), new { id = resultado.Cliente!.Id }, resultado.Cliente);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] AtualizarClienteDto dto)
        {
            var resultado = await _clienteService.AtualizarAsync(id, dto);

            if (!resultado.Sucesso)
            {
                if (resultado.Erro == "Cliente não encontrado.")
                    return NotFound(resultado.Erro);

                return BadRequest(resultado.Erro);
            }

            return Ok(resultado.Cliente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var resultado = await _clienteService.ExcluirAsync(id);

            if (!resultado.Sucesso)
                return NotFound(resultado.Erro);

            return NoContent();
        }
    }
}