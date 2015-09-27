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
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace BurageSnap
{
    public enum OutputFormat
    {
        Jpg,
        Png
    }

    public class Config
    {
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public Point Location { get; set; }
        public bool TopMost { get; set; }
        public int Interval { get; set; } = 1000;
        public int RingBuffer { get; set; }
        public string[] TitleHistory { get; set; } = {"艦隊これくしょん -艦これ- - オンラインゲーム - DMM.com"};
        public string Folder { get; set; }
        public OutputFormat Format { get; set; } = OutputFormat.Jpg;
        public bool Continuous { get; set; }
        public bool AnimationGif { get; set; }

        public Config()
        {
            Folder = BaseDir;
        }

        public static Config Load()
        {
            try
            {
                using (var file = File.OpenText("config.xml"))
                {
                    var config = (Config)new XmlSerializer(typeof(Config)).Deserialize(file);
                    config.Folder = PrependBaseDir(config.Folder);
                    return config;
                }
            }
            catch (IOException)
            {
            }
            return new Config();
        }

        public void Save()
        {
            Folder = StripBaseDir(Folder);
            using (var file = File.CreateText("config.xml"))
                new XmlSerializer(typeof(Config)).Serialize(file, this);
        }

        private static string StripBaseDir(string path)
        {
            if (BaseDir == null)
                return path;
            if (!path.StartsWith(BaseDir))
                return path;
            path = path.Substring(BaseDir.Length);
            return path.StartsWith(Path.DirectorySeparatorChar.ToString()) ? path.Substring(1) : path;
        }

        private static string PrependBaseDir(string path)
        {
            if (BaseDir == null)
                return path;
            if (Path.IsPathRooted(path))
                return path;
            return Path.Combine(BaseDir, path);
        }
    }
}