﻿// Copyright (C) 2016 Kazuhiro Fujieda <fujieda@users.osdn.me>
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

using System.Windows;
using MahApps.Metro.Controls;
using Prism.Interactivity;

namespace BurageSnap
{
    internal class MetroPopupWindowAction : PopupWindowAction
    {
        public static Window GetOwner(DependencyObject obj)
        {
            return (Window)obj.GetValue(OwnerProperty);
        }

        public static void SetOwner(DependencyObject obj, Window value)
        {
            obj.SetValue(OwnerProperty, value);
        }

        public static readonly DependencyProperty OwnerProperty = DependencyProperty.RegisterAttached("Owner", typeof(Window), typeof(MetroPopupWindowAction), new PropertyMetadata());

        protected override Window CreateWindow()
        {
            return new MetroWindow
            {
                Style = (Style)Application.Current.FindResource("WindowStyle"),
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                Owner = GetOwner(this)
            };
        }
    }
}