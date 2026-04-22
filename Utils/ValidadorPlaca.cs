using System.Text.RegularExpressions;

namespace Oficina.API.Utils
{
    public static class ValidadorPlaca
    {
        public static bool EhValida(string placa)
        {
            if (string.IsNullOrWhiteSpace(placa))
                return false;

            placa = placa.Trim().ToUpper();

            var placaAntiga = @"^[A-Z]{3}[0-9]{4}$";
            var placaMercosul = @"^[A-Z]{3}[0-9][A-Z][0-9]{2}$";

            return Regex.IsMatch(placa, placaAntiga) || Regex.IsMatch(placa, placaMercosul);
        }
    }
}