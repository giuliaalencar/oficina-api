using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oficina.API.DAL;
using Oficina.API.DTOs;
using Oficina.API.Business;
using System.Security.Claims;

namespace Oficina.API.Controllers
{
    [ApiController]
    [Route("api/ordens-servico")]
    [Authorize]
    public class OrdensServicoController : ControllerBase
    {
        private readonly OrdemServicoService _service;
        private readonly OrcamentoPdfService _orcamentoPdfService;
        private readonly AppDbContext _context;

        public OrdensServicoController(
            OrdemServicoService service,
            OrcamentoPdfService orcamentoPdfService,
            AppDbContext context)
        {
            _service = service;
            _orcamentoPdfService = orcamentoPdfService;
            _context = context;
        }

        [Authorize(Roles = "ADMIN,FUNCIONARIO,CLIENTE")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (User.IsInRole("CLIENTE"))
            {
                var email = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrWhiteSpace(email))
                    return Unauthorized();

                var ordensCliente = await _service.ListarPorClienteAsync(email);

                return Ok(ordensCliente);
            }

            var ordens = await _service.ListarAsync();

            return Ok(ordens);
        }

        [Authorize(Roles = "ADMIN,FUNCIONARIO")]
        [HttpGet("resumo")]
        public async Task<IActionResult> GetResumo()
        {
            var ordensFinalizadas = await _context.OrdensServico
                .Where(o => o.Status == "Finalizada" || o.Status == "Entregue")
                .ToListAsync();

            var totalOrdens = await _context.OrdensServico.CountAsync();

            double tempoMedioHoras = 0;

            if (ordensFinalizadas.Any())
            {
                tempoMedioHoras = ordensFinalizadas
                    .Average(o => (DateTime.Now - o.DataEntrada).TotalHours);
            }

            return Ok(new
            {
                totalOrdens,
                ordensFinalizadas = ordensFinalizadas.Count,
                tempoMedioHoras = Math.Round(tempoMedioHoras, 2)
            });
        }

        [Authorize(Roles = "ADMIN,FUNCIONARIO,CLIENTE")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var os = await _service.BuscarPorIdAsync(id);

            if (os == null)
                return NotFound();

            if (User.IsInRole("CLIENTE"))
            {
                var email = User.FindFirstValue(ClaimTypes.Email);

                if (os.Veiculo?.EmailCliente != email)
                    return Forbid();
            }

            return Ok(os);
        }

        [Authorize(Roles = "ADMIN,FUNCIONARIO,CLIENTE")]
        [HttpGet("{id}/orcamento-pdf")]
        public async Task<IActionResult> GerarOrcamentoPdf(int id)
        {
            string? emailCliente = null;

            if (User.IsInRole("CLIENTE"))
            {
                emailCliente = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrWhiteSpace(emailCliente))
                    return Unauthorized();
            }

            var resultado = await _orcamentoPdfService.GerarAsync(id, emailCliente);

            if (!resultado.Sucesso)
            {
                if (resultado.Erro?.Contains("não encontrada", StringComparison.OrdinalIgnoreCase) == true)
                    return NotFound(resultado.Erro);

                return Forbid();
            }

            return File(resultado.Pdf!, "application/pdf", resultado.NomeArquivo);
        }

        [Authorize(Roles = "ADMIN,FUNCIONARIO")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CriarOrdemServicoDto dto)
        {
            var resultado = await _service.CriarAsync(dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok(resultado.OS);
        }

        [Authorize(Roles = "ADMIN,FUNCIONARIO")]
        [HttpPost("{id}/itens")]
        public async Task<IActionResult> AdicionarItem(int id, [FromBody] AdicionarItemOrdemServicoDto dto)
        {
            var resultado = await _service.AdicionarItemAsync(id, dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok();
        }

        [Authorize(Roles = "ADMIN,FUNCIONARIO")]
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


