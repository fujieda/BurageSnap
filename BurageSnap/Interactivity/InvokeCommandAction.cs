// Copyright (C) 2026 Kazuhiro Fujieda <fujieda@roundwide.com>
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
using Microsoft.Xaml.Behaviors;

namespace BurageSnap.Interactivity;

/// <summary>
/// Prism.Interactivity.InvokeCommandAction互換の自作実装（TriggerParameterPathサポート含む）
/// </summary>
public class InvokeCommandAction : TriggerAction<DependencyObject>
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(InvokeCommandAction));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty TriggerParameterPathProperty =
        DependencyProperty.Register(
            nameof(TriggerParameterPath),
            typeof(string),
            typeof(InvokeCommandAction));

    public string TriggerParameterPath
    {
        get => (string)GetValue(TriggerParameterPathProperty);
        set => SetValue(TriggerParameterPathProperty, value);
    }

    protected override void Invoke(object parameter)
    {
        if (Command == null)
            return;

        var commandParameter = parameter;

        if (!string.IsNullOrEmpty(TriggerParameterPath) && parameter != null)
        {
            commandParameter = GetPropertyValue(parameter, TriggerParameterPath);
        }

        if (Command.CanExecute(commandParameter))
        {
            Command.Execute(commandParameter);
        }
    }

    private object GetPropertyValue(object obj, string propertyPath)
    {
        if (obj == null || string.IsNullOrEmpty(propertyPath))
            return obj;

        var propertyInfo = obj.GetType().GetProperty(propertyPath, BindingFlags.Public | BindingFlags.Instance);
        return propertyInfo?.GetValue(obj);
    }
}
