using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Oficina.API.DAL;
using Oficina.API.DTOs;
using Xunit;

namespace Controller.IntegrationTests;

public class ControllersIntegrationTests : IClassFixture<OficinaApiFactory>
{
    private readonly OficinaApiFactory _factory;

    public ControllersIntegrationTests(OficinaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_RetornaToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = "admin@teste.com",
            Senha = "123456"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = await LerTokenAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = "admin@teste.com",
            Senha = "senha-errada"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Usuarios_SemToken_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/usuarios");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Usuarios_FuncionarioNaoPodeGerenciarUsuarios()
    {
        var client = await CriarClienteAutenticadoAsync("funcionario@teste.com", "123456");

        var listarResponse = await client.GetAsync("/api/auth/usuarios");
        var cadastrarResponse = await client.PostAsJsonAsync("/api/auth/usuarios", new CriarUsuarioDTO
        {
            Nome = "Usuario Bloqueado",
            Email = $"bloqueado.{Guid.NewGuid():N}@teste.com",
            Senha = "123456",
            Perfil = 2
        });
        var resetarResponse = await client.PostAsJsonAsync("/api/auth/resetar-senha", new LoginDto
        {
            Email = "cliente@teste.com",
            Senha = "123456"
        });

        Assert.Equal(HttpStatusCode.Forbidden, listarResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cadastrarResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, resetarResponse.StatusCode);
    }

    [Fact]
    public async Task Usuarios_AdminPodeListarCadastrarEResetarSenha()
    {
        var client = await CriarClienteAutenticadoAsync("admin@teste.com", "123456");
        var email = $"usuario.integracao.{Guid.NewGuid():N}@teste.com";

        var criarResponse = await client.PostAsJsonAsync("/api/auth/usuarios", new CriarUsuarioDTO
        {
            Nome = "Usuario Integracao",
            Email = email,
            Senha = "123456",
            Perfil = 3
        });

        Assert.Equal(HttpStatusCode.OK, criarResponse.StatusCode);

        var listarResponse = await client.GetAsync("/api/auth/usuarios");
        Assert.Equal(HttpStatusCode.OK, listarResponse.StatusCode);

        var resetarResponse = await client.PostAsJsonAsync("/api/auth/resetar-senha", new LoginDto
        {
            Email = email,
            Senha = "654321"
        });

        Assert.Equal(HttpStatusCode.OK, resetarResponse.StatusCode);

        var loginComNovaSenha = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Senha = "654321"
        });

