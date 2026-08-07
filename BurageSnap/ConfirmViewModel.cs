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

using System.Windows.Input;
using BurageSnap.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurageSnap;

public class ConfirmViewModel : ObservableObject, IInteractionRequestAware
{
    public INotification Notification { get; set; }
    public Action FinishInteraction { get; set; }

    public ICommand YesCommand { get; }
    public ICommand NoCommand { get; }

    public ConfirmViewModel()
    {
        YesCommand = new RelayCommand(YesInteraction);
        NoCommand = new RelayCommand(NoInteraction);
    }

    private void YesInteraction()
    {
        ((IConfirmation)Notification).Confirmed = true;
        FinishInteraction();
    }

    private void NoInteraction()
    {
        ((IConfirmation)Notification).Confirmed = false;
        FinishInteraction();
    }
}