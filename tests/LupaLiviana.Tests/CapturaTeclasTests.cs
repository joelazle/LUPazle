using System.Windows.Forms;
using Xunit;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana.Tests;

/// <summary>Pruebas de la captura de teclas y combinaciones, sin tocar Windows.</summary>
public sealed class CapturaTeclasTests
{
    private readonly HashSet<int> _presionadas = [];
    private readonly CapturaTeclas _captura = new();

    private Func<int, bool> Leer => teclaVirtual => _presionadas.Contains(teclaVirtual);

    [Fact]
    public void SeCapturaUnaCombinacionDeModificadoresYTecla()
    {
        _captura.Iniciar(ModoCaptura.Atajo, Leer, 0);
        _presionadas.Add(VK_CONTROL);
        _presionadas.Add(VK_MENU);
        _presionadas.Add((int)Keys.Up);

        ResultadoCaptura resultado = _captura.Procesar(400);

        Assert.Equal(EstadoCaptura.CambioDetectado, resultado.Estado);
        Assert.NotNull(resultado.Atajo);
        Atajo atajo = resultado.Atajo!.Value;
        Assert.Equal(MOD_CONTROL | MOD_ALT, atajo.Modificadores);
        Assert.Equal(Keys.Up, atajo.Tecla);
        Assert.False(_captura.Activa);
    }

    [Fact]
    public void SoloPresionarModificadoresNoTerminaLaCaptura()
    {
        _captura.Iniciar(ModoCaptura.Atajo, Leer, 0);
        _presionadas.Add(VK_CONTROL);

        Assert.Equal(EstadoCaptura.Pendiente, _captura.Procesar(400).Estado);
        Assert.Equal(EstadoCaptura.Pendiente, _captura.Procesar(500).Estado);
        Assert.True(_captura.Activa);
    }

    [Fact]
    public void EscapeCancela()
    {
        _captura.Iniciar(ModoCaptura.Atajo, Leer, 0);
        _presionadas.Add(VK_ESCAPE);

        ResultadoCaptura resultado = _captura.Procesar(400);

        Assert.Equal(EstadoCaptura.Cancelada, resultado.Estado);
        Assert.False(_captura.Activa);
    }

    [Fact]
    public void ElTiempoMaximoCancelaSolo()
    {
        _captura.Iniciar(ModoCaptura.Atajo, Leer, 0);

        ResultadoCaptura resultado = _captura.Procesar(CapturaTeclas.DuracionMaximaMs + 1);

        Assert.Equal(EstadoCaptura.Cancelada, resultado.Estado);
        Assert.False(_captura.Activa);
    }

    [Fact]
    public void UnaTeclaPresionadaAntesDeEmpezarNoSeCaptura()
    {
        _presionadas.Add((int)Keys.F9);
        _captura.Iniciar(ModoCaptura.Boton, Leer, 0);

        Assert.Equal(EstadoCaptura.Pendiente, _captura.Procesar(400).Estado);
    }

    [Fact]
    public void UnaTeclaPresionadaDuranteLaEsperaSeCapturaAlTerminar()
    {
        _captura.Iniciar(ModoCaptura.Boton, Leer, 0);

        _presionadas.Add((int)Keys.F9);
        Assert.Equal(EstadoCaptura.Pendiente, _captura.Procesar(100).Estado);

        ResultadoCaptura resultado = _captura.Procesar(400);

        Assert.Equal(EstadoCaptura.CambioDetectado, resultado.Estado);
        Assert.Equal(Keys.F9, resultado.Boton!.Value.Tecla);
    }

    [Fact]
    public void UnAtajoSinModificadorAvisaPeroSigueEsperando()
    {
        _captura.Iniciar(ModoCaptura.Atajo, Leer, 0);
        _presionadas.Add((int)Keys.F9);

        ResultadoCaptura resultado = _captura.Procesar(400);

        Assert.Equal(EstadoCaptura.Error, resultado.Estado);
        Assert.Contains("modificador", resultado.Mensaje);
        Assert.True(_captura.Activa);
    }

    [Fact]
    public void ElBotonCapturaUnaTeclaSuelta()
    {
        _captura.Iniciar(ModoCaptura.Boton, Leer, 0);
        _presionadas.Add((int)Keys.F8);

        ResultadoCaptura resultado = _captura.Procesar(400);

        Assert.Equal(EstadoCaptura.CambioDetectado, resultado.Estado);
        Assert.False(resultado.Boton!.Value.EsMouse);
        Assert.Equal(Keys.F8, resultado.Boton!.Value.Tecla);
    }

    [Fact]
    public void ElBotonIgnoraUnModificadorSoloYCapturaLaTecla()
    {
        _captura.Iniciar(ModoCaptura.Boton, Leer, 0);

        _presionadas.Add(VK_CONTROL);
        Assert.Equal(EstadoCaptura.Pendiente, _captura.Procesar(400).Estado);

        _presionadas.Add((int)Keys.F9);
        ResultadoCaptura resultado = _captura.Procesar(500);

        Assert.Equal(EstadoCaptura.CambioDetectado, resultado.Estado);
        Assert.Equal(Keys.F9, resultado.Boton!.Value.Tecla);
    }

    [Fact]
    public void SiCtrlYLaTeclaSePresionanJuntosSeCapturaLaTecla()
    {
        _captura.Iniciar(ModoCaptura.Boton, Leer, 0);
        _presionadas.Add(VK_CONTROL);
        _presionadas.Add((int)Keys.F9);

        ResultadoCaptura resultado = _captura.Procesar(400);

        Assert.Equal(EstadoCaptura.CambioDetectado, resultado.Estado);
        Assert.Equal(Keys.F9, resultado.Boton!.Value.Tecla);
    }

    [Fact]
    public void ElBotonAceptaLosBotonesLateralesDelMouse()
    {
        _captura.Iniciar(ModoCaptura.Boton, Leer, 0);
        _presionadas.Add(VK_XBUTTON2);

        ResultadoCaptura resultado = _captura.Procesar(400);

        Assert.Equal(EstadoCaptura.CambioDetectado, resultado.Estado);
        Assert.True(resultado.Boton!.Value.EsMouse);
        Assert.Equal(BotonMouse.XButton2, resultado.Boton!.Value.Mouse);
    }

    [Fact]
    public void UnAtajoNoAceptaBotonesDelMouse()
    {
        _captura.Iniciar(ModoCaptura.Atajo, Leer, 0);
        _presionadas.Add(VK_XBUTTON1);

        ResultadoCaptura resultado = _captura.Procesar(400);

        Assert.Equal(EstadoCaptura.Error, resultado.Estado);
        Assert.True(_captura.Activa);
    }

    [Fact]
    public void CancelarManualDetieneLaCaptura()
    {
        _captura.Iniciar(ModoCaptura.Boton, Leer, 0);
        _presionadas.Add((int)Keys.F9);

        _captura.Cancelar();

        Assert.False(_captura.Activa);
        Assert.Equal(EstadoCaptura.Ninguno, _captura.Procesar(400).Estado);
    }
}