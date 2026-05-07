using Microsoft.AspNetCore.Identity;
using Oficina.API.Business;
using Oficina.API.DTOs;
using Oficina.API.Models;

namespace Business.UnitTests
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task LoginAsync_DeveRetornarToken_QuandoEmailESenhaForemValidos()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await AdicionarUsuarioAsync(context, "Admin", "admin@teste.com", "123456", "ADMIN");

            var token = await service.LoginAsync(new LoginDto
            {
                Email = "admin@teste.com",
                Senha = "123456"
            });

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public async Task LoginAsync_DeveRetornarToken_QuandoEmailTiverEspacos()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await AdicionarUsuarioAsync(context, "Admin", "admin@teste.com", "123456", "ADMIN");

            var token = await service.LoginAsync(new LoginDto
            {
                Email = "  admin@teste.com  ",
                Senha = "123456"
            });

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public async Task LoginAsync_DeveRetornarToken_QuandoSenhaEstiverComHash()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = "Admin",
                Email = "admin@teste.com",
                Perfil = "ADMIN"
            };

            usuario.Senha = new PasswordHasher<Usuario>().HashPassword(usuario, "123456");
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var token = await service.LoginAsync(new LoginDto
            {
                Email = "admin@teste.com",
                Senha = "123456"
            });

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public async Task LoginAsync_DeveRetornarNull_QuandoUsuarioNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());

            var token = await service.LoginAsync(new LoginDto
            {
                Email = "naoexiste@teste.com",
                Senha = "123456"
            });

            Assert.Null(token);
        }

        [Fact]
        public async Task LoginAsync_DeveRetornarNull_QuandoSenhaForInvalida()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await AdicionarUsuarioAsync(context, "Admin", "admin@teste.com", "123456", "ADMIN");

            var token = await service.LoginAsync(new LoginDto
            {
                Email = "admin@teste.com",
                Senha = "senha-errada"
            });

            Assert.Null(token);
        }

        [Fact]
        public async Task ListarUsuariosAsync_DeveRetornarUsuariosOrdenados()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await AdicionarUsuarioAsync(context, "Zelia", "zelia@teste.com", "123456", "CLIENTE");
            await AdicionarUsuarioAsync(context, "Ana", "ana@teste.com", "123456", "ADMIN");

            var usuarios = await service.ListarUsuariosAsync();

            Assert.Equal(2, usuarios.Count);
            Assert.Equal("Ana", usuarios[0].Nome);
            Assert.Equal("Zelia", usuarios[1].Nome);
            Assert.Equal("ADMIN", usuarios[0].Perfil);
        }

        [Theory]
        [InlineData(1, "ADMIN")]
        [InlineData(2, "CLIENTE")]
        [InlineData(3, "FUNCIONARIO")]
        public async Task CadastrarUsuarioAsync_DeveCadastrarUsuario_QuandoPerfilForValido(int perfil, string perfilEsperado)
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());

            var resultado = await service.CadastrarUsuarioAsync(new CriarUsuarioDTO
            {
                Nome = " Usuario Novo ",
                Email = " usuario@teste.com ",
                Senha = "123456",
                Perfil = perfil
            });

            var usuario = context.Usuarios.FirstOrDefault(u => u.Email == "usuario@teste.com");

            Assert.True(resultado.Sucesso);
            Assert.Null(resultado.Erro);
            Assert.NotNull(usuario);
            Assert.Equal("Usuario Novo", usuario!.Nome);
            Assert.Equal(perfilEsperado, usuario.Perfil);
        }

        [Theory]
        [InlineData("", "usuario@teste.com", "123456", 1, "nome")]
        [InlineData("Usuario", "", "123456", 1, "email")]
        [InlineData("Usuario", "usuario@teste.com", "", 1, "senha")]
        [InlineData("Usuario", "usuario@teste.com", "123456", 9, "Perfil")]
        public async Task CadastrarUsuarioAsync_DeveRetornarErro_QuandoDadosForemInvalidos(
            string nome,
            string email,
            string senha,
            int perfil,
            string mensagemEsperada)
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());

            var resultado = await service.CadastrarUsuarioAsync(new CriarUsuarioDTO
            {
                Nome = nome,
                Email = email,
                Senha = senha,
                Perfil = perfil
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains(mensagemEsperada, resultado.Erro);
            Assert.Empty(context.Usuarios);
        }

        [Fact]
        public async Task CadastrarUsuarioAsync_DeveRetornarErro_QuandoEmailJaExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await AdicionarUsuarioAsync(context, "Admin", "admin@teste.com", "123456", "ADMIN");

            var resultado = await service.CadastrarUsuarioAsync(new CriarUsuarioDTO
            {
                Nome = "Outro Admin",
                Email = "admin@teste.com",
                Senha = "123456",
                Perfil = 1
            });

            Assert.False(resultado.Sucesso);
            Assert.Contains("email", resultado.Erro);
        }

        [Fact]
        public async Task ResetarSenhaAsync_DeveAlterarSenha_QuandoUsuarioExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await AdicionarUsuarioAsync(context, "Admin", "admin@teste.com", "123456", "ADMIN");

            var resultado = await service.ResetarSenhaAsync("  admin@teste.com  ", " nova123 ");
            var usuario = context.Usuarios.First(u => u.Email == "admin@teste.com");

            Assert.True(resultado.Sucesso);
            Assert.Equal("nova123", usuario.Senha);
        }

        [Fact]
        public async Task ResetarSenhaAsync_DeveRetornarErro_QuandoUsuarioNaoExistir()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());

            var resultado = await service.ResetarSenhaAsync("naoexiste@teste.com", "123456");

            Assert.False(resultado.Sucesso);
            Assert.Equal("Usuário não encontrado.", resultado.Erro);
        }

        [Fact]
        public async Task ResetarSenhaAsync_DeveRetornarErro_QuandoSenhaEstiverVazia()
        {
            using var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await AdicionarUsuarioAsync(context, "Admin", "admin@teste.com", "123456", "ADMIN");

            var resultado = await service.ResetarSenhaAsync("admin@teste.com", "   ");

            Assert.False(resultado.Sucesso);
            Assert.Equal("Informe a nova senha.", resultado.Erro);
        }

        private static async Task AdicionarUsuarioAsync(
            Oficina.API.DAL.AppDbContext context,
            string nome,
            string email,
            string senha,
            string perfil)
        {
            context.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = nome,
                Email = email,
                Senha = senha,
                Perfil = perfil
            });

            await context.SaveChangesAsync();
        }
    }
}
