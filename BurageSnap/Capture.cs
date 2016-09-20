// Copyright (C) 2015 Kazuhiro Fujieda <fujieda@users.osdn.me>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BurageSnap.Properties;

namespace BurageSnap
{
    public class CaptureError : Exception
    {
        public CaptureError(string message) : base(message)
        {
        }
    }

    public class Capture
    {
        private const int WidthMin = 600, HeightMin = 400;
        private IntPtr _hWnd;
        private Rect _windowRect;
        private Rectangle _rectangle;
        private string _title;

        public Bitmap CaptureGameScreen()
        {
            if (_hWnd == IntPtr.Zero || _rectangle.IsEmpty)
                throw new CaptureError(Resources.Capture_Internal_error);
            using (var bmp = CaptureWindow(_hWnd, _windowRect))
                return bmp.Clone(_rectangle, bmp.PixelFormat);
        }

        public Bitmap CaptureGameScreen(string[] titles)
        {
            int index;
            _hWnd = FindWindow(titles, out index);
            if (_hWnd == IntPtr.Zero)
                throw new CaptureError(Resources.Capture_Search_error);
            var rect = new Rect();
            GetWindowRect(_hWnd, ref rect);
            using (var bmp = CaptureWindow(_hWnd, rect))
            {
                var rectangle = DetectGameScreen(bmp);
                if (!rectangle.IsEmpty)
                {
                    _windowRect = rect;
                    _rectangle = rectangle;
                    _title = titles[index];
                }
                else
                {
                    using (var file = File.Create("debug.png"))
                        bmp.Save(file, ImageFormat.Png);
                    if (_rectangle.IsEmpty || !_windowRect.Equals(rect) || _title != titles[index])
                        throw new CaptureError(Resources.Capture_Extract_error);
                }
                return bmp.Clone(_rectangle, bmp.PixelFormat);
            }
        }