        Assert.Equal(HttpStatusCode.OK, loginComNovaSenha.StatusCode);
    }

    [Fact]
    public async Task Usuarios_AdminRecebeBadRequestQuandoDadosInvalidos()
    {
        var client = await CriarClienteAutenticadoAsync("admin@teste.com", "123456");

        var perfilInvalidoResponse = await client.PostAsJsonAsync("/api/auth/usuarios", new CriarUsuarioDTO
        {
            Nome = "Usuario Perfil Invalido",
            Email = $"perfil.invalido.{Guid.NewGuid():N}@teste.com",
            Senha = "123456",
            Perfil = 0
        });

        var resetarUsuarioInexistenteResponse = await client.PostAsJsonAsync("/api/auth/resetar-senha", new LoginDto
        {
            Email = $"nao.existe.{Guid.NewGuid():N}@teste.com",
            Senha = "123456"
        });

        Assert.Equal(HttpStatusCode.BadRequest, perfilInvalidoResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, resetarUsuarioInexistenteResponse.StatusCode);
    }

    [Fact]
    public async Task Clientes_AdminExecutaCrudCompleto()
    {
        var client = await CriarClienteAutenticadoAsync("admin@teste.com", "123456");

        var listarResponse = await client.GetAsync("/api/clientes");
        Assert.Equal(HttpStatusCode.OK, listarResponse.StatusCode);

        var criarInvalidoResponse = await client.PostAsJsonAsync("/api/clientes", new CriarClienteDto
        {
            Nome = "Cliente Invalido",
            Email = $"cliente.invalido.{Guid.NewGuid():N}@teste.com",
            Telefone = "11999999999",
            CpfCnpj = "111"
        });

        Assert.Equal(HttpStatusCode.BadRequest, criarInvalidoResponse.StatusCode);

        var criarResponse = await client.PostAsJsonAsync("/api/clientes", new CriarClienteDto
        {
            Nome = "Cliente Controller",
            Email = $"cliente.controller.{Guid.NewGuid():N}@teste.com",
            Telefone = "11999999999",
            CpfCnpj = "12345678901"
        });

        Assert.Equal(HttpStatusCode.Created, criarResponse.StatusCode);

        var cliente = await criarResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = cliente.GetProperty("id").GetGuid();

        var buscarResponse = await client.GetAsync($"/api/clientes/{id}");
        Assert.Equal(HttpStatusCode.OK, buscarResponse.StatusCode);

        var atualizarResponse = await client.PutAsJsonAsync($"/api/clientes/{id}", new AtualizarClienteDto
        {
            Nome = "Cliente Controller Atualizado",
            Email = $"cliente.controller.atualizado.{Guid.NewGuid():N}@teste.com",
            Telefone = "11888888888",
            CpfCnpj = "12345678901"
        });

        Assert.Equal(HttpStatusCode.OK, atualizarResponse.StatusCode);

        var atualizarInvalidoResponse = await client.PutAsJsonAsync($"/api/clientes/{id}", new AtualizarClienteDto
        {
            Nome = "Cliente CPF Invalido",
            Email = $"cliente.cpf.invalido.{Guid.NewGuid():N}@teste.com",
            Telefone = "11777777777",
            CpfCnpj = "111"
        });

        Assert.Equal(HttpStatusCode.BadRequest, atualizarInvalidoResponse.StatusCode);

        var atualizarNaoEncontradoResponse = await client.PutAsJsonAsync($"/api/clientes/{Guid.NewGuid()}", new AtualizarClienteDto
        {
            Nome = "Cliente Nao Encontrado",
            Email = $"cliente.nao.encontrado.{Guid.NewGuid():N}@teste.com",
            Telefone = "11666666666",
            CpfCnpj = "12345678901"
        });

        Assert.Equal(HttpStatusCode.NotFound, atualizarNaoEncontradoResponse.StatusCode);

        var excluirResponse = await client.DeleteAsync($"/api/clientes/{id}");
        Assert.Equal(HttpStatusCode.NoContent, excluirResponse.StatusCode);

        var buscarExcluidoResponse = await client.GetAsync($"/api/clientes/{id}");
        Assert.Equal(HttpStatusCode.NotFound, buscarExcluidoResponse.StatusCode);

        var excluirNaoEncontradoResponse = await client.DeleteAsync($"/api/clientes/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, excluirNaoEncontradoResponse.StatusCode);
    }

    [Fact]
    public async Task Veiculos_AdminExecutaCrudCompleto()
    {
        var client = await CriarClienteAutenticadoAsync("admin@teste.com", "123456");
        var clienteId = await CriarClienteAsync(client);

        var criarInvalidoResponse = await client.PostAsJsonAsync("/api/veiculos", new CriarVeiculoDto
        {
            ClienteId = Guid.NewGuid(),
            Placa = "ABC1D23",
            Marca = "Honda",
            Modelo = "Civic",
            Ano = 2024
        });

        Assert.Equal(HttpStatusCode.BadRequest, criarInvalidoResponse.StatusCode);

        var criarResponse = await client.PostAsJsonAsync("/api/veiculos", new CriarVeiculoDto
        {
            ClienteId = clienteId,
            Placa = "ABC1D23",
            Marca = "Honda",
            Modelo = "Civic",
            Ano = 2024
        });

        Assert.Equal(HttpStatusCode.Created, criarResponse.StatusCode);

        var veiculo = await criarResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = veiculo.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/veiculos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/veiculos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/veiculos?clienteId={clienteId}")).StatusCode);

        var atualizarResponse = await client.PutAsJsonAsync($"/api/veiculos/{id}", new AtualizarVeiculoDto
        {
            Placa = "XYZ1A23",
            Marca = "Toyota",
            Modelo = "Corolla",
            Ano = 2025
        });

        Assert.Equal(HttpStatusCode.OK, atualizarResponse.StatusCode);

        var atualizarInvalidoResponse = await client.PutAsJsonAsync($"/api/veiculos/{id}", new AtualizarVeiculoDto
        {
            Placa = "111",
            Marca = "Toyota",
            Modelo = "Corolla",
            Ano = 2025
        });

        Assert.Equal(HttpStatusCode.BadRequest, atualizarInvalidoResponse.StatusCode);

        var atualizarNaoEncontradoResponse = await client.PutAsJsonAsync($"/api/veiculos/{Guid.NewGuid()}", new AtualizarVeiculoDto
        {
            Placa = "DEF1A23",
            Marca = "Fiat",
            Modelo = "Argo",
            Ano = 2022
        });

        Assert.Equal(HttpStatusCode.NotFound, atualizarNaoEncontradoResponse.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/veiculos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/veiculos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/veiculos/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Itens_AdminExecutaCrudCompleto()
    {
        var client = await CriarClienteAutenticadoAsync("admin@teste.com", "123456");

        var criarResponse = await client.PostAsJsonAsync("/api/itens", new CriarItemDto
        {
            Descricao = "Filtro de oleo",
            Tipo = "Peca",
            Valor = 45,
            Estoque = 10
        });

        Assert.Equal(HttpStatusCode.Created, criarResponse.StatusCode);

        var item = await criarResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = item.GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/itens/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/itens")).StatusCode);

        var atualizarResponse = await client.PutAsJsonAsync($"/api/itens/{id}", new AtualizarItemDto
        {
            Descricao = "Filtro atualizado",
            Tipo = "Peca",
            Valor = 55,
            Estoque = 8
        });

        Assert.Equal(HttpStatusCode.OK, atualizarResponse.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/itens/999999", new AtualizarItemDto
        {
            Descricao = "Item inexistente",
            Tipo = "Peca",
            Valor = 10,
            Estoque = 1
        })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/itens/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/itens/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/itens/999999")).StatusCode);
    }

    [Fact]
    public async Task OrdensServico_AdminExecutaFluxoCompleto()
    {
        var client = await CriarClienteAutenticadoAsync("admin@teste.com", "123456");
        var clienteId = await CriarClienteAsync(client);
        var veiculoId = await CriarVeiculoAsync(client, clienteId);
        var itemId = await CriarItemAsync(client);

        var criarResponse = await client.PostAsJsonAsync("/api/ordens-servico", new CriarOrdemServicoDto
        {
            VeiculoId = veiculoId
        });

        Assert.Equal(HttpStatusCode.OK, criarResponse.StatusCode);

        var ordem = await criarResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ordemId = ordem.GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/ordens-servico")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/ordens-servico/{ordemId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/ordens-servico/resumo")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/ordens-servico/999999")).StatusCode);

        var adicionarItemResponse = await client.PostAsJsonAsync($"/api/ordens-servico/{ordemId}/itens", new AdicionarItemOrdemServicoDto
        {
            ItemId = itemId,
            Quantidade = 2
        });

        Assert.Equal(HttpStatusCode.OK, adicionarItemResponse.StatusCode);

        var pdfResponse = await client.GetAsync($"/api/ordens-servico/{ordemId}/orcamento-pdf");
        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray()));

        await AtualizarStatusComSucessoAsync(client, ordemId, "Em Diagnóstico");
        await AtualizarStatusComSucessoAsync(client, ordemId, "Aguardando Aprovação");
        await AtualizarStatusComSucessoAsync(client, ordemId, "Em Execução");
        await AtualizarStatusComSucessoAsync(client, ordemId, "Finalizada");

        var resumoComFinalizadaResponse = await client.GetAsync("/api/ordens-servico/resumo");
        Assert.Equal(HttpStatusCode.OK, resumoComFinalizadaResponse.StatusCode);
    }

    [Fact]
    public async Task OrdensServico_RetornaBadRequestQuandoFluxoInvalido()
    {
        var client = await CriarClienteAutenticadoAsync("admin@teste.com", "123456");

        var criarComVeiculoInexistenteResponse = await client.PostAsJsonAsync("/api/ordens-servico", new CriarOrdemServicoDto
        {
            VeiculoId = Guid.NewGuid()
        });

        var adicionarEmOrdemInexistenteResponse = await client.PostAsJsonAsync("/api/ordens-servico/999999/itens", new AdicionarItemOrdemServicoDto
        {
            ItemId = 999999,
            Quantidade = 1
        });

        var atualizarStatusInexistenteResponse = await client.PutAsJsonAsync("/api/ordens-servico/999999/status", new AtualizarStatusOrdemServicoDto
        {
            Status = "Em Diagnóstico"
        });

        Assert.Equal(HttpStatusCode.BadRequest, criarComVeiculoInexistenteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, adicionarEmOrdemInexistenteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, atualizarStatusInexistenteResponse.StatusCode);
    }

    [Fact]
    public async Task ClienteSoConsegueAcessarOrdensAutorizadas()
    {
        var adminClient = await CriarClienteAutenticadoAsync("admin@teste.com", "123456");
        var clienteId = await CriarClienteAsync(adminClient, "cliente@teste.com");
        var outroClienteId = await CriarClienteAsync(adminClient, $"outro.cliente.{Guid.NewGuid():N}@teste.com");
        var veiculoId = await CriarVeiculoAsync(adminClient, clienteId);
        var outroVeiculoId = await CriarVeiculoAsync(adminClient, outroClienteId);

        var criarResponse = await adminClient.PostAsJsonAsync("/api/ordens-servico", new CriarOrdemServicoDto
        {
            VeiculoId = veiculoId
        });
        var criarOutraResponse = await adminClient.PostAsJsonAsync("/api/ordens-servico", new CriarOrdemServicoDto
        {
            VeiculoId = outroVeiculoId
        });

        Assert.Equal(HttpStatusCode.OK, criarResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, criarOutraResponse.StatusCode);

        var ordem = await criarResponse.Content.ReadFromJsonAsync<JsonElement>();
        var outraOrdem = await criarOutraResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ordemId = ordem.GetProperty("id").GetInt32();
        var outraOrdemId = outraOrdem.GetProperty("id").GetInt32();

        var clienteClient = await CriarClienteAutenticadoAsync("cliente@teste.com", "123456");

        Assert.Equal(HttpStatusCode.OK, (await clienteClient.GetAsync("/api/ordens-servico")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clienteClient.GetAsync($"/api/ordens-servico/{ordemId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clienteClient.GetAsync($"/api/ordens-servico/{ordemId}/orcamento-pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteClient.GetAsync($"/api/ordens-servico/{outraOrdemId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteClient.GetAsync($"/api/ordens-servico/{outraOrdemId}/orcamento-pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteClient.PostAsJsonAsync("/api/ordens-servico", new CriarOrdemServicoDto { VeiculoId = veiculoId })).StatusCode);
    }

    [Fact]
    public async Task WeatherForecast_RetornaPrevisoes()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/WeatherForecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var previsoes = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5, previsoes.GetArrayLength());
    }

    [Fact]
    public async Task OrdensServico_ClienteSemEmailNoToken_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CriarTokenSemEmail());

        var response = await client.GetAsync("/api/ordens-servico");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task ControllersProtegidos_SemToken_RetornamUnauthorized()
    {
        var client = _factory.CreateClient();

        var clientesResponse = await client.GetAsync("/api/clientes");
        var veiculosResponse = await client.GetAsync("/api/veiculos");
        var itensResponse = await client.GetAsync("/api/itens");
        var ordensResponse = await client.GetAsync("/api/ordens-servico");
        var resumoResponse = await client.GetAsync("/api/ordens-servico/resumo");

        Assert.Equal(HttpStatusCode.Unauthorized, clientesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, veiculosResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, itensResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, ordensResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, resumoResponse.StatusCode);
    }

    [Fact]
    public async Task ClienteNaoPodeAcessarCadastrosInternos()
    {
        var client = await CriarClienteAutenticadoAsync("cliente@teste.com", "123456");

        var clientesResponse = await client.GetAsync("/api/clientes");
        var veiculosResponse = await client.GetAsync("/api/veiculos");
        var itensResponse = await client.GetAsync("/api/itens");

        Assert.Equal(HttpStatusCode.Forbidden, clientesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, veiculosResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, itensResponse.StatusCode);
    }

    [Fact]
    public async Task FuncionarioPodeExecutarFluxoOperacional()
    {
        var client = await CriarClienteAutenticadoAsync("funcionario@teste.com", "123456");

        var clienteId = await CriarClienteAsync(client, $"funcionario.cliente.{Guid.NewGuid():N}@teste.com");
        var veiculoId = await CriarVeiculoAsync(client, clienteId);
        var itemId = await CriarItemAsync(client);

        var criarOrdemResponse = await client.PostAsJsonAsync("/api/ordens-servico", new CriarOrdemServicoDto
        {
            VeiculoId = veiculoId
        });

        Assert.Equal(HttpStatusCode.OK, criarOrdemResponse.StatusCode);

        var ordem = await criarOrdemResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ordemId = ordem.GetProperty("id").GetInt32();

        var adicionarItemResponse = await client.PostAsJsonAsync($"/api/ordens-servico/{ordemId}/itens", new AdicionarItemOrdemServicoDto
        {
            ItemId = itemId,
            Quantidade = 1
        });

        var statusResponse = await client.PutAsJsonAsync($"/api/ordens-servico/{ordemId}/status", new AtualizarStatusOrdemServicoDto
        {
            Status = "Em Diagnóstico"
        });

        var resumoResponse = await client.GetAsync("/api/ordens-servico/resumo");

        Assert.Equal(HttpStatusCode.OK, adicionarItemResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resumoResponse.StatusCode);
    }

    [Fact]
    public async Task ClienteNaoPodeAlterarOrdemServico()
    {
        var adminClient = await CriarClienteAutenticadoAsync("admin@teste.com", "123456");
        var clienteId = await CriarClienteAsync(adminClient, "cliente@teste.com");
        var veiculoId = await CriarVeiculoAsync(adminClient, clienteId);
        var itemId = await CriarItemAsync(adminClient);

        var criarOrdemResponse = await adminClient.PostAsJsonAsync("/api/ordens-servico", new CriarOrdemServicoDto
        {
            VeiculoId = veiculoId
        });

        Assert.Equal(HttpStatusCode.OK, criarOrdemResponse.StatusCode);

        var ordem = await criarOrdemResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ordemId = ordem.GetProperty("id").GetInt32();

        var clienteClient = await CriarClienteAutenticadoAsync("cliente@teste.com", "123456");

        var resumoResponse = await clienteClient.GetAsync("/api/ordens-servico/resumo");
        var adicionarItemResponse = await clienteClient.PostAsJsonAsync($"/api/ordens-servico/{ordemId}/itens", new AdicionarItemOrdemServicoDto
        {
            ItemId = itemId,
            Quantidade = 1
        });
        var statusResponse = await clienteClient.PutAsJsonAsync($"/api/ordens-servico/{ordemId}/status", new AtualizarStatusOrdemServicoDto
        {
            Status = "Em Diagnóstico"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resumoResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, adicionarItemResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, statusResponse.StatusCode);
    }
    private async Task<HttpClient> CriarClienteAutenticadoAsync(string email, string senha)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Senha = senha
        });

        loginResponse.EnsureSuccessStatusCode();

        var token = await LerTokenAsync(loginResponse);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static async Task<string> LerTokenAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString() ?? string.Empty;
    }

    private static async Task<Guid> CriarClienteAsync(HttpClient client, string? email = null)
    {
        var response = await client.PostAsJsonAsync("/api/clientes", new CriarClienteDto
        {
            Nome = "Cliente Integracao",
            Email = email ?? $"cliente.integracao.{Guid.NewGuid():N}@teste.com",
            Telefone = "11999999999",
            CpfCnpj = "12345678901"
        });

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CriarVeiculoAsync(HttpClient client, Guid clienteId)
    {
        var response = await client.PostAsJsonAsync("/api/veiculos", new CriarVeiculoDto
        {
            ClienteId = clienteId,
            Placa = $"TST{Random.Shared.Next(1000, 9999)}",
            Marca = "Honda",
            Modelo = "Fit",
            Ano = 2023
        });

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }

    private static async Task<int> CriarItemAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/itens", new CriarItemDto
        {
            Descricao = "Servico de integracao",
            Tipo = "Servico",
            Valor = 100,
            Estoque = 20
        });

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetInt32();
    }

    private static string CriarTokenSemEmail()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, "Cliente Sem Email"),
            new(ClaimTypes.Role, "CLIENTE"),
            new("perfil", "CLIENTE")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("minha-chave-super-secreta-com-mais-de-32-caracteres"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "OficinaAPI",
            audience: "OficinaAPIUsers",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task AtualizarStatusComSucessoAsync(HttpClient client, int ordemId, string status)
    {
        var response = await client.PutAsJsonAsync($"/api/ordens-servico/{ordemId}/status", new AtualizarStatusOrdemServicoDto
        {
            Status = status
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}


