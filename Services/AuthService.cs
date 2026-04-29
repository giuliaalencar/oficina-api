using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Oficina.API.Context;
using Oficina.API.DTOs;
using Oficina.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Oficina.API.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string?> LoginAsync(LoginDto dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Senha == dto.Senha);

            if (usuario == null)
                return null;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Perfil)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<(bool Sucesso, string? Erro)> CadastrarUsuarioAsync(CriarUsuarioDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                return (false, "Informe o nome.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                return (false, "Informe o email.");

            if (string.IsNullOrWhiteSpace(dto.Senha))
                return (false, "Informe a senha.");

            var emailJaExiste = await _context.Usuarios.AnyAsync(u => u.Email == dto.Email);

            if (emailJaExiste)
                return (false, "Usuário já cadastrado.");

            var perfilNome = dto.Perfil switch
{
    1 => "ADMIN",
    2 => "CLIENTE",
    _ => ""
};

if (string.IsNullOrWhiteSpace(perfilNome))
    return (false, "Perfil inválido. Use 1 para ADMIN ou 2 para CLIENTE.");

            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = dto.Nome,
                Email = dto.Email,
                Senha = dto.Senha,
                Perfil = perfil
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}
