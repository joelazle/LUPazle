using Xunit;

namespace LupaLiviana.Tests;

/// <summary>Pruebas del botón de zoom y de los atajos globales.</summary>
public sealed class BotonYAtajosTests
{
    [Theory]
    [InlineData("F9", "F9")]
    [InlineData("f9", "F9")]
    [InlineData("Space", "Espacio")]
    [InlineData("Oemtilde", "Ñ / ~")]
    [InlineData("D5", "5")]
    public void LasTeclasSeLeenDesdeElTextoDeConfiguracion(string texto, string nombreVisible)
    {
        Assert.True(BotonZoom.TryParse(texto, out BotonZoom boton));
        Assert.False(boton.EsMouse);
        Assert.Equal(nombreVisible, boton.NombreVisible);
    }

    [Theory]
    [InlineData("MouseXButton1", "XButton1")]
    [InlineData("xbutton2", "XButton2")]
    [InlineData("mouse4", "XButton1")]
    [InlineData("Mouse5", "XButton2")]
    public void LosBotonesLateralesDelMouseSeLeenDesdeElTexto(string texto, string esperado)
    {
        Assert.True(BotonZoom.TryParse(texto, out BotonZoom boton));
        Assert.True(boton.EsMouse);
        Assert.Equal(Enum.Parse<BotonMouse>(esperado), boton.Mouse);
    }

    [Fact]
    public void ElBotonSeGuardaConUnIdentificadorQueSePuedeVolverALeer()
    {
        var original = new BotonZoom(System.Windows.Forms.Keys.F8, BotonMouse.Ninguno);

        Assert.True(BotonZoom.TryParse(original.Id, out BotonZoom recuperado));
        Assert.Equal(original, recuperado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tecla inexistente")]
    [InlineData("None")]
    public void UnTextoInvalidoNoProduceBoton(string texto)
    {
        Assert.False(BotonZoom.TryParse(texto, out _));
    }

    [Fact]
    public void ElBotonPorDefectoEsF9()
    {
        Assert.Equal(System.Windows.Forms.Keys.F9, BotonZoom.PorDefecto.Tecla);
        Assert.False(BotonZoom.PorDefecto.EsMouse);
    }

    [Theory]
    [InlineData("Ctrl+Alt+Up", 0x0002u | 0x0001u, System.Windows.Forms.Keys.Up)]
    [InlineData("Ctrl+Alt+Arriba", 0x0002u | 0x0001u, System.Windows.Forms.Keys.Up)]
    [InlineData("ctrl+shift+f1", 0x0002u | 0x0004u, System.Windows.Forms.Keys.F1)]
    [InlineData("Win+Down", 0x0008u, System.Windows.Forms.Keys.Down)]
    public void LosAtajosSeLeenDesdeElTexto(string texto, uint modificadores, System.Windows.Forms.Keys tecla)
    {
        Assert.True(Atajo.TryParse(texto, out Atajo atajo));
        Assert.Equal(modificadores, atajo.Modificadores);
        Assert.Equal(tecla, atajo.Tecla);
        Assert.True(atajo.EsValido);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Up")]
    [InlineData("Ctrl")]
    [InlineData("Ctrl+Alt")]
    public void UnAtajoInvalidoSeRechaza(string texto)
    {
        Assert.False(Atajo.TryParse(texto, out _));
    }

    [Theory]
    [InlineData("Ctrl+Alt+Up", 0x0002u | 0x0001u, System.Windows.Forms.Keys.Up)]
    [InlineData("Ctrl+Shift+F1", 0x0002u | 0x0004u, System.Windows.Forms.Keys.F1)]
    [InlineData("Alt+Win+Down", 0x0001u | 0x0008u, System.Windows.Forms.Keys.Down)]
    [InlineData("Ctrl+Alt+Shift+Win+F12", 0x000Fu, System.Windows.Forms.Keys.F12)]
    public void ElAtajoSeEscribeEnFormaCanonica(string texto, uint modificadores, System.Windows.Forms.Keys tecla)
    {
        var atajo = new Atajo(modificadores, tecla);

        Assert.Equal(texto, atajo.HaciaTexto());
        Assert.True(Atajo.TryParse(atajo.HaciaTexto(), out Atajo recuperado));
        Assert.Equal(atajo, recuperado);
    }

    [Fact]
    public void ElNombreVisibleDelAtajoEsEnEspanol()
    {
        var atajo = new Atajo(0x0002u | 0x0001u, System.Windows.Forms.Keys.Up);

        Assert.Equal("Ctrl + Alt + Flecha arriba", atajo.NombreVisible);
        Assert.Equal("Sin atajo", Atajo.Ninguno.NombreVisible);
        Assert.Equal("", Atajo.Ninguno.HaciaTexto());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UnAtajoOpcionalVacioEsValidoYQuedaDeshabilitado(string texto)
    {
        Assert.True(Atajo.TryParseOpcional(texto, out Atajo atajo));
        Assert.False(atajo.EsValido);
        Assert.Equal(Atajo.Ninguno, atajo);
    }

    [Fact]
    public void UnAtajoOpcionalConFormatoInvalidoSeRechaza()
    {
        Assert.False(Atajo.TryParseOpcional("F9", out _));
        Assert.False(Atajo.TryParseOpcional("Ctrl+Alt", out _));
    }
}
