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

namespace BurageSnap.Interactivity;

/// <summary>
/// Prism.Interactivity.InteractionRequest互換の自作実装
/// </summary>
public interface INotification
{
    string Title { get; set; }
    object Content { get; set; }
}

public interface IConfirmation : INotification
{
    bool Confirmed { get; set; }
}

public class Notification : INotification
{
    public string Title { get; set; }
    public object Content { get; set; }
}

public class Confirmation : Notification, IConfirmation
{
    public bool Confirmed { get; set; }
}

public interface IInteractionRequestAware
{
    INotification Notification { get; set; }
    Action FinishInteraction { get; set; }
}

public class InteractionRequestedEventArgs : EventArgs
{
    public object Context { get; }
    public Action Callback { get; }

    public InteractionRequestedEventArgs(object context, Action callback)
    {
        Context = context;
        Callback = callback;
    }
}

public class InteractionRequest<T> where T : INotification
{
    public event EventHandler<InteractionRequestedEventArgs> Raised;

    public void Raise(T notification)
    {
        Raise(notification, null);
    }

    public void Raise(T notification, Action<T> callback)
    {
        var handler = Raised;
        if (handler != null)
        {
            handler(this, new InteractionRequestedEventArgs(notification, () => callback?.Invoke(notification)));
        }
    }
}
