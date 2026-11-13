using Xunit;

namespace LupaLiviana.Tests;

/// <summary>Pruebas de lectura y escritura de la configuración en disco.</summary>
public sealed class ConfigServiceTests : IDisposable
{
    private readonly string _carpeta;

    public ConfigServiceTests()
    {
        _carpeta = Path.Combine(Path.GetTempPath(), "LupaLivianaTests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void SiElArchivoNoExisteSeUsanLosValoresPorDefectoYSeCreaElArchivo()
    {
        var servicio = new ConfigService(_carpeta);

        Config config = servicio.Cargar();

        Assert.Equal(2.0, config.NivelZoom);
        Assert.Equal("F9", config.BotonZoom);
        Assert.Equal(250, config.UmbralToqueMs);
        Assert.Equal(NivelSuavizado.Bajo, config.Suavizado);
        Assert.Equal(FiltroColor.Ninguno, config.Filtro);
        Assert.Equal("Ctrl+Alt+Up", config.AtajoSubirZoom);
        Assert.Equal("Ctrl+Alt+Down", config.AtajoBajarZoom);
        Assert.Equal("", config.AtajoActivar);
        Assert.Equal("", config.AtajoSiguienteFiltro);
        Assert.True(config.Activado);
        Assert.True(File.Exists(servicio.RutaArchivo));
    }

    [Fact]
    public void LaConfiguracionSeGuardaYSeVuelveALeerIgual()
    {
        var servicio = new ConfigService(_carpeta);
        var original = new Config
        {
            NivelZoom = 4.0,
            BotonZoom = "MouseXButton2",
            UmbralToqueMs = 150,
            Suavizado = NivelSuavizado.Alto,
            Filtro = FiltroColor.AltoContraste,
            AtajoSubirZoom = "Ctrl+Shift+F1",
            AtajoBajarZoom = "Win+Down",
            AtajoActivar = "Ctrl+Alt+F8",
            AtajoSiguienteFiltro = "Ctrl+Alt+F7",
            IniciarConWindows = true,
            Activado = false,
        };

        Assert.True(servicio.Guardar(original));

        Config recuperada = servicio.Cargar();

        Assert.Equal(4.0, recuperada.NivelZoom);
        Assert.Equal("MouseXButton2", recuperada.BotonZoom);
        Assert.Equal(150, recuperada.UmbralToqueMs);
        Assert.Equal(NivelSuavizado.Alto, recuperada.Suavizado);
        Assert.Equal(FiltroColor.AltoContraste, recuperada.Filtro);
        Assert.Equal("Ctrl+Shift+F1", recuperada.AtajoSubirZoom);
        Assert.Equal("Win+Down", recuperada.AtajoBajarZoom);
        Assert.Equal("Ctrl+Alt+F8", recuperada.AtajoActivar);
        Assert.Equal("Ctrl+Alt+F7", recuperada.AtajoSiguienteFiltro);
        Assert.True(recuperada.IniciarConWindows);
        Assert.False(recuperada.Activado);
    }

    [Fact]
    public void UnArchivoCorruptoDevuelveLosValoresPorDefectoYGuardaUnaCopia()
    {
        var servicio = new ConfigService(_carpeta);
        Directory.CreateDirectory(_carpeta);
        File.WriteAllText(servicio.RutaArchivo, "{ esto no es un json válido ");

        Config config = servicio.Cargar();

        Assert.Equal(2.0, config.NivelZoom);
        Assert.Equal("F9", config.BotonZoom);
        Assert.True(File.Exists(servicio.RutaArchivo + ".roto"));
    }

    [Fact]
    public void LosValoresFueraDeRangoSeCorrigen()
    {
        var config = new Config
        {
            NivelZoom = 50,
            UmbralToqueMs = -10,
            BotonZoom = "no existe",
            AtajoSubirZoom = "sin modificador",
            AtajoBajarZoom = "sin modificador",
        };

        config.Normalizar();

        Assert.Equal(8.0, config.NivelZoom);
        Assert.Equal(80, config.UmbralToqueMs);
        Assert.Equal("F9", config.BotonZoom);
        Assert.Equal("Ctrl+Alt+Up", config.AtajoSubirZoom);
        Assert.Equal("Ctrl+Alt+Down", config.AtajoBajarZoom);
    }

    [Fact]
    public void UnAtajoVacioQuedaDeshabilitado()
    {
        var config = new Config
        {
            AtajoSubirZoom = "",
            AtajoBajarZoom = "   ",
            AtajoActivar = "",
            AtajoSiguienteFiltro = "",
        };

        config.Normalizar();

        Assert.Equal("", config.AtajoSubirZoom);
        Assert.Equal("", config.AtajoBajarZoom);
        Assert.Equal("", config.AtajoActivar);
        Assert.Equal("", config.AtajoSiguienteFiltro);
    }

    [Fact]
    public void LosAtajosValidosSeGuardanEnFormaCanonica()
    {
        var config = new Config
        {
            AtajoSubirZoom = "ctrl+shift+f1",
            AtajoBajarZoom = "ALT + Win + Down",
            AtajoActivar = "control+alt+f8",
            AtajoSiguienteFiltro = "Ctrl+Alt+F7",
        };

        config.Normalizar();

        Assert.Equal("Ctrl+Shift+F1", config.AtajoSubirZoom);
        Assert.Equal("Alt+Win+Down", config.AtajoBajarZoom);
        Assert.Equal("Ctrl+Alt+F8", config.AtajoActivar);
        Assert.Equal("Ctrl+Alt+F7", config.AtajoSiguienteFiltro);
    }

    [Fact]
    public void LosAtajosOpcionalesInvalidosQuedanVacios()
    {
        var config = new Config
        {
            AtajoActivar = "F9",
            AtajoSiguienteFiltro = "Ctrl+Alt",
        };

        config.Normalizar();

        Assert.Equal("", config.AtajoActivar);
        Assert.Equal("", config.AtajoSiguienteFiltro);
    }

    [Fact]
    public void UnNivelDeZoomInvalidoSeReemplazaPorElValorPorDefecto()
    {
        var config = new Config { NivelZoom = double.NaN };

        config.Normalizar();

        Assert.Equal(2.0, config.NivelZoom);
    }

    [Fact]
    public void LosValoresDeEnumEnMinusculasTambienSeAceptan()
    {
        var servicio = new ConfigService(_carpeta);
        Directory.CreateDirectory(_carpeta);
        File.WriteAllText(
            servicio.RutaArchivo,
            "{ \"nivelZoom\": 3, \"suavizado\": \"alto\", \"filtro\": \"invertir\", \"botonZoom\": \"F7\" }");

        Config config = servicio.Cargar();

        Assert.Equal(3.0, config.NivelZoom);
        Assert.Equal(NivelSuavizado.Alto, config.Suavizado);
        Assert.Equal(FiltroColor.Invertir, config.Filtro);
        Assert.Equal("F7", config.BotonZoom);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_carpeta))
            {
                Directory.Delete(_carpeta, recursive: true);
            }
        }
        catch (IOException)
        {
            // La carpeta temporal se puede borrar en otro momento.
        }
    }
}
