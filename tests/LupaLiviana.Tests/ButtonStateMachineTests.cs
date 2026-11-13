using Xunit;

namespace LupaLiviana.Tests;

/// <summary>
/// Pruebas de la máquina de estados del botón de zoom. No tocan Windows:
/// se le pasa el estado del botón y un reloj simulado.
/// </summary>
public sealed class ButtonStateMachineTests
{
    private const int Umbral = 250;

    [Fact]
    public void PrimerPresionActivaElSeguimientoDeInmediato()
    {
        var maquina = new ButtonStateMachine();

        Assert.Equal(EventoBoton.EmpezarSeguir, maquina.Actualizar(true, 1000, Umbral));
        Assert.Equal(ModoLupa.Siguiendo, maquina.Modo);
    }

    [Fact]
    public void ToqueCortoDejaElZoomFijoAlCentro()
    {
        var maquina = new ButtonStateMachine();

        maquina.Actualizar(true, 1000, Umbral);
        EventoBoton evento = maquina.Actualizar(false, 1100, Umbral);

        Assert.Equal(EventoBoton.FijarCentro, evento);
        Assert.Equal(ModoLupa.Fijo, maquina.Modo);
    }

    [Fact]
    public void PulsacionMantenidaApagaElZoomAlSoltar()
    {
        var maquina = new ButtonStateMachine();

        maquina.Actualizar(true, 1000, Umbral);
        EventoBoton evento = maquina.Actualizar(false, 1250, Umbral);

        Assert.Equal(EventoBoton.Apagar, evento);
        Assert.Equal(ModoLupa.Apagado, maquina.Modo);
    }

    [Fact]
    public void TocarEstandoFijoApagaElZoomEnElEventoDePresionar()
    {
        var maquina = new ButtonStateMachine();
        maquina.Actualizar(true, 0, Umbral);
        maquina.Actualizar(false, 50, Umbral);
        Assert.Equal(ModoLupa.Fijo, maquina.Modo);

        Assert.Equal(EventoBoton.Apagar, maquina.Actualizar(true, 5000, Umbral));
        Assert.Equal(ModoLupa.Apagado, maquina.Modo);

        // El soltar correspondiente se ignora.
        Assert.Equal(EventoBoton.Ninguno, maquina.Actualizar(false, 5050, Umbral));
        Assert.Equal(ModoLupa.Apagado, maquina.Modo);
    }

    [Fact]
    public void RepeticionAutomaticaDeTeclaNoGeneraEventosExtra()
    {
        var maquina = new ButtonStateMachine();

        Assert.Equal(EventoBoton.EmpezarSeguir, maquina.Actualizar(true, 0, Umbral));

        for (long tiempo = 10; tiempo < 200; tiempo += 10)
        {
            Assert.Equal(EventoBoton.Ninguno, maquina.Actualizar(true, tiempo, Umbral));
        }

        Assert.Equal(EventoBoton.FijarCentro, maquina.Actualizar(false, 200, Umbral));
    }

    [Fact]
    public void LaRepeticionAutomaticaNoAcortaLaDuracionDeLaPulsacion()
    {
        var maquina = new ButtonStateMachine();

        maquina.Actualizar(true, 0, Umbral);
        for (long tiempo = 10; tiempo <= 400; tiempo += 10)
        {
            maquina.Actualizar(true, tiempo, Umbral);
        }

        Assert.Equal(EventoBoton.Apagar, maquina.Actualizar(false, 401, Umbral));
        Assert.Equal(ModoLupa.Apagado, maquina.Modo);
    }

    [Fact]
    public void SoltarSinHaberPresionadoNoHaceNada()
    {
        var maquina = new ButtonStateMachine();

        Assert.Equal(EventoBoton.Ninguno, maquina.Actualizar(false, 100, Umbral));
        Assert.Equal(ModoLupa.Apagado, maquina.Modo);
    }

    [Fact]
    public void DespuesDeApagarUnaNuevaPulsacionVuelveAActivarElSeguimiento()
    {
        var maquina = new ButtonStateMachine();

        maquina.Actualizar(true, 0, Umbral);
        maquina.Actualizar(false, 500, Umbral);
        Assert.Equal(ModoLupa.Apagado, maquina.Modo);

        Assert.Equal(EventoBoton.EmpezarSeguir, maquina.Actualizar(true, 1000, Umbral));
        Assert.Equal(ModoLupa.Siguiendo, maquina.Modo);
    }

    [Fact]
    public void ReiniciarVuelveAlEstadoApagado()
    {
        var maquina = new ButtonStateMachine();
        maquina.Actualizar(true, 0, Umbral);
        Assert.Equal(ModoLupa.Siguiendo, maquina.Modo);

        maquina.Reiniciar();

        Assert.Equal(ModoLupa.Apagado, maquina.Modo);
        Assert.Equal(EventoBoton.Ninguno, maquina.Actualizar(false, 100, Umbral));
    }

    [Theory]
    [InlineData(249, "FijarCentro")]
    [InlineData(250, "Apagar")]
    [InlineData(400, "Apagar")]
    public void ElUmbralSeparaElToqueCortoDeLaPulsacionMantenida(long duracion, string esperado)
    {
        var maquina = new ButtonStateMachine();
        maquina.Actualizar(true, 0, Umbral);

        Assert.Equal(Enum.Parse<EventoBoton>(esperado), maquina.Actualizar(false, duracion, Umbral));
    }
}
