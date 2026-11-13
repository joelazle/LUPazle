using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LupaLiviana;

/// <summary>
/// Configuración del programa. Se guarda como JSON en
/// %AppData%\LupaLiviana\config.json y se puede editar a mano.
/// </summary>
internal sealed class Config
{
    /// <summary>Nivel de zoom (de 1.5 a 8).</summary>
    public double NivelZoom { get; set; } = 2.0;

    /// <summary>Botón de zoom: "F9", "MouseXButton1", "MouseXButton2", etc.</summary>
    public string BotonZoom { get; set; } = "F9";

    /// <summary>Duración en milisegundos que separa un "toque" de una pulsación mantenida.</summary>
    public int UmbralToqueMs { get; set; } = 250;

    /// <summary>Suavizado del seguimiento del mouse.</summary>
    public NivelSuavizado Suavizado { get; set; } = NivelSuavizado.Bajo;

    /// <summary>Filtro de color aplicado a la pantalla ampliada.</summary>
    public FiltroColor Filtro { get; set; } = FiltroColor.Ninguno;

    /// <summary>Atajo global para subir el nivel de zoom (texto, por ejemplo "Ctrl+Alt+Up").</summary>
    public string AtajoSubirZoom { get; set; } = "Ctrl+Alt+Up";

    /// <summary>Atajo global para bajar el nivel de zoom.</summary>
    public string AtajoBajarZoom { get; set; } = "Ctrl+Alt+Down";

    /// <summary>Indica si el programa se inicia con Windows.</summary>
    public bool IniciarConWindows { get; set; }

    /// <summary>Indica si las funciones de la lupa están habilitadas.</summary>
    public bool Activado { get; set; } = true;

    /// <summary>Corrige valores fuera de rango o inválidos sin descartar el resto.</summary>
    public void Normalizar()
    {
        if (double.IsNaN(NivelZoom) || double.IsInfinity(NivelZoom))
        {
            NivelZoom = 2.0;
        }

        NivelZoom = Math.Clamp(Math.Round(NivelZoom, 2), 1.5, 8.0);
        UmbralToqueMs = Math.Clamp(UmbralToqueMs, 80, 2000);

        if (!Enum.IsDefined(Suavizado))
        {
            Suavizado = NivelSuavizado.Bajo;
        }

        if (!Enum.IsDefined(Filtro))
        {
            Filtro = FiltroColor.Ninguno;
        }

        if (!LupaLiviana.BotonZoom.TryParse(BotonZoom, out _))
        {
            BotonZoom = "F9";
        }

        if (!Atajo.TryParse(AtajoSubirZoom, out _))
        {
            AtajoSubirZoom = "Ctrl+Alt+Up";
        }

        if (!Atajo.TryParse(AtajoBajarZoom, out _))
        {
            AtajoBajarZoom = "Ctrl+Alt+Down";
        }
    }
}

/// <summary>
/// Lectura y escritura de la configuración en disco. Si el archivo no existe
/// (o está dañado) se usan valores por defecto; en el caso de un archivo
/// dañado se guarda una copia con extensión ".roto" antes de reemplazarlo.
/// </summary>
internal sealed class ConfigService
{
    private const string NombreArchivo = "config.json";

    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public ConfigService(string? carpeta = null)
    {
        RutaCarpeta = carpeta ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LupaLiviana");
        RutaArchivo = Path.Combine(RutaCarpeta, NombreArchivo);
    }

    /// <summary>Carpeta donde vive la configuración.</summary>
    public string RutaCarpeta { get; }

    /// <summary>Ruta completa del archivo config.json.</summary>
    public string RutaArchivo { get; }

    /// <summary>Carga la configuración; ante cualquier problema devuelve los valores por defecto.</summary>
    public Config Cargar()
    {
        try
        {
            if (!File.Exists(RutaArchivo))
            {
                var nuevos = new Config();
                nuevos.Normalizar();
                Guardar(nuevos);
                return nuevos;
            }

            string json = File.ReadAllText(RutaArchivo);
            Config? config = JsonSerializer.Deserialize<Config>(json, Opciones);
            if (config is null)
            {
                throw new JsonException("El archivo de configuración está vacío.");
            }

            config.Normalizar();
            return config;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            RespaldarArchivoRoto();

            var config = new Config();
            config.Normalizar();
            return config;
        }
    }

    /// <summary>Guarda la configuración. Devuelve false si no se pudo escribir.</summary>
    public bool Guardar(Config config)
    {
        try
        {
            Directory.CreateDirectory(RutaCarpeta);

            // Escritura atómica: primero a un archivo temporal y después se reemplaza.
            string temporal = RutaArchivo + ".tmp";
            string json = JsonSerializer.Serialize(config, Opciones);
            File.WriteAllText(temporal, json);
            File.Move(temporal, RutaArchivo, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Garantiza que el archivo exista (por ejemplo, para abrirlo desde el menú).</summary>
    public void AsegurarArchivo(Config config)
    {
        if (!File.Exists(RutaArchivo))
        {
            Guardar(config);
        }
    }

    private void RespaldarArchivoRoto()
    {
        try
        {
            if (File.Exists(RutaArchivo))
            {
                File.Copy(RutaArchivo, RutaArchivo + ".roto", overwrite: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Si no se puede respaldar, igual se siguen usando los valores por defecto.
        }
    }
}