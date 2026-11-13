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
    internal const int SM_XVIRTUALSCREEN = 76;
    internal const int SM_YVIRTUALSCREEN = 77;
    internal const int SM_CXVIRTUALSCREEN = 78;
    internal const int SM_CYVIRTUALSCREEN = 79;

    /// <summary>Si el punto no cae en ningún monitor, se usa el más cercano.</summary>
    internal const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    // Códigos de tecla virtual.
    internal const int VK_LBUTTON = 0x01;
    internal const int VK_RBUTTON = 0x02;
    internal const int VK_MBUTTON = 0x04;
    internal const int VK_XBUTTON1 = 0x05;
    internal const int VK_XBUTTON2 = 0x06;
    internal const int VK_ESCAPE = 0x1B;
    internal const int VK_SHIFT = 0x10;
    internal const int VK_CONTROL = 0x11;
    internal const int VK_MENU = 0x12;
    internal const int VK_LWIN = 0x5B;
    internal const int VK_RWIN = 0x5C;
    internal const int VK_LSHIFT = 0xA0;
    internal const int VK_RSHIFT = 0xA1;
    internal const int VK_LCONTROL = 0xA2;
    internal const int VK_RCONTROL = 0xA3;
    internal const int VK_LMENU = 0xA4;
    internal const int VK_RMENU = 0xA5;

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

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>Datos de un monitor que devuelve GetMonitorInfo.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
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

    [DllImport("user32.dll")]
    internal static extern IntPtr MonitorFromPoint(POINT punto, uint banderas);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO informacion);

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