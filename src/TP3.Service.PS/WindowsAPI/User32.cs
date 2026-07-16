using System.Runtime.InteropServices;

public class User32
{

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
    public static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll", EntryPoint = "GetDC")]
    public static extern IntPtr GetDC(IntPtr ptr);

    [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
    public static extern int GetSystemMetrics(int abc);

    [DllImport("user32.dll", EntryPoint = "GetWindowDC")]
    public static extern IntPtr GetWindowDC(IntPtr ptr);

    [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
    public static extern IntPtr ReleaseDC(IntPtr hWnd,
       IntPtr hDc);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    // 2. Define the MONITORINFO structure to hold monitor details
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    // 3. Define the callback delegate matching MonitorEnumProc
    public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    // 4. Import the external Win32 functions
    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

}
