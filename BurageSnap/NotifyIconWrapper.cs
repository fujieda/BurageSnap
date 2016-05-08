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
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace BurageSnap
{
    public class NotifyIconWrapper : FrameworkElement
    {
        private readonly NotifyIcon _notifyIcon;

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(NotifyIconWrapper), new PropertyMetadata(
                (d, e) =>
                {
                    if (((NotifyIconWrapper)d)._notifyIcon == null)
                        return;
                    ((NotifyIconWrapper)d)._notifyIcon.Text = (string)e.NewValue;
                }));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly RoutedEvent OpenSelectedEvent = EventManager.RegisterRoutedEvent("OpenSelected",
            RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(NotifyIconWrapper));

        public event RoutedEventHandler OpenSelected
        {
            add { AddHandler(OpenSelectedEvent, value);}
            remove { RemoveHandler(OpenSelectedEvent, value);}
        }

        public static readonly RoutedEvent ExitSelectedEvent = EventManager.RegisterRoutedEvent("ExitSelected",
            RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(NotifyIconWrapper));

        public event RoutedEventHandler ExitSelected
        {
            add { AddHandler(ExitSelectedEvent, value); }
            remove { RemoveHandler(ExitSelectedEvent, value); }
        }

        public NotifyIconWrapper()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;
            _notifyIcon = new NotifyIcon
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = true,
                ContextMenuStrip = CreateContextMenu()
            };
            _notifyIcon.DoubleClick += OpenItemOnClick;
            Application.Current.Exit += (obj, args) => { _notifyIcon.Dispose(); };
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem(Properties.Resources.NotifyIcon_Open);
            openItem.Click += OpenItemOnClick;
            var exitItem = new ToolStripMenuItem(Properties.Resources.NotifyIcon_Exit);
            exitItem.Click += ExitItemOnClick;
            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(exitItem);
            return contextMenu;
        }

        private void OpenItemOnClick(object sender, EventArgs eventArgs)
        {
            var args = new RoutedEventArgs(OpenSelectedEvent);
            RaiseEvent(args);
        }

        private void ExitItemOnClick(object sender, EventArgs eventArgs)
        {
            var args = new RoutedEventArgs(ExitSelectedEvent);
            RaiseEvent(args);
        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
        }
    }
}