using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CarbonIntensity
{
    class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SystemParametersInfo(UInt32 action, UInt32 uParam, String vParam, UInt32 winIni);

        private static readonly UInt32 SPI_SETDESKWALLPAPER = 0x14;
        private static readonly UInt32 SPIF_UPDATEINIFILE = 0x01;
        private static readonly UInt32 SPIF_SENDWININICHANGE = 0x02;

        static public void SetWallpaper(String path)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            key.SetValue(@"WallpaperStyle", 0.ToString()); // 2 is stretched
            key.SetValue(@"TileWallpaper", 0.ToString());

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }

        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            await ProcessRepositories();
        }

        private static async Task ProcessRepositories()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
                );
            client.DefaultRequestHeaders.Add("User-Agent", "Grid Intensity Wallpaper Generator");

            var stringTask = client.GetStringAsync("https://api.carbonintensity.org.uk/intensity/date");

            var msg = await stringTask;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var results = JsonSerializer.Deserialize<IntensityDateModel>(msg, options);

            using (var image = new Bitmap(1920, 1080))
            {
                using (Graphics g = Graphics.FromImage(image))
                {
                    double factor = 256.0f / image.Width;
                    int columnWidth = image.Width / results.data.Count;

                    int x = 0;

                    const int lower = 50;
                    const int upper = 250;

                    Rectangle rectNow = new Rectangle();
                    int co2now = 0;

                    Color shadePrevious = Color.White;

                    foreach (var intensityDate in results.data)
                    {
                        bool now = intensityDate.from <= DateTime.Now && intensityDate.to >= DateTime.Now;

                        int value = (intensityDate.intensity.actual == null) ? intensityDate.intensity.forecast : (int)intensityDate.intensity.actual;

                        Color shade = Color.White;

                        if (value <= lower)
                        {
                            shade = Color.Green;
                        }
                        else if (value >= upper)
                        {
                            shade = Color.Red;
                        }
                        else
                        {
                            double c = (double)(value - lower) / (double)(upper - lower);

                            c = 1.0f - c;

                            //Console.WriteLine("{0} : {1}", value, c);

                            shade = GetBlendedColor((int)(c * 100.0f));
                        }

                        if (shadePrevious == Color.White)
                            shadePrevious = shade;

                        LinearGradientBrush brush = new LinearGradientBrush(
                            new Point(0, 0), 
                            new Point(columnWidth, 0), 
                            shadePrevious, 
                            shade
                            );

                        shadePrevious = shade;

                        //g.FillRectangle(new SolidBrush(shade), x, 0, columnWidth, image.Height);
                        g.FillRectangle(brush, x, 0, columnWidth, image.Height);

                        if (now)
                        {
                            rectNow = new Rectangle(x, -20, columnWidth, image.Height + 20);
                            co2now = value;
                        }

                        x += columnWidth;
                    }

                    Color color = Color.FromArgb(127, Color.AliceBlue);

                    //g.DrawRectangle(new Pen(color, 5.0f), rectNow);
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;
                    g.DrawLine(new Pen(color, 5.0f), rectNow.X + rectNow.Width / 2, rectNow.Y, rectNow.X + rectNow.Width / 2, rectNow.Height);
                    g.DrawString(
                        co2now.ToString(),
                        new Font("Calibri", 64),
                        new SolidBrush(color),
                        1780, 920
                        );
                    g.DrawString(
                        String.Format("gC0₂/kWh", co2now),
                        new Font("Calibri", 16),
                        new SolidBrush(color),
                        1790, 1000
                        );
                }

                const string filename = @"c:\temp\intensity.png";

                using (var output = File.Open(filename, FileMode.OpenOrCreate))
                {
                    image.Save(output, ImageFormat.Png);
                }

                if (File.Exists(filename))
                {
                    SetWallpaper(filename);
                }
            }
        }

        private static Color GetBlendedColor(int percentage)
        {
            if (percentage < 50)
            {
                return Interpolate(Color.Red, Color.Yellow, percentage / 50.0);
            }
            else
            {
                return Interpolate(Color.Yellow, Color.Green, (percentage - 50) / 50.0);
            }
        }

        private static Color Interpolate(Color color1, Color color2, double fraction)
        {
            double r = Interpolate(color1.R, color2.R, fraction);
            double g = Interpolate(color1.G, color2.G, fraction);
            double b = Interpolate(color1.B, color2.B, fraction);

            return Color.FromArgb((int)Math.Round(r), (int)Math.Round(g), (int)Math.Round(b));
        }

        private static double Interpolate(double d1, double d2, double fraction)
        {
            return d1 + (d2 - d1) * fraction;
        }
    }
}
