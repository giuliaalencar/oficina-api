using Microsoft.AspNetCore.Identity;
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
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (usuario == null)
                    return null;

                var senhaValida = false;

                if (usuario.Senha == dto.Senha)
                {
                    senhaValida = true;
                }
                else
                {
                    try
                    {
                        var passwordHasher = new PasswordHasher<Usuario>();
                        var resultadoSenha = passwordHasher.VerifyHashedPassword(usuario, usuario.Senha, dto.Senha);

                        senhaValida = resultadoSenha != PasswordVerificationResult.Failed;
                    }
                    catch
                    {
                        senhaValida = false;
                    }
                }

                if (!senhaValida)
                    return null;

                return GerarToken(usuario);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro no login:");
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<List<UsuarioDTO>> ListarUsuariosAsync()
        {
            return await _context.Usuarios
                .OrderBy(u => u.Nome)
                .Select(u => new UsuarioDTO
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    Perfil = u.Perfil
                })
                .ToListAsync();
        }

        public async Task<(bool Sucesso, string? Erro)> CadastrarUsuarioAsync(CriarUsuarioDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                return (false, "Informe o nome.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                return (false, "Informe o email.");

            if (string.IsNullOrWhiteSpace(dto.Senha))
                return (false, "Informe a senha.");

            var perfilNome = dto.Perfil switch
            {
                1 => "ADMIN",
                2 => "CLIENTE",
                3 => "FUNCIONARIO",
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(perfilNome))
                return (false, "Perfil inválido. Use 1 para ADMIN, 2 para CLIENTE ou 3 para FUNCIONARIO.");

            var emailJaExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == dto.Email);

            if (emailJaExiste)
                return (false, "Já existe um usuário com este email.");

            var usuario = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Senha = dto.Senha,
                Perfil = perfilNome
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> ResetarSenhaAsync(string email, string novaSenha)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (usuario == null)
                    return (false, "Usuário não encontrado.");

                if (string.IsNullOrWhiteSpace(novaSenha))
                    return (false, "Informe a nova senha.");

                usuario.Senha = novaSenha;

                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao resetar senha:");
                Console.WriteLine(ex.Message);

                return (false, "Erro ao resetar senha.");
            }
        }

        private string GerarToken(Usuario usuario)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "chave_padrao_muito_segura_para_desenvolvimento";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "OficinaAPI";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "OficinaFront";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Perfil),
                new Claim("perfil", usuario.Perfil)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
