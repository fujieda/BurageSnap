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
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BurageSnap
{
    public class AnimationGifEncoder
    {
        private BinaryWriter _stream;
        private double _scale;
        private bool _firstFrame;
        private int[,] _prevFrame;
        private readonly BlockingCollection<Frame> _originalFrame = new BlockingCollection<Frame>();
        private readonly BlockingCollection<Frame> _scaledFrame = new BlockingCollection<Frame>();
        private readonly BlockingCollection<Frame> _optimizedFrame = new BlockingCollection<Frame>();
        private readonly BlockingCollection<Frame> _learnedNeuQuant = new BlockingCollection<Frame>();
        private readonly BlockingCollection<Frame> _bpp8Frame = new BlockingCollection<Frame>();
        private readonly Task[] _tasks = new Task[5];

        private class Frame
        {
            public Bitmap Bitmap;
            public NeuQuant NeuQuant;
            public int Delay;
            public Rectangle Rectangle;
            public int[] Pixels;
        }

        public bool IsLoop { get; set; } = true;

        public void Start(Stream stream, double scale = 1.0)
        {
            _scale = scale;
            _stream = new BinaryWriter(stream);
            _stream.Write(Encoding.ASCII.GetBytes("GIF89a"));
            _firstFrame = true;
            StartPipeline();
        }

        private void StartPipeline()
        {
            _tasks[0] = Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        var frame = _originalFrame.Take();
                        frame.Bitmap = ScaleImage(frame.Bitmap, _scale);
                        _scaledFrame.Add(frame);
                    }
                }
                catch (InvalidOperationException)
                {
                    _scaledFrame.CompleteAdding();
                }
            });
            _tasks[1] = Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        var frame = _scaledFrame.Take();
                        _optimizedFrame.Add(OptimizeFrame(frame));
                    }
                }
                catch (InvalidOperationException)
                {
                    _optimizedFrame.CompleteAdding();
                }
            });
            _tasks[2] = Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        var frame = _optimizedFrame.Take();
                        frame.NeuQuant = frame.Pixels == null
                            ? new NeuQuant(frame.Bitmap, 10)
                            : new NeuQuant(frame.Pixels, frame.Rectangle.Width, frame.Rectangle.Height, 10);
                        frame.NeuQuant.Init();
                        _learnedNeuQuant.Add(frame);
                    }
                }
                catch (InvalidOperationException)
                {
                    _learnedNeuQuant.CompleteAdding();
                }
            });
            _tasks[3] = Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        var frame = _learnedNeuQuant.Take();
                        frame.Bitmap.Dispose();
                        frame.Bitmap = frame.NeuQuant.CreateBitmap();
                        _bpp8Frame.Add(frame);
                    }
                }
                catch (InvalidOperationException)
                {
                    _bpp8Frame.CompleteAdding();
                }
            });
            _tasks[4] = Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        var frame = _bpp8Frame.Take();
                        AddFrame(frame.Bitmap, frame.Rectangle, frame.Delay);
                        frame.Bitmap.Dispose();
                    }
                }
                catch (InvalidOperationException)
                {
                }
            });
        }

        public void AddFrame(Bitmap bmp, int delay)
        {
            _originalFrame.Add(new Frame {Bitmap = bmp, Delay = delay});
        }

        private Frame OptimizeFrame(Frame frame)
        {
            if (_prevFrame == null)
            {
                _prevFrame = ConvertBitmapToArray(frame.Bitmap);
                frame.Rectangle = new Rectangle(0, 0, frame.Bitmap.Width, frame.Bitmap.Height);
            }
            else
            {
                var cur = ConvertBitmapToArray(frame.Bitmap);
                frame.Bitmap.Dispose();
                frame = DifferentialFrame(cur, _prevFrame, frame);
                _prevFrame = cur;
            }
            return frame;
        }

        private Bitmap ScaleImage(Bitmap bmp, double scale)
        {
            var scaled = new Bitmap(
                (int)Math.Ceiling(bmp.Width * scale),
                (int)Math.Ceiling(bmp.Height * scale), PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, 0, 0, scaled.Width, scaled.Height);
            }
            return scaled;
        }

        private void AddFrame(Bitmap bmp, Rectangle rect, int delay)
        {
            using (var gif = new MemoryStream())
            {
                bmp.Save(gif, ImageFormat.Gif);
                AddFrame(gif, rect, delay);
            }
            bmp.Dispose();
        }

        private void AddFrame(MemoryStream gif, Rectangle rect, int delay)
        {
            gif.Position = 6; // skip header
            var lsd = new byte[7];
            gif.Read(lsd, 0, 7); // read LSD
            var colorTable = new byte[0];
            var tableSize = 0;
            if ((lsd[4] & 0x80) != 0) // have global color table
            {
                tableSize = lsd[4] & 7;
                colorTable = new byte[(1 << tableSize + 1) * 3];
                gif.Read(colorTable, 0, colorTable.Length);
            }
            if (_firstFrame)
            {
                lsd[4] &= 0x78; // drop global color table flag and size of global color table
                _stream.Write(lsd);
                if (IsLoop)
                    WriteAppExt();
            }
            if (gif.GetBuffer()[gif.Position] == 0x21)
                gif.Position += 8; // skip graphic control extension
            var idc = new byte[10];
            gif.Read(idc, 0, 10);
            if ((idc[9] & 0x80) == 0)
            {
                idc[9] = (byte)(0x80 | idc[9] & 0x78 | tableSize);
            }
            else
            {
                tableSize = idc[9] & 7;
                colorTable = new byte[(1 << tableSize + 1) * 3];
                gif.Read(colorTable, 0, colorTable.Length);
            }
            WriteGce(delay);
            SetRectangle(idc, rect);
            _stream.Write(idc);
            _stream.Write(colorTable);
            _stream.Write(gif.GetBuffer(), (int)gif.Position, (int)(gif.Length - gif.Position - 1));
            _firstFrame = false;
        }

        public void WriteAppExt()
        {
            _stream.Write(new byte[] {0x21, 0xff, 0x0b});
            _stream.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));
            _stream.Write((byte)3);
            _stream.Write((byte)1);
            _stream.Write((short)0);
            _stream.Write((byte)0x00);
        }

        public void WriteGce(int delay)
        {
            _stream.Write(new byte[] {0x21, 0xf9, 0x04});
            _stream.Write((byte)0x01); // have transparent index
            _stream.Write((short)delay);
            _stream.Write((byte)0x00); // 0 is the transparent index
            _stream.Write((byte)0x00);
        }

        private void SetRectangle(byte[] idc, Rectangle rect)
        {
            var s = new BinaryWriter(new MemoryStream(idc));
            s.Seek(1, SeekOrigin.Begin);
            s.Write((ushort)rect.X);
            s.Write((ushort)rect.Y);
            s.Write((ushort)rect.Width);
            s.Write((ushort)rect.Height);
        }

        private int[,] ConvertBitmapToArray(Bitmap bmp)
        {
            var width = bmp.Width;
            var height = bmp.Height;
            var data = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var array = new int[height, width];
            unsafe
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var ptr = (byte*)data.Scan0 + y * data.Stride + x * 3;
                        array[y, x] = ptr[0] | ptr[1] << 8 | ptr[2] << 16 | 0xff << 24;
                    }
                }
            }
            bmp.UnlockBits(data);
            return array;
        }

        private Frame DifferentialFrame(int[,] cur, int[,] prev, Frame frame)
        {
            var rect = FindDifferentBounds(cur, prev);
            if (rect.IsEmpty)
                rect = new Rectangle(0, 0, 1, 1);
            frame.Pixels = new int[rect.Width * rect.Height];
            for (var y = 0; y < rect.Height; y++)
            {
                var y1 = y + rect.Y;
                for (var x = 0; x < rect.Width; x++)
                {
                    var x1 = x + rect.X;
                    var col = cur[y1, x1];
                    frame.Pixels[y * rect.Width + x] = col != prev[y1, x1] ? col : 0;
                }
            }
            frame.Rectangle = rect;
            return frame;
        }

        private Rectangle FindDifferentBounds(int[,] cur, int[,] prev)
        {
            var width = cur.GetUpperBound(1);
            var height = cur.GetUpperBound(0);
            var r = Rectangle.Empty;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (cur[y, x] == prev[y, x])
                        continue;
                    r.Y = y;
                    goto foundY;
                }
            }
            return Rectangle.Empty;
            foundY:
            for (var x = 0; x < width; x++)
            {
                for (var y = r.Y; y < height; y++)
                {
                    if (cur[y, x] == prev[y, x])
                        continue;
                    r.X = x;
                    goto foundX;
                }
            }
            foundX:
            for (var x = width - 1; x >= r.X; x--)
            {
                for (var y = r.Y; y < height; y++)
                {
                    if (cur[y, x] == prev[y, x])
                        continue;
                    r.Width = x + 1 - r.X;
                    goto foundWidth;
                }
            }
            foundWidth:
            for (var y = height - 1; y >= r.Y; y--)
            {
                for (var x = r.X; x < r.X + r.Width; x++)
                {
                    if (cur[y, x] == prev[y, x])
                        continue;
                    r.Height = y + 1 - r.Y;
                    goto foundHeight;
                }
            }
            foundHeight:
            return r;
        }

        public void Finish()
        {
            if (_stream == null)
                return;
            _originalFrame.CompleteAdding();
            Task.WaitAll(_tasks);
            _stream.Write((byte)0x3b);
            _stream.Close();
        }
    }
}