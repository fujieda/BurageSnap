// Copyright (C) 2015 Kazuhiro Fujieda <fujieda@users.osdn.me>
//
// This program is part of BurageSnap.
//
// BurageSnap is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace BurageSnap
{
    public class Capture
    {
        private const int WidthMin = 600, HeightMin = 400;
        private IntPtr _hWnd;
        private Rectangle _rectangle;

        public Bitmap CaptureGameScreen()
        {
            if (_hWnd == IntPtr.Zero || _rectangle.IsEmpty)
                return null;
            using (var bmp = CaptureWindow(_hWnd))
                return bmp.Clone(_rectangle, bmp.PixelFormat);
        }

        public Bitmap CaptureGameScreen(string title)
        {
            _hWnd = FindWindow(title);
            if (_hWnd == IntPtr.Zero)
                return null;
            using (var bmp = CaptureWindow(_hWnd))
            {
                _rectangle = DetectGameScreen(bmp);
                if (_rectangle.IsEmpty)
                    return null;
                return bmp.Clone(_rectangle, bmp.PixelFormat);
            }
        }

        private IntPtr FindWindow(string title)
        {
            var found = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                var rect = new Rect();
                if (GetWindowRect(hWnd, ref rect) == 0 || rect.Right - rect.Left < WidthMin ||
                    rect.Bottom - rect.Top < HeightMin)
                    return true;
                if (!GetWindowText(hWnd).Contains(title))
                    return true;
                found = hWnd;
                return false;
            }, IntPtr.Zero);
            return found;
        }

        public static string GetWindowText(IntPtr hWnd)
        {
            var size = GetWindowTextLength(hWnd);
            if (size == 0)
                return "";
            var sb = new StringBuilder(size + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        public static Bitmap CaptureWindow(IntPtr hWnd)
        {
            var rect = new Rect();
            GetWindowRect(hWnd, ref rect);
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            bmp.Save("debug.jpg", ImageFormat.Jpeg);
            return bmp;
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hWnd, ref Rect lpRec);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private Rectangle DetectGameScreen(Bitmap bmp)
        {
            var height = bmp.Height;
            var width = bmp.Width;
            var map = new byte[height, width];
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            unsafe
            {
                var ptr = (byte*)data.Scan0;
                for (var y = 0; y < data.Height; y++)
                {
                    for (var x = 0; x < data.Width; x++)
                    {
                        var p = ptr + y * data.Stride + x * 4;
                        map[y, x] = (byte)(p[0] == 255 && p[1] == 255 && p[2] == 255 ? 1 : 0);
                    }
                }
            }
            bmp.UnlockBits(data);
            const int corner = 40;
            for (var y = 0; y < height; y++)
            {
                var n = 0;
                for (var x = 0; x < width; x++)
                {
                    if ((map[y, x] & 1) == 1)
                    {
                        if (++n >= corner)
                            map[y, x - corner + 1] |= 2;
                    }
                    else
                    {
                        n = 0;
                    }
                }
            }
            for (var x = 0; x < width; x++)
            {
                var n = 0;
                for (var y = 0; y < height; y++)
                {
                    if ((map[y, x] & 1) == 1)
                    {
                        if (++n >= corner)
                            map[y - corner + 1, x] |= 4;
                    }
                    else
                    {
                        n = 0;
                    }
                }
            }
            var rect = new Rectangle();
            var found = false;
            for (var y = 0; y < height - corner; y++)
            {
                for (var x = 0; x < height - corner; x++)
                {
                    if (!(map[y, x] == 7 && map[y + 1, x + 1] == 0))
                        continue;
                    rect.X = x + 1;
                    rect.Y = y + 1;
                    for (var x1 = rect.X; x1 < width; x1++)
                    {
                        if ((map[rect.Y, x1] & 4) == 0)
                            continue;
                        rect.Width = x1 - rect.X;
                        break;
                    }
                    if (rect.Width < WidthMin)
                        continue;
                    for (var y1 = rect.Y; y1 < height; y1++)
                    {
                        if ((map[y1, rect.X] & 2) == 0)
                            continue;
                        rect.Height = y1 - rect.Y;
                        break;
                    }
                    if (rect.Height < HeightMin)
                        continue;
                    found = true;
                    break;
                }
                if (found)
                    break;
            }
            return found ? rect : Rectangle.Empty;
        }
    }
}