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

using System.Reflection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using BurageSnap.Properties;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

namespace BurageSnap
{
    internal class MainWindowViewModel : BindableBase
    {
        public Main Main { get; }
        public ICommand LoadedCommand { get; private set; }
        public ICommand ClosingCommand { get; private set; }
        public ICommand BrowseCommand { get; private set; }
        public ICommand OptionCommand { get; private set; }
        public ICommand CaptureCommand { get; private set; }
        public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();
        public InteractionRequest<IConfirmation> OptionViewRequest { get; } = new InteractionRequest<IConfirmation>();
        public ICommand NotifyIconOpenCommand { get; private set; }
        public ICommand NotifyIconExitCommand { get; private set; }

        public InteractionRequest<INotification> ShowBaloonTipRequest { get; } =
            new InteractionRequest<INotification>();

        public bool BurstMode
        {
            get { return Main.Config.Continuous; }
            set
            {
                Main.Config.Continuous = value;
                OnPropertyChanged(() => CaptureButtonText);
            }
        }

        public bool AllowChangeSettings => !Main.Capturing;

        public string CaptureButtonText
            => BurstMode
                ? Main.Capturing
                    ? Resources.MainWindow_Stop
                    : Resources.MainWindow_Start
                : Resources.MainWindow_Capture;

        public bool ShowInTaskbar => !(WindowState == WindowState.Minimized && Main.Config.ResideInSystemTray);

        public WindowState WindowState
        {
            get { return Main.Config.WindowState; }
            set
            {
                if (Main.Config.WindowState == value)
                    return;
                Main.Config.WindowState = value;
                OnPropertyChanged(() => WindowState);
                OnPropertyChanged(() => ShowInTaskbar);
            }
        }

        public MainWindowViewModel()
        {
            Main = new Main();
            Main.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "Capturing")
                {
                    OnPropertyChanged(() => CaptureButtonText);
                    OnPropertyChanged(() => AllowChangeSettings);
                }
            };
            LoadedCommand = new DelegateCommand(Loaded);
            ClosingCommand = new DelegateCommand<CancelEventArgs>(Closing);
            BrowseCommand = new DelegateCommand(Main.OpenPictureFolder);
            OptionCommand = new DelegateCommand(SelectOption);
            CaptureCommand = new DelegateCommand(Capture);
            NotifyIconOpenCommand = new DelegateCommand(() => { WindowState = WindowState.Normal; });
            NotifyIconExitCommand = new DelegateCommand(() =>
            {
                Terminate();
                Application.Current.Shutdown();
            });
        }

        private void Loaded()
        {
            RestoreLocation();
            SetHotKey();
            _globelHotKey.HotKeyPressed += Capture;
        }

        private void RestoreLocation()
        {
            var window = Application.Current.MainWindow;
            window.Topmost = Main.Config.TopMost;
            var location = Main.Config.Location;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (location.X == double.MinValue)
                return;
            var width = window.Width;
            var height = window.Height;
            var newBounds = new Rect(location.X, location.Y, width, height);
            if (!IsVisibleOnScreen(newBounds))
                return;
            window.Left = location.X;
            window.Top = location.Y;
        }

        private void SelectOption()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            OptionViewRequest.Raise(new Confirmation
            {
                Title = assembly.Name + " " + assembly.Version.Major + "." + assembly.Version.Minor + " - " +
                        Resources.OptionView_Option,
                Content = new OptionContent(Main.Config)
            }, c =>
            {
                if (!c.Confirmed)
                    return;
                ((OptionContent)c.Content).ToConfig(Main.Config);
                Application.Current.MainWindow.Topmost = Main.Config.TopMost;
                SetHotKey();
            });
        }

        private readonly GlobelHotKey _globelHotKey = new GlobelHotKey();

        private void SetHotKey()
        {
            var config = Main.Config;
            _globelHotKey.Register(Application.Current.MainWindow, config.HotKeyModifier, config.HotKey);
        }

        private void Closing(CancelEventArgs e)
        {
            if (Main.Config.ResideInSystemTray)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
            else
            {
                Terminate();
            }
        }

        public void Terminate()
        {
            SaveConfig();
            _globelHotKey.Unregister();
        }

        private void SaveConfig()
        {
            var config = Main.Config;
            var main = Application.Current.MainWindow;
            config.Location = main.WindowState == WindowState.Normal
                ? new Point(main.Left, main.Top)
                : new Point(main.RestoreBounds.Left, main.RestoreBounds.Top);
            config.Save();
        }

        public static bool IsVisibleOnScreen(Rect rect)
        {
            return new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight).IntersectsWith(rect);
        }

        private void Capture()
        {
            try
            {
                if (!BurstMode)
                {
                    Main.OneShot();
                    Notify(Resources.MainWindow_Captured);
                    return;
                }
                if (!Main.Capturing)
                {
                    Main.StartCapture();
                    Notify(Resources.MainWindow_Capture_started);
                }
                else
                {
                    Main.StopCapture();
                    Notify(Resources.MainWindow_Capture_ended);
                    ConfirmSaveBuffer();
                }
            }
            catch (CaptureError e)
            {
                if (Main.Config.Notify)
                    ShowBaloonTipRequest.Raise(new Notification {Title = Resources.MainWindow_Error, Content = e.Message});
            }
        }

        private void ConfirmSaveBuffer()
        {
            WindowState = WindowState.Normal;
            ConfirmationRequest.Raise(new Confirmation {Title = Resources.ConfirmView_Title}, c =>
            {
                if (c.Confirmed)
                    Main.SaveBuffer();
                else
                    Main.DiscardBuffer();
            });
        }

        private void Notify(string message)
        {
            if (!Main.Config.Notify)
                return;
            var title = Main.WindowTitle;
            if (title == "")
            {
                ShowBaloonTipRequest.Raise(new Notification
                {
                    Title = Resources.MainWindow_Error,
                    Content = Main.CaptureResult
                });
                return;
            }
            if (title.Length > 22)
                title = title.Substring(0, 22) + "...";
            ShowBaloonTipRequest.Raise(new Notification {Title = message, Content = title});
        }
    }
}