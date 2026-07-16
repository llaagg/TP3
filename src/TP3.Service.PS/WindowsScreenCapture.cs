namespace TP3.Service.PS
{
    public static class WindowsScreenCapture
    {
        public static byte[] GetPng()
        {
            using var image = WG.CaptureWindow();

            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var bytes = ms.ToArray();

            return bytes;
        }
    }
}