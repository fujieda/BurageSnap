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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using BurageSnap.Properties;

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
        public Action<string, DateTime> ReportCaptureResult { private get; set; }

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
            var frame = CaptureFrame(true);
            _ringBuffer.Size = _config.RingBuffer;
            if (_config.RingBuffer == 0)
                SaveFrame(frame);
            else
                AddFrame(frame);
            var dummy = 0u;
            _timeProc = TimerCallback; // avoid to be collected by GC
            _timerId = timeSetEvent(_config.Interval == 0 ? 1u : (uint)_config.Interval, 0, _timeProc, ref dummy,
                TIME_PERIODIC | TIME_KILL_SYNCHRONOUS);
        }

        public void Stop()
        {
            if (_timerId != 0)
                timeKillEvent(_timerId);
        }

        public void DiscardBuffer()
        {
            _ringBuffer.Clear();
        }

        [DllImport("winmm.dll")]
        private static extern uint timeSetEvent(uint delay, uint resolution, TimeProc timeProc,
            ref uint user, uint eventType);

        private delegate void TimeProc(uint timerId, uint msg, ref uint user, ref uint rsv1, uint rsv2);

        [DllImport("winmm.dll")]
        private static extern uint timeKillEvent(uint timerId);

        // ReSharper disable InconsistentNaming
        private const int TIME_PERIODIC = 0x0001;

        private const int TIME_KILL_SYNCHRONOUS = 0x0100;
        // ReSharper restore InconsistentNaming

        private void TimerCallback(uint timerId, uint msg, ref uint user, ref uint rsv1, uint rsv2)
        {
            if (!Monitor.TryEnter(_lockObj))
                return;
            try
            {
                var frame = CaptureFrame();
                if (_config.RingBuffer == 0)
                    SaveFrame(frame);
                else
                    AddFrame(frame);
            }
            catch
            {
                timeKillEvent(timerId);
                throw;
            }
            Monitor.Exit(_lockObj);
        }

        private Frame CaptureFrame(bool initial = false)
        {
            var bmp = initial
                ? _screenCapture.CaptureGameScreen(_config.TitleHistory)
                : _screenCapture.CaptureGameScreen();
            var now = DateTime.Now;
            ReportCaptureResult(_screenCapture.Title, now);
            return new Frame {Time = now, Bitmap = bmp};
        }

        private void SaveFrame(Frame frame)
        {
            try
            {
                var parameters = new EncoderParameters(1);
                parameters.Param[0] = new EncoderParameter(Encoder.Quality, _config.JpegQuality);
                using (var fs = OpenFile(frame.Time, _config.Format == OutputFormat.Jpg ? ".jpg" : ".png"))
                    frame.Bitmap.Save(fs,
                        GetEncoder(_config.Format == OutputFormat.Jpg ? ImageFormat.Jpeg : ImageFormat.Png),
                        parameters);
            }
            catch (IOException)
            {
                throw new CaptureError(Resources.Recorder_IO_error, Resources.Recorder_Cant_output_image_file);
            }
            finally
            {
                frame.Dispose();
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
        }

        private void AddFrame(Frame frame)
        {
            _ringBuffer.Add(frame);
        }

        public void SaveBuffer()
        {
            if (_config.AnimationGif)
            {
                SaveRingBufferAsAnimattionGif();
            }
            else
            {
                foreach (var frame in _ringBuffer)
                    SaveFrame(frame);
            }
            _ringBuffer.Clear();
        }

        private void SaveRingBufferAsAnimattionGif()
        {
            var encoder = new AnimationGifEncoder();
            try
            {
                Frame prev = null;
                foreach (var frame in _ringBuffer)
                {
                    if (prev == null)
                        encoder.Start(OpenFile(frame.Time, ".gif"), 560.0 / frame.Bitmap.Width);
                    if (prev != null)
                        encoder.AddFrame(prev.Bitmap, (int)((frame.Time - prev.Time).TotalMilliseconds / 10.0));
                    prev = frame;
                }
                if (prev != null)
                    encoder.AddFrame(prev.Bitmap, 0);
            }
            catch (IOException)
            {
                throw new CaptureError(Resources.Recorder_IO_error, Resources.Recorder_Cant_output_image_file);
            }
            finally
            {
                encoder.Finish();
                _ringBuffer.Clear();
            }
        }

        public void GenerateAnimationGifFromFiles()
        {
            _ringBuffer.Size = _config.RingBuffer;
            foreach (var path in Directory.EnumerateFiles(DateTime.Now.ToString(DateFormat)))
            {
                if (!path.EndsWith(".png"))
                    continue;
                var name = Path.GetFileNameWithoutExtension(path);
                var date = DateTime.ParseExact(name, "yyyy-MM-dd HH-mm-ss.fff", CultureInfo.InvariantCulture);
                _ringBuffer.Add(new Frame {Time = date, Bitmap = new Bitmap(path)});
            }
            SaveRingBufferAsAnimattionGif();
        }

        private Stream OpenFile(DateTime time, string ext)
        {
            var dir = _config.Folder;
            if (_config.DailyFolder)
                dir = Path.Combine(_config.Folder, time.ToString(DateFormat));
            Directory.CreateDirectory(dir);
            return File.OpenWrite(Path.Combine(dir, time.ToString("yyyy-MM-dd HH-mm-ss.fff") + ext));
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