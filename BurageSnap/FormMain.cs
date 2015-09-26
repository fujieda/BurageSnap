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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using BurageSnap.Properties;

namespace BurageSnap
{
    public partial class FormMain : Form
    {
        private IntPtr _hWnd;
        private Rectangle _rectangle;
        private readonly Config _config;
        private readonly OptionDialog _optionDialog;
        private const int WidthMin = 600, HeightMin = 400;
        private const string DateFormat = "yyyy-MM-dd";
        private bool _captureing;
        private readonly object _lockObj = new object();
        private uint _timerId;
        private TimeProc _timeProc;
        private readonly Frame[] _ringBuffer = new Frame[128];
        private int _ringMask = 128 - 1;
        private int _ringTop, _ringBottom, _ringSize;

        private class Frame : IDisposable
        {
            public DateTime Time { get; set; }
            public Bitmap Bitmap { get; set; }

            public void Dispose()
            {
                Bitmap.Dispose();
            }
        }

        public FormMain()
        {
            InitializeComponent();
            _config = Config.Load();
            _ringSize = _config.RingBuffer;
            _optionDialog = new OptionDialog(_config);
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            if (_config.Location.IsEmpty)
                return;
            var newb = Bounds;
            newb.Location = _config.Location;
            if (IsVisibleOnAnyScreen(newb))
                Location = _config.Location;
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _config.Location = Location;
            _config.Save();
        }

        public static bool IsVisibleOnAnyScreen(Rectangle rect)
        {
            return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(rect));
        }

        private void buttonOption_Click(object sender, EventArgs e)
        {
            if (_optionDialog.ShowDialog(this) != DialogResult.OK)
                return;
            TopMost = _config.TopMost;
            _ringSize = _config.RingBuffer;
        }

        private void buttonCapture_Click(object sender, EventArgs e)
        {
            RedetectWindow();
            if (!checkBoxContinuous.Checked)
            {
                SaveFrame(SingleShot());
                return;
            }
            if (!_captureing)
            {
                buttonCapture.Text = Resources.FormMain_buttonCapture_Click_Stop;
                if (_ringSize == 0)
                    SaveFrame(SingleShot());
                else
                    AddFrame(SingleShot());
                var dummy = 0u;
                _timeProc = TimerCallback; // avoid to be collected by GC
                _timerId = timeSetEvent(_config.Interval == 0 ? 1u : (uint)_config.Interval, 0, _timeProc, ref dummy, 1);
                _captureing = true;
            }
            else
            {
                if (_timerId != 0)
                    timeKillEvent(_timerId);
                if (_ringSize != 0)
                    SaveRingBuffer();
                buttonCapture.Text = Resources.FormMain_buttonCapture_Click_Start;
                _captureing = false;
            }
        }

        private void AddFrame(Frame frame)
        {
            var n = _ringBottom - _ringTop;
            if (n < 0)
                n += _ringBuffer.Length;
            if (n >= _ringSize)
            {
                if (_ringBuffer[_ringTop] != null)
                {
                    _ringBuffer[_ringTop].Dispose();
                    _ringBuffer[_ringTop] = null;
                }
                _ringTop = (_ringTop + 1) & _ringMask;
            }
            _ringBuffer[_ringBottom] = frame;
            _ringBottom = (_ringBottom + 1) & _ringMask;
        }

        private void SaveRingBuffer()
        {
            for (var i = _ringTop; i != _ringBottom; i = (i + 1) % _ringMask)
            {
                SaveFrame(_ringBuffer[i]);
                _ringBuffer[i] = null;
            }
        }

        [DllImport("winmm.dll")]
        private static extern uint timeSetEvent(uint delay, uint resolution, TimeProc timeProc,
            ref uint user, uint eventType);

        private delegate void TimeProc(uint timerId, uint msg, ref uint user, ref uint rsv1, uint rsv2);

        [DllImport("winmm.dll")]
        private static extern uint timeKillEvent(uint timerId);

        private void TimerCallback(uint timerId, uint msg, ref uint user, ref uint rsv1, uint rsv2)
        {
            if (!Monitor.TryEnter(_lockObj))
                return;
            var frame = SingleShot();
            if (frame == null)
            {
                timeKillEvent(timerId);
                return;
            }
            if (_ringSize == 0)
                SaveFrame(frame);
            else
                AddFrame(frame);
            Monitor.Exit(_lockObj);
        }

        private void checkBoxContinuous_CheckedChanged(object sender, EventArgs e)
        {
            buttonCapture.Text = checkBoxContinuous.Checked
                ? Resources.FormMain_buttonCapture_Click_Start
                : Resources.FormMain_checkBoxContinuous_CheckedChanged_Capture;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var dir = now.ToString(DateFormat);
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch
            {
                return;
            }
            Process.Start(dir);
        }

        private void RedetectWindow()
        {
            _hWnd = IntPtr.Zero;
            _rectangle = new Rectangle();
        }

        private Frame SingleShot()
        {
            if (_hWnd == IntPtr.Zero)
                _hWnd = FindWindow(_config.TitleHistory[0]);
            if (_hWnd == IntPtr.Zero)
                return null;
            using (var bmp = CaptureWindow(_hWnd))
            {
                if (_rectangle.IsEmpty)
                    _rectangle = DetectGameScreen(bmp);
                if (_rectangle.IsEmpty)
                    return null;
                return new Frame { Time = ReportCaptureTime(DateTime.Now), Bitmap = bmp.Clone(_rectangle, bmp.PixelFormat) };
            }
        }

        private DateTime ReportCaptureTime(DateTime time)
        {
            BeginInvoke(new Action(() => { labelTimeStamp.Text = time.ToString("HH:mm:ss.fff"); }));
            return time;
        }

        private void SaveFrame(Frame frame)
        {
            if (frame == null)
                return;
            var dir = Path.Combine(_config.Folder, frame.Time.ToString(DateFormat));
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch
            {
                return;
            }
            var path = Path.Combine(dir, frame.Time.ToString("yyyy-MM-dd HH-mm-ss.fff") +
                                         (_config.Format == OutputFormat.Jpg ? ".jpg" : ".png"));
            using (var fs = File.OpenWrite(path))
                frame.Bitmap.Save(fs, _config.Format == OutputFormat.Jpg ? ImageFormat.Jpeg : ImageFormat.Png);
            frame.Dispose();

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
            var winDC = GetWindowDC(hWnd);
            var rect = new Rect();
            GetWindowRect(hWnd, ref rect);
            var bmp = new Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bmp);
            var hDC = g.GetHdc();
            BitBlt(hDC, 0, 0, bmp.Width, bmp.Height, winDC, 0, 0, SRCCOPY);
            g.ReleaseHdc(hDC);
            g.Dispose();
            ReleaseDC(hWnd, winDC);
            return bmp;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        // ReSharper disable once InconsistentNaming
        private const int SRCCOPY = 0xcc0020;

        [DllImport("gdi32.dll")]
        private static extern int BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight,
            IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

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