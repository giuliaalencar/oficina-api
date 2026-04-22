using System.Text.RegularExpressions;

namespace Oficina.API.Utils
{
    public static class ValidadorDocumento
    {
        public static bool EhValido(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return false;

            documento = Regex.Replace(documento, "[^0-9]", "");

            return documento.Length == 11 || documento.Length == 14;
        }
    }
}