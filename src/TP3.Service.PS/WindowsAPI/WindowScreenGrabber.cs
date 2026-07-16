using System.Drawing;
using System.Runtime.InteropServices;

public static class WG
{
    /// <summary>
    /// Captures a window and returns it as an Image.
    /// If handle is null, it will capture the entire screen.
    /// </summary>
    public static Image CaptureWindow(IntPtr? handle = null)
    {
        // get the hDC of the target window
        IntPtr hWnd = handle ?? IntPtr.Zero;
        IntPtr hdcSrc = User32.GetWindowDC(hWnd);

        // get the size
        var rectTarget = handle.HasValue ? hWnd : User32.GetDesktopWindow();
        RECT windowRect = new RECT();
        if (!User32.GetWindowRect(rectTarget, ref windowRect))
        {
            throw new InvalidOperationException("Failed to get window rectangle.");
        }

        int width = windowRect.right - windowRect.left;
        int height = windowRect.bottom - windowRect.top;
        // create a device context we can copy to
        IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
        // create a bitmap we can copy it to,
        // using GetDeviceCaps to get the width/height
        IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
        // select the bitmap object
        IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
        // bitblt over
        GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
        // restore selection
        GDI32.SelectObject(hdcDest, hOld);
        // clean up 
        GDI32.DeleteDC(hdcDest);
        User32.ReleaseDC(handle ?? IntPtr.Zero, hdcSrc);

        // get a .NET image object for it
        Image img = Image.FromHbitmap(hBitmap!);
        // free up the Bitmap object
        GDI32.DeleteObject(hBitmap);

        return img;
    }

    public static Image CaptureMonitor(IntPtr handle)
    {
        User32.MONITORINFO info = new User32.MONITORINFO();
        info.cbSize = Marshal.SizeOf(typeof(User32.MONITORINFO));

        if (!User32.GetMonitorInfo(handle, ref info))
        {
            throw new InvalidOperationException("Failed to get monitor information.");
        }

        return CaptureScreenArea(info.rcMonitor.left, info.rcMonitor.top, info.rcMonitor.right - info.rcMonitor.left, info.rcMonitor.bottom - info.rcMonitor.top);
    }

    private static Image CaptureScreenArea(int left, int top, int width, int height)
    {
        IntPtr hdcSrc = User32.GetDC(IntPtr.Zero);
        if (hdcSrc == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get the desktop device context.");
        }

        IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
        IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
        IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);

        try
        {
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, left, top, GDI32.SRCCOPY);
            return Image.FromHbitmap(hBitmap);
        }
        finally
        {
            GDI32.SelectObject(hdcDest, hOld);
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(IntPtr.Zero, hdcSrc);
            GDI32.DeleteObject(hBitmap);
        }
    }

    internal static IEnumerable<ScreenInfo> GetScreens()
    {

        List<ScreenInfo> screens = new List<ScreenInfo>();

        //User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorCallback, IntPtr.Zero);
        User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
        {
            User32.MONITORINFO info = new User32.MONITORINFO();
            info.cbSize = Marshal.SizeOf(typeof(User32.MONITORINFO));

            if (User32.GetMonitorInfo(hMonitor, ref info))
            {
                int width = info.rcMonitor.right - info.rcMonitor.left;
                int height = info.rcMonitor.bottom - info.rcMonitor.top;
                bool isPrimary = (info.dwFlags & 1) != 0; // MONITORINFOF_PRIMARY
                
                screens.Add(new ScreenInfo { Handle = hMonitor, IsPrimary = isPrimary, Width = width, Height = height });
            }
            return true; // Continue enumeration
        }, IntPtr.Zero);
        
        return screens;
    }

}

public class ScreenInfo
{
    public IntPtr Handle { get; set; }
    public bool IsPrimary { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}