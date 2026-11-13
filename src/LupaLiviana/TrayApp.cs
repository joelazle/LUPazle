using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Win32;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana;

/// <summary>
/// Aplicación de bandeja: no tiene ventana principal. Administra el ícono,
/// el menú contextual, el timer de ~60 Hz que lee el botón de zoom y el
/// seguimiento del mouse.
/// </summary>
internal sealed class TrayApp : ApplicationContext
{
    private const string NombreApp = "Lupa Liviana";
    // Windows cuantiza los temporizadores de ventana a ~15,6 ms, así que un
    // intervalo de 10 ms termina dando unos 60 Hz reales (verificado).
    private const int IntervaloTimerMs = 10;
    private const int IdAtajoActivar = 1;
    private const int IdAtajoSubir = 2;
    private const int IdAtajoBajar = 3;
    private const int IdAtajoSiguienteFiltro = 4;
    private const int LargoMaximoTextoIcono = 63;
    private const string ClaveRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string NombreRun = "LupaLiviana";

    private static readonly double[] NivelesZoom = [1.5, 2.0, 3.0, 4.0, 6.0, 8.0];
    private static readonly int[] UmbralesToque = [150, 250, 400];

    private readonly ConfigService _configService = new();
    private readonly MagnifierService _lupa = new();
    private readonly ButtonStateMachine _maquina = new();
    private readonly VentanaAtajos _ventanaAtajos;
    private readonly RegistroAtajos _registroAtajos;
    private readonly CapturaTeclas _captura = new();
    private readonly Icon _iconoNormal;
    private readonly Icon _iconoActivo;
    private readonly NotifyIcon _iconoTray;
    private readonly ContextMenuStrip _menu;
    private readonly System.Windows.Forms.Timer _timer;

    // Ítems del menú que hay que actualizar cada vez que se abre.
    private ToolStripMenuItem _itemActivar = null!;
    private ToolStripMenuItem _itemBotonActual = null!;
    private ToolStripMenuItem _subMenuZoom = null!;
    private ToolStripMenuItem _subMenuUmbral = null!;
    private ToolStripMenuItem _subMenuSuavizado = null!;
    private ToolStripMenuItem _subMenuFiltro = null!;
    private ToolStripMenuItem _itemInicioWindows = null!;

    private Config _config;
    private BotonZoom _boton;
    private Atajo _atajoActivar;
    private Atajo _atajoSubir;
    private Atajo _atajoBajar;
    private Atajo _atajoSiguienteFiltro;
    private FormularioTeclas? _ventanaTeclas;

    private bool _avisoFalloLupa;
    private bool _limpiado;

