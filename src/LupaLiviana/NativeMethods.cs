using System.Runtime.InteropServices;

namespace LupaLiviana;

/// <summary>
/// Declaraciones de las funciones de Windows que usa el programa.
///
/// Solo se utilizan funciones oficiales y de consulta: la API de lupa
/// (magnification.dll), la lectura del estado de teclas, la posición del
/// mouse, el registro de atajos y el ícono de la bandeja. No se instalan
/// hooks globales ni se inyecta código en otros procesos.
/// </summary>
internal static class NativeMethods
{
    // Índices de GetSystemMetrics.
    internal const int SM_CXSCREEN = 0;
    internal const int SM_CYSCREEN = 1;

    // Códigos de tecla virtual.
    internal const int VK_LBUTTON = 0x01;
    internal const int VK_RBUTTON = 0x02;
    internal const int VK_MBUTTON = 0x04;
    internal const int VK_XBUTTON1 = 0x05;
    internal const int VK_XBUTTON2 = 0x06;
    internal const int VK_ESCAPE = 0x1B;

    // Modificadores de RegisterHotKey.
    internal const uint MOD_ALT = 0x0001;
    internal const uint MOD_CONTROL = 0x0002;
    internal const uint MOD_SHIFT = 0x0004;
    internal const uint MOD_WIN = 0x0008;
    internal const uint MOD_NOREPEAT = 0x4000;

    internal const int WM_HOTKEY = 0x0312;

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// Matriz de color de 5x5 que usa MagSetColorEffect. Las primeras cuatro
    /// filas transforman RGBA y la última fila suma el valor constante.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MAGCOLOREFFECT
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
        public float[] Transform;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out POINT punto);

    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int teclaVirtual);

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int indice);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr ventana, int identificador, uint modificadores, uint tecla);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr ventana, int identificador);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr icono);

    [DllImport("magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool MagInitialize();

    [DllImport("magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool MagUninitialize();

    [DllImport("magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool MagSetFullscreenTransform(float factor, int desplazamientoX, int desplazamientoY);

    [DllImport("magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool MagGetFullscreenTransform(out float factor, out int desplazamientoX, out int desplazamientoY);

    [DllImport("magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool MagSetColorEffect(IntPtr ventana, ref MAGCOLOREFFECT efecto);

    /// <summary>Indica si una tecla o botón está presionado en este momento.</summary>
    internal static bool EstaPresionada(int teclaVirtual)
    {
        return (GetAsyncKeyState(teclaVirtual) & 0x8000) != 0;
    }
}