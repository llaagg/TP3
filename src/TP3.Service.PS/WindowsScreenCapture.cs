using System.Collections.Generic;
using System.IO;
using System.Drawing;
using TP3.Service.PS.WindowsAPI;

namespace TP3.Service.PS
{
    public static class WindowsScreenCapture
    {
        public static byte[] GetPng(nint handle)
        {
            using var image = WindowsAPI.WebApiWrapper.CaptureMonitor(handle);

            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var bytes = ms.ToArray();

            return bytes;
        }

        internal static IEnumerable<ScreenInfo> GetScreens()
        {
            var screens = WindowsAPI.WebApiWrapper.GetScreens();
            foreach (var screen in screens)
            {
                yield return screen;
            }
        }
    }
}