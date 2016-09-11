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
            ClosingCommand = new DelegateCommand(Closing);
            BrowseCommand = new DelegateCommand(Main.OpenPictureFolder);
            OptionCommand = new DelegateCommand(SelectOption);
            CaptureCommand = new DelegateCommand(Capture);
        }

        private void Loaded()
        {
            RestoreLocation();
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
            });
        }

        private void Closing()
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
            if (!BurstMode)
            {
                Main.OneShot();
                return;
            }
            if (!Main.Capturing)
            {
                Main.StartCapture();
            }
            else
            {
                Main.StopCapture();
                ConfirmSaveBuffer();
            }
        }

        private void ConfirmSaveBuffer()
        {
            ConfirmationRequest.Raise(new Confirmation {Title = Resources.ConfirmView_Title}, c =>
            {
                if (c.Confirmed)
                    Main.SaveBuffer();
                else
                    Main.DiscardBuffer();
            });
        }
    }
}