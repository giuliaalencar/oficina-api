using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Oficina.API.DAL;
using Oficina.API.Models;

namespace Oficina.API.Business
{
    public class EstoqueEmailService
    {
        private const int QuantidadeMinimaPadrao = 5;
        private const string EmailDestinoPadrao = "giulia.sia@hotmail.com";

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EstoqueEmailService> _logger;

        public EstoqueEmailService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<EstoqueEmailService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> NotificarItensComBaixoEstoqueAsync(string motivo)
        {
            var quantidadeMinima = ObterQuantidadeMinima();
            var emailDestino = _configuration["Estoque:EmailDestino"] ?? EmailDestinoPadrao;

            var itens = await _context.Itens
                .AsNoTracking()
                .OrderBy(item => item.Estoque)
                .ThenBy(item => item.Descricao)
                .ToListAsync();

            var itensBaixoEstoque = itens
                .Where(item => EhPeca(item.Tipo) && EstoqueDisponivel(item) <= quantidadeMinima)
                .ToList();

            if (!itensBaixoEstoque.Any())
            {
                _logger.LogInformation("E-mail de estoque baixo não enviado porque não existem peças com estoque disponível menor ou igual a {QuantidadeMinima}.", quantidadeMinima);
                return false;
            }

            var assunto = $"Oficina - alerta de estoque baixo ({itensBaixoEstoque.Count} item(ns))";
            var corpo = MontarCorpoEmail(itensBaixoEstoque, quantidadeMinima, motivo);

            return await EnviarEmailAsync(emailDestino, assunto, corpo, itensBaixoEstoque.Count);
        }

        private int ObterQuantidadeMinima()
        {
            var quantidadeConfigurada = _configuration.GetValue<int?>("Estoque:QuantidadeMinima");

            if (quantidadeConfigurada.HasValue && quantidadeConfigurada.Value >= 0)
            {
                return quantidadeConfigurada.Value;
            }

            return QuantidadeMinimaPadrao;
        }

        private static string MontarCorpoEmail(List<Item> itens, int quantidadeMinima, string motivo)
        {
            var corpo = new StringBuilder();

            corpo.AppendLine("Olá!");
            corpo.AppendLine();
            corpo.AppendLine("Existem itens com baixa quantidade no estoque da oficina.");
            corpo.AppendLine($"Quantidade mínima configurada: {quantidadeMinima}");
            corpo.AppendLine($"Motivo do disparo: {motivo}");
            corpo.AppendLine($"Data do alerta: {DateTime.Now:dd/MM/yyyy HH:mm}");
            corpo.AppendLine();
            corpo.AppendLine("Itens com estoque baixo:");
            corpo.AppendLine();

            foreach (var item in itens)
            {
                corpo.AppendLine($"- #{item.Id} | {item.Descricao} | Estoque: {item.Estoque} | Reservado: {item.EstoqueReservado} | Disponível: {EstoqueDisponivel(item)} | Valor: {item.Valor:C}");
            }

            corpo.AppendLine();
            corpo.AppendLine("Acesse o sistema da oficina para repor o estoque.");

            return corpo.ToString();
        }

        private static bool EhPeca(string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return false;

            var valor = tipo.Trim();

            return valor.Equals("Peca", StringComparison.OrdinalIgnoreCase) ||
                   valor.Equals("Pe\u00e7a", StringComparison.OrdinalIgnoreCase) ||
                   valor.Equals("Pe\u00c3\u00a7a", StringComparison.OrdinalIgnoreCase);
        }

        private static int EstoqueDisponivel(Item item)
        {
            return item.Estoque - item.EstoqueReservado;
        }

        private async Task<bool> EnviarEmailAsync(string destinatario, string assunto, string corpo, int totalItens)
        {
            var host = _configuration["Email:SmtpHost"];
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var from = _configuration["Email:From"] ?? username;
            var port = _configuration.GetValue("Email:SmtpPort", 587);
            var enableSsl = _configuration.GetValue("Email:EnableSsl", true);

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(from))
            {
                _logger.LogWarning("E-mail de estoque baixo não enviado porque as configurações SMTP não estão completas.");
                return false;
            }

            try
            {
                using var mensagem = new MailMessage
                {
                    From = new MailAddress(from, "Oficina"),
                    Subject = assunto,
                    Body = corpo,
                    IsBodyHtml = false,
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8
                };

                mensagem.To.Add(destinatario);

                using var smtp = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password)
                };

                await smtp.SendMailAsync(mensagem);
                _logger.LogInformation(
                    "E-mail de estoque baixo enviado para {Destinatario}. Total de itens: {TotalItens}.",
                    destinatario,
                    totalItens);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar e-mail de estoque baixo.");
                return false;
            }
        }
    }
}
