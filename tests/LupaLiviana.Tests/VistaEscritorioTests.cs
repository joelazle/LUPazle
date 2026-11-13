using Xunit;

namespace LupaLiviana.Tests;

/// <summary>
/// Pruebas del cálculo de la vista ampliada. No usan Windows: simulan uno o
/// varios monitores con rectángulos y verifican el desplazamiento resultante.
/// </summary>
public sealed class VistaEscritorioTests
{
    private static readonly RectanguloPantalla UnMonitor = new(0, 0, 2560, 1440);

    // Primaria de 2560x1440 en (0,0) y secundaria de 1920x1080 a la izquierda.
    private static readonly RectanguloPantalla EscritorioConMonitorIzquierdo = new(-1920, 0, 4480, 1440);
    private static readonly RectanguloPantalla MonitorIzquierdo = new(-1920, 0, 1920, 1080);

    // Primaria de 2560x1440 en (0,0) y secundaria de 1920x1080 a la derecha.
    private static readonly RectanguloPantalla EscritorioConMonitorDerecho = new(0, 0, 4480, 1440);
    private static readonly RectanguloPantalla MonitorDerecho = new(2560, 0, 1920, 1080);

    // Secundaria de 2560x1080 arriba de la principal.
    private static readonly RectanguloPantalla EscritorioConMonitorArriba = new(0, -1080, 2560, 2520);
    private static readonly RectanguloPantalla MonitorArriba = new(0, -1080, 2560, 1080);

    [Fact]
    public void ElCentroDeLaVistaFijaEnUnSoloMonitorEsElDeSiempre()
    {
        (int X, int Y) = CalculoVista.Desplazamiento(2f, 1280f, 720f, 1280f, 720f, UnMonitor);

        Assert.Equal(640, X);
        Assert.Equal(360, Y);
    }

    [Theory]
    [InlineData(100f, 100f, 0, 0)]
    [InlineData(1280f, 720f, 640, 360)]
    [InlineData(1700f, 800f, 1060, 440)]
    [InlineData(2500f, 1400f, 1280, 720)]
    public void ElSeguimientoEnUnMonitorDaElDesplazamientoEsperado(
        float cursorX,
        float cursorY,
        int esperadoX,
        int esperadoY)
    {
        (int X, int Y) = CalculoVista.Desplazamiento(2f, cursorX, cursorY, 1280f, 720f, UnMonitor);

        Assert.Equal(esperadoX, X);
        Assert.Equal(esperadoY, Y);
    }

    [Fact]
    public void EnUnSoloMonitorElCalculoCoincideConLaFormulaAnterior()
    {
        const float nivel = 2f;
        const int ancho = 2560;
        const int alto = 1440;
        double anchoVisible = ancho / (double)nivel;
        double altoVisible = alto / (double)nivel;

        for (int x = 0; x <= ancho; x += 80)
        {
            for (int y = 0; y <= alto; y += 80)
            {
                int esperadoX = (int)Math.Round(
                    Math.Clamp(x - (anchoVisible / 2.0), 0.0, ancho - anchoVisible));
                int esperadoY = (int)Math.Round(
                    Math.Clamp(y - (altoVisible / 2.0), 0.0, alto - altoVisible));

                (int X, int Y) = CalculoVista.Desplazamiento(
                    nivel, x, y, ancho / 2f, alto / 2f, UnMonitor);

                Assert.Equal(esperadoX, X);
                Assert.Equal(esperadoY, Y);
            }
        }
    }

    [Fact]
    public void LaVistaFijaSeCentraEnElMonitorDeLaIzquierda()
    {
        float centroX = MonitorIzquierdo.CentroX;
        float centroY = MonitorIzquierdo.CentroY;

        (int X, int Y) = CalculoVista.Desplazamiento(
            2f, centroX, centroY, centroX, centroY, EscritorioConMonitorIzquierdo);

        Assert.Equal(-960f, centroX);
        Assert.Equal(540f, centroY);
        Assert.Equal(-480, X);
        Assert.Equal(270, Y);

        // Lo que se ve en el monitor izquierdo es [X - 960, X], centrado en -960.
        float bordeIzquierdo = X + (MonitorIzquierdo.Izquierda / 2f);
        float bordeDerecho = X + (MonitorIzquierdo.Derecha / 2f);
        Assert.Equal(-1440f, bordeIzquierdo);
        Assert.Equal(-480f, bordeDerecho);
        Assert.InRange(bordeIzquierdo, (float)MonitorIzquierdo.Izquierda, (float)MonitorIzquierdo.Derecha);
        Assert.InRange(bordeDerecho, (float)MonitorIzquierdo.Izquierda, (float)MonitorIzquierdo.Derecha);
    }

