using static LupaLiviana.NativeMethods;

namespace LupaLiviana;

/// <summary>Filtro de color que se aplica a la pantalla ampliada.</summary>
internal enum FiltroColor
{
    Ninguno,
    Invertir,
    EscalaDeGrises,
    AltoContraste,
}

/// <summary>Intensidad del suavizado del seguimiento del mouse.</summary>
internal enum NivelSuavizado
{
    Ninguno,
    Bajo,
    Alto,
}

/// <summary>
/// Envoltorio de la API de lupa de Windows (magnification.dll) en modo
/// pantalla completa.
///
/// El zoom se aplica a todo el escritorio. El mouse se mapea solo: por eso
/// NO se llama a MagSetInputTransform, tal como pide el diseño.
/// </summary>
internal sealed class MagnifierService
{
    private bool _inicializada;
    private bool _seguimientoIniciado;
    private float _xSuave;
    private float _ySuave;
    private float _ultimoNivel = -1f;
    private int _ultimoDesplazamientoX = int.MinValue;
    private int _ultimoDesplazamientoY = int.MinValue;

    /// <summary>
    /// Si una ejecución anterior terminó de forma anormal, la pantalla puede
    /// haber quedado ampliada. Esto se comprueba y se corrige al arrancar.
    /// </summary>
    public static void LimpiarRestosDeSesionAnterior()
    {
        try
        {
            if (!MagInitialize())
            {
                return;
            }

            if (MagGetFullscreenTransform(out float factor, out _, out _) && Math.Abs(factor - 1f) > 0.001f)
            {
                MagSetFullscreenTransform(1f, 0, 0);
            }

            MagUninitialize();
        }
        catch (Exception)
        {
            // Si la API de lupa no está disponible, no hay nada que limpiar.
        }
    }

    /// <summary>
    /// Deja la pantalla en 1.0 y libera la API. Se usa ante errores, al cerrar
    /// la sesión de Windows y como red de seguridad al salir del programa.
    /// </summary>
    public static void RestaurarPantalla()
    {
        try
        {
            if (!MagInitialize())
            {
                return;
            }

            var identidad = CrearMatriz(FiltroColor.Ninguno);
            MagSetColorEffect(IntPtr.Zero, ref identidad);
            MagSetFullscreenTransform(1f, 0, 0);
            MagUninitialize();
        }
        catch (Exception)
        {
            // Nunca se debe propagar un error desde la rutina de emergencia.
        }
    }

    /// <summary>Inicializa la API de lupa si todavía no lo está.</summary>
    public bool AsegurarInicializada()
    {
        if (_inicializada)
        {
            return true;
        }

        try
        {
            _inicializada = MagInitialize();
        }
        catch (Exception)
        {
            _inicializada = false;
        }

        return _inicializada;
    }

    /// <summary>Aplica (o quita) el filtro de color sobre la pantalla ampliada.</summary>
    public void AplicarFiltro(FiltroColor filtro)
    {
        if (!_inicializada)
        {
            return;
        }

        var matriz = CrearMatriz(filtro);
        MagSetColorEffect(IntPtr.Zero, ref matriz);
    }

    /// <summary>
    /// Centra la vista ampliada en el cursor del mouse. Se llama desde el timer
    /// de ~60 Hz. <paramref name="factorSuavizado"/> vale 1 para seguir el cursor
    /// sin suavizado y se acerca a 0 cuanto más suave es el movimiento.
    /// No reserva memoria en cada llamada.
    /// </summary>
    public void SeguirCursor(float nivel, float factorSuavizado)
    {
        if (!AsegurarInicializada() || !GetCursorPos(out POINT cursor))
        {
            return;
        }

        float destinoX = cursor.X;
        float destinoY = cursor.Y;

        if (!_seguimientoIniciado || factorSuavizado >= 0.999f)
        {
            // Primer cuadro del seguimiento: arrancar exactamente en el cursor.
            _xSuave = destinoX;
            _ySuave = destinoY;
            _seguimientoIniciado = true;
        }
        else
        {
            _xSuave += (destinoX - _xSuave) * factorSuavizado;
            _ySuave += (destinoY - _ySuave) * factorSuavizado;

            if (Math.Abs(destinoX - _xSuave) < 0.5f)
            {
                _xSuave = destinoX;
            }

            if (Math.Abs(destinoY - _ySuave) < 0.5f)
            {
                _ySuave = destinoY;
            }
        }

        Aplicar(nivel, _xSuave, _ySuave);
    }

    /// <summary>Fija la vista ampliada en el centro de la pantalla principal.</summary>
    public void FijarCentro(float nivel)
    {
        if (!AsegurarInicializada())
        {
            return;
        }

        _seguimientoIniciado = false;

        int ancho = GetSystemMetrics(SM_CXSCREEN);
        int alto = GetSystemMetrics(SM_CYSCREEN);
        Aplicar(nivel, ancho / 2f, alto / 2f);
    }

