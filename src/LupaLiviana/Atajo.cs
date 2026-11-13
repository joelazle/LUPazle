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

    /// <summary>Atajo ausente u opcional deshabilitado.</summary>
    public static Atajo Ninguno => default;

    /// <summary>Texto canónico que se guarda en el JSON, por ejemplo "Ctrl+Alt+Up".</summary>
    public string HaciaTexto()
    {
        if (!EsValido)
        {
            return "";
        }

        var partes = new List<string>(5);
        AgregarModificadores(partes);
        partes.Add(Tecla.ToString());
        return string.Join("+", partes);
    }

    /// <summary>Texto para mostrar en la interfaz, por ejemplo "Ctrl + Alt + Flecha arriba".</summary>
    public string NombreVisible
    {
        get
        {
            if (!EsValido)
            {
                return "Sin atajo";
            }

            var partes = new List<string>(5);
            AgregarModificadores(partes);
            partes.Add(Teclas.NombreVisible(Tecla));
            return string.Join(" + ", partes);
        }
    }

    private void AgregarModificadores(List<string> partes)
    {
        if ((Modificadores & MOD_CONTROL) != 0)
        {
            partes.Add("Ctrl");
        }

        if ((Modificadores & MOD_ALT) != 0)
        {
            partes.Add("Alt");
        }

        if ((Modificadores & MOD_SHIFT) != 0)
        {
            partes.Add("Shift");
        }

        if ((Modificadores & MOD_WIN) != 0)
        {
            partes.Add("Win");
        }
    }

    /// <summary>
    /// Lee un atajo que puede estar vacío (deshabilitado). Devuelve true si el
    /// texto es válido o está vacío; false si tiene un error de formato.
    /// </summary>
    public static bool TryParseOpcional(string? texto, out Atajo atajo)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            atajo = Ninguno;
            return true;
        }

        return TryParse(texto, out atajo);
    }

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