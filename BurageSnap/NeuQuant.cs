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

/* NeuQuant Neural-Net Quantization Algorithm
 * ------------------------------------------
 *
 * Copyright (c) 1994 Anthony Dekker
 *
 * NEUQUANT Neural-Net quantization algorithm by Anthony Dekker, 1994.
 * See "Kohonen neural networks for optimal colour quantization"
 * in "Network: Computation in Neural Systems" Vol. 5 (1994) pp 351-367.
 * for a discussion of the algorithm.
 * See also  http://www.acm.org/~dekker/NEUQUANT.HTML
 *
 * Any party obtaining a copy of these files from the author, directly or
 * indirectly, is granted, free of charge, a full and unrestricted irrevocable,
 * world-wide, paid up, royalty-free, nonexclusive right and license to deal
 * in this software and documentation files (the "Software"), including without
 * limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons who receive
 * copies from any such party to do so, with the only requirement being
 * that this copyright notice remain intact.
 */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BurageSnap
{
    public class NeuQuant
    {
        private const int Cycles = 100; // learning cycles
        private const int NetSize = 255; // colors used
        private const int Specials = 2; // reserved colors
        private const int CutNetSize = NetSize - Specials;
        private const int MaxNetPos = NetSize - 1;
        private const int InitRad = NetSize / 8; // for 256 cols, radius starts at 32
        private const int RadiusBiasShift = 6;
        private const int RadiusBias = 1 << RadiusBiasShift;
        private const int InitBiasRadius = InitRad * RadiusBias;
        private const int RadiusDec = 30; // factor of 1/30 each cycle
        private const int AlphaBiasShift = 10; // alpha starts at 1
        private const int InitAlpha = 1 << AlphaBiasShift; // biased by 10 bits
        private const double Gamma = 1024.0;
        private const double Beta = 1.0 / 1024.0;
        private const double BetaGamma = Beta * Gamma;

        private readonly double[][] _network = new double[NetSize][]; // the network itself
        private readonly int[][] _colormap = new int[NetSize][]; // the network itself
        private readonly int[] _netIndex = new int[256]; // for network lookup - really 256
        private readonly double[] _bias = new double[NetSize]; // bias and freq arrays for learning
        private readonly double[] _freq = new double[NetSize];

        // four primes near 500 - assume no image has a length so large
        // that it is divisible by all four primes
        private const int Prime1 = 499;
        private const int Prime2 = 491;
        private const int Prime3 = 487;
        private const int Prime4 = 503;

        private readonly int[] _pixels;
        private readonly int _sampleFac;
        private readonly int _width;
        private readonly int _height;

        public NeuQuant(int[] pixels, int width, int height, int sample)
        {
            if (sample < 1 || 30 < sample)
                throw new ArgumentException();
            _sampleFac = sample;
            _width = width;
            _height = height;
            _pixels = pixels;
            SetUpArrays();
        }

        public NeuQuant(Bitmap bmp) : this(bmp, 1)
        {
        }

        public NeuQuant(Bitmap bmp, int sample) : this(GetPixels(bmp), bmp.Width, bmp.Height, sample)
        {
        }

        public static Bitmap Quantize(Bitmap bmp, int sample)
        {
            var nq = new NeuQuant(bmp, sample);
            nq.Init();
            return nq.CreateBitmap();
        }

        public Bitmap CreateBitmap()
        {
            var bmp8 = new Bitmap(_width, _height, PixelFormat.Format8bppIndexed);
            var palette = bmp8.Palette;
            palette.Entries[0] = Color.FromArgb(0, 0, 0, 0); // 0 is the transparent index
            for (var i = 0; i < NetSize; i++)
                palette.Entries[i + 1] = GetColor(i);
            bmp8.Palette = palette;
            var data8 = bmp8.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.WriteOnly, bmp8.PixelFormat);
            for (var h = 0; h < _height; h++)
            {
                for (var x = 0; x < _width; x++)
                {
                    unsafe
                    {
                        var pix = _pixels[h * _width + x];
                        var ptr = (byte*)data8.Scan0 + h * data8.Stride + x;
                        *ptr = pix == 0 ? (byte)0 : (byte)(Lookup(pix) + 1);
                    }
                }
            }
            bmp8.UnlockBits(data8);
            return bmp8;
        }

        public int ColorCount => NetSize;

        public Color GetColor(int i)
        {
            if (i < 0 || i >= NetSize)
                return Color.Empty;
            return Color.FromArgb(_colormap[i][2], _colormap[i][1], _colormap[i][0]);
        }

        private void SetUpArrays()
        {
            _network[0] = new[] {0.0, 0.0, 0.0};
            _network[1] = new[] {255.0, 255.0, 255.0};

            for (var i = 0; i < Specials; i++)
            {
                _freq[i] = 1.0 / NetSize;
                _bias[i] = 0.0;
            }

            for (var i = Specials; i < NetSize; i++)
            {
                var p = _network[i] = new double[3];
                p[0] = 255.0 * (i - Specials) / CutNetSize;
                p[1] = 255.0 * (i - Specials) / CutNetSize;
                p[2] = 255.0 * (i - Specials) / CutNetSize;
                _freq[i] = 1.0 / NetSize;
                _freq[i] = 0.0;
            }

            for (var i = 0; i < NetSize; i++)
                _colormap[i] = new int[4];
        }

        private static int[] GetPixels(Bitmap bmp)
        {
            var width = bmp.Width;
            var height = bmp.Height;
            var pixels = new int[width * height];
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(data.Scan0, pixels, 0, width * height);
            bmp.UnlockBits(data);
            return pixels;
        }

        public void Init()
        {
            Learn();
            Fix();
            InxBuild();
        }

        private void Learn()
        {
            var biasRadius = InitBiasRadius;
            var alphadec = 30 + (_sampleFac - 1) / 3;
            var lengthCount = _pixels.Length;
            var samplepixels = lengthCount / _sampleFac;
            var delta = samplepixels / Cycles;
            var alpha = InitAlpha;

            var rad = biasRadius >> RadiusBiasShift;
            if (rad <= 1)
                rad = 0;

            var step = lengthCount % Prime1 != 0
                ? Prime1
                : lengthCount % Prime2 != 0
                    ? Prime2
                    : lengthCount % Prime3 != 0 ? Prime3 : Prime4;

            var pos = 0;
            var i = 0;
            while (i < samplepixels)
            {
                var p = _pixels[pos];
                var red = (p >> 16) & 0xff;
                var green = (p >> 8) & 0xff;
                var blue = p & 0xff;

                double b = blue;
                double g = green;
                double r = red;

                var j = SpecialFind(b, g, r);
                j = j < 0 ? Contest(b, g, r) : j;

                if (j >= Specials)
                {
                    // don't learn for specials
                    var a = (1.0 * alpha) / InitAlpha;
                    AlterSingle(a, j, b, g, r);
                    if (rad > 0)
                        AlterNeigh(a, rad, j, b, g, r); // alter neighbours
                }

                pos += step;
                while (pos >= lengthCount)
                    pos -= lengthCount;

                i++;
                if (delta == 0 || i % delta != 0)
                    continue;
                alpha -= alpha / alphadec;
                biasRadius -= biasRadius / RadiusDec;
                rad = biasRadius >> RadiusBiasShift;
                if (rad <= 1)
                    rad = 0;
            }
        }

        private void AlterSingle(double alpha, int i, double b, double g, double r)
        {
            // Move neuron i towards biased (b,g,r) by factor alpha
            var n = _network[i]; // alter hit neuron
            n[0] -= alpha * (n[0] - b);
            n[1] -= alpha * (n[1] - g);
            n[2] -= alpha * (n[2] - r);
        }

        private void AlterNeigh(double alpha, int rad, int i, double b, double g, double r)
        {
            var lo = i - rad;
            if (lo < Specials - 1) lo = Specials - 1;
            var hi = i + rad;
            if (hi > NetSize) hi = NetSize;

            var j = i + 1;
            var k = i - 1;
            var q = 0;
            while ((j < hi) || (k > lo))
            {
                var a = alpha * (rad * rad - q * q) / (rad * rad);
                q++;
                if (j < hi)
                {
                    var p = _network[j];
                    p[0] -= a * (p[0] - b);
                    p[1] -= a * (p[1] - g);
                    p[2] -= a * (p[2] - r);
                    j++;
                }
                if (k > lo)
                {
                    var p = _network[k];
                    p[0] -= a * (p[0] - b);
                    p[1] -= a * (p[1] - g);
                    p[2] -= a * (p[2] - r);
                    k--;
                }
            }
        }

        private int Contest(double b, double g, double r)
        {
            // Search for biased BGR values
            // finds closest neuron (min dist) and updates freq
            // finds best neuron (min dist-bias) and returns position
            // for frequently chosen neurons, freq[i] is high and bias[i] is negative
            // bias[i] = gamma*((1/netsize)-freq[i])

            var bestd = double.MaxValue;
            var bestbiasd = bestd;
            var bestpos = -1;
            var bestbiaspos = bestpos;
            for (var i = Specials; i < NetSize; i++)
            {
                var n = _network[i];
                var dist = Math.Abs(n[0] - b) + Math.Abs(n[1] - g) + Math.Abs(n[2] - r);
                if (dist < bestd)
                {
                    bestd = dist;
                    bestpos = i;
                }
                var biasdist = dist - _bias[i];
                if (biasdist < bestbiasd)
                {
                    bestbiasd = biasdist;
                    bestbiaspos = i;
                }
                _freq[i] -= Beta * _freq[i];
                _bias[i] += BetaGamma * _freq[i];
            }
            _freq[bestpos] += Beta;
            _bias[bestpos] -= BetaGamma;
            return bestbiaspos;
        }

        private int SpecialFind(double b, double g, double r)
        {
            for (var i = 0; i < Specials; i++)
            {
                var n = _network[i];
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (n[0] == b && n[1] == g && n[2] == r)
                    return i;
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }
            return -1;
        }

        private void Fix()
        {
            for (var i = 0; i < NetSize; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var x = (int)(0.5 + _network[i][j]);
                    if (x < 0) x = 0;
                    if (x > 255) x = 255;
                    _colormap[i][j] = x;
                }
                _colormap[i][3] = i;
            }
        }

        private void InxBuild()
        {
            // Insertion sort of network and building of netindex[0..255]
            var previouscol = 0;
            var startpos = 0;

            for (var i = 0; i < NetSize; i++)
            {
                var smallpos = i;
                var smallval = _colormap[i][1]; // index on g
                // find smallest in i..netsize-1
                for (var j = i + 1; j < NetSize; j++)
                {
                    var g = _colormap[j][1];
                    if (g < smallval)
                    {
                        // index on g
                        smallpos = j;
                        smallval = g;
                    }
                }
                // swap p (i) and q (smallpos) entries
                if (i != smallpos)
                {
                    var tmp = _colormap[smallpos];
                    _colormap[smallpos] = _colormap[i];
                    _colormap[i] = tmp;
                }
                // smallval entry is now in position i
                if (smallval != previouscol)
                {
                    _netIndex[previouscol] = (startpos + i) >> 1;
                    for (var j = previouscol + 1; j < smallval; j++)
                        _netIndex[j] = i;
                    previouscol = smallval;
                    startpos = i;
                }
            }
            _netIndex[previouscol] = (startpos + MaxNetPos) >> 1;
            for (var j = previouscol + 1; j < 256; j++)
                _netIndex[j] = MaxNetPos; // really 256
        }

        public int Lookup(int pixel) => InxSearch(pixel);

        public int Lookup(Color c) => InxSearch(c.R << 16 | c.G << 8 | c.B);

        private int InxSearch(int pixel)
        {
            var r = (pixel >> 16) & 0xff;
            var g = (pixel >> 8) & 0xff;
            var b = pixel & 0xff;

            // Search for BGR values 0..255 and return colour index
            var bestd = 1000; // biggest possible dist is 256*3
            var best = -1;
            var i = _netIndex[g]; // index on g
            var j = i - 1; // start at netindex[g] and work outwards

            while (i < NetSize || j >= 0)
            {
                if (i < NetSize)
                {
                    var p = _colormap[i];
                    var dist = p[1] - g; // inx key
                    if (dist >= bestd)
                    {
                        i = NetSize; // stop iter
                    }
                    else
                    {
                        var db = p[0] - b;
                        dist = (dist < 0 ? -dist : dist) + (db < 0 ? -db : db);
                        if (dist < bestd)
                        {
                            var dr = p[2] - r;
                            dist += dr < 0 ? -dr : dr;
                            if (dist < bestd)
                            {
                                bestd = dist;
                                best = i;
                            }
                        }
                        i++;
                    }
                }
                if (j >= 0)
                {
                    var p = _colormap[j];
                    var dist = g - p[1]; // inx key - reverse dif
                    if (dist >= bestd)
                    {
                        j = -1; // stop iter
                    }
                    else
                    {
                        var db = p[0] - b;
                        dist = (dist < 0 ? -dist : dist) + (db < 0 ? -db : db);
                        if (dist < bestd)
                        {
                            var dr = p[2] - r;
                            dist += dr < 0 ? -dr : dr;
                            if (dist < bestd)
                            {
                                bestd = dist;
                                best = j;
                            }
                        }
                        j--;
                    }
                }
            }
            return best;
        }
    }
}