        private IntPtr FindWindow(string[] titles, out int index)
        {
            var found = IntPtr.Zero;
            var idx = 0;
            EnumWindows((hWnd, lParam) =>
            {
                var rect = new Rect();
                if (GetWindowRect(hWnd, ref rect) == 0 || rect.Right - rect.Left < WidthMin ||
                    rect.Bottom - rect.Top < HeightMin)
                    return true;
                var text = GetWindowText(hWnd);
                for (var i = 0; i < titles.Length; i++)
                    if (text.Contains(titles[i]))
                    {
                        found = hWnd;
                        idx = i;
                        return false;
                    }
                return true;
            }, IntPtr.Zero);
            index = idx;
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

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        public static Bitmap CaptureWindow(IntPtr hWnd, Rect rect)
        {
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
        public struct Rect : IEquatable<Rect>
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public bool Equals(Rect other)
            {
                return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
            }
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
            for (var y = 1; y < height - 1; y++)
            {
                if (!CheckEdge(map, 0, width - 1, y, y, Edge.HorizontalTop))
                    continue;
                for (var x = 1; x < width - 1; x++)
                {
                    var rect = Rectangle.Empty;
                    rect.Y = y;
                    if (!CheckEdge(map, x, x, rect.Y, height - 1, Edge.VerticalLeft))
                        continue;
                    rect.X = x;
                    rect = FindBottomAndRight(map, rect);
                    if (rect == Rectangle.Empty)
                        continue;
                    if (!CheckEdgeStrict(map, rect.X, rect.Right, y, y, Edge.HorizontalTop))
                        break;
                    if (!CheckEdgeStrict(map, x, x, rect.Y, rect.Bottom, Edge.VerticalLeft))
                        continue;
                    RoundUpRectangle(map, ref rect);
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
                if (!CheckEdge(map, rect.X, width - 1, y, y, Edge.HorizontalBottom))
                    continue;
                rect.Height = y - rect.Y + 1;
                rect.Width = 0;
                for (var x = rect.X; x < width - 1; x++)
                {
                    if (!CheckEdgeStrict(map, x, x, rect.Y, rect.Bottom, Edge.VerticalRight))
                        continue;
                    rect.Width = x - rect.X + 1;
                    break;
                }
                if (rect.Width == 0)
                    continue;
                if (CheckEdgeStrict(map, rect.X, rect.Right, rect.Bottom, rect.Bottom, Edge.HorizontalBottom))
                    break;
            }
            if (rect.Width == 0)
                return Rectangle.Empty;
            // check a smaller rectangle
            for (var y = rect.Y; y <= rect.Bottom; y++)
            {
                if (CheckEdgeStrict(map, rect.X, rect.Right, y, y, Edge.HorizontalBottom))
                {
                    rect.Height = y - rect.Y + 1;
                    break;
                }
            }
            return rect.Width >= WidthMin && rect.Height >= HeightMin ? rect : Rectangle.Empty;
        }

        private bool CheckEdge(byte[,] map, int left, int right, int top, int bottom, Edge edge)
        {
            var n = 0;
            switch (edge)
            {
                case Edge.HorizontalTop:
                    for (var x = left; x <= right; x++)
                    {
                        if (!(map[top - 1, x] == 1 && map[top, x] == 0))
                            continue;
                        if (++n < WidthMin / 3)
                            continue;
                        return true;
                    }
                    return false;
                case Edge.VerticalLeft:
                    for (var y = top; y <= bottom; y++)
                    {
                        if (!(map[y, left - 1] == 1 && map[y, left] == 0))
                            continue;
                        if (++n < HeightMin / 3)
                            continue;
                        return true;
                    }
                    return false;
                case Edge.HorizontalBottom:
                    for (var x = left; x <= right; x++)
                    {
                        if (!(map[bottom, x] == 0 && map[bottom + 1, x] == 1))
                            continue;
                        if (++n < WidthMin / 3)
                            continue;
                        return true;
                    }
                    return false;
                case Edge.VerticalRight:
                    for (var y = top; y <= bottom; y++)
                    {
                        if (!(map[y, right] == 0 && map[y, right + 1] == 1))
                            continue;
                        if (++n < HeightMin / 3)
                            continue;
                        return true;
                    }
                    return false;
            }
            return false;
        }

        private bool CheckEdgeStrict(byte[,] map, int left, int right, int top, int bottom, Edge edge)
        {
            return CheckEdge(map, left, right, top, bottom, edge) &&
                   CheckEitherEndClean(map, left, right, top, bottom, edge) &&
                   CheckEnoughLength(map, left, right, top, bottom, edge);
        }

        private bool CheckEitherEndClean(byte[,] map, int left, int right, int top, int bottom, Edge edge)
        {
            switch (edge)
            {
                case Edge.HorizontalTop:
                    for (var x = left; x <= left + WidthMin / 10; x++)
                    {
                        if (map[top - 1, x] == 0)
                            goto tright;
                    }
                    return true;
                    tright:
                    for (var x = right; x >= right - WidthMin / 10; x--)
                    {
                        if (map[top - 1, x] == 0)
                            return false;
                    }
                    return true;
                case Edge.VerticalLeft:
                    for (var y = top; y <= top + HeightMin / 10; y++)
                    {
                        if (map[y, left - 1] == 0)
                            goto lbottom;
                    }
                    return true;
                    lbottom:
                    for (var y = bottom; y >= bottom - HeightMin / 10; y--)
                    {
                        if (map[y, left - 1] == 0)
                            return false;
                    }
                    return true;
                case Edge.HorizontalBottom:
                    for (var x = left; x <= left + WidthMin / 10; x++)
                    {
                        if (map[bottom + 1, x] == 0)
                            goto bright;
                    }
                    return true;
                    bright:
                    for (var x = right; x >= right - WidthMin / 10; x--)
                    {
                        if (map[bottom + 1, x] == 0)
                            return false;
                    }
                    return true;
                case Edge.VerticalRight:
                    for (var y = top; y <= top + HeightMin / 10; y++)
                    {
                        if (map[y, right + 1] == 0)
                            goto rbottom;
                    }
                    return true;
                    rbottom:
                    for (var y = bottom; y >= bottom - HeightMin / 10; y--)
                    {
                        if (map[y, right + 1] == 0)
                            return false;
                    }
                    return true;
            }
            return false;
        }

        private bool CheckEnoughLength(byte[,] map, int left, int right, int top, int bottom, Edge edge)
        {
            var n = 0;
            var hlen = (right - left + 1) * 0.7;
            var vlen = (bottom - top + 1) * 0.7;
            switch (edge)
            {
                case Edge.HorizontalTop:
                    for (var x = left; x <= right; x++)
                    {
                        if (map[top - 1, x] == 1)
                            n++;
                    }
                    return n >= hlen;
                case Edge.VerticalLeft:
                    for (var y = top; y <= bottom; y++)
                    {
                        if (map[y, left - 1] == 1)
                            n++;
                    }
                    return n >= vlen;
                case Edge.HorizontalBottom:
                    for (var x = left; x <= right; x++)
                    {
                        if (map[bottom + 1, x] == 1)
                            n++;
                    }
                    return n >= hlen;
                case Edge.VerticalRight:
                    for (var y = top; y <= bottom; y++)
                    {
                        if (map[y, right + 1] == 1)
                            n++;
                    }
                    return n >= vlen;
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

        public void RoundUpRectangle(byte[,] map, ref Rectangle rect)
        {
            var r = rect.Height % 10;
            if (r != 0)
            {
                var top = 0;
                var bottom = 0;
                for (var x = rect.X; x < rect.Right; x++)
                {
                    if (map[rect.Top - 1, x] == 1)
                        top++;
                    if (map[rect.Bottom + 1, x] == 1)
                        bottom++;
                }
                rect.Height += 10 - r;
                if (top <= bottom) // expand unbiguous edge
                    rect.Y -= 10 - r;
            }
            r = rect.Width % 10;
            if (r != 0)
            {
                var left = 0;
                var right = 0;
                for (var y = rect.Y; y < rect.Bottom; y++)
                {
                    if (map[y, rect.Left - 1] == 1)
                        left++;
                    if (map[y, rect.Right + 1] == 1)
                        right++;
                }
                rect.Width += 10 - r;
                if (right <= left) // expand unbiguous edge
                    rect.X -= 10 - r;
            }
        }
    }
}