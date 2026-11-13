using System.Windows.Forms;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana;

/// <summary>
/// Atajo de teclado global (RegisterHotKey): combinación de modificadores más
/// una tecla. Se guarda en el JSON como texto, por ejemplo "Ctrl+Alt+Up".
/// </summary>
internal readonly record struct Atajo(uint Modificadores, Keys Tecla)
{
    /// <summary>Atajo por defecto para subir el nivel de zoom.</summary>
    public static Atajo DefectoSubir => new(MOD_CONTROL | MOD_ALT, Keys.Up);

    /// <summary>Atajo por defecto para bajar el nivel de zoom.</summary>
    public static Atajo DefectoBajar => new(MOD_CONTROL | MOD_ALT, Keys.Down);

    /// <summary>Un atajo es válido solo si tiene al menos un modificador y una tecla.</summary>
    public bool EsValido => Modificadores != 0 && Tecla != Keys.None;

    /// <summary>Convierte un texto del estilo "Ctrl+Alt+Up" en un atajo.</summary>
    public static bool TryParse(string? texto, out Atajo atajo)
    {
        atajo = default;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        uint modificadores = 0;
        Keys tecla = Keys.None;

        foreach (string parte in texto.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (parte.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modificadores |= MOD_CONTROL;
                    continue;

                case "alt":
                    modificadores |= MOD_ALT;
                    continue;

                case "shift":
                    modificadores |= MOD_SHIFT;
                    continue;

                case "win":
                case "windows":
                    modificadores |= MOD_WIN;
                    continue;
            }

            if (tecla != Keys.None || !TryParseTecla(parte, out tecla))
            {
                return false;
            }
        }

        if (modificadores == 0 || tecla == Keys.None)
        {
            return false;
        }

        atajo = new Atajo(modificadores, tecla);
        return true;
    }

    private static bool TryParseTecla(string texto, out Keys tecla)
    {
        switch (texto.ToLowerInvariant())
        {
            case "arriba":
            case "up":
                tecla = Keys.Up;
                return true;

            case "abajo":
            case "down":
                tecla = Keys.Down;
                return true;

            case "izquierda":
            case "left":
                tecla = Keys.Left;
                return true;

            case "derecha":
            case "right":
                tecla = Keys.Right;
                return true;
        }

        return Enum.TryParse(texto, ignoreCase: true, out tecla) && tecla != Keys.None;
    }
}