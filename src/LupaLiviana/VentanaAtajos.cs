using System.Windows.Forms;
using static LupaLiviana.NativeMethods;

namespace LupaLiviana;

/// <summary>
/// Ventana invisible que solo existe para recibir los mensajes WM_HOTKEY de
/// RegisterHotKey. No se muestra nunca y no aparece en la barra de tareas.
/// </summary>
internal sealed class VentanaAtajos : NativeWindow
{
    /// <summary>Se dispara con el identificador del atajo que se presionó.</summary>
    public event Action<int>? AtajoPresionado;

    public VentanaAtajos()
    {
        var parametros = new CreateParams
        {
            Caption = "Lupa Liviana (atajos)",
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0,
            Style = 0,
            ExStyle = 0,
        };

        CreateHandle(parametros);
    }

    /// <summary>Registra un atajo global. Devuelve false si otra aplicación ya lo usa.</summary>
    public bool Registrar(int identificador, uint modificadores, uint tecla)
    {
        if (Handle == IntPtr.Zero || tecla == 0)
        {
            return false;
        }

        return RegisterHotKey(Handle, identificador, modificadores | MOD_NOREPEAT, tecla);
    }

    /// <summary>Libera un atajo global registrado.</summary>
    public void Liberar(int identificador)
    {
        if (Handle != IntPtr.Zero)
        {
            UnregisterHotKey(Handle, identificador);
        }
    }

    /// <summary>Destruye la ventana oculta.</summary>
    public void Destruir()
    {
        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            AtajoPresionado?.Invoke((int)m.WParam);
        }

        base.WndProc(ref m);
    }
}