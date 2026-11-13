using System.Windows.Forms;
using Xunit;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana.Tests;

/// <summary>Pruebas del conjunto de teclas configurable y sus validaciones.</summary>
public sealed class ConfigTeclasTests
{
    [Fact]
    public void LosValoresPorDefectoSonF9YLosDosAtajosDeNivel()
    {
        ConfigTeclas teclas = ConfigTeclas.PorDefecto;

        Assert.Equal(Keys.F9, teclas.Boton.Tecla);
        Assert.Equal(Atajo.DefectoSubir, teclas.Subir);
        Assert.Equal(Atajo.DefectoBajar, teclas.Bajar);
        Assert.False(teclas.Activar.EsValido);
        Assert.False(teclas.SiguienteFiltro.EsValido);
        Assert.Null(teclas.Validar());
    }

    [Fact]
    public void SeLeeYSeEscribeLaConfiguracionSinPerderValores()
    {
        var config = new Config
        {
            BotonZoom = "MouseXButton1",
            AtajoActivar = "Ctrl+Alt+F8",
            AtajoSubirZoom = "Ctrl+Shift+F1",
            AtajoBajarZoom = "",
            AtajoSiguienteFiltro = "Alt+F7",
        };

        ConfigTeclas teclas = ConfigTeclas.Desde(config);
        var salida = new Config();
        teclas.AplicarA(salida);

        Assert.Equal(BotonMouse.XButton1, teclas.Boton.Mouse);
        Assert.Equal("MouseXButton1", salida.BotonZoom);
        Assert.Equal("Ctrl+Alt+F8", salida.AtajoActivar);
        Assert.Equal("Ctrl+Shift+F1", salida.AtajoSubirZoom);
        Assert.Equal("", salida.AtajoBajarZoom);
        Assert.Equal("Alt+F7", salida.AtajoSiguienteFiltro);
    }

    [Fact]
    public void UnAtajoRepetidoEsUnError()
    {
        var teclas = ConfigTeclas.PorDefecto with
        {
            Bajar = Atajo.DefectoSubir,
        };

        string? error = teclas.Validar();

        Assert.NotNull(error);
        Assert.Contains("asignado", error);
        Assert.Contains("Ctrl + Alt + Flecha arriba", error);
    }

    [Fact]
    public void LosAtajosDeshabilitadosNoCuentanComoRepetidos()
    {
        var teclas = ConfigTeclas.PorDefecto with
        {
            Activar = Atajo.Ninguno,
            SiguienteFiltro = Atajo.Ninguno,
        };

        Assert.Null(teclas.Validar());
    }

    [Fact]
    public void AvisaSiElAtajoIncluyeLaTeclaDelBotonDeZoom()
    {
        var teclas = ConfigTeclas.PorDefecto with
        {
            Subir = new Atajo(MOD_ALT, Keys.F9),
        };

        IReadOnlyList<string> avisos = teclas.Advertencias();

        Assert.Single(avisos);
        Assert.Contains("Alt + F9", avisos[0]);
        Assert.Contains("F9", avisos[0]);
    }

    [Fact]
    public void NoAvisaSiElBotonDeZoomEsDelMouse()
    {
        var teclas = ConfigTeclas.PorDefecto with
        {
            Boton = new BotonZoom(Keys.None, BotonMouse.XButton1),
            Subir = new Atajo(MOD_ALT, Keys.F9),
        };

        Assert.Empty(teclas.Advertencias());
    }

    [Fact]
    public void NoAvisaSiLasTeclasSonDistintas()
    {
        Assert.Empty(ConfigTeclas.PorDefecto.Advertencias());
    }
}