using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using BurageSnap;

namespace CaptureTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                return;
            var capture = new Capture();
            using (var bmp = new Bitmap(args[0]))
            {
                var rectangle = capture.DetectGameScreen(bmp);
                if (rectangle.IsEmpty)
                {
                    Console.WriteLine(@"extract error");
                    return;
                }
                using (var file = File.Create(Path.Combine(Path.GetDirectoryName(args[0]) ?? "", Path.GetFileNameWithoutExtension(args[0]) + "_result" + Path.GetExtension(args[0]))))
                using (var cripped = bmp.Clone(rectangle, bmp.PixelFormat))
                    cripped.Save(file, ImageFormat.Png);
            }
        }
    }
}
