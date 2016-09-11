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
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

namespace BurageSnap
{
    public class OptionViewModel : BindableBase, IInteractionRequestAware
    {
        private INotification _notification;

        public INotification Notification
        {
            get { return _notification; }
            set
            {
                Options = value.Content as OptionContent;
                SetProperty(ref _notification, value);
            }
        }

        public Action FinishInteraction { get; set; }

        private OptionContent _options;

        public OptionContent Options
        {
            get { return _options; }
            set { SetProperty(ref _options, value); }
        }

        private string _title;

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public ICommand OkCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand SelectedCommand { get; private set; }
        public ICommand AddTitleCommand { get; private set; }
        public ICommand RemoveTitleCommand { get; private set; }

        public OptionViewModel()
        {
            OkCommand = new DelegateCommand(OkInteraction);
            CancelCommand = new DelegateCommand(CancelInteraction);
            SelectedCommand= new DelegateCommand<object[]>(Selected);
            AddTitleCommand = new DelegateCommand(AddTitle);
            RemoveTitleCommand = new DelegateCommand(RemoveTitle);
        }

        public void OkInteraction()
        {
            ((IConfirmation)Notification).Confirmed = true;
            FinishInteraction();
        }

        public void CancelInteraction()
        {
            ((IConfirmation)Notification).Confirmed = false;
            FinishInteraction();
        }

        public void Selected(object[] args)
        {
            var title = args.FirstOrDefault() as string;
            if (title == null)
                return;
            Title = title;
        }

        public void AddTitle()
        {
            if (Options.WindowTitles.Contains(Title))
                return;
            Options.WindowTitles.Add(Title);
        }

        public void RemoveTitle()
        {
            Options.WindowTitles.Remove(Title);
        }
    }

}