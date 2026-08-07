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
using Microsoft.Xaml.Behaviors;

namespace BurageSnap.Interactivity;

/// <summary>
/// Prism.Interactivity.InteractionRequest.InteractionRequestTrigger互換の自作実装
/// </summary>
public class InteractionRequestTrigger : TriggerBase<FrameworkElement>
{
    public static readonly DependencyProperty SourceObjectProperty =
        DependencyProperty.Register(
            nameof(SourceObject),
            typeof(object),
            typeof(InteractionRequestTrigger),
            new PropertyMetadata(OnSourceObjectChanged));

    public object SourceObject
    {
        get => GetValue(SourceObjectProperty);
        set => SetValue(SourceObjectProperty, value);
    }

    private static void OnSourceObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var trigger = (InteractionRequestTrigger)d;

        if (e.OldValue != null)
        {
            trigger.UnsubscribeFromSourceObject(e.OldValue);
        }

        if (e.NewValue != null)
        {
            trigger.SubscribeToSourceObject(e.NewValue);
        }
    }

    private void SubscribeToSourceObject(object source)
    {
        var eventInfo = source.GetType().GetEvent("Raised");
        if (eventInfo != null)
        {
            var handler = new EventHandler<InteractionRequestedEventArgs>(OnEventRaised);
            eventInfo.AddEventHandler(source, handler);
        }
    }

    private void UnsubscribeFromSourceObject(object source)
    {
        var eventInfo = source.GetType().GetEvent("Raised");
        if (eventInfo != null)
        {
            var handler = new EventHandler<InteractionRequestedEventArgs>(OnEventRaised);
            eventInfo.RemoveEventHandler(source, handler);
        }
    }

    private void OnEventRaised(object sender, InteractionRequestedEventArgs e)
    {
        InvokeActions(e);
    }
}
