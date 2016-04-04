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
        public ICommand CaptureCommand { get; private set; }
        public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();

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
            CaptureCommand = new DelegateCommand(Capture);
        }

        private void Loaded()
        {
            var location = Main.Config.Location;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (location.X == double.MinValue)
                return;
            var main = Application.Current.MainWindow;
            var width = main.Width;
            var height = main.Width;
            var newBounds = new Rect(location.X, location.Y, width, height);
            if (!IsVisibleOnScreen(newBounds))
                return;
            main.Left = location.X;
            main.Top = location.Y;
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