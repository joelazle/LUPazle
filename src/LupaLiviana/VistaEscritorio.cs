using System.Runtime.InteropServices;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana;

/// <summary>
/// Rectángulo de pantalla en píxeles físicos. El origen (0,0) es la esquina
/// superior izquierda de la pantalla principal; un monitor ubicado a la
/// izquierda o arriba de ella tiene coordenadas negativas.
/// </summary>
internal readonly record struct RectanguloPantalla(int Izquierda, int Arriba, int Ancho, int Alto)
{
    /// <summary>Borde derecho (exclusivo).</summary>
    public int Derecha => Izquierda + Ancho;

    /// <summary>Borde inferior (exclusivo).</summary>
    public int Abajo => Arriba + Alto;

    /// <summary>Centro horizontal, redondeado hacia abajo.</summary>
    public int CentroX => Izquierda + (Ancho / 2);

    /// <summary>Centro vertical, redondeado hacia abajo.</summary>
    public int CentroY => Arriba + (Alto / 2);

    /// <summary>Indica si el rectángulo tiene un tamaño utilizable.</summary>
    public bool EsValido => Ancho > 0 && Alto > 0;
}

/// <summary>
/// Calcula el desplazamiento que espera MagSetFullscreenTransform.
///
/// La API interpreta el desplazamiento como el punto del escritorio sin
/// ampliar que se ve en la esquina superior izquierda de la pantalla
/// principal. Con un nivel Z, en un punto P de la pantalla (medido desde la
/// principal) se ve el punto del escritorio P / Z + desplazamiento. Por eso,
/// para que el punto C aparezca en la posición de pantalla S:
///
///     desplazamiento = C - S / Z
///
/// No accede a Windows: recibe los rectángulos desde afuera, lo que permite
/// probar el cálculo con distintas configuraciones de monitores.
/// </summary>
internal static class CalculoVista
{
    /// <summary>
    /// Devuelve el desplazamiento para mostrar <paramref name="centroX"/>,
    /// <paramref name="centroY"/> en la posición de pantalla indicada, sin
    /// salirse del escritorio virtual.
    /// </summary>
    public static (int X, int Y) Desplazamiento(
        float nivel,
        float centroX,
        float centroY,
        float pantallaX,
        float pantallaY,
        RectanguloPantalla escritorio)
    {
        if (!escritorio.EsValido || float.IsNaN(nivel) || float.IsInfinity(nivel))
        {
            return (0, 0);
        }

        if (nivel < 1f)
        {
            nivel = 1f;
        }

        double desplazamientoX = centroX - (pantallaX / nivel);
        double desplazamientoY = centroY - (pantallaY / nivel);

        // Todo el escritorio virtual ampliado tiene que quedar dentro del
        // escritorio real. El borde izquierdo visible es
        // desplazamiento + izquierda / Z; despejando queda:
        //     desplazamiento >= izquierda * (1 - 1 / Z)
        // y lo mismo para el borde derecho y para la altura.
        double factor = 1.0 - (1.0 / nivel);
        double minimoX = escritorio.Izquierda * factor;
        double maximoX = escritorio.Derecha * factor;
        double minimoY = escritorio.Arriba * factor;
        double maximoY = escritorio.Abajo * factor;

        desplazamientoX = Math.Clamp(desplazamientoX, minimoX, maximoX);
        desplazamientoY = Math.Clamp(desplazamientoY, minimoY, maximoY);

        return ((int)Math.Round(desplazamientoX), (int)Math.Round(desplazamientoY));
    }
}

/// <summary>
/// Consulta los monitores instalados. Es la única parte de la geometría que
/// usa Windows; si algo falla, se usa la pantalla principal, que es el
/// comportamiento que tenía el programa antes de soportar varios monitores.
/// </summary>
internal static class EscritorioWindows
{
    private static readonly int TamanoMonitorInfo = Marshal.SizeOf<MONITORINFO>();

    /// <summary>Rectángulo que abarca todos los monitores.</summary>
    public static RectanguloPantalla Virtual()
    {
        var escritorio = new RectanguloPantalla(
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN));

        return escritorio.EsValido ? escritorio : Principal();
    }

    /// <summary>Rectángulo de la pantalla principal, siempre en (0,0).</summary>
    public static RectanguloPantalla Principal()
    {
        return new RectanguloPantalla(0, 0, GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));
    }

    /// <summary>
    /// Monitor que contiene el punto indicado. Si Windows no puede informarlo,
    /// devuelve la pantalla principal.
    /// </summary>
    public static RectanguloPantalla MonitorEn(int x, int y)
    {
        try
        {
            IntPtr monitor = MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var info = new MONITORINFO { cbSize = TamanoMonitorInfo };
                if (GetMonitorInfo(monitor, ref info))
                {
                    var rectangulo = new RectanguloPantalla(
                        info.rcMonitor.Left,
                        info.rcMonitor.Top,
                        info.rcMonitor.Right - info.rcMonitor.Left,
                        info.rcMonitor.Bottom - info.rcMonitor.Top);

                    if (rectangulo.EsValido)
                    {
                        return rectangulo;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Si la consulta falla, se sigue con la pantalla principal.
        }

        return Principal();
    }
}