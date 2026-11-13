namespace LupaLiviana;

/// <summary>Modo actual de la lupa.</summary>
internal enum ModoLupa
{
    /// <summary>Sin zoom (pantalla normal).</summary>
    Apagado,

    /// <summary>El zoom sigue al cursor del mouse mientras el botón está presionado.</summary>
    Siguiendo,

    /// <summary>El zoom quedó fijo en el centro de la pantalla.</summary>
    Fijo,
}

/// <summary>Acción que debe ejecutar la aplicación como consecuencia de un cambio de estado.</summary>
internal enum EventoBoton
{
    Ninguno,
    EmpezarSeguir,
    FijarCentro,
    Apagar,
}

/// <summary>
/// Máquina de estados del único botón de zoom.
///
/// Reglas:
/// <list type="bullet">
/// <item>Apagado + presionar → empieza a seguir al mouse de inmediato.</item>
/// <item>Siguiendo + soltar antes del umbral → queda fijo en el centro.</item>
/// <item>Siguiendo + soltar después del umbral → se apaga.</item>
/// <item>Fijo + presionar → se apaga y se ignora el soltar que sigue.</item>
/// </list>
///
/// No accede a Windows: recibe el estado del botón y el reloj desde afuera,
/// lo que permite probarla con pruebas unitarias.
/// </summary>
internal sealed class ButtonStateMachine
{
    private bool _presionadoAntes;
    private long _inicioPresionMs;

    /// <summary>Modo actual de la lupa.</summary>
    public ModoLupa Modo { get; private set; } = ModoLupa.Apagado;

    /// <summary>
    /// Procesa una lectura del botón y devuelve la acción a ejecutar.
    /// Es seguro llamarla muchas veces por segundo: la repetición automática
    /// del teclado no genera eventos nuevos porque solo se miran los cambios
    /// de estado (flanco de subida y de bajada).
    /// </summary>
    /// <param name="presionado">Estado físico actual del botón.</param>
    /// <param name="ahoraMs">Reloj monótono en milisegundos.</param>
    /// <param name="umbralMs">Duración a partir de la cual la pulsación se considera "mantenida".</param>
    public EventoBoton Actualizar(bool presionado, long ahoraMs, int umbralMs)
    {
        if (presionado)
        {
            if (_presionadoAntes)
            {
                // Sigue presionado (posible repetición automática del teclado).
                return EventoBoton.Ninguno;
            }

            _presionadoAntes = true;
            _inicioPresionMs = ahoraMs;

            if (Modo == ModoLupa.Fijo)
            {
                // Estaba fijo: un toque lo apaga y el soltar posterior se ignora.
                Modo = ModoLupa.Apagado;
                return EventoBoton.Apagar;
            }

            Modo = ModoLupa.Siguiendo;
            return EventoBoton.EmpezarSeguir;
        }

        if (!_presionadoAntes)
        {
            return EventoBoton.Ninguno;
        }

        _presionadoAntes = false;

        if (Modo != ModoLupa.Siguiendo)
        {
            // Se había apagado en el evento de presionar: el soltar no hace nada.
            return EventoBoton.Ninguno;
        }

        long duracion = ahoraMs - _inicioPresionMs;
        if (duracion < umbralMs)
        {
            Modo = ModoLupa.Fijo;
            return EventoBoton.FijarCentro;
        }

        Modo = ModoLupa.Apagado;
        return EventoBoton.Apagar;
    }

    /// <summary>Vuelve al estado inicial sin generar ninguna acción.</summary>
    public void Reiniciar()
    {
        _presionadoAntes = false;
        _inicioPresionMs = 0;
        Modo = ModoLupa.Apagado;
    }
}