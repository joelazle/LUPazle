using System.Globalization;
using System.Windows.Forms;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana;

/// <summary>
/// Utilidades de teclas compartidas por el menú, la ventana de configuración y
/// los atajos: nombres para mostrar y detección de modificadores.
/// </summary>
internal static class Teclas
{
    /// <summary>Nombre en español de una tecla del teclado.</summary>
    public static string NombreVisible(Keys tecla) => tecla switch
    {
        Keys.Up => "Flecha arriba",
        Keys.Down => "Flecha abajo",
        Keys.Left => "Flecha izquierda",
        Keys.Right => "Flecha derecha",
        Keys.Space => "Espacio",
        Keys.Enter => "Enter",
        Keys.Escape => "Escape",
        Keys.Tab => "Tab",
        Keys.Back => "Retroceso",
        Keys.Oemtilde => "Ñ / ~",
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + ((int)tecla - (int)Keys.D0))).ToString(CultureInfo.InvariantCulture),
        >= Keys.NumPad0 and <= Keys.NumPad9 => "Num " + ((int)tecla - (int)Keys.NumPad0).ToString(CultureInfo.InvariantCulture),
        _ => tecla.ToString(),
    };

    /// <summary>
    /// Indica si el código de tecla virtual corresponde a un modificador
    /// (Ctrl, Alt, Shift o Win, en cualquiera de sus variantes izquierda/derecha).
    /// </summary>
    public static bool EsModificador(int teclaVirtual) => teclaVirtual is
        VK_SHIFT or VK_CONTROL or VK_MENU or
        VK_LSHIFT or VK_RSHIFT or VK_LCONTROL or VK_RCONTROL or VK_LMENU or VK_RMENU or
        VK_LWIN or VK_RWIN;

    /// <summary>Arma la máscara de modificadores que están presionados en este momento.</summary>
    public static uint ModificadoresActuales(Func<int, bool> estaPresionada)
    {
        uint modificadores = 0;

        if (estaPresionada(VK_CONTROL) || estaPresionada(VK_LCONTROL) || estaPresionada(VK_RCONTROL))
        {
            modificadores |= MOD_CONTROL;
        }

        if (estaPresionada(VK_MENU) || estaPresionada(VK_LMENU) || estaPresionada(VK_RMENU))
        {
            modificadores |= MOD_ALT;
        }

        if (estaPresionada(VK_SHIFT) || estaPresionada(VK_LSHIFT) || estaPresionada(VK_RSHIFT))
        {
            modificadores |= MOD_SHIFT;
        }

        if (estaPresionada(VK_LWIN) || estaPresionada(VK_RWIN))
        {
            modificadores |= MOD_WIN;
        }

        return modificadores;
    }
}