    public TrayApp()
    {
        _config = _configService.Cargar();
        _boton = BotonZoom.TryParse(_config.BotonZoom, out BotonZoom botonLeido) ? botonLeido : BotonZoom.PorDefecto;
        _atajoActivar = LeerAtajoOpcional(_config.AtajoActivar);
        _atajoSubir = LeerAtajoOpcional(_config.AtajoSubirZoom);
        _atajoBajar = LeerAtajoOpcional(_config.AtajoBajarZoom);
        _atajoSiguienteFiltro = LeerAtajoOpcional(_config.AtajoSiguienteFiltro);

        _iconoNormal = IconoTray.Crear(false);
        _iconoActivo = IconoTray.Crear(true);

        _menu = new ContextMenuStrip();
        ConstruirMenu();

        _iconoTray = new NotifyIcon
        {
            Icon = _iconoNormal,
            Text = NombreApp,
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _iconoTray.MouseClick += AlClickEnIcono;

        _ventanaAtajos = new VentanaAtajos();
        _ventanaAtajos.AtajoPresionado += AlAtajoGlobal;
        _registroAtajos = new RegistroAtajos(
            (identificador, modificadores, tecla) => _ventanaAtajos.Registrar(identificador, modificadores, tecla),
            identificador => _ventanaAtajos.Liberar(identificador));

        _timer = new System.Windows.Forms.Timer { Interval = IntervaloTimerMs };
        _timer.Tick += AlTick;

        SystemEvents.SessionEnding += AlTerminarSesion;
        Application.ApplicationExit += AlSalirAplicacion;

        AplicarInicioConWindows();

        if (_config.Activado)
        {
            ActivarPrograma();
        }
        else
        {
            ActualizarIcono();
            AplicarRegistroAtajos();
        }
    }

    // ------------------------------------------------------------------
    // Menú de la bandeja
    // ------------------------------------------------------------------

    private void ConstruirMenu()
    {
        _menu.ShowImageMargin = false;

        _itemActivar = new ToolStripMenuItem("Activar Lupa Liviana");
        _itemActivar.Click += (_, _) => AlternarActivacion();
        _menu.Items.Add(_itemActivar);
        _menu.Items.Add(new ToolStripSeparator());

        _subMenuZoom = new ToolStripMenuItem("Nivel de zoom");
        foreach (double nivel in NivelesZoom)
        {
            _subMenuZoom.DropDownItems.Add(CrearOpcion(TextoNivel(nivel), nivel, CambiarNivel));
        }

        _menu.Items.Add(_subMenuZoom);

        var subMenuBoton = new ToolStripMenuItem("Botón de zoom");
        _itemBotonActual = new ToolStripMenuItem("Botón actual: F9") { Enabled = false };
        subMenuBoton.DropDownItems.Add(_itemBotonActual);
        subMenuBoton.DropDownItems.Add(new ToolStripSeparator());
        subMenuBoton.DropDownItems.Add(CrearOpcion("Cambiar botón…", true, _ => IniciarCaptura()));
        _menu.Items.Add(subMenuBoton);

        _subMenuUmbral = new ToolStripMenuItem("Sensibilidad del toque");
        foreach (int umbral in UmbralesToque)
        {
            _subMenuUmbral.DropDownItems.Add(CrearOpcion(TextoUmbral(umbral), umbral, CambiarUmbral));
        }

        _menu.Items.Add(_subMenuUmbral);

        _subMenuSuavizado = new ToolStripMenuItem("Suavizado");
        _subMenuSuavizado.DropDownItems.Add(CrearOpcion("Sin suavizado", NivelSuavizado.Ninguno, CambiarSuavizado));
        _subMenuSuavizado.DropDownItems.Add(CrearOpcion("Bajo (recomendado)", NivelSuavizado.Bajo, CambiarSuavizado));
        _subMenuSuavizado.DropDownItems.Add(CrearOpcion("Alto", NivelSuavizado.Alto, CambiarSuavizado));
        _menu.Items.Add(_subMenuSuavizado);

        _subMenuFiltro = new ToolStripMenuItem("Filtro de color");
        _subMenuFiltro.DropDownItems.Add(CrearOpcion("Ninguno", FiltroColor.Ninguno, CambiarFiltro));
        _subMenuFiltro.DropDownItems.Add(CrearOpcion("Invertir", FiltroColor.Invertir, CambiarFiltro));
        _subMenuFiltro.DropDownItems.Add(CrearOpcion("Escala de grises", FiltroColor.EscalaDeGrises, CambiarFiltro));
        _subMenuFiltro.DropDownItems.Add(CrearOpcion("Alto contraste", FiltroColor.AltoContraste, CambiarFiltro));
        _menu.Items.Add(_subMenuFiltro);

        _menu.Items.Add(CrearOpcion("Configurar teclas…", true, _ => AbrirConfiguracionTeclas()));

        _menu.Items.Add(new ToolStripSeparator());

        _itemInicioWindows = new ToolStripMenuItem("Iniciar con Windows");
        _itemInicioWindows.Click += (_, _) => AlternarInicioConWindows();
        _menu.Items.Add(_itemInicioWindows);

        _menu.Items.Add(CrearOpcion("Abrir archivo de configuración", true, _ => AbrirConfiguracion()));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(CrearOpcion("Salir", true, _ => Salir()));

        _menu.Opening += (_, _) => ActualizarMenu();
    }

    private static ToolStripMenuItem CrearOpcion<T>(string texto, T valor, Action<T> alElegir)
    {
        var item = new ToolStripMenuItem(texto) { Tag = valor };
        item.Click += (_, _) => alElegir(valor);
        return item;
    }

    private void ActualizarMenu()
    {
        _itemActivar.Checked = _config.Activado;
        _itemBotonActual.Text = $"Botón actual: {_boton.NombreVisible}";

        foreach (ToolStripMenuItem item in _subMenuZoom.DropDownItems.OfType<ToolStripMenuItem>())
        {
            item.Checked = item.Tag is double nivel && Math.Abs(nivel - _config.NivelZoom) < 0.001;
        }

        foreach (ToolStripMenuItem item in _subMenuUmbral.DropDownItems.OfType<ToolStripMenuItem>())
        {
            item.Checked = item.Tag is int umbral && umbral == _config.UmbralToqueMs;
        }

        foreach (ToolStripMenuItem item in _subMenuSuavizado.DropDownItems.OfType<ToolStripMenuItem>())
        {
            item.Checked = item.Tag is NivelSuavizado suavizado && suavizado == _config.Suavizado;
        }

        foreach (ToolStripMenuItem item in _subMenuFiltro.DropDownItems.OfType<ToolStripMenuItem>())
        {
            item.Checked = item.Tag is FiltroColor filtro && filtro == _config.Filtro;
        }

        _itemInicioWindows.Checked = _config.IniciarConWindows;
    }

    private void AlternarActivacion()
    {
        if (_config.Activado)
        {
            DesactivarPrograma();
        }
        else
        {
            ActivarPrograma();
        }

        GuardarConfig();
    }

    private void ActivarPrograma()
    {
        _config.Activado = true;
        _maquina.Reiniciar();
        _avisoFalloLupa = false;

        _timer.Start();
        AplicarRegistroAtajos();
        ActualizarIcono();
    }

    private void DesactivarPrograma()
    {
        _config.Activado = false;
        _captura.Cancelar();
        _maquina.Reiniciar();
        _timer.Stop();
        AplicarRegistroAtajos();

        _lupa.Apagar();
        ActualizarIcono();
    }

    private void AlternarInicioConWindows()
    {
        _config.IniciarConWindows = !_config.IniciarConWindows;
        AplicarInicioConWindows();
        GuardarConfig();
    }

    private void CambiarNivel(double nivel)
    {
        _config.NivelZoom = nivel;
        GuardarConfig();

        // Si el zoom está fijo, se vuelve a centrar con el nuevo nivel.
        // Si está siguiendo, el próximo tick aplica el cambio.
        if (_maquina.Modo == ModoLupa.Fijo)
        {
            _lupa.FijarCentro((float)nivel);
        }

        ActualizarIcono();
    }

    private void CambiarNivelPorPaso(int direccion)
    {
        double actual = _config.NivelZoom;
        double nuevo;

        if (direccion > 0)
        {
            nuevo = NivelesZoom[^1];
            foreach (double nivel in NivelesZoom)
            {
                if (nivel > actual + 0.001)
                {
                    nuevo = nivel;
                    break;
                }
            }
        }
        else
        {
            nuevo = NivelesZoom[0];
            for (int i = NivelesZoom.Length - 1; i >= 0; i--)
            {
                if (NivelesZoom[i] < actual - 0.001)
                {
                    nuevo = NivelesZoom[i];
                    break;
                }
            }
        }

        if (Math.Abs(nuevo - actual) > 0.001)
        {
            CambiarNivel(nuevo);
        }
    }

    private void CambiarUmbral(int umbral)
    {
        _config.UmbralToqueMs = umbral;
        GuardarConfig();
    }

    private void CambiarSuavizado(NivelSuavizado suavizado)
    {
        _config.Suavizado = suavizado;
        GuardarConfig();
    }

    private void CambiarFiltro(FiltroColor filtro)
    {
        _config.Filtro = filtro;
        GuardarConfig();

        if (_maquina.Modo != ModoLupa.Apagado)
        {
            _lupa.AplicarFiltro(filtro);
        }
    }

    // ------------------------------------------------------------------
    // Captura del nuevo botón (desde el menú)
    // ------------------------------------------------------------------

    private void IniciarCaptura()
    {
        if (!_config.Activado)
        {
            Avisar("Primero activá Lupa Liviana para poder cambiar el botón.");
            return;
        }

        if (_captura.Activa)
        {
            _captura.Cancelar();
            Avisar("No se cambió el botón de zoom.");
            return;
        }

        // Se apaga el zoom para no quedar en un estado raro mientras se configura.
        _lupa.Apagar();
        _maquina.Reiniciar();
        ActualizarIcono();

        _captura.Iniciar(ModoCaptura.Boton, EstaPresionada, Environment.TickCount64);

        Avisar(
            "Presioná la tecla (o el botón lateral del mouse) que querés usar como botón de zoom." +
            Environment.NewLine +
            "Escape cancela.");
    }

    private void ProcesarCaptura()
    {
        ResultadoCaptura resultado = _captura.Procesar(Environment.TickCount64);

        if (resultado.Estado == EstadoCaptura.Cancelada)
        {
            Avisar(resultado.Mensaje);
            return;
        }

        if (resultado.Estado != EstadoCaptura.CambioDetectado || resultado.Boton is not BotonZoom boton)
        {
            return;
        }

        _boton = boton;
        _config.BotonZoom = boton.Id;
        GuardarConfig();
        _maquina.Reiniciar();
        Avisar($"Botón de zoom: {boton.NombreVisible}");
    }

    // ------------------------------------------------------------------
    // Timer de ~60 Hz
    // ------------------------------------------------------------------

    private void AlTick(object? remitente, EventArgs e)
    {
        if (_captura.Activa)
        {
            ProcesarCaptura();
            return;
        }

        long ahora = Environment.TickCount64;
        bool presionado = EstaPresionada(_boton.TeclaVirtual);
        EventoBoton evento = _maquina.Actualizar(presionado, ahora, _config.UmbralToqueMs);

        switch (evento)
        {
            case EventoBoton.EmpezarSeguir:
                if (AsegurarLupaDisponible())
                {
                    _lupa.AplicarFiltro(_config.Filtro);
                    ActualizarIcono();
                }

                break;

            case EventoBoton.FijarCentro:
                if (AsegurarLupaDisponible())
                {
                    _lupa.AplicarFiltro(_config.Filtro);
                    _lupa.FijarCentro((float)_config.NivelZoom);
                    ActualizarIcono();
                }

                break;

            case EventoBoton.Apagar:
                _lupa.Apagar();
                ActualizarIcono();
                break;
        }

        if (_maquina.Modo == ModoLupa.Siguiendo)
        {
            _lupa.SeguirCursor((float)_config.NivelZoom, FactorSuavizado);
        }
    }

    private bool AsegurarLupaDisponible()
    {
        if (_lupa.AsegurarInicializada())
        {
            return true;
        }

        _maquina.Reiniciar();
        ActualizarIcono();

        if (!_avisoFalloLupa)
        {
            _avisoFalloLupa = true;
            Avisar("Windows no pudo inicializar la lupa de pantalla. Revisá que la API de ampliación esté disponible.");
        }

        return false;
    }

    private float FactorSuavizado => _config.Suavizado switch
    {
        NivelSuavizado.Ninguno => 1f,
        NivelSuavizado.Bajo => 0.35f,
        _ => 0.15f,
    };

    // ------------------------------------------------------------------
    // Atajos globales
    // ------------------------------------------------------------------

    private static Atajo LeerAtajoOpcional(string texto)
    {
        return Atajo.TryParse(texto, out Atajo atajo) ? atajo : Atajo.Ninguno;
    }

    private Dictionary<int, Atajo> AtajosDeseados()
    {
        var teclas = new ConfigTeclas(
            _boton,
            _atajoActivar,
            _atajoSubir,
            _atajoBajar,
            _atajoSiguienteFiltro);

        return ConstruirAtajos(teclas, _config.Activado);
    }

    private static Dictionary<int, Atajo> ConstruirAtajos(ConfigTeclas teclas, bool activado)
    {
        var deseados = new Dictionary<int, Atajo>(4);

        // El atajo de activar/desactivar queda registrado siempre: si no, no
        // habría forma de volver a encender la lupa sin el mouse.
        if (teclas.Activar.EsValido)
        {
            deseados[IdAtajoActivar] = teclas.Activar;
        }

        if (activado)
        {
            if (teclas.Subir.EsValido)
            {
                deseados[IdAtajoSubir] = teclas.Subir;
            }

            if (teclas.Bajar.EsValido)
            {
                deseados[IdAtajoBajar] = teclas.Bajar;
            }

            if (teclas.SiguienteFiltro.EsValido)
            {
                deseados[IdAtajoSiguienteFiltro] = teclas.SiguienteFiltro;
            }
        }

        return deseados;
    }

    private void AplicarRegistroAtajos()
    {
        IReadOnlyList<int> fallidos = _registroAtajos.Aplicar(AtajosDeseados());
        if (fallidos.Count > 0)
        {
            Avisar("No se pudo registrar uno de los atajos. Puede que otro programa ya los esté usando.");
        }
    }

    private void AlAtajoGlobal(int identificador)
    {
        if (identificador == IdAtajoActivar)
        {
            AlternarActivacion();
            Avisar(_config.Activado ? "Lupa Liviana activada." : "Lupa Liviana desactivada.");
            return;
        }

        if (!_config.Activado)
        {
            return;
        }

        switch (identificador)
        {
            case IdAtajoSubir:
                CambiarNivelPorPaso(1);
                break;

            case IdAtajoBajar:
                CambiarNivelPorPaso(-1);
                break;

            case IdAtajoSiguienteFiltro:
                CiclarFiltro();
                break;
        }
    }

    private void CiclarFiltro()
    {
        CambiarFiltro(Filtros.Siguiente(_config.Filtro));
        Avisar($"Filtro de color: {Filtros.NombreVisible(_config.Filtro)}");
    }

    // ------------------------------------------------------------------
    // Ventana de configuración de teclas
    // ------------------------------------------------------------------

    private void AbrirConfiguracionTeclas()
    {
        if (_ventanaTeclas is { IsDisposed: false })
        {
            _ventanaTeclas.Activate();
            return;
        }

        // Mientras se configuran las teclas no debe haber zoom ni atajos activos:
        // así probar una combinación no cambia el nivel ni dispara acciones.
        _captura.Cancelar();
        _lupa.Apagar();
        _maquina.Reiniciar();
        ActualizarIcono();
        _timer.Stop();
        _registroAtajos.LiberarTodo();

        using var ventana = new FormularioTeclas(ConfigTeclas.Desde(_config), AplicarTeclasDesdeVentana);
        _ventanaTeclas = ventana;

        try
        {
            if (ventana.ShowDialog() == DialogResult.OK)
            {
                Avisar("Teclas guardadas.");
            }
        }
        finally
        {
            _ventanaTeclas = null;
            AplicarRegistroAtajos();

            if (_config.Activado)
            {
                _timer.Start();
            }
        }
    }

    /// <summary>
    /// Valida y aplica los valores elegidos en la ventana. Devuelve un mensaje
    /// de error (la ventana sigue abierta) o null si se aplicaron.
    /// </summary>
    private string? AplicarTeclasDesdeVentana(ConfigTeclas nuevas)
    {
        string? error = nuevas.Validar();
        if (error is not null)
        {
            return error;
        }

        Dictionary<int, Atajo> deseados = ConstruirAtajos(nuevas, _config.Activado);
        IReadOnlyList<int> fallidos = _registroAtajos.Aplicar(deseados);

        if (fallidos.Count > 0)
        {
            // Se vuelve a registrar el conjunto anterior, que quedó libre al abrir la ventana.
            _registroAtajos.Aplicar(AtajosDeseados());

            string nombres = string.Join(
                ", ",
                fallidos.Select(identificador => deseados.TryGetValue(identificador, out Atajo atajo)
                    ? atajo.NombreVisible
                    : $"atajo {identificador}"));

            return $"No se pudo registrar {nombres}. Puede que otro programa ya esté usando esa combinación.";
        }

        nuevas.AplicarA(_config);
        _boton = nuevas.Boton;
        _atajoActivar = nuevas.Activar;
        _atajoSubir = nuevas.Subir;
        _atajoBajar = nuevas.Bajar;
        _atajoSiguienteFiltro = nuevas.SiguienteFiltro;

        GuardarConfig();
        _maquina.Reiniciar();
        ActualizarIcono();
        return null;
    }

    // ------------------------------------------------------------------
    // Ícono, avisos y otros comandos
    // ------------------------------------------------------------------

    private void AlClickEnIcono(object? remitente, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && !_menu.Visible)
        {
            _menu.Show(Cursor.Position);
        }
    }

