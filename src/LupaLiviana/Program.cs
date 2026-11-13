using System.Threading;
using System.Windows.Forms;

namespace LupaLiviana;

internal static class Program
{
    // Nombre único del Mutex para impedir dos instancias.
    private const string NombreMutex = @"Local\LupaLiviana.InstanciaUnica.7F42C1D3";

    private static bool _errorMostrado;

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, NombreMutex, out bool esPrimeraInstancia);

        if (!esPrimeraInstancia)
        {
            MessageBox.Show(
                "Lupa Liviana ya se está ejecutando.\n\n" +
                "Buscá el ícono de la lupa en la bandeja del sistema, junto al reloj.",
                "Lupa Liviana",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Windows Forms se encarga del DPI a través de app.manifest (PerMonitorV2).
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += AlErrorEnHiloPrincipal;
        AppDomain.CurrentDomain.UnhandledException += AlErrorFatal;
        AppDomain.CurrentDomain.ProcessExit += AlProcesoTerminando;

        try
        {
            // Si una ejecución anterior quedó colgada con la pantalla ampliada,
            // se restaura antes de empezar.
            MagnifierService.LimpiarRestosDeSesionAnterior();

            using var aplicacion = new TrayApp();
            Application.Run(aplicacion);
        }
        finally
        {
            // Red de seguridad: pase lo que pase, la pantalla vuelve a 1.0.
            MagnifierService.RestaurarPantalla();
            GC.KeepAlive(mutex);
        }
    }

    private static void AlErrorEnHiloPrincipal(object sender, ThreadExceptionEventArgs e)
    {
        if (_errorMostrado)
        {
            return;
        }

        _errorMostrado = true;
        MagnifierService.RestaurarPantalla();

        try
        {
            MessageBox.Show(
                "Ocurrió un error inesperado y Lupa Liviana se va a cerrar.\n\n" +
                e.Exception.Message,
                "Lupa Liviana",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception)
        {
            // Si tampoco se puede mostrar el mensaje, se cierra igual.
        }

        Application.Exit();
    }

    private static void AlErrorFatal(object sender, UnhandledExceptionEventArgs e)
    {
        MagnifierService.RestaurarPantalla();

        if (_errorMostrado)
        {
            return;
        }

        _errorMostrado = true;

        try
        {
            string detalle = e.ExceptionObject is Exception ex ? ex.Message : "Error desconocido.";
            MessageBox.Show(
                "Lupa Liviana se cerró por un error inesperado.\n\n" + detalle,
                "Lupa Liviana",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception)
        {
            // La pantalla ya quedó restaurada; el mensaje es opcional.
        }
    }

    private static void AlProcesoTerminando(object? sender, EventArgs e)
    {
        MagnifierService.RestaurarPantalla();
    }
}