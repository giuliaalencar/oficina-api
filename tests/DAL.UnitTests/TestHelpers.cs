using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Oficina.API.DAL;

namespace DAL.UnitTests
{
    public static class TestHelpers
    {
        public static AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        public static IConfiguration CriarConfiguracaoJwt()
        {
            var valores = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave_de_teste_com_tamanho_suficiente_para_jwt_123456",
                ["Jwt:Issuer"] = "OficinaAPI",
                ["Jwt:Audience"] = "OficinaFront"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(valores)
                .Build();
        }
    }
}


