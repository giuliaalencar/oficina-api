using Oficina.API.Business;
using Oficina.API.DTOs;

namespace Business.UnitTests
{
    public class AuthServiceExcecaoTests
    {
        [Fact]
        public async Task LoginAsync_DeveRetornarNull_QuandoOcorrerExcecaoInterna()
        {
            var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await context.DisposeAsync();

            var token = await service.LoginAsync(new LoginDto
            {
                Email = "admin@teste.com",
                Senha = "123456"
            });

            Assert.Null(token);
        }

        [Fact]
        public async Task CadastrarUsuarioAsync_DeveRetornarErro_QuandoOcorrerExcecaoInterna()
        {
            var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await context.DisposeAsync();

            var resultado = await service.CadastrarUsuarioAsync(new CriarUsuarioDTO
            {
                Nome = "Admin",
                Email = "admin@teste.com",
                Senha = "123456",
                Perfil = 1
            });

            Assert.False(resultado.Sucesso);
            Assert.False(string.IsNullOrWhiteSpace(resultado.Erro));
        }

        [Fact]
        public async Task ResetarSenhaAsync_DeveRetornarErro_QuandoOcorrerExcecaoInterna()
        {
            var context = TestHelpers.CriarContexto();
            var service = new AuthService(context, TestHelpers.CriarConfiguracaoJwt());
            await context.DisposeAsync();

            var resultado = await service.ResetarSenhaAsync("admin@teste.com", "123456");

            Assert.False(resultado.Sucesso);
            Assert.False(string.IsNullOrWhiteSpace(resultado.Erro));
        }
    }
}
