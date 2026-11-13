namespace LupaLiviana;

/// <summary>
/// Conjunto de teclas configurable: el botón de zoom y los cuatro atajos
/// globales. Se usa para mover los valores entre la ventana de configuración
/// y la aplicación, sin tocar el archivo hasta que el usuario guarda.
/// </summary>
internal sealed record ConfigTeclas(
    BotonZoom Boton,
    Atajo Activar,
    Atajo Subir,
    Atajo Bajar,
    Atajo SiguienteFiltro)
{
    /// <summary>Valores de fábrica: F9 para el zoom y los dos atajos de nivel.</summary>
    public static ConfigTeclas PorDefecto => new(
        BotonZoom.PorDefecto,
        Atajo.Ninguno,
        Atajo.DefectoSubir,
        Atajo.DefectoBajar,
        Atajo.Ninguno);

    /// <summary>Lee los valores de la configuración guardada.</summary>
    public static ConfigTeclas Desde(Config config)
    {
        BotonZoom boton = BotonZoom.TryParse(config.BotonZoom, out BotonZoom leido)
            ? leido
            : BotonZoom.PorDefecto;

        return new ConfigTeclas(
            boton,
            LeerOpcional(config.AtajoActivar),
            LeerOpcional(config.AtajoSubirZoom),
            LeerOpcional(config.AtajoBajarZoom),
            LeerOpcional(config.AtajoSiguienteFiltro));
    }

    /// <summary>Escribe los valores en la configuración (todavía sin guardar).</summary>
    public void AplicarA(Config config)
    {
        config.BotonZoom = Boton.Id;
        config.AtajoActivar = Activar.HaciaTexto();
        config.AtajoSubirZoom = Subir.HaciaTexto();
        config.AtajoBajarZoom = Bajar.HaciaTexto();
        config.AtajoSiguienteFiltro = SiguienteFiltro.HaciaTexto();
    }

    /// <summary>Devuelve un mensaje de error si los valores no son válidos; null si están bien.</summary>
    public string? Validar()
    {
        var acciones = Acciones().Where(par => par.Atajo.EsValido).ToArray();

        for (int i = 0; i < acciones.Length; i++)
        {
            for (int j = i + 1; j < acciones.Length; j++)
            {
                if (acciones[i].Atajo == acciones[j].Atajo)
                {
                    return $"El atajo {acciones[i].Atajo.NombreVisible} está asignado a " +
                        $"«{acciones[i].Nombre}» y a «{acciones[j].Nombre}». Elegí combinaciones distintas.";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Avisos que no impiden guardar: por ejemplo, un atajo que incluye la
    /// misma tecla del botón de zoom y que por lo tanto también lo dispararía.
    /// </summary>
    public IReadOnlyList<string> Advertencias()
    {
        var avisos = new List<string>();

        if (Boton.EsMouse)
        {
            return avisos;
        }

        foreach ((string nombre, Atajo atajo) in Acciones())
        {
            if (atajo.EsValido && atajo.Tecla == Boton.Tecla)
            {
                avisos.Add(
                    $"El atajo {atajo.NombreVisible} («{nombre}») incluye la tecla " +
                    $"{Boton.NombreVisible}, que es el botón de zoom: al usarlo también se va a mover la lupa.");
            }
        }

        return avisos;
    }

    private IEnumerable<(string Nombre, Atajo Atajo)> Acciones()
    {
        yield return ("activar o desactivar Lupa Liviana", Activar);
        yield return ("subir el nivel de zoom", Subir);
        yield return ("bajar el nivel de zoom", Bajar);
        yield return ("cambiar el filtro de color", SiguienteFiltro);
    }

    private static Atajo LeerOpcional(string texto)
    {
        return Atajo.TryParse(texto, out Atajo atajo) ? atajo : Atajo.Ninguno;
    }
}