    /// <summary>Vuelve el zoom a 1.0 y libera la API de lupa.</summary>
    public void Apagar()
    {
        try
        {
            if (_inicializada)
            {
                var identidad = CrearMatriz(FiltroColor.Ninguno);
                MagSetColorEffect(IntPtr.Zero, ref identidad);
                MagSetFullscreenTransform(1f, 0, 0);
                MagUninitialize();
            }
        }
        catch (Exception)
        {
            // Si algo falla, igual se restauran los valores internos.
        }
        finally
        {
            _inicializada = false;
            _seguimientoIniciado = false;
            _ultimoNivel = -1f;
            _ultimoDesplazamientoX = int.MinValue;
            _ultimoDesplazamientoY = int.MinValue;
        }
    }

    /// <summary>
    /// Convierte un punto de la pantalla (en píxeles físicos) en el
    /// desplazamiento que espera MagSetFullscreenTransform y lo aplica.
    ///
    /// Según la documentación, xOffset/yOffset son las coordenadas, en la
    /// pantalla sin ampliar, del punto que se muestra en la esquina superior
    /// izquierda. Para centrar el punto P hay que restarle la mitad del área
    /// visible (ancho / nivel).
    /// </summary>
    private void Aplicar(float nivel, float centroX, float centroY)
    {
        if (!_inicializada)
        {
            return;
        }

        if (nivel < 1f)
        {
            nivel = 1f;
        }

        int ancho = GetSystemMetrics(SM_CXSCREEN);
        int alto = GetSystemMetrics(SM_CYSCREEN);
        if (ancho <= 0 || alto <= 0)
        {
            return;
        }

        double anchoVisible = ancho / (double)nivel;
        double altoVisible = alto / (double)nivel;

        double desplazamientoX = centroX - (anchoVisible / 2.0);
        double desplazamientoY = centroY - (altoVisible / 2.0);

        // Limitar a los bordes: nunca se muestra "fuera" de la pantalla.
        double maximoX = Math.Max(0.0, ancho - anchoVisible);
        double maximoY = Math.Max(0.0, alto - altoVisible);
        desplazamientoX = Math.Clamp(desplazamientoX, 0.0, maximoX);
        desplazamientoY = Math.Clamp(desplazamientoY, 0.0, maximoY);

        int x = (int)Math.Round(desplazamientoX);
        int y = (int)Math.Round(desplazamientoY);

        // Evitar llamadas redundantes a la API cuando nada cambió.
        if (Math.Abs(nivel - _ultimoNivel) < 0.0005f && x == _ultimoDesplazamientoX && y == _ultimoDesplazamientoY)
        {
            return;
        }

        bool aplicado = MagSetFullscreenTransform(nivel, x, y);
        if (aplicado)
        {
            _ultimoNivel = nivel;
            _ultimoDesplazamientoX = x;
            _ultimoDesplazamientoY = y;
        }
    }

    /// <summary>Construye la matriz de color que corresponde a cada filtro.</summary>
    private static MAGCOLOREFFECT CrearMatriz(FiltroColor filtro)
    {
        float[] valores;

        switch (filtro)
        {
            case FiltroColor.Invertir:
                valores =
                [
                    -1f, 0f, 0f, 0f, 0f,
                    0f, -1f, 0f, 0f, 0f,
                    0f, 0f, -1f, 0f, 0f,
                    0f, 0f, 0f, 1f, 0f,
                    1f, 1f, 1f, 0f, 1f,
                ];
                break;

            case FiltroColor.EscalaDeGrises:
                valores =
                [
                    0.299f, 0.299f, 0.299f, 0f, 0f,
                    0.587f, 0.587f, 0.587f, 0f, 0f,
                    0.114f, 0.114f, 0.114f, 0f, 0f,
                    0f, 0f, 0f, 1f, 0f,
                    0f, 0f, 0f, 0f, 1f,
                ];
                break;

            case FiltroColor.AltoContraste:
                valores =
                [
                    2f, 0f, 0f, 0f, 0f,
                    0f, 2f, 0f, 0f, 0f,
                    0f, 0f, 2f, 0f, 0f,
                    0f, 0f, 0f, 1f, 0f,
                    -0.5f, -0.5f, -0.5f, 0f, 1f,
                ];
                break;

            default:
                valores =
                [
                    1f, 0f, 0f, 0f, 0f,
                    0f, 1f, 0f, 0f, 0f,
                    0f, 0f, 1f, 0f, 0f,
                    0f, 0f, 0f, 1f, 0f,
                    0f, 0f, 0f, 0f, 1f,
                ];
                break;
        }

        return new MAGCOLOREFFECT { Transform = valores };
    }
}