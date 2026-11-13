using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LupaLiviana;

/// <summary>
/// Genera por código los íconos de la bandeja del sistema, sin archivos
/// externos. Hay dos variantes: normal (azul) y con el zoom activo (verde).
/// </summary>
internal static class IconoTray
{
    private const int Lado = 32;

    /// <summary>Crea el ícono correspondiente al estado de la lupa.</summary>
    public static Icon Crear(bool activo)
    {
        using var mapa = new Bitmap(Lado, Lado, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(mapa))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            Color principal = activo
                ? Color.FromArgb(255, 46, 204, 113)
                : Color.FromArgb(255, 74, 144, 226);

            Color claro = activo
                ? Color.FromArgb(255, 235, 255, 240)
                : Color.FromArgb(255, 235, 242, 255);

            using var relleno = new SolidBrush(Color.FromArgb(activo ? 210 : 160, principal));
            using var lapiz = new Pen(claro, 2.4f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };

            var lente = new RectangleF(3f, 3f, 17f, 17f);
            g.FillEllipse(relleno, lente);
            g.DrawEllipse(lapiz, lente);
            g.DrawLine(lapiz, 18.5f, 18.5f, 27.5f, 27.5f);

            if (activo)
            {
                // Punto central que distingue el ícono cuando el zoom está encendido.
                using var punto = new SolidBrush(claro);
                g.FillEllipse(punto, 8.5f, 8.5f, 6f, 6f);
            }
        }

        IntPtr handle = mapa.GetHicon();
        try
        {
            using Icon temporal = Icon.FromHandle(handle);
            return (Icon)temporal.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}