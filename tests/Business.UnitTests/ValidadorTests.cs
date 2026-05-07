using Oficina.API.Utils;

namespace Business.UnitTests
{
    public class ValidadorTests
    {
        [Theory]
        [InlineData("12345678901")]
        [InlineData("123.456.789-01")]
        [InlineData("12345678901234")]
        [InlineData("12.345.678/0001-99")]
        public void ValidadorDocumento_DeveRetornarTrue_QuandoDocumentoTiverTamanhoValido(string documento)
        {
            var valido = ValidadorDocumento.EhValido(documento);

            Assert.True(valido);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123")]
        [InlineData("123456789012345")]
        public void ValidadorDocumento_DeveRetornarFalse_QuandoDocumentoForInvalido(string? documento)
        {
            var valido = ValidadorDocumento.EhValido(documento!);

            Assert.False(valido);
        }

        [Theory]
        [InlineData("ABC1234")]
        [InlineData("ABC1D23")]
        [InlineData(" abc1d23 ")]
        public void ValidadorPlaca_DeveRetornarTrue_QuandoPlacaForValida(string placa)
        {
            var valido = ValidadorPlaca.EhValida(placa);

            Assert.True(valido);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("1234567")]
        [InlineData("ABCD123")]
        [InlineData("ABC12D3")]
        public void ValidadorPlaca_DeveRetornarFalse_QuandoPlacaForInvalida(string? placa)
        {
            var valido = ValidadorPlaca.EhValida(placa!);

            Assert.False(valido);
        }
    }
}
