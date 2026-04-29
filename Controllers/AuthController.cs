using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.API.DTOs;
using Oficina.API.Services;

namespace Oficina.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _authService.LoginAsync(dto);

            if (token == null)
                return Unauthorized("ERR_005 - Login inválido.");

            return Ok(new { token });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("usuarios")]
        public async Task<IActionResult> CadastrarUsuario([FromBody] CriarUsuarioDTO dto)
        {
            var resultado = await _authService.CadastrarUsuarioAsync(dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok("Usuário cadastrado com sucesso.");
        }
    }
}
