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
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace BurageSnap.Interactivity;

/// <summary>
/// Prism.Interactivity.PopupWindowAction互換の自作実装
/// </summary>
public abstract class PopupWindowAction : TriggerAction<FrameworkElement>
{
    public static readonly DependencyProperty IsModalProperty =
        DependencyProperty.Register(
            nameof(IsModal),
            typeof(bool),
            typeof(PopupWindowAction),
            new PropertyMetadata(true));

    public bool IsModal
    {
        get => (bool)GetValue(IsModalProperty);
        set => SetValue(IsModalProperty, value);
    }

    public static readonly DependencyProperty WindowStartupLocationProperty =
        DependencyProperty.Register(
            nameof(WindowStartupLocation),
            typeof(WindowStartupLocation),
            typeof(PopupWindowAction),
            new PropertyMetadata(WindowStartupLocation.CenterScreen));

    public WindowStartupLocation WindowStartupLocation
    {
        get => (WindowStartupLocation)GetValue(WindowStartupLocationProperty);
        set => SetValue(WindowStartupLocationProperty, value);
    }

    public static readonly DependencyProperty WindowContentProperty =
        DependencyProperty.Register(
            nameof(WindowContent),
            typeof(FrameworkElement),
            typeof(PopupWindowAction));

    public FrameworkElement WindowContent
    {
        get => (FrameworkElement)GetValue(WindowContentProperty);
        set => SetValue(WindowContentProperty, value);
    }

    public static readonly DependencyProperty WindowStyleProperty =
        DependencyProperty.Register(
            nameof(WindowStyle),
            typeof(Style),
            typeof(PopupWindowAction));

    public Style WindowStyle
    {
        get => (Style)GetValue(WindowStyleProperty);
        set => SetValue(WindowStyleProperty, value);
    }

    protected override void Invoke(object parameter)
    {
        var args = parameter as InteractionRequestedEventArgs;
        if (args == null)
            return;

        var context = args.Context as INotification;
        if (context == null)
            return;

        var window = CreateWindow();
        window.Title = context.Title;
        window.WindowStartupLocation = WindowStartupLocation;

        if (WindowStyle != null)
        {
            window.Style = WindowStyle;
        }

        var content = WindowContent;
        window.Content = content;

        if (content?.DataContext is IInteractionRequestAware aware)
        {
            aware.Notification = context;
            aware.FinishInteraction = () =>
            {
                window.Close();
                args.Callback?.Invoke();
            };
        }

        if (IsModal)
        {
            window.ShowDialog();
        }
        else
        {
            window.Show();
        }
    }

    protected abstract Window CreateWindow();
}
