using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oficina.API.DAL;
using Oficina.API.Models;
using Oficina.API.Business;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"] ?? "chave_padrao_muito_segura_para_desenvolvimento";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "OficinaAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "OficinaFront";

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite("Data Source=oficina.db");
    }
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<VeiculoService>();
builder.Services.AddScoped<OrdemServicoService>();
builder.Services.AddScoped<OrcamentoPdfService>();
builder.Services.AddScoped<EstoqueEmailService>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oficina.API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Digite: Bearer seu_token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao aplicar migrations:");
            Console.WriteLine(ex.Message);
        }

        var passwordHasher = new PasswordHasher<Usuario>();

        GarantirUsuarioPadrao(context, passwordHasher, "Admin Novo", "admin@teste.com", "123456", "ADMIN");
        GarantirUsuarioPadrao(context, passwordHasher, "Cliente Teste", "cliente@teste.com", "123456", "CLIENTE");
        GarantirUsuarioPadrao(context, passwordHasher, "Funcionario Teste", "funcionario@teste.com", "123456", "FUNCIONARIO");

        context.SaveChanges();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Erro ao criar usuarios padrao:");
        Console.WriteLine(ex.Message);
    }
}

app.Run();

static void GarantirUsuarioPadrao(
    AppDbContext context,
    PasswordHasher<Usuario> passwordHasher,
    string nome,
    string email,
    string senha,
    string perfil)
{
    var usuario = context.Usuarios.FirstOrDefault(u => u.Email == email);

    if (usuario == null)
    {
        usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Email = email
        };

        context.Usuarios.Add(usuario);
    }

    usuario.Nome = nome;
    usuario.Perfil = perfil;

    if (!SenhaPadraoValida(usuario, passwordHasher, senha))
        usuario.Senha = passwordHasher.HashPassword(usuario, senha);
}

static bool SenhaPadraoValida(Usuario usuario, PasswordHasher<Usuario> passwordHasher, string senha)
{
    if (usuario.Senha == senha)
    {
        usuario.Senha = passwordHasher.HashPassword(usuario, senha);
        return true;
    }

    try
    {
        var resultado = passwordHasher.VerifyHashedPassword(usuario, usuario.Senha, senha);
        return resultado != PasswordVerificationResult.Failed;
    }
    catch
    {
        return false;
    }
}

public partial class Program { }

