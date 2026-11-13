using System.Globalization;
using System.Windows.Forms;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana;

/// <summary>Botones laterales del mouse que se pueden usar como botón de zoom.</summary>
internal enum BotonMouse
{
    Ninguno,
    XButton1,
    XButton2,
}

/// <summary>
/// Botón de zoom: una tecla del teclado o uno de los botones laterales del
/// mouse. Se guarda en el JSON como un texto corto ("F9", "MouseXButton1").
/// </summary>
internal readonly record struct BotonZoom(Keys Tecla, BotonMouse Mouse)
{
    /// <summary>Valor por defecto: F9.</summary>
    public static BotonZoom PorDefecto => new(Keys.F9, BotonMouse.Ninguno);

    /// <summary>Indica si el botón es del mouse en lugar del teclado.</summary>
    public bool EsMouse => Mouse != BotonMouse.Ninguno;

    /// <summary>Código de tecla virtual que se consulta con GetAsyncKeyState.</summary>
    public int TeclaVirtual
    {
        get
        {
            if (EsMouse)
            {
                return Mouse == BotonMouse.XButton1 ? VK_XBUTTON1 : VK_XBUTTON2;
            }

            return (int)Tecla;
        }
    }

    /// <summary>Identificador que se guarda en el archivo de configuración.</summary>
    public string Id => EsMouse
        ? (Mouse == BotonMouse.XButton1 ? "MouseXButton1" : "MouseXButton2")
        : Tecla.ToString();

    /// <summary>Nombre para mostrar en el menú de la bandeja.</summary>
    public string NombreVisible
    {
        get
        {
            if (EsMouse)
            {
                return Mouse == BotonMouse.XButton1
                    ? "Botón lateral 1 del mouse (atrás)"
                    : "Botón lateral 2 del mouse (adelante)";
            }

            return Tecla switch
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
                >= Keys.D0 and <= Keys.D9 => ((char)('0' + ((int)Tecla - (int)Keys.D0))).ToString(CultureInfo.InvariantCulture),
                >= Keys.NumPad0 and <= Keys.NumPad9 => "Num " + ((int)Tecla - (int)Keys.NumPad0).ToString(CultureInfo.InvariantCulture),
                _ => Tecla.ToString(),
            };
        }
    }

    /// <summary>Convierte el texto del archivo de configuración en un botón.</summary>
    public static bool TryParse(string? texto, out BotonZoom boton)
    {
        boton = PorDefecto;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        switch (texto.Trim().ToLowerInvariant())
        {
            case "mousexbutton1":
            case "xbutton1":
            case "mouse4":
                boton = new BotonZoom(Keys.None, BotonMouse.XButton1);
                return true;

            case "mousexbutton2":
            case "xbutton2":
            case "mouse5":
                boton = new BotonZoom(Keys.None, BotonMouse.XButton2);
                return true;
        }

        if (Enum.TryParse(texto, ignoreCase: true, out Keys tecla) && tecla != Keys.None)
        {
            boton = new BotonZoom(tecla, BotonMouse.Ninguno);
            return true;
        }

        return false;
    }

    public override string ToString() => NombreVisible;
}