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

using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using BurageSnap.Interactivity;
using BurageSnap.Properties;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Application = System.Windows.Application;
using Point = System.Windows.Point;

namespace BurageSnap;

internal class MainWindowViewModel : ObservableObject
{
    public Main Main { get; }
    public ICommand LoadedCommand { get; }
    public ICommand ClosingCommand { get; }
    public ICommand BrowseCommand { get; }
    public ICommand OptionCommand { get; }
    public ICommand CaptureCommand { get; }
    public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();
    public InteractionRequest<IConfirmation> OptionViewRequest { get; } = new InteractionRequest<IConfirmation>();
    public ICommand NotifyIconOpenCommand { get; }
    public ICommand NotifyIconExitCommand { get; }

    public InteractionRequest<INotification> ShowBalloonTipRequest { get; } =
        new InteractionRequest<INotification>();

    public bool BurstMode
    {
        get => Main.Config.Continuous;
        set
        {
            Main.Config.Continuous = value;
            OnPropertyChanged(nameof(CaptureButtonText));
        }
    }

    public bool AllowChangeSettings => !Main.Capturing;

    public string CaptureButtonText
        => BurstMode
            ? Main.Capturing
                ? Resources.MainWindow_Stop
                : Resources.MainWindow_Start
            : Resources.MainWindow_Capture;

    private bool _showInTaskbar = true;

    public bool ShowInTaskbar
    {
        get => _showInTaskbar;
        set => SetProperty(ref _showInTaskbar, value);
    }

    private WindowStyle _windowStyle;

    public WindowStyle WindowStyle
    {
        get => _windowStyle;
        set => SetProperty(ref _windowStyle, value);
    }

    private WindowState _windowState = WindowState.Normal;

    public WindowState WindowState
    {
        get => _windowState;
        set
        {
            if (_windowState == value)
                return;
            Main.Config.WindowState = value;
            SetProperty(ref _windowState, value);
            var hide = WindowState == WindowState.Minimized && Main.Config.ResideInSystemTray;
            ShowInTaskbar = !hide;
            WindowStyle = hide ? WindowStyle.ToolWindow : WindowStyle.SingleBorderWindow;
        }
    }

    public MainWindowViewModel()
    {
        Main = new Main();
        Main.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == "Capturing")
            {
                OnPropertyChanged(nameof(CaptureButtonText));
                OnPropertyChanged(nameof(AllowChangeSettings));
            }
        };
        LoadedCommand = new RelayCommand(Loaded);
        ClosingCommand = new RelayCommand<CancelEventArgs>(Closing);
        BrowseCommand = new RelayCommand(Main.OpenPictureFolder);
        OptionCommand = new RelayCommand(SelectOption);
        CaptureCommand = new RelayCommand(Capture);
        NotifyIconOpenCommand = new RelayCommand(() => { WindowState = WindowState.Normal; });
        NotifyIconExitCommand = new RelayCommand(() =>
        {
            Terminate();
            Application.Current.Shutdown();
        });
    }

    private void Loaded()
    {
        RestoreLocation();
        WindowState = Main.Config.WindowState;
        SetHotKey();
        _globalHotKey.HotKeyPressed += Capture;
    }

    private void RestoreLocation()
    {
        var window = Application.Current.MainWindow;
        if (window == null)
            return;
        window.Topmost = Main.Config.TopMost;
        var location = Main.Config.Location;
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
            var main = Application.Current.MainWindow;
            if (main == null)
                return;
            main.Topmost = Main.Config.TopMost;
            SetHotKey();
        });
    }

    private readonly GlobalHotKey _globalHotKey = new GlobalHotKey();

    private void SetHotKey()
    {
        var config = Main.Config;
        _globalHotKey.Register(Application.Current.MainWindow, config.HotKeyModifier, config.HotKey);
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

    private void Terminate()
    {
        SaveConfig();
        _globalHotKey.UnRegister();
    }

    private void SaveConfig()
    {
        var config = Main.Config;
        var main = Application.Current.MainWindow;
        if (main == null)
            return;
        config.Location = main.WindowState == WindowState.Normal
            ? new Point(main.Left, main.Top)
            : new Point(main.RestoreBounds.Left, main.RestoreBounds.Top);
        config.Save();
    }

    private static bool IsVisibleOnScreen(Rect rect)
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
                ShowBalloonTipRequest.Raise(new Notification
                    {Title = Resources.MainWindow_Error, Content = e.Message});
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
            ShowBalloonTipRequest.Raise(new Notification
            {
                Title = Resources.MainWindow_Error,
                Content = Main.CaptureResult
            });
            return;
        }
        if (title.Length > 22)
            title = title.Substring(0, 22) + "...";
        ShowBalloonTipRequest.Raise(new Notification {Title = message, Content = title});
    }
}