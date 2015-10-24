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

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using static System.Math;

namespace BurageSnap
{
    public class OctreeQuantizer
    {
        private const int Depth = 6;
        private const int Colors = 255;
        private readonly OctreeNode _root = new OctreeNode();
        private int _leafCount;
        private readonly List<OctreeNode>[] _depth = new List<OctreeNode>[Depth - 1];
        private IEnumerable<OctreeNode> _full;

        public OctreeQuantizer()
        {
            for (var i = 0; i < _depth.Length; i++)
                _depth[i] = new List<OctreeNode>();
        }

        public static Bitmap Quantize(Bitmap bmp)
        {
            var oq = new OctreeQuantizer();
            var width = bmp.Width;
            var height = bmp.Height;
            var result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            var data32 = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            var data8 = result.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, result.PixelFormat);
            unsafe
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var p32 = (byte*)data32.Scan0 + y * data32.Stride + x * 4;
                        if (p32[3] != 0)
                            oq.AddPixcel(p32[2], p32[1], p32[0]);
                    }
                }
                oq.Reduce();
                var pallet = result.Palette;
                oq.SetPalette(pallet.Entries);
                result.Palette = pallet;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var p32 = (byte*)data32.Scan0 + y * data32.Stride + x * 4;
                        var p8 = (byte*)data8.Scan0 + y * data8.Stride + x;
                        *p8 = p32[3] == 0 ? (byte)0 : (byte)oq.GetIndex(p32[2], p32[1], p32[0]);
                    }
                }
            }
            bmp.UnlockBits(data32);
            result.UnlockBits(data8);
            return result;
        }

        private void AddPixcel(int r, int g, int b)
        {
            var node = _root;
            for (var i = 0; i < Depth; i++)
            {
                if (node.Children == null)
                    node.Children = new OctreeNode[8];
                var shift = 7 - i;
                var idx = (r >> shift & 1) << 2 | (g >> shift & 1) << 1 | (b >> shift & 1);
                if (node.Children[idx] == null)
                {
                    var n = new OctreeNode();
                    node.Children[idx] = n;
                    if (i < Depth - 1)
                        _depth[i].Add(n);
                }
                node = node.Children[idx];
            }
            if (!node.Leaf)
                _leafCount++;
            node.Leaf = true;
            node.RefCount++;
            node.R += r;
            node.G += g;
            node.B += b;
        }

        private void Reduce()
        {
            var full = new List<OctreeNode>();
            for (var i = Depth - 2; i >= 0; i--)
            {
                full.AddRange(_depth[i]);
                _depth[i] = null;
            }
            full.Add(_root);
            foreach (var node in full)
                node.RefCount = node.Children.Where(n => n != null).Sum(n => n.RefCount);
            _full = full.OrderBy(n => n.RefCount);
            foreach (var node in _full)
            {
                if (_leafCount <= Colors)
                    break;
                ReduceNode(node);
            }
            _full = _full.SkipWhile(n => n.Leaf);
        }

        private void ReduceNode(OctreeNode node)
        {
            foreach (var child in node.Children.Where(n => n != null))
            {
                _leafCount--;
                node.R += child.R;
                node.G += child.G;
                node.B += child.B;
            }
            node.Leaf = true;
            _leafCount++;
            node.Children = null;
        }

        private void SetPalette(Color[] palette)
        {
            var idx = 0;
            palette[idx++] = Color.FromArgb(0, 0, 0, 0); // 0 is the transparent index
            foreach (var leaf in
                from node in _full from child in node.Children where child != null && child.Leaf select child)
            {
                leaf.Index = idx++;
                palette[leaf.Index] = Color.FromArgb(
                    (int)Round(leaf.R / (double)leaf.RefCount),
                    (int)Round(leaf.G / (double)leaf.RefCount),
                    (int)Round(leaf.B / (double)leaf.RefCount));
            }
            for (; idx < Colors; idx++)
                palette[idx] = Color.FromArgb(0, 0, 0);
        }

        private int GetIndex(int r, int g, int b)
        {
            var node = _root;
            for (var i = 0; i < Depth; i++)
            {
                var shift = 7 - i;
                var idx = (r >> shift & 1) << 2 | (g >> shift & 1) << 1 | (b >> shift & 1);
                node = node.Children[idx];
                if (node.Leaf)
                    return node.Index;
            }
            return 0;
        }

        private class OctreeNode
        {
            public int RefCount;
            public int R, G, B;
            public int Index;
            public OctreeNode[] Children;
            public bool Leaf;
        }
    }
}