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
    }
}