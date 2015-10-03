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
                PixelFormat.Format24bppRgb);
            unsafe
            {
                var ptr = (byte*)data.Scan0;
                for (var y = 0; y < data.Height; y++)
                {
                    for (var x = 0; x < data.Width; x++)
                    {
                        var p = ptr + y * data.Stride + x * 3;
                        map[y, x] = (byte)(p[0] == 255 && p[1] == 255 && p[2] == 255 ? 1 : 0);
                    }
                }
            }
            bmp.UnlockBits(data);
            for (var y = 0; y < height - 1; y++)
            {
                var rect = Rectangle.Empty;
                if (!CheckEdge(map, 0, width, y, y, Edge.HorizontalTop))
                    continue;
                rect.Y = y + 1;
                for (var x = 0; x < width - 1; x++)
                {
                    if (!CheckEdge(map, x, x, rect.Y, height, Edge.VerticalLeft))
                        continue;
                    rect.X = x + 1;
                    rect = FindBottomAndRight(map, rect);
                    if (rect == Rectangle.Empty)
                        continue;
                    if (!CheckEdge(map, rect.X, rect.Right, y, y, Edge.HorizontalTop))
                        break;
                    if (CheckEdge(map, x, x, rect.Y, rect.Bottom, Edge.VerticalLeft))
                        return rect;
                }
            }
            return Rectangle.Empty;
        }

        private Rectangle FindBottomAndRight(byte[,] map, Rectangle rect)
        {
            var height = map.GetLength(0);
            var width = map.GetLength(1);
            for (var y = rect.Y; y < height - 1; y++)
            {
                if (!CheckEdge(map, rect.X, width, y, y, Edge.HorizontalBottom))
                    continue;
                rect.Height = y - rect.Y + 1;
                rect.Width = 0;
                for (var x = rect.X; x < width - 1; x++)
                {
                    if (!CheckEdge(map, x, x, rect.Y, rect.Bottom, Edge.VerticalRight))
                        continue;
                    rect.Width = x - rect.X + 1;
                    break;
                }
                if (rect.Width == 0)
                    return Rectangle.Empty;
                if (!CheckEdge(map, rect.X, rect.Right, y, y, Edge.HorizontalBottom))
                    continue;
                return rect.Width >= WidthMin && rect.Height >= HeightMin ? rect : Rectangle.Empty;
            }
            return Rectangle.Empty;
        }

        private bool CheckEdge(byte[,] map, int left, int right, int top, int bottom, Edge edge)
        {
            var n = 0;
            switch (edge)
            {
                case Edge.HorizontalTop:
                    for (; left < right; left++)
                    {
                        if (!(map[top, left] == 1 && map[top + 1, left] == 0))
                            continue;
                        if (++n < WidthMin * 2 / 3)
                            continue;
                        return true;
                    }
                    return false;
                case Edge.VerticalLeft:
                    for (; top < bottom; top++)
                    {
                        if (!(map[top, left] == 1 && map[top, left + 1] == 0))
                            continue;
                        if (++n < HeightMin * 2 / 3)
                            continue;
                        return true;
                    }
                    return false;
                case Edge.HorizontalBottom:
                    for (; left < right; left++)
                    {
                        if (!(map[bottom, left] == 0 && map[bottom + 1, left] == 1))
                            continue;
                        if (++n < WidthMin * 2 / 3)
                            continue;
                        return true;
                    }
                    return false;
                case Edge.VerticalRight:
                    for (; top < bottom; top++)
                    {
                        if (!(map[top, right] == 0 && map[top, right + 1] == 1))
                            continue;
                        if (++n < HeightMin * 2 / 3)
                            continue;
                        return true;
                    }
                    return false;
            }
            return false;
        }

        private enum Edge
        {
            HorizontalTop,
            VerticalLeft,
            HorizontalBottom,
            VerticalRight
        }
    }
}