    private void ActualizarIcono()
    {
        bool activo = _maquina.Modo != ModoLupa.Apagado;
        Icon deseado = activo ? _iconoActivo : _iconoNormal;

        if (!ReferenceEquals(_iconoTray.Icon, deseado))
        {
            _iconoTray.Icon = deseado;
        }

        ActualizarTextoIcono();
    }

    private void ActualizarTextoIcono()
    {
        string estado = _maquina.Modo switch
        {
            ModoLupa.Siguiendo => "siguiendo al mouse",
            ModoLupa.Fijo => "fijo al centro",
            _ => "apagado",
        };

        string texto = string.Create(
            CultureInfo.InvariantCulture,
            $"{NombreApp} — {_config.NivelZoom:0.#}x — {estado}");

        if (texto.Length > LargoMaximoTextoIcono)
        {
            texto = texto[..LargoMaximoTextoIcono];
        }

        _iconoTray.Text = texto;
    }

    private void Avisar(string mensaje)
    {
        try
        {
            _iconoTray.ShowBalloonTip(3500, NombreApp, mensaje, ToolTipIcon.Info);
        }
        catch (Exception)
        {
            // Si la bandeja no está disponible, el aviso es opcional.
        }
    }

    private void GuardarConfig()
    {
        if (!_configService.Guardar(_config))
        {
            Avisar("No se pudo guardar la configuración en disco.");
        }
    }

