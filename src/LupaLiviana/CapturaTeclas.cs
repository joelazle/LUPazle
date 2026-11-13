using System.Windows.Forms;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana;

/// <summary>Qué se está capturando: una tecla suelta o una combinación de atajo.</summary>
internal enum ModoCaptura
{
    /// <summary>Una tecla suelta o un botón lateral del mouse (botón de zoom).</summary>
    Boton,

    /// <summary>Modificadores más una tecla (atajo global).</summary>
    Atajo,
}

/// <summary>Resultado de una lectura de la captura.</summary>
internal enum EstadoCaptura
{
    /// <summary>La captura no está activa.</summary>
    Ninguno,

    /// <summary>Sigue esperando una tecla.</summary>
    Pendiente,

    /// <summary>Se detectó un valor aceptable.</summary>
    CambioDetectado,

    /// <summary>Se canceló por Escape, por tiempo o manualmente.</summary>
    Cancelada,

    /// <summary>Se presionó algo que no sirve, pero la captura continúa.</summary>
    Error,
}

/// <summary>Resultado concreto de una lectura de <see cref="CapturaTeclas"/>.</summary>
internal readonly record struct ResultadoCaptura(
    EstadoCaptura Estado,
    BotonZoom? Boton,
    Atajo? Atajo,
    string Mensaje)
{
    public static ResultadoCaptura Nada => new(EstadoCaptura.Ninguno, null, null, "");
    public static ResultadoCaptura Pendiente => new(EstadoCaptura.Pendiente, null, null, "");
    public static ResultadoCaptura Cambio(BotonZoom boton) => new(EstadoCaptura.CambioDetectado, boton, null, "");
    public static ResultadoCaptura Cambio(Atajo atajo) => new(EstadoCaptura.CambioDetectado, null, atajo, "");
    public static ResultadoCaptura Cancelada(string mensaje) => new(EstadoCaptura.Cancelada, null, null, mensaje);
    public static ResultadoCaptura Fallo(string mensaje) => new(EstadoCaptura.Error, null, null, mensaje);
}

/// <summary>
/// Captura la próxima tecla o combinación que presione el usuario consultando
/// el estado del teclado (GetAsyncKeyState), sin instalar hooks.
///
/// No accede a Windows directamente: recibe la función de lectura y el reloj
/// desde afuera, lo que permite probarla con pruebas unitarias.
/// </summary>
internal sealed class CapturaTeclas
{
    /// <summary>Se ignora este primer instante para no capturar el click del menú.</summary>
    public const int EsperaInicialMs = 350;

    /// <summary>Tiempo máximo antes de cancelar sola.</summary>
    public const int DuracionMaximaMs = 10_000;

    private readonly bool[] _antes = new bool[256];
    private Func<int, bool>? _leer;
    private long _inicioMs;

    /// <summary>Indica si hay una captura en curso.</summary>
    public bool Activa { get; private set; }

    /// <summary>Modo de la captura en curso.</summary>
    public ModoCaptura Modo { get; private set; }

    /// <summary>Empieza a capturar y guarda el estado inicial del teclado y el mouse.</summary>
    public void Iniciar(ModoCaptura modo, Func<int, bool> estaPresionada, long ahoraMs)
    {
        Modo = modo;
        _leer = estaPresionada;
        _inicioMs = ahoraMs;
        Activa = true;

        for (int teclaVirtual = 0; teclaVirtual < _antes.Length; teclaVirtual++)
        {
            _antes[teclaVirtual] = estaPresionada(teclaVirtual);
        }
    }

    /// <summary>Cancela la captura sin devolver ningún valor.</summary>
    public void Cancelar()
    {
        Activa = false;
        _leer = null;
    }

    /// <summary>Procesa una lectura nueva del teclado.</summary>
    public ResultadoCaptura Procesar(long ahoraMs)
    {
        if (!Activa || _leer is null)
        {
            return ResultadoCaptura.Nada;
        }

        long transcurrido = ahoraMs - _inicioMs;

        if (transcurrido < EsperaInicialMs)
        {
            // Todavía no se actualiza el estado anterior: si el usuario ya tenía
            // apretada la tecla, se captura al terminar la espera.
            return ResultadoCaptura.Pendiente;
        }

        if (transcurrido > DuracionMaximaMs)
        {
            Activa = false;
            return ResultadoCaptura.Cancelada("Se acabó el tiempo para elegir la tecla.");
        }

        int nuevo = 0;

        // Se actualizan todos los estados aunque se corte la captura, para no
        // volver a detectar la misma tecla en la próxima lectura. Los
        // modificadores solos no cuentan como tecla principal, así Ctrl+F9
        // captura F9 aunque Ctrl se haya presionado en el mismo instante.
        for (int teclaVirtual = 1; teclaVirtual < _antes.Length; teclaVirtual++)
        {
            bool actual = _leer(teclaVirtual);

            if (actual && !_antes[teclaVirtual] && nuevo == 0 && !Teclas.EsModificador(teclaVirtual))
            {
                nuevo = teclaVirtual;
            }

            _antes[teclaVirtual] = actual;
        }

        if (nuevo == 0)
        {
            return ResultadoCaptura.Pendiente;
        }

        if (nuevo == VK_ESCAPE)
        {
            Activa = false;
            return ResultadoCaptura.Cancelada("No se cambió la tecla.");
        }

        if (Modo == ModoCaptura.Boton)
        {
            if (nuevo == VK_LBUTTON || nuevo == VK_RBUTTON || nuevo == VK_MBUTTON)
            {
                return ResultadoCaptura.Pendiente;
            }

            if (nuevo == VK_XBUTTON1 || nuevo == VK_XBUTTON2)
            {
                Activa = false;
                BotonMouse mouse = nuevo == VK_XBUTTON1 ? BotonMouse.XButton1 : BotonMouse.XButton2;
                return ResultadoCaptura.Cambio(new BotonZoom(Keys.None, mouse));
            }

            if (Teclas.EsModificador(nuevo))
            {
                // Un modificador solo no es un botón: se ignora (Ctrl+F9 captura F9).
                return ResultadoCaptura.Pendiente;
            }

            Activa = false;
            return ResultadoCaptura.Cambio(new BotonZoom((Keys)nuevo, BotonMouse.Ninguno));
        }

        if (nuevo == VK_LBUTTON || nuevo == VK_RBUTTON || nuevo == VK_MBUTTON ||
            nuevo == VK_XBUTTON1 || nuevo == VK_XBUTTON2)
        {
            return ResultadoCaptura.Fallo("Los atajos usan el teclado: presioná una tecla junto con Ctrl, Alt, Shift o Win.");
        }

        if (Teclas.EsModificador(nuevo))
        {
            return ResultadoCaptura.Pendiente;
        }

        uint modificadores = Teclas.ModificadoresActuales(_leer);
        if (modificadores == 0)
        {
            return ResultadoCaptura.Fallo("El atajo necesita al menos un modificador: Ctrl, Alt, Shift o Win.");
        }

        Activa = false;
        return ResultadoCaptura.Cambio(new Atajo(modificadores, (Keys)nuevo));
    }
}