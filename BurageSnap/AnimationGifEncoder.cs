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

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace BurageSnap
{
    public class AnimationGifEncoder
    {
        private BinaryWriter _stream;
        private bool _firstFrame;

        public bool IsLoop { get; set; } = true;

        public void Start(Stream stream)
        {
            _stream = new BinaryWriter(stream);
            _stream.Write(Encoding.ASCII.GetBytes("GIF89a"));
            _firstFrame = true;
        }

        public void AddFrame(Bitmap bmp, int delay)
        {
            var gif = new MemoryStream();
            bmp.Save(gif, ImageFormat.Gif);
            gif.Position = 6; // skip header
            var lsd = new byte[7];
            gif.Read(lsd, 0, 7); // read LSD
            var colorTable = new byte[0];
            var tableSize = 0;
            if ((lsd[4] & 0x80) != 0)
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
            WriteGce(delay);
            var idc = new byte[10];
            gif.Read(idc, 0, 10);
            if ((idc[9] & 0x80) == 0)
            {
                idc[9] = (byte)(0x80 | idc[9] & 0x78 | tableSize);
                _stream.Write(idc);
                _stream.Write(colorTable); // set global color table
            }
            else
            {
                _stream.Write(idc);
            }
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
            _stream.Write(new byte[] {0x21, 0xf9, 0x04, 0x00});
            _stream.Write((short)delay);
            _stream.Write((short)0x00);
        }

        public void Finish()
        {
            if (_stream == null)
                return;
            _stream.Write((byte)0x3b);
            _stream.Close();
        }
    }
}