    private void AplicarInicioConWindows()
    {
        try
        {
            using RegistryKey? clave = Registry.CurrentUser.OpenSubKey(ClaveRun, writable: true);
            if (clave is null)
            {
                return;
            }

            if (_config.IniciarConWindows)
            {
                string? ejecutable = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(ejecutable))
                {
                    clave.SetValue(NombreRun, $"\"{ejecutable}\"");
                }
            }
            else if (clave.GetValue(NombreRun) is not null)
            {
                clave.DeleteValue(NombreRun, throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            Avisar("No se pudo actualizar el inicio automático con Windows.");
        }
    }

    private void AbrirConfiguracion()
    {
        try
        {
            _configService.AsegurarArchivo(_config);

            Process.Start(new ProcessStartInfo
            {
                FileName = _configService.RutaArchivo,
                UseShellExecute = true,
            });

            Avisar("Los cambios del archivo se aplican la próxima vez que inicies Lupa Liviana.");
        }
        catch (Exception)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _configService.RutaCarpeta,
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                Avisar("No se pudo abrir la carpeta de configuración.");
            }
        }
    }

    private void AlTerminarSesion(object sender, SessionEndingEventArgs e)
    {
        // Antes de que Windows cierre la sesión, la pantalla debe volver a 1.0.
        _maquina.Reiniciar();
        _lupa.Apagar();
    }

    private void AlSalirAplicacion(object? remitente, EventArgs e)
    {
        Limpiar();
    }

    private void Salir()
    {
        Limpiar();
        ExitThread();
    }

    // ------------------------------------------------------------------
    // Cierre y limpieza
    // ------------------------------------------------------------------

    protected override void ExitThreadCore()
    {
        Limpiar();
        base.ExitThreadCore();
    }

    private void Limpiar()
    {
        if (_limpiado)
        {
            return;
        }

        _limpiado = true;

        try
        {
            _timer.Stop();
            _timer.Tick -= AlTick;
        }
        catch (Exception)
        {
            // El timer puede estar ya liberado.
        }

        _registroAtajos.LiberarTodo();

        _ventanaAtajos.AtajoPresionado -= AlAtajoGlobal;
        _ventanaAtajos.Destruir();

        SystemEvents.SessionEnding -= AlTerminarSesion;
        Application.ApplicationExit -= AlSalirAplicacion;

        _lupa.Apagar();
        MagnifierService.RestaurarPantalla();

        _iconoTray.Visible = false;
        _iconoTray.MouseClick -= AlClickEnIcono;
        _iconoTray.Dispose();

        _menu.Dispose();
        _iconoNormal.Dispose();
        _iconoActivo.Dispose();
    }

    private static string TextoNivel(double nivel)
    {
        return nivel.ToString("0.#", CultureInfo.InvariantCulture) + "x";
    }

    private static string TextoUmbral(int umbral)
    {
        return umbral switch
        {
            150 => "150 ms (más sensible)",
            250 => "250 ms (normal)",
            _ => "400 ms (menos sensible)",
        };
    }
}