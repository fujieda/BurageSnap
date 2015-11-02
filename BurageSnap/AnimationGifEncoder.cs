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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace BurageSnap
{
    public class AnimationGifEncoder
    {
        private BinaryWriter _stream;
        private double _scale;
        private bool _firstFrame;
        private readonly BlockingCollection<Frame> _scaledFrame = new BlockingCollection<Frame>();
        private readonly BlockingCollection<Frame> _resultingFrame = new BlockingCollection<Frame>();
        private int _sequence;
        private Task _task;

        private struct Frame
        {
            public int Sequence;
            public Bitmap Bitmap;
            public int Delay;
            public Rectangle Rectangle;
            public int[] Pixels;
            public MemoryStream Gif;
        }

        public bool IsLoop { get; set; } = true;

        public void Start(Stream stream, double scale = 1.0)
        {
            _scale = scale;
            _stream = new BinaryWriter(stream);
            _stream.Write(Encoding.ASCII.GetBytes("GIF89a"));
            _firstFrame = true;
            _sequence = 0;
            StartPipeline();
        }

        public void AddFrame(Bitmap bmp, int delay)
        {
            _scaledFrame.Add(ScaleFrame(new Frame {Sequence = _sequence++, Bitmap = bmp, Delay = delay}, _scale));
        }

        private void StartPipeline()
        {
            _task = Task.Run(() => // optimize frames
            {
                var tasks = new List<Task>();
                try
                {
                    Frame? prev = null;
                    while (true)
                    {
                        var cur = _scaledFrame.Take();
                        var prev1 = prev;
                        tasks.Add(Task.Run(() => { _resultingFrame.Add(ProcessFrame(cur, prev1)); }));
                        prev = cur;
                    }
                }
                catch (InvalidOperationException)
                {
                    Task.WaitAll(tasks.ToArray());
                    _resultingFrame.CompleteAdding();
                }
            });
        }

        public void Finish()
        {
            if (_stream == null)
                return;
            _scaledFrame.CompleteAdding();
            _task.Wait();
            foreach (var frame in _resultingFrame.OrderBy(f => f.Sequence))
                AddFrame(frame.Gif, frame.Rectangle, frame.Delay);
            _stream.Write((byte)0x3b);
            _stream.Close();
        }

        private Frame ProcessFrame(Frame cur, Frame? prev)
        {
            var frame = DifferentialFrame(cur, prev);
            var nq = new NeuQuant(frame.Pixels, frame.Rectangle.Width, frame.Rectangle.Height, 10);
            nq.Init();
            using (var bmp = nq.CreateBitmap())
                frame.Gif = ConvertToGif(bmp);
            return frame;
        }

        private Frame ScaleFrame(Frame frame, double scale)
        {
            var srcWidth = frame.Bitmap.Width;
            var srcHeight = frame.Bitmap.Height;
            var width = (int)Ceiling(srcWidth * scale);
            var height = (int)Ceiling(srcHeight * scale);
            using (var scaled = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(frame.Bitmap, new Rectangle(0, 0, width, height));
                }
                frame.Pixels = ConvertBitmapToArray(scaled);
                frame.Rectangle = new Rectangle(0, 0, width, height);
                return frame;
            }
        }

        private MemoryStream ConvertToGif(Bitmap bmp)
        {
            var gif = new MemoryStream();
            bmp.Save(gif, ImageFormat.Gif);
            return gif;
        }

        private Frame DifferentialFrame(Frame curFrame, Frame? prevFrame)
        {
            if (prevFrame == null)
                return curFrame;
            var orgRect = curFrame.Rectangle;
            var rect = FindDifferentBounds(curFrame, (Frame)prevFrame);
            if (rect.IsEmpty)
                rect = new Rectangle(0, 0, 1, 1);
            var pixels = new int[rect.Width * rect.Height];
            for (var y = 0; y < rect.Height; y++)
            {
                var y1 = y + rect.Y;
                for (var x = 0; x < rect.Width; x++)
                {
                    var x1 = x + rect.X;
                    var col = curFrame.Pixels[y1 * orgRect.Width + x1];
                    pixels[y * rect.Width + x] = col != prevFrame.Value.Pixels[y1 * orgRect.Width + x1] ? col : 0;
                }
            }
            curFrame.Rectangle = rect;
            curFrame.Pixels = pixels;
            return curFrame;
        }

        private Rectangle FindDifferentBounds(Frame curFrame, Frame prevFrame)
        {
            var width = curFrame.Rectangle.Width;
            var height = curFrame.Rectangle.Height;
            var cur = curFrame.Pixels;
            var prev = prevFrame.Pixels;
            var r = Rectangle.Empty;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (cur[y * width + x] == prev[y * width + x])
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
                    if (cur[y * width + x] == prev[y * width + x])
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
                    if (cur[y * width + x] == prev[y * width + x])
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
                    if (cur[y * width + x] == prev[y * width + x])
                        continue;
                    r.Height = y + 1 - r.Y;
                    goto foundHeight;
                }
            }
            foundHeight:
            return r;
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
            _stream.Write(new byte[] { 0x21, 0xff, 0x0b });
            _stream.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));
            _stream.Write((byte)3);
            _stream.Write((byte)1);
            _stream.Write((short)0);
            _stream.Write((byte)0x00);
        }

        public void WriteGce(int delay)
        {
            _stream.Write(new byte[] { 0x21, 0xf9, 0x04 });
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

        private int[] ConvertBitmapToArray(Bitmap bmp)
        {
            var width = bmp.Width;
            var height = bmp.Height;
            var data = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var array = new int[height * width];
            Marshal.Copy(data.Scan0, array, 0, width * height);
            bmp.UnlockBits(data);
            return array;
        }
    }
}