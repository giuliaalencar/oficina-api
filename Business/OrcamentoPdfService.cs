using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Oficina.API.DAL;
using Oficina.API.Models;

namespace Oficina.API.Business
{
    public class OrcamentoPdfService
    {
        private readonly AppDbContext _context;

        public OrcamentoPdfService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Sucesso, string? Erro, byte[]? Pdf, string? NomeArquivo)> GerarAsync(int ordemId, string? emailCliente = null)
        {
            var ordem = await _context.OrdensServico
                .AsNoTracking()
                .Include(o => o.Veiculo)
                .ThenInclude(v => v!.Cliente)
                .Include(o => o.Itens)
                .ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(o => o.Id == ordemId);

            if (ordem == null)
                return (false, "Ordem de serviço não encontrada.", null, null);

            if (!string.IsNullOrWhiteSpace(emailCliente) &&
                !string.Equals(ordem.Veiculo?.Cliente?.Email, emailCliente, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Você não tem permissão para acessar este orçamento.", null, null);
            }

            var linhas = MontarLinhas(ordem);
            var pdf = CriarPdf("Orcamento da Ordem de Servico", linhas);
            var nomeArquivo = $"orcamento-os-{ordem.Id}.pdf";

            return (true, null, pdf, nomeArquivo);
        }

        private static List<string> MontarLinhas(OrdemServico ordem)
        {
            var cultura = new CultureInfo("pt-BR");
            var veiculo = ordem.Veiculo;
            var cliente = veiculo?.Cliente;
            var linhas = new List<string>
            {
                $"Ordem de Servico: #{ordem.Id}",
                $"Data: {ordem.DataEntrada:dd/MM/yyyy HH:mm}",
                $"Status: {ordem.Status}",
                "",
                "Cliente",
                $"Nome: {cliente?.Nome ?? "Nao informado"}",
                $"Email: {cliente?.Email ?? "Nao informado"}",
                $"Telefone: {cliente?.Telefone ?? "Nao informado"}",
                $"CPF/CNPJ: {cliente?.CpfCnpj ?? "Nao informado"}",
                "",
                "Veículo",
                $"Placa: {veiculo?.Placa ?? "Nao informado"}",
                $"Marca/Modelo: {veiculo?.Marca ?? "Nao informado"} {veiculo?.Modelo ?? string.Empty}".Trim(),
                $"Ano: {veiculo?.Ano.ToString() ?? "Nao informado"}",
                "",
                "Itens do Orçamento"
            };

            if (ordem.Itens.Count == 0)
            {
                linhas.Add("Nenhum item adicionado.");
            }
            else
            {
                foreach (var item in ordem.Itens)
                {
                    var descricao = item.Item?.Descricao ?? $"Item #{item.ItemId}";
                    var totalItem = item.Quantidade * item.Valor;
                    linhas.Add($"{descricao} | Qtd: {item.Quantidade} | Unitario: {item.Valor.ToString("C", cultura)} | Total: {totalItem.ToString("C", cultura)}");
                }
            }

            linhas.Add("");
            linhas.Add($"Valor total: {ordem.ValorTotal.ToString("C", cultura)}");
            linhas.Add("");
            linhas.Add("Documento gerado automaticamente pelo sistema Oficina.");

            return linhas;
        }

        private static byte[] CriarPdf(string titulo, IReadOnlyList<string> linhas)
        {
            var conteudo = new StringBuilder();
            conteudo.AppendLine("BT");
            conteudo.AppendLine("/F2 20 Tf");
            conteudo.AppendLine($"1 0 0 1 50 795 Tm ({Escapar(titulo)}) Tj");
            conteudo.AppendLine("/F1 11 Tf");

            var y = 760;
            foreach (var linhaOriginal in linhas.SelectMany(QuebrarLinha))
            {
                if (y < 60)
                    break;

                if (string.IsNullOrWhiteSpace(linhaOriginal))
                {
                    y -= 16;
                    continue;
                }

                conteudo.AppendLine($"1 0 0 1 50 {y} Tm ({Escapar(linhaOriginal)}) Tj");
                y -= 16;
            }

            conteudo.AppendLine("ET");

            var stream = conteudo.ToString();
            var objetos = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream"
            };

            var pdf = new StringBuilder();
            var offsets = new List<int> { 0 };

            pdf.AppendLine("%PDF-1.4");

            for (var i = 0; i < objetos.Count; i++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
                pdf.AppendLine($"{i + 1} 0 obj");
                pdf.AppendLine(objetos[i]);
                pdf.AppendLine("endobj");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
            pdf.AppendLine("xref");
            pdf.AppendLine($"0 {objetos.Count + 1}");
            pdf.AppendLine("0000000000 65535 f ");

            foreach (var offset in offsets.Skip(1))
                pdf.AppendLine($"{offset:D10} 00000 n ");

            pdf.AppendLine("trailer");
            pdf.AppendLine($"<< /Size {objetos.Count + 1} /Root 1 0 R >>");
            pdf.AppendLine("startxref");
            pdf.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
            pdf.AppendLine("%%EOF");

            return Encoding.ASCII.GetBytes(pdf.ToString());
        }

        private static IEnumerable<string> QuebrarLinha(string linha)
        {
            var texto = linha;
            const int limite = 95;

            if (texto.Length <= limite)
            {
                yield return texto;
                yield break;
            }

            for (var i = 0; i < texto.Length; i += limite)
                yield return texto.Substring(i, Math.Min(limite, texto.Length - i));
        }

        private static string Escapar(string texto)
        {
            var resultado = new StringBuilder();

            foreach (var caractere in texto)
            {
                resultado.Append(caractere switch
                {
                    '\\' => "\\\\",
                    '(' => "\\(",
                    ')' => "\\)",
                    'á' => "\\341",
                    'à' => "\\340",
                    'ã' => "\\343",
                    'â' => "\\342",
                    'Á' => "\\301",
                    'À' => "\\300",
                    'Ã' => "\\303",
                    'Â' => "\\302",
                    'é' => "\\351",
                    'ê' => "\\352",
                    'É' => "\\311",
                    'Ê' => "\\312",
                    'í' => "\\355",
                    'Í' => "\\315",
                    'ó' => "\\363",
                    'õ' => "\\365",
                    'ô' => "\\364",
                    'Ó' => "\\323",
                    'Õ' => "\\325",
                    'Ô' => "\\324",
                    'ú' => "\\372",
                    'Ú' => "\\332",
                    'ç' => "\\347",
                    'Ç' => "\\307",
                    _ when caractere >= 32 && caractere <= 126 => caractere.ToString(),
                    _ => string.Empty
                });
            }

            return resultado.ToString();
        }
    }
}
