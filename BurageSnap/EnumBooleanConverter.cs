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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BurageSnap
{
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;
            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;
            var parameterValue = Enum.Parse(value.GetType(), parameterString);
            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;
            return Enum.Parse(targetType, parameterString);
        }
    }
}