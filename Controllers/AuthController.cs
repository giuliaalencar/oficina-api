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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _authService.LoginAsync(dto);

            if (token == null)
                return Unauthorized("Email ou senha inválidos.");

            return Ok(new { token });
        }

        [Authorize]
        [HttpGet("usuarios")]
        public async Task<IActionResult> ListarUsuarios()
        {
            if (!UsuarioEhAdmin())
                return Forbid();

            var usuarios = await _authService.ListarUsuariosAsync();

            return Ok(usuarios);
        }

        [Authorize]
        [HttpPost("usuarios")]
        public async Task<IActionResult> CadastrarUsuario([FromBody] CriarUsuarioDTO dto)
        {
            if (!UsuarioEhAdmin())
                return Forbid();

            var resultado = await _authService.CadastrarUsuarioAsync(dto);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok(new { mensagem = "Usuário cadastrado com sucesso." });
        }

        [Authorize]
        [HttpPost("resetar-senha")]
        public async Task<IActionResult> ResetarSenha([FromBody] LoginDto dto)
        {
            if (!UsuarioEhAdmin())
                return Forbid();

            var resultado = await _authService.ResetarSenhaAsync(dto.Email, dto.Senha);

            if (!resultado.Sucesso)
                return BadRequest(resultado.Erro);

            return Ok(new { mensagem = "Senha resetada com sucesso." });
        }

        private bool UsuarioEhAdmin()
        {
            return User.Claims.Any(c =>
                (c.Type == "perfil" || c.Type.EndsWith("/role") || c.Type == "role") &&
                c.Value == "ADMIN"
            );
        }
    }
}
