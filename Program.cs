using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oficina.API.Context;
using Oficina.API.Models;
using Oficina.API.Services;
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

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                    return false;

                var uri = new Uri(origin);
                var host = uri.Host.ToLower();

                return
                    origin == "http://localhost:4200" ||
                    origin == "https://oficina-front.vercel.app" ||
                    host.EndsWith(".vercel.app");
            })
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

        if (!context.Usuarios.Any(u => u.Email == "adminnovo@teste.com"))
        {
            var admin = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = "Admin Novo",
                Email = "adminnovo@teste.com",
                Senha = "123456",
                Perfil = "ADMIN"
            };

            context.Usuarios.Add(admin);
        }

        if (!context.Usuarios.Any(u => u.Email == "cliente@teste.com"))
        {
            var cliente = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = "Cliente Teste",
                Email = "cliente@teste.com",
                Senha = "123456",
                Perfil = "CLIENTE"
            };

            context.Usuarios.Add(cliente);
        }

        if (!context.Usuarios.Any(u => u.Email == "funcionario@teste.com"))
        {
            var funcionario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = "Funcionario Teste",
                Email = "funcionario@teste.com",
                Senha = "123456",
                Perfil = "FUNCIONARIO"
            };

            context.Usuarios.Add(funcionario);
        }

        context.SaveChanges();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Erro ao criar usuarios padrao:");
        Console.WriteLine(ex.Message);
    }
}

app.Run();