    [Fact]
    public void LaVistaFijaSeCentraEnElMonitorDeLaDerecha()
    {
        float centroX = MonitorDerecho.CentroX;
        float centroY = MonitorDerecho.CentroY;

        (int X, int Y) = CalculoVista.Desplazamiento(
            2f, centroX, centroY, centroX, centroY, EscritorioConMonitorDerecho);

        Assert.Equal(3520f, centroX);
        Assert.Equal(1760, X);
        Assert.Equal(270, Y);

        float bordeIzquierdo = X + (MonitorDerecho.Izquierda / 2f);
        float bordeDerecho = X + (MonitorDerecho.Derecha / 2f);
        Assert.Equal(3040f, bordeIzquierdo);
        Assert.Equal(4000f, bordeDerecho);
        Assert.InRange(bordeIzquierdo, (float)MonitorDerecho.Izquierda, (float)MonitorDerecho.Derecha);
        Assert.InRange(bordeDerecho, (float)MonitorDerecho.Izquierda, (float)MonitorDerecho.Derecha);
    }

    [Fact]
    public void LaVistaFijaSeCentraEnElMonitorDeArriba()
    {
        float centroX = MonitorArriba.CentroX;
        float centroY = MonitorArriba.CentroY;

        (int X, int Y) = CalculoVista.Desplazamiento(
            2f, centroX, centroY, centroX, centroY, EscritorioConMonitorArriba);

        Assert.Equal(1280f, centroX);
        Assert.Equal(-540f, centroY);
        Assert.Equal(640, X);
        Assert.Equal(-270, Y);

        float bordeSuperior = Y + (MonitorArriba.Arriba / 2f);
        float bordeInferior = Y + (MonitorArriba.Abajo / 2f);
        Assert.Equal(-810f, bordeSuperior);
        Assert.Equal(-270f, bordeInferior);
        Assert.InRange(bordeSuperior, (float)MonitorArriba.Arriba, (float)MonitorArriba.Abajo);
        Assert.InRange(bordeInferior, (float)MonitorArriba.Arriba, (float)MonitorArriba.Abajo);
    }

    [Theory]
    [InlineData(-1920f, 0f)]
    [InlineData(-1920f, 1440f)]
    [InlineData(0f, 0f)]
    [InlineData(2560f, 1440f)]
    [InlineData(3000f, 2000f)]
    public void LaVistaNuncaSeSaleDelEscritorioVirtual(float pantallaX, float pantallaY)
    {
        const float nivel = 2f;
        (int X, int Y) = CalculoVista.Desplazamiento(
            nivel, pantallaX, pantallaY, pantallaX, pantallaY, EscritorioConMonitorIzquierdo);

        double factor = 1.0 - (1.0 / nivel);
        Assert.InRange((double)X, -1920.0 * factor, 2560.0 * factor);
        Assert.InRange((double)Y, 0.0, 1440.0 * factor);

        // El área visible en cada extremo del escritorio tampoco se sale.
        Assert.True(X + (-1920.0 / nivel) >= -1920.0);
        Assert.True(X + (2560.0 / nivel) <= 2560.0);
        Assert.True(Y + (0.0 / nivel) >= 0.0);
        Assert.True(Y + (1440.0 / nivel) <= 1440.0);
    }

    [Fact]
    public void ConNivelUnoNoHayDesplazamiento()
    {
        (int X, int Y) = CalculoVista.Desplazamiento(1f, 2000f, 1200f, 1280f, 720f, UnMonitor);

        Assert.Equal(0, X);
        Assert.Equal(0, Y);
    }

    [Fact]
    public void UnEscritorioInvalidoNoDesplaza()
    {
        (int X, int Y) = CalculoVista.Desplazamiento(2f, 500f, 500f, 500f, 500f, default);

        Assert.Equal(0, X);
        Assert.Equal(0, Y);
    }

    [Fact]
    public void UnNivelInvalidoNoDesplaza()
    {
        Assert.Equal((0, 0), CalculoVista.Desplazamiento(float.NaN, 500f, 500f, 500f, 500f, UnMonitor));
        Assert.Equal((0, 0), CalculoVista.Desplazamiento(float.PositiveInfinity, 500f, 500f, 500f, 500f, UnMonitor));
    }

    [Fact]
    public void ElCentroYLosBordesDelMonitorSeCalculanBien()
    {
        var monitor = new RectanguloPantalla(-1920, -200, 1920, 1080);

        Assert.Equal(-960, monitor.CentroX);
        Assert.Equal(340, monitor.CentroY);
        Assert.Equal(0, monitor.Derecha);
        Assert.Equal(880, monitor.Abajo);
        Assert.True(monitor.EsValido);
        Assert.False(default(RectanguloPantalla).EsValido);
    }
}