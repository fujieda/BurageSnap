// Copyright (C) 2016 Kazuhiro Fujieda <fujieda@users.osdn.me>
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

using System.Collections.ObjectModel;
using System.Linq;

namespace BurageSnap
{
    public class OptionContent
    {
        public bool TopMost { get; set; }
        public int Interval { get; set; }
        public int RingBuffer { get; set; }
        public ObservableCollection<string> WindowTitles { get; set; }
        public string Folder { get; set; }
        public bool DailyFolder { get; set; }
        public OutputFormat Format { get; set; }
        public bool AnimationGif { get; set; }

        public OptionContent(Config config)
        {
            foreach (var dst in GetType().GetProperties())
            {
                var src = config.GetType().GetProperty(dst.Name);
                if (src == null)
                    continue;
                dst.SetValue(this, src.GetValue(config, null), null);
            }
            WindowTitles = new ObservableCollection<string>(config.TitleHistory);
        }

        public void ToConfig(Config config)
        {
            foreach (var src in GetType().GetProperties())
            {
                var dst = config.GetType().GetProperty(src.Name);
                dst?.SetValue(config, src.GetValue(this, null), null);
            }
            config.TitleHistory = WindowTitles.ToArray();
        }
    }
}