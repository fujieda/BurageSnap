﻿// Copyright (C) 2015 Kazuhiro Fujieda <fujieda@users.osdn.me>
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace BurageSnap
{
    public enum OutputFormat
    {
        Jpg,
        Png
    }

    public class LocationPerMachine
    {
        public string MachineName { get; set; }
        public Point Location { get; set; }
    }

    public class Config
    {
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public Point Location { get; set; } = new Point(double.MinValue, double.MinValue);
        public List<LocationPerMachine> LocationList { get; set; } = new List<LocationPerMachine>();
        public bool TopMost { get; set; }
        public bool ResideInSystemTray { get; set; }
        public bool Notify { get; set; } = true;
        public WindowState WindowState { get; set; }
        public int Interval { get; set; } = 200;
        public int RingBuffer { get; set; } = 25;

        public string[] TitleHistory { get; set; } =
        {
            "艦隊これくしょん -艦これ- - オンラインゲーム - DMM GAMES",
            "千年戦争アイギス - DMM GAMES",
            "FLOWER KNIGHT GIRL - DMM GAMES",
            "刀剣乱舞-ONLINE- - オンラインゲーム - DMM GAMES"
        };

        public string Folder { get; set; }
        public bool DailyFolder { get; set; } = true;
        public OutputFormat Format { get; set; } = OutputFormat.Jpg;
        public int JpegQuality { get; set; } = 70;
        public bool Continuous { get; set; }
        public bool AnimationGif { get; set; }
        public int HotKeyModifier { get; set; }
        public string HotKey { get; set; } = "";

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
                    FixTitles(config);
                    foreach (var l in config.LocationList)
                    {
                        if (l.MachineName == Environment.MachineName)
                        {
                            config.Location = l.Location;
                            break;
                        }
                    }
                    return config;
                }
            }
            catch (IOException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            return new Config();
        }

        private static void FixTitles(Config config)
        {
            for (var i = 0; i < config.TitleHistory.Length; i++)
            {
                var title = config.TitleHistory[i];
                if (!title.EndsWith("DMM.com"))
                    continue;
                config.TitleHistory[i] = title.Substring(0, title.Length - 7) + "DMM GAMES";
            }
        }

        public void Save()
        {
            LocationList = LocationList.Where(l => l.MachineName != Environment.MachineName).Concat(new[]
                {new LocationPerMachine {MachineName = Environment.MachineName, Location = Location}}).ToList();
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