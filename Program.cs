using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oficina.API.Context;
using Oficina.API.Models;
using Oficina.API.Services;
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
                origin == "http://localhost:4200" ||
                origin == "https://oficina-front.vercel.app" ||
                origin.EndsWith(".vercel.app"))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    context.Database.Migrate();

    var passwordHasher = new PasswordHasher<Usuario>();

    if (!context.Usuarios.Any(u => u.Email == "giulia.sia@hotmail.com"))
    {
        var admin = new Usuario
        {
            Nome = "Giulia Admin",
            Email = "giulia.sia@hotmail.com",
            Perfil = "ADMIN"
        };

        admin.Senha = passwordHasher.HashPassword(admin, "123456");

        context.Usuarios.Add(admin);
    }

    if (!context.Usuarios.Any(u => u.Email == "cliente@teste.com"))
    {
        var cliente = new Usuario
        {
            Nome = "Cliente Teste",
            Email = "cliente@teste.com",
            Perfil = "CLIENTE"
        };

        cliente.Senha = passwordHasher.HashPassword(cliente, "123456");

        context.Usuarios.Add(cliente);
    }

    if (!context.Usuarios.Any(u => u.Email == "funcionario@teste.com"))
    {
        var funcionario = new Usuario
        {
            Nome = "Funcionário Teste",
            Email = "funcionario@teste.com",
            Perfil = "FUNCIONARIO"
        };

        funcionario.Senha = passwordHasher.HashPassword(funcionario, "123456");

        context.Usuarios.Add(funcionario);
    }

    context.SaveChanges();
}

app.Run();
