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
        public string Summary { get; }

        public CaptureError(string summary, string message) : base(message)
        {
            Summary = summary;
        }
    }

    public class Capture
    {
        private const int WidthMin = 600, HeightMin = 400;
        private IntPtr _hWnd;
        private Rect _windowRect;
        private Rectangle _rectangle;

        public string Title { get; private set; }

        public Bitmap CaptureGameScreen()
        {
            if (_hWnd == IntPtr.Zero || _rectangle.IsEmpty)
                throw new CaptureError(Resources.Capture_Internal_error, Resources.Capture_Internal_error);
            using (var bmp = CaptureWindow(_hWnd, _windowRect))
                return bmp.Clone(_rectangle, bmp.PixelFormat);
        }

        public Bitmap CaptureGameScreen(string[] titles)
        {
            int index;
            _hWnd = FindWindow(titles, out index);
            if (_hWnd == IntPtr.Zero)
                throw new CaptureError(Resources.Capture_Search_error,
                    Resources.Capture_Cant_find_window);
            var rect = new Rect();
            GetWindowRect(_hWnd, ref rect);
            using (var bmp = CaptureWindow(_hWnd, rect))
            {
                var rectangle = DetectGameScreen(bmp);
                if (!rectangle.IsEmpty)
                {
                    _windowRect = rect;
                    _rectangle = rectangle;
                    Title = titles[index];
                }
                else
                {
                    using (var file = File.Create("debug.png"))
                        bmp.Save(file, ImageFormat.Png);
                    if (_rectangle.IsEmpty || !_windowRect.Equals(rect) || Title != titles[index])
                        throw new CaptureError(Resources.Capture_Extract_error,
                            Resources.Capture_Cant_extract_game_screen);
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

        public Rectangle DetectGameScreen(Bitmap bmp)
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
            for (var y = 1; y < height; y++)
            {
                if (!CheckEdgeHorizontalTop(map, 0, width, y, y))
                    continue;
                for (var x = 1; x < width; x++)
                {
                    var rect = Rectangle.Empty;
                    rect.Y = y;
                    if (!CheckEdgeVerticalLeft(map, x, x, rect.Y, height))
                        continue;
                    rect.X = x;
                    rect = FindBottomAndRight(map, rect);
                    if (rect == Rectangle.Empty)
                        continue;
                    if (!CheckEdgeStrictHorizontalTop(map, rect.X, rect.Right, y, y))
                        break;
                    if (!CheckEdgeStrictVerticalLeft(map, x, x, rect.Y, rect.Bottom))
                        continue;
                    rect = FindTopAndLeft(map, rect);
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
            for (var y = rect.Y; y < height; y++)
            {
                if (!CheckEdgeHorizontalBottom(map, rect.X, width, y, y))
                    continue;
                rect.Height = y - rect.Y;
                rect.Width = 0;
                for (var x = rect.X; x < width; x++)
                {
                    if (!CheckEdgeStrictVerticalRight(map, x, x, rect.Y, rect.Bottom))
                        continue;
                    rect.Width = x - rect.X;
                    break;
                }
                if (rect.Width == 0)
                    continue;
                if (CheckEdgeStrictHorizontalBottom(map, rect.X, rect.Right, rect.Bottom, rect.Bottom))
                    break;
            }
            if (rect.Width == 0)
                return Rectangle.Empty;
            // check a smaller rectangle
            for (var y = rect.Y; y <= rect.Bottom; y++)
            {
                if (CheckEdgeStrictHorizontalBottom(map, rect.X, rect.Right, y, y))
                {
                    rect.Height = y - rect.Y;
                    break;
                }
            }
            return rect.Width >= WidthMin && rect.Height >= HeightMin ? rect : Rectangle.Empty;
        }

        private Rectangle FindTopAndLeft(byte[,] map, Rectangle rect)
        {
            for (var y = rect.Bottom - 1; y >= rect.Y; y--)
            {
                if (CheckEdgeStrictHorizontalTop(map, rect.Left, rect.Right, y, y))
                {
                    rect.Height += rect.Y - y;
                    rect.Y = y;
                    break;
                }
            }
            for (var x = rect.Right - 1; x >= rect.X; x--)
            {
                if (CheckEdgeStrictVerticalLeft(map, x, x, rect.Top, rect.Bottom))
                {
                    rect.Width += rect.X - x;
                    rect.X = x;
                    break;
                }
            }
            return rect;
        }

        const int EdgeWidth = WidthMin / 2;
        const int EdgeHeight = HeightMin / 2;

        // ReSharper disable UnusedParameter.Local
        private bool CheckEdgeHorizontalTop(byte[,] map, int left, int right, int top, int bottom)

        {
            var n = 0;
            for (var x = left; x < right; x++)
            {
                if (!(map[top - 1, x] == 1 && map[top, x] == 0))
                    continue;
                if (++n < EdgeWidth)
                    continue;
                return true;
            }
            return false;
        }

        private bool CheckEdgeVerticalLeft(byte[,] map, int left, int right, int top, int bottom)
        {
            var n = 0;
            for (var y = top; y < bottom; y++)
            {
                if (!(map[y, left - 1] == 1 && map[y, left] == 0))
                    continue;
                if (++n < EdgeHeight)
                    continue;
                return true;
            }
            return false;
        }

        private bool CheckEdgeHorizontalBottom(byte[,] map, int left, int right, int top, int bottom)
        {
            var n = 0;
            for (var x = left; x < right; x++)
            {
                if (!(map[bottom - 1, x] == 0 && map[bottom, x] == 1))
                    continue;
                if (++n < EdgeWidth)
                    continue;
                return true;
            }
            return false;
        }

        private bool CheckEdgeVerticalRight(byte[,] map, int left, int right, int top, int bottom)
        {
            var n = 0;
            for (var y = top; y < bottom; y++)
            {
                if (!(map[y, right - 1] == 0 && map[y, right] == 1))
                    continue;
                if (++n < EdgeHeight)
                    continue;
                return true;
            }
            return false;
        }

        private bool CheckEdgeStrictHorizontalTop(byte[,] map, int left, int right, int top, int bottom)
        {
            return CheckEdgeHorizontalTop(map, left, right, top, bottom) &&
                   CheckBothEndCleanHorizontalTop(map, left, right, top, bottom) &&
                   CheckEnoughLengthHorizontalTop(map, left, right, top, bottom);
        }

        private bool CheckEdgeStrictVerticalLeft(byte[,] map, int left, int right, int top, int bottom)
        {
            return CheckEdgeVerticalLeft(map, left, right, top, bottom) &&
                   CheckBothEndCleanVerticalLeft(map, left, right, top, bottom) &&
                   CheckEnoughLengthVerticalLeft(map, left, right, top, bottom);
        }

        private bool CheckEdgeStrictHorizontalBottom(byte[,] map, int left, int right, int top, int bottom)
        {
            return CheckEdgeHorizontalBottom(map, left, right, top, bottom) &&
                   CheckBothEndCleanHorizontalBottom(map, left, right, top, bottom) &&
                   CheckEnoughLengthHorizontalBottom(map, left, right, top, bottom);
        }

        private bool CheckEdgeStrictVerticalRight(byte[,] map, int left, int right, int top, int bottom)
        {
            return CheckEdgeVerticalRight(map, left, right, top, bottom) &&
                   CheckBothEndCleanVerticalRight(map, left, right, top, bottom) &&
                   CheckEnoughLengthVerticalRight(map, left, right, top, bottom);
        }

        const int DecorationThickness = 20;

        private bool CheckBothEndCleanHorizontalTop(byte[,] map, int left, int right, int top, int bottom)
        {
            for (var margin = 0; margin < DecorationThickness; margin++)
            {
                if (top - margin - 1 < 0)
                    return false;
                for (var x = left; x <= left + WidthMin / 10; x++)
                {
                    if (map[top - margin - 1, x] == 0)
                        goto last;
                }
                for (var x = right; x >= right - WidthMin / 10; x--)
                {
                    if (map[top - margin - 1, x] == 0)
                        goto last;
                }
                return true;
                last:;
            }
            return false;
        }

        private bool CheckBothEndCleanVerticalLeft(byte[,] map, int left, int right, int top, int bottom)
        {
            for (var margin = 0; margin < DecorationThickness; margin++)
            {
                if (left - margin - 1 < 0)
                    return false;
                for (var y = top; y <= top + HeightMin / 10; y++)
                {
                    if (map[y, left - margin - 1] == 0)
                        goto last;
                }
                for (var y = bottom; y >= bottom - HeightMin / 10; y--)
                {
                    if (map[y, left - margin - 1] == 0)
                        goto last;
                }
                return true;
                last:;
            }
            return false;
        }

        private bool CheckBothEndCleanHorizontalBottom(byte[,] map, int left, int right, int top, int bottom)
        {
            for (var margin = 0; margin < DecorationThickness; margin++)
            {
                if (bottom + margin >= map.GetLength(0))
                    return false;
                for (var x = left; x <= left + WidthMin / 10; x++)
                {
                    if (map[bottom + margin, x] == 0)
                        goto last;
                }
                for (var x = right; x >= right - WidthMin / 10; x--)
                {
                    if (map[bottom + margin, x] == 0)
                        goto last;
                }
                return true;
                last:;
            }
            return false;
        }

        private bool CheckBothEndCleanVerticalRight(byte[,] map, int left, int right, int top, int bottom)
        {
            for (var margin = 0; margin < DecorationThickness; margin++)
            {
                if (right + margin >= map.GetLength(1))
                    return false;
                for (var y = top; y <= top + HeightMin / 10; y++)
                {
                    if (map[y, right + margin] == 0)
                        goto last;
                }
                for (var y = bottom; y >= bottom - HeightMin / 10; y--)
                {
                    if (map[y, right + margin] == 0)
                        goto last;
                }
                return true;
                last:;
            }
            return false;
        }

        private const float EnoughLengthRatio = 0.6f;

        private bool CheckEnoughLengthHorizontalTop(byte[,] map, int left, int right, int top, int bottom)
        {
            var n = 0;
            for (var x = left; x <= right; x++)
            {
                if (map[top - 1, x] == 1)
                    n++;
            }
            return n >= (right - left + 1) * EnoughLengthRatio;
        }

        private bool CheckEnoughLengthVerticalLeft(byte[,] map, int left, int right, int top, int bottom)
        {
            var n = 0;
            for (var y = top; y <= bottom; y++)
            {
                if (map[y, left - 1] == 1)
                    n++;
            }
            return n >= (bottom - top + 1) * EnoughLengthRatio;
        }

        private bool CheckEnoughLengthHorizontalBottom(byte[,] map, int left, int right, int top, int bottom)
        {
            var n = 0;
            for (var x = left; x <= right; x++)
            {
                if (map[bottom, x] == 1)
                    n++;
            }
            return n >= (right - left + 1) * EnoughLengthRatio;
        }

        private bool CheckEnoughLengthVerticalRight(byte[,] map, int left, int right, int top, int bottom)
        {
            var n = 0;
            for (var y = top; y <= bottom; y++)
            {
                if (map[y, right] == 1)
                    n++;
            }
            return n >= (bottom - top + 1) * EnoughLengthRatio;
        }
        // ReSharper restore UnusedParameter.Local

        // For drop pictures in KanColle with a white region on the top side
        public void RoundUpRectangle(byte[,] map, ref Rectangle rect)
        {
            if (rect.Width % 10 != 0)
                return;
            var top = 0;
            for (var x = rect.X; x < rect.Right; x++)
            {
                if (map[rect.Top - 1, x] == 1)
                    top++;
            }
            var r = rect.Height % 10;
            if (top > rect.Width / 2 && r != 0)
                rect.Height += 10 - r;
        }
    }
}