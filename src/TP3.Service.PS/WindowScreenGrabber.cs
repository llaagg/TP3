using System.Drawing;

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
}