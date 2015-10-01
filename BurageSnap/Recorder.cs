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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace BurageSnap
{
    public class Recorder
    {
        public const string DateFormat = "yyyy-MM-dd";
        private readonly Config _config;
        private readonly Capture _screenCapture = new Capture();
        private readonly RingBuffer _ringBuffer = new RingBuffer();
        private uint _timerId;
        private TimeProc _timeProc;
        private readonly object _lockObj = new object();
        public Action<DateTime> ReportCaptureTime { private get; set; }

        public Recorder(Config config)
        {
            _config = config;
        }

        public void OneShot()
        {
            SaveFrame(CaptureFrame(true));
        }

        public void Start()
        {
            if (_config.RingBuffer == 0)
            {
                var frame = CaptureFrame(true);
                if (frame == null)
                    return;
                SaveFrame(frame);
            }
            else
            {
                _ringBuffer.Size = _config.RingBuffer;
                var frame = CaptureFrame(true);
                if (frame == null)
                    return;
                AddFrame(CaptureFrame(true));
            }
            var dummy = 0u;
            _timeProc = TimerCallback; // avoid to be collected by GC
            _timerId = timeSetEvent(_config.Interval == 0 ? 1u : (uint)_config.Interval, 0, _timeProc, ref dummy, 1);
        }

        public void Stop()
        {
            if (_timerId != 0)
                timeKillEvent(_timerId);
            if (_config.RingBuffer != 0)
                SaveRingBuffer();
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
            var frame = CaptureFrame();
            if (frame == null)
            {
                timeKillEvent(timerId);
                return;
            }
            if (_config.RingBuffer == 0)
                SaveFrame(frame);
            else
                AddFrame(frame);
            Monitor.Exit(_lockObj);
        }

        private Frame CaptureFrame(bool initial = false)
        {
            var bmp = initial
                ? _screenCapture.CaptureGameScreen(_config.TitleHistory[0])
                : _screenCapture.CaptureGameScreen();
            if (bmp == null)
            {
                ReportCaptureTime(DateTime.MinValue);
                return null;
            }
            var now = DateTime.Now;
            ReportCaptureTime(now);
            return new Frame {Time = now, Bitmap = bmp};
        }

        private void SaveFrame(Frame frame)
        {
            if (frame == null)
                return;
            using (var fs = OpenFile(frame.Time, _config.Format == OutputFormat.Jpg ? ".jpg" : ".png"))
                frame.Bitmap.Save(fs, _config.Format == OutputFormat.Jpg ? ImageFormat.Jpeg : ImageFormat.Png);
            frame.Dispose();
        }

        private void AddFrame(Frame frame)
        {
            _ringBuffer.Add(frame);
        }

        private void SaveRingBuffer()
        {
            if (_config.AnimationGif)
            {
                SaveRingBufferAsAnimattionGif();
                return;
            }
            foreach (var frame in _ringBuffer)
                SaveFrame(frame);
            _ringBuffer.Clear();
        }

        private void SaveRingBufferAsAnimattionGif()
        {
            var encoder = new AnimationGifEncoder();
            Frame prev = null;
            foreach (var frame in _ringBuffer)
            {
                var bmp = frame.Bitmap;
                frame.Bitmap = ReduceSize(bmp);
                bmp.Dispose();
                if (prev == null)
                    encoder.Start(OpenFile(frame.Time, ".gif"));
                if (prev != null)
                    encoder.AddFrame(prev.Bitmap, (int)((frame.Time - prev.Time).TotalMilliseconds / 10.0));
                prev = frame;
            }
            if (prev != null)
                encoder.AddFrame(prev.Bitmap, 0);
            encoder.Finish();
            _ringBuffer.Clear();
        }

        private Stream OpenFile(DateTime time, string ext)
        {
            var dir = Path.Combine(_config.Folder, time.ToString(DateFormat));
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch
            {
                return null;
            }
            return File.OpenWrite(Path.Combine(dir, time.ToString("yyyy-MM-dd HH-mm-ss.fff") + ext));
        }

        private Bitmap ReduceSize(Bitmap bmp)
        {
            var half = new Bitmap(bmp.Width / 2, bmp.Height / 2, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(half))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(bmp, 0, 0, half.Width, half.Height);
            }
            return half;
        }

        private class RingBuffer : IEnumerable<Frame>
        {
            private readonly Frame[] _buffer = new Frame[128];
            private const int Mask = 128 - 1;
            private int _top, _bottom;

            public int Size { private get; set; }

            public void Add(Frame frame)
            {
                var n = _bottom - _top;
                if (n < 0)
                    n += _buffer.Length;
                if (n >= Size)
                {
                    if (_buffer[_top] != null)
                    {
                        _buffer[_top].Dispose();
                        _buffer[_top] = null;
                    }
                    _top = (_top + 1) & Mask;
                }
                _buffer[_bottom] = frame;
                _bottom = (_bottom + 1) & Mask;
            }

            public IEnumerator<Frame> GetEnumerator()
            {
                for (var i = _top; i != _bottom; i = (i + 1) % Mask)
                    yield return _buffer[i];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Clear()
            {
                for (var i = _top; i != _bottom; i = (i + 1) % Mask)
                {
                    _buffer[i]?.Dispose();
                    _buffer[i] = null;
                }
                _top = _bottom = 0;
            }
        }

        private class Frame : IDisposable
        {
            public DateTime Time { get; set; }
            public Bitmap Bitmap { get; set; }

            public void Dispose()
            {
                Bitmap.Dispose();
            }
        }
    }
}