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

namespace BurageSnap
{
    public class KeyModifier
    {

        public int Value { get; set; }

        public bool Alt
        {
            get { return (Value & 1) != 0; }
            set
            {
                if (value)
                    Value |= 1;
                else
                    Value &= ~1;
            }
        }

        public bool Control
        {
            get { return (Value & 2) != 0; }
            set
            {
                if (value)
                    Value |= 2;
                else
                    Value &= ~2;
            }
        }

        public bool Shift
        {
            get { return (Value & 4) != 0; }
            set
            {
                if (value)
                    Value |= 4;
                else
                    Value &= ~4;
            }
        }

        public bool Win
        {
            get { return (Value & 8) != 0; }
            set
            {
                if (value)
                    Value |= 8;
                else
                    Value &= ~8;
            }
        }
    }
}
