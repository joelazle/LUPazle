using System.Drawing;
using System.Windows.Forms;

namespace LupaLiviana;

/// <summary>
/// Ventana "Configurar teclas". Muestra el botón de zoom y los atajos
/// globales, permite cambiar cada uno capturando la próxima tecla y solo
/// aplica los cambios al presionar Guardar.
///
/// La captura usa <see cref="CapturaTeclas"/> (consulta del estado del
/// teclado, sin hooks) desde un timer de la interfaz.
/// </summary>
internal sealed class FormularioTeclas : Form
{
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_CLOSE = 0xF060;

    private static readonly Font FuenteNormal = new("Segoe UI", 11F);
    private static readonly Font FuenteValor = new("Segoe UI Semibold", 11F);
    private static readonly Font FuenteChica = new("Segoe UI", 9.5F);

    private static readonly Color ColorError = Color.FromArgb(176, 0, 32);
    private static readonly Color ColorAviso = Color.FromArgb(160, 90, 0);
    private static readonly Color ColorInfo = Color.FromArgb(0, 51, 153);
    private static readonly Color ColorApagado = Color.FromArgb(90, 90, 90);

    private readonly Func<ConfigTeclas, string?> _alGuardar;
    private readonly CapturaTeclas _captura = new();
    private readonly System.Windows.Forms.Timer _timer;
    private readonly List<Fila> _filas = [];

    private readonly Label _estado;
    private readonly Label _mensajes;
    private readonly Button _guardar;
    private readonly Button _cancelar;
    private readonly Button _restablecer;

    private ConfigTeclas _valores;
    private Campo? _campoEnCaptura;
    private long _finCapturaMs;

    private enum Campo
    {
        Boton,
        Activar,
        Subir,
        Bajar,
        SiguienteFiltro,
    }

    private sealed record Fila(Campo Campo, TextBox Valor, Button Cambiar, Button? Quitar);

    public FormularioTeclas(ConfigTeclas inicial, Func<ConfigTeclas, string?> alGuardar)
    {
        _valores = inicial;
        _alGuardar = alGuardar;

        Text = "Configurar teclas — Lupa Liviana";
        Font = FuenteNormal;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(800, 620);
        MinimumSize = new Size(800, 600);

        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(18),
        };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(raiz);

        var instrucciones = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            MaximumSize = new Size(710, 0),
            Margin = new Padding(0, 0, 0, 6),
            Text =
                "Elegí una acción y apretá «Cambiar». Después presioná la tecla o combinación que quieras usar.\r\n" +
                "Escape cancela. Los atajos se aplican a todo el sistema, incluso sobre el juego; si otro " +
                "programa ya usa la combinación, Windows no la deja registrar.",
        };
        raiz.Controls.Add(instrucciones, 0, 0);

        var tabla = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            Margin = new Padding(0, 6, 0, 6),
        };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        raiz.Controls.Add(tabla, 0, 1);

        AgregarFila(tabla, "Botón de zoom", Campo.Boton, puedeQuitar: false,
            ayuda: "La palanca: mantener para seguir al mouse, tocar para fijar.");
        AgregarFila(tabla, "Activar o desactivar la lupa", Campo.Activar, puedeQuitar: true,
            ayuda: "Funciona aunque la lupa esté desactivada.");
        AgregarFila(tabla, "Subir el nivel de zoom", Campo.Subir, puedeQuitar: true);
        AgregarFila(tabla, "Bajar el nivel de zoom", Campo.Bajar, puedeQuitar: true);
        AgregarFila(tabla, "Siguiente filtro de color", Campo.SiguienteFiltro, puedeQuitar: true,
            ayuda: "Ninguno → Invertir → Grises → Alto contraste.");

        _estado = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            MaximumSize = new Size(710, 0),
            Margin = new Padding(0, 6, 0, 0),
            ForeColor = ColorApagado,
            Text = "Los cambios se aplican cuando apretás Guardar.",
        };
        raiz.Controls.Add(_estado, 0, 3);

        _mensajes = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            MaximumSize = new Size(710, 0),
            Margin = new Padding(0, 6, 0, 0),
            ForeColor = ColorAviso,
        };
        raiz.Controls.Add(_mensajes, 0, 4);

        var botones = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 12, 0, 0),
        };

        _guardar = CrearBoton("Guardar", Font);
        _guardar.Click += (_, _) => Guardar();
        botones.Controls.Add(_guardar);

        _cancelar = CrearBoton("Cancelar", Font);
        _cancelar.DialogResult = DialogResult.Cancel;
        botones.Controls.Add(_cancelar);

        _restablecer = CrearBoton("Restablecer valores", FuenteChica);
        _restablecer.Click += (_, _) => Restablecer();
        botones.Controls.Add(_restablecer);

        raiz.Controls.Add(botones, 0, 5);

        _timer = new System.Windows.Forms.Timer { Interval = 10 };
        _timer.Tick += AlTickCaptura;

        ActualizarValores();
        MostrarMensajes();
    }

    // ------------------------------------------------------------------
    // Construcción
    // ------------------------------------------------------------------

    private static Button CrearBoton(string texto, Font fuente)
    {
        return new Button
        {
            Text = texto,
            Font = fuente,
            AutoSize = true,
            MinimumSize = new Size(120, 38),
            Padding = new Padding(8, 2, 8, 2),
            Margin = new Padding(8, 0, 0, 0),
        };
    }

    private void AgregarFila(TableLayoutPanel tabla, string accion, Campo campo, bool puedeQuitar, string? ayuda = null)
    {
        int indice = tabla.RowCount;
        tabla.RowCount = indice + 1;
        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        string textoAccion = ayuda is null ? accion : $"{accion}\r\n{ayuda}";
        var etiqueta = new Label
        {
            Text = textoAccion,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = ayuda is null ? FuenteNormal : FuenteChica,
            Margin = new Padding(0, 4, 8, 4),
        };

        var valor = new TextBox
        {
            ReadOnly = true,
            TabStop = false,
            Font = FuenteValor,
            Dock = DockStyle.Fill,
            BackColor = SystemColors.Window,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 9, 6, 9),
            AccessibleName = accion,
        };

        var cambiar = CrearBoton("Cambiar", FuenteChica);
        cambiar.MinimumSize = new Size(96, 34);
        cambiar.Anchor = AnchorStyles.Left;
        cambiar.Click += (_, _) => AlternarCaptura(campo);

        Button? quitar = null;
        if (puedeQuitar)
        {
            quitar = CrearBoton("Quitar", FuenteChica);
            quitar.MinimumSize = new Size(84, 34);
            quitar.Anchor = AnchorStyles.Left;
            quitar.Click += (_, _) => QuitarAtajo(campo);
        }

        tabla.Controls.Add(etiqueta, 0, indice);
        tabla.Controls.Add(valor, 1, indice);
        tabla.Controls.Add(cambiar, 2, indice);
        if (quitar is not null)
        {
            tabla.Controls.Add(quitar, 3, indice);
        }

        _filas.Add(new Fila(campo, valor, cambiar, quitar));
    }

    // ------------------------------------------------------------------
    // Captura
    // ------------------------------------------------------------------

    private void AlternarCaptura(Campo campo)
    {
        if (_captura.Activa)
        {
            TerminarCaptura("No se cambió la tecla.");
            return;
        }

        _campoEnCaptura = campo;
        Fila? fila = _filas.FirstOrDefault(candidata => candidata.Campo == campo);
        if (fila is null)
        {
            return;
        }

        ModoCaptura modo = campo == Campo.Boton ? ModoCaptura.Boton : ModoCaptura.Atajo;
        _captura.Iniciar(modo, NativeMethods.EstaPresionada, Environment.TickCount64);

        foreach (Fila otra in _filas)
        {
            otra.Cambiar.Enabled = ReferenceEquals(otra, fila);
            otra.Cambiar.Text = ReferenceEquals(otra, fila) ? "Cancelar" : "Cambiar";
            if (otra.Quitar is not null)
            {
                otra.Quitar.Enabled = false;
            }
        }

        _guardar.Enabled = false;
        _cancelar.Enabled = true;
        _restablecer.Enabled = false;

        fila.Valor.Text = modo == ModoCaptura.Boton
            ? "Presioná una tecla…"
            : "Presioná la combinación…";
        fila.Valor.ForeColor = ColorInfo;
        MostrarEstado(
            modo == ModoCaptura.Boton
                ? "Esperando una tecla o un botón lateral del mouse. Escape cancela."
                : "Esperando la combinación de teclas. Escape cancela.",
            esError: false);
        _timer.Start();
    }

    private void AlTickCaptura(object? remitente, EventArgs e)
    {
        ResultadoCaptura resultado = _captura.Procesar(Environment.TickCount64);

        switch (resultado.Estado)
        {
            case EstadoCaptura.Pendiente:
            case EstadoCaptura.Ninguno:
                return;

            case EstadoCaptura.Error:
                MostrarEstado(resultado.Mensaje, esError: true);
                return;

            case EstadoCaptura.Cancelada:
                TerminarCaptura(resultado.Mensaje);
                return;

            case EstadoCaptura.CambioDetectado:
                if (resultado.Boton is BotonZoom boton)
                {
                    _valores = _valores with { Boton = boton };
                    TerminarCaptura($"Botón de zoom: {boton.NombreVisible}.", esError: false);
                }
                else if (resultado.Atajo is Atajo atajo)
                {
                    _valores = ConAtajo(atajo);
                    TerminarCaptura($"Atajo nuevo: {atajo.NombreVisible}.", esError: false);
                }

                return;
        }
    }

    private void TerminarCaptura(string mensaje, bool esError = false)
    {
        _timer.Stop();
        _captura.Cancelar();
        _campoEnCaptura = null;
        _finCapturaMs = Environment.TickCount64;

        foreach (Fila fila in _filas)
        {
            fila.Cambiar.Enabled = true;
            fila.Cambiar.Text = "Cambiar";
        }

        _guardar.Enabled = true;
        _restablecer.Enabled = true;

        ActualizarValores();
        MostrarMensajes();
        MostrarEstado(mensaje, esError);
    }

    private ConfigTeclas ConAtajo(Atajo atajo) => _campoEnCaptura switch
    {
        Campo.Activar => _valores with { Activar = atajo },
        Campo.Subir => _valores with { Subir = atajo },
        Campo.Bajar => _valores with { Bajar = atajo },
        Campo.SiguienteFiltro => _valores with { SiguienteFiltro = atajo },
        _ => _valores,
    };

    private void QuitarAtajo(Campo campo)
    {
        _valores = campo switch
        {
            Campo.Activar => _valores with { Activar = Atajo.Ninguno },
            Campo.Subir => _valores with { Subir = Atajo.Ninguno },
            Campo.Bajar => _valores with { Bajar = Atajo.Ninguno },
            Campo.SiguienteFiltro => _valores with { SiguienteFiltro = Atajo.Ninguno },
            _ => _valores,
        };

        ActualizarValores();
        MostrarMensajes();
        MostrarEstado("Atajo quitado. Apretá Guardar para aplicar el cambio.", esError: false);
    }

    // ------------------------------------------------------------------
    // Valores y acciones
    // ------------------------------------------------------------------

    private void ActualizarValores()
    {
        foreach (Fila fila in _filas)
        {
            if (fila.Campo == Campo.Boton)
            {
                fila.Valor.Text = _valores.Boton.NombreVisible;
            }
            else
            {
                Atajo atajo = AtajoDe(fila.Campo);
                fila.Valor.Text = atajo.NombreVisible;
                if (fila.Quitar is not null)
                {
                    fila.Quitar.Enabled = atajo.EsValido;
                }
            }

            fila.Valor.ForeColor = SystemColors.WindowText;
        }
    }

    private Atajo AtajoDe(Campo campo) => campo switch
    {
        Campo.Activar => _valores.Activar,
        Campo.Subir => _valores.Subir,
        Campo.Bajar => _valores.Bajar,
        _ => _valores.SiguienteFiltro,
    };

    private void MostrarMensajes()
    {
        string? error = _valores.Validar();
        if (error is not null)
        {
            _mensajes.ForeColor = ColorError;
            _mensajes.Text = error;
            return;
        }

        IReadOnlyList<string> avisos = _valores.Advertencias();
        _mensajes.ForeColor = ColorAviso;
        _mensajes.Text = avisos.Count == 0
            ? "Podés dejar un atajo sin asignar con «Quitar»."
            : string.Join("\r\n", avisos);
    }

    private void MostrarEstado(string mensaje, bool esError)
    {
        _estado.ForeColor = esError ? ColorError : ColorInfo;
        _estado.Text = mensaje;
    }

    private void Restablecer()
    {
        _valores = ConfigTeclas.PorDefecto;
        ActualizarValores();
        MostrarMensajes();
        MostrarEstado("Se cargaron los valores por defecto. Apretá Guardar para aplicarlos.", esError: false);
    }

    private void Guardar()
    {
        if (_captura.Activa)
        {
            TerminarCaptura("No se cambió la tecla.");
        }

        string? error = _valores.Validar();
        if (error is not null)
        {
            _mensajes.ForeColor = ColorError;
            _mensajes.Text = error;
            return;
        }

        error = _alGuardar(_valores);
        if (error is not null)
        {
            _mensajes.ForeColor = ColorError;
            _mensajes.Text = error;
            MostrarEstado("No se aplicaron los cambios.", esError: true);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    // ------------------------------------------------------------------
    // Cierre
    // ------------------------------------------------------------------

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            if (_captura.Activa)
            {
                TerminarCaptura("No se cambió la tecla.");
                return true;
            }

            // El mismo Escape que cerró la captura no debe cerrar la ventana.
            if (Environment.TickCount64 - _finCapturaMs < 300)
            {
                return true;
            }

            DialogResult = DialogResult.Cancel;
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void WndProc(ref Message m)
    {
        // Alt+F4 durante la captura cancela la captura, no la ventana.
        if (m.Msg == WM_SYSCOMMAND && _captura.Activa &&
            (m.WParam.ToInt64() & 0xFFF0) == SC_CLOSE)
        {
            TerminarCaptura("No se cambió la tecla.");
            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _timer.Stop();
        _captura.Cancelar();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }
}