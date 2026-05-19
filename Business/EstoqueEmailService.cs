using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Oficina.API.DAL;
using Oficina.API.Models;

namespace Oficina.API.Business
{
    public class EstoqueEmailResultado
    {
        public bool EmailEnviado { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int QuantidadeMinima { get; set; }
        public int TotalItensBaixoEstoque { get; set; }
        public bool SmtpConfigurado { get; set; }
        public string EmailDestino { get; set; } = string.Empty;
        public List<string> Itens { get; set; } = new();
    }

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
            var resultado = await NotificarItensComBaixoEstoqueDetalhadoAsync(motivo);
            return resultado.EmailEnviado;
        }

        public async Task<EstoqueEmailResultado> NotificarItensComBaixoEstoqueDetalhadoAsync(string motivo)
        {
            var quantidadeMinima = ObterQuantidadeMinima();
            var emailDestino = _configuration["Estoque:EmailDestino"] ?? EmailDestinoPadrao;
            var smtpConfigurado = SmtpEstaConfigurado();

            var itens = await _context.Itens
                .AsNoTracking()
                .OrderBy(item => item.Estoque)
                .ThenBy(item => item.Descricao)
                .ToListAsync();

            var itensBaixoEstoque = itens
                .Where(item => ControlaEstoque(item.Tipo) && EstoqueDisponivel(item) <= quantidadeMinima)
                .ToList();

            if (!itensBaixoEstoque.Any())
            {
                _logger.LogInformation(
                    "E-mail de estoque baixo nao enviado porque nao existem itens com estoque disponivel menor ou igual a {QuantidadeMinima}.",
                    quantidadeMinima);

                return CriarResultado(
                    emailEnviado: false,
                    mensagem: "Nenhum item com estoque baixo encontrado.",
                    quantidadeMinima,
                    smtpConfigurado,
                    emailDestino,
                    itensBaixoEstoque);
            }

            if (!smtpConfigurado)
            {
                _logger.LogWarning("E-mail de estoque baixo nao enviado porque as configuracoes SMTP nao estao completas.");

                return CriarResultado(
                    emailEnviado: false,
                    mensagem: "SMTP nao configurado. Configure Email__Password, Email__Username, Email__From, Email__SmtpHost e Email__SmtpPort no Render.",
                    quantidadeMinima,
                    smtpConfigurado,
                    emailDestino,
                    itensBaixoEstoque);
            }

            var assunto = $"Oficina - alerta de estoque baixo ({itensBaixoEstoque.Count} item(ns))";
            var corpo = MontarCorpoEmail(itensBaixoEstoque, quantidadeMinima, motivo);
            var emailEnviado = await EnviarEmailAsync(emailDestino, assunto, corpo, itensBaixoEstoque.Count);

            return CriarResultado(
                emailEnviado,
                emailEnviado
                    ? $"E-mail de estoque baixo enviado para {emailDestino}."
                    : "Erro ao enviar e-mail de estoque baixo. Confira Email__Password e os logs do Render.",
                quantidadeMinima,
                smtpConfigurado,
                emailDestino,
                itensBaixoEstoque);
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

            corpo.AppendLine("Ola!");
            corpo.AppendLine();
            corpo.AppendLine("Existem itens com baixa quantidade no estoque da oficina.");
            corpo.AppendLine($"Quantidade minima configurada: {quantidadeMinima}");
            corpo.AppendLine($"Motivo do disparo: {motivo}");
            corpo.AppendLine($"Data do alerta: {DateTime.Now:dd/MM/yyyy HH:mm}");
            corpo.AppendLine();
            corpo.AppendLine("Itens com estoque baixo:");
            corpo.AppendLine();

            foreach (var item in itens)
            {
                corpo.AppendLine($"- #{item.Id} | {item.Descricao} | Tipo: {item.Tipo} | Estoque: {item.Estoque} | Reservado: {item.EstoqueReservado} | Disponivel: {EstoqueDisponivel(item)} | Valor: {item.Valor:C}");
            }

            corpo.AppendLine();
            corpo.AppendLine("Acesse o sistema da oficina para repor o estoque.");

            return corpo.ToString();
        }

        private static bool ControlaEstoque(string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return true;

            var valor = tipo.Trim();

            return !valor.Equals("Servico", StringComparison.OrdinalIgnoreCase) &&
                   !valor.Equals("Serviço", StringComparison.OrdinalIgnoreCase) &&
                   !valor.Equals("ServiÃ§o", StringComparison.OrdinalIgnoreCase);
        }

        private static int EstoqueDisponivel(Item item)
        {
            return item.Estoque - item.EstoqueReservado;
        }

        private bool SmtpEstaConfigurado()
        {
            var host = _configuration["Email:SmtpHost"];
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var from = _configuration["Email:From"] ?? username;

            return !string.IsNullOrWhiteSpace(host) &&
                   !string.IsNullOrWhiteSpace(username) &&
                   !string.IsNullOrWhiteSpace(password) &&
                   !string.IsNullOrWhiteSpace(from);
        }

        private static EstoqueEmailResultado CriarResultado(
            bool emailEnviado,
            string mensagem,
            int quantidadeMinima,
            bool smtpConfigurado,
            string emailDestino,
            List<Item> itens)
        {
            return new EstoqueEmailResultado
            {
                EmailEnviado = emailEnviado,
                Mensagem = mensagem,
                QuantidadeMinima = quantidadeMinima,
                TotalItensBaixoEstoque = itens.Count,
                SmtpConfigurado = smtpConfigurado,
                EmailDestino = emailDestino,
                Itens = itens
                    .Select(item => $"#{item.Id} - {item.Descricao} - disponivel: {EstoqueDisponivel(item)}")
                    .ToList()
            };
        }

        private async Task<bool> EnviarEmailAsync(string destinatario, string assunto, string corpo, int totalItens)
        {
            var host = _configuration["Email:SmtpHost"];
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var from = _configuration["Email:From"] ?? username;
            var port = _configuration.GetValue("Email:SmtpPort", 587);
            var enableSsl = _configuration.GetValue("Email:EnableSsl", true);
            var timeoutSeconds = _configuration.GetValue("Email:SmtpTimeoutSeconds", 20);

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(from))
            {
                _logger.LogWarning("E-mail de estoque baixo nao enviado porque as configuracoes SMTP nao estao completas.");
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
                    Timeout = timeoutSeconds * 1000,
                    Credentials = new NetworkCredential(username, password)
                };

                await smtp.SendMailAsync(mensagem).WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
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
