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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BurageSnap.Properties;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

namespace BurageSnap
{
    public class OptionViewModel : BindableBase, IInteractionRequestAware, INotifyDataErrorInfo
    {
        private INotification _notification;

        public INotification Notification
        {
            get { return _notification; }
            set
            {
                Options = (OptionContent)value.Content;
                Interval = Options.Interval.ToString();
                RingBuffer = Options.RingBuffer.ToString();
                AnimationGif = Options.AnimationGif;
                Modifier = new KeyModifier {Value = Options.HotKeyModifier};
                HotKey = Options.HotKey;
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

        private string _interval;

        public string Interval
        {
            get { return _interval; }
            set
            {
                SetProperty(ref _interval, value);
                int result;
                if (!int.TryParse(_interval, out result) || result < 10 || result > 1000 * 1000)
                {
                    SetError(Resources.OptionView_Validate_interval);
                }
                else
                {
                    ClearError();
                }
                _options.Interval = result;
            }
        }

        private string _ringBuffer;

        public string RingBuffer
        {
            get { return _ringBuffer; }
            set
            {
                SetProperty(ref _ringBuffer, value);
                int result;
                if (!int.TryParse(value, out result) || result < 0 || result > 100)
                {
                    SetError(Resources.OptionView_Validate_ring_buffer);
                }
                else if (_options.AnimationGif && result < 2)
                {
                    SetError(Resources.OptionView_Validate_ring_buffer_for_animation_GIF);
                }
                else
                {
                    ClearError();
                }
                _options.RingBuffer = result;
            }
        }

        private bool _animationGif;

        public bool AnimationGif
        {
            get { return _animationGif; }
            set
            {
                SetProperty(ref _animationGif, value);
                if (value && _options.RingBuffer <= 1)
                {
                    // ReSharper disable once ExplicitCallerInfoArgument
                    SetError(Resources.OptionView_Validate_ring_buffer_for_animation_GIF, nameof(RingBuffer));
                }
                else
                {
                    // ReSharper disable once ExplicitCallerInfoArgument
                    ClearError(nameof(RingBuffer));
                }
                _options.AnimationGif = value;
            }
        }

        private string _title;

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public IEnumerable<string> KeyList => GlobelHotKey.KeyList;

        private KeyModifier _modifier;

        public KeyModifier Modifier
        {
            get { return _modifier; }
            set { SetProperty(ref _modifier, value); }
        }

        private string _hotKey;

        public string HotKey
        {
            get { return _hotKey; }
            set
            {
                SetProperty(ref _hotKey, value);
                if (value == "")
                {
                    Modifier.Value = 0;
                    OnPropertyChanged(() => Modifier);
                }
                OnPropertyChanged(() => IsKeySelected);
            }
        }

        public bool IsKeySelected => HotKey != "";

        private readonly ErrorsContainer<string> _errors;

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; private set; }
        public ICommand SelectedCommand { get; private set; }
        public ICommand AddTitleCommand { get; private set; }
        public ICommand RemoveTitleCommand { get; private set; }
        public ICommand ChooseWindowCommand { get; private set; }
        public ICommand UnloadedCommand { get; private set; }

        public OptionViewModel()
        {
            _errors = new ErrorsContainer<string>(OnErrorsChanged);

            OkCommand = new DelegateCommand(OkInteraction, () => !HasErrors);
            CancelCommand = new DelegateCommand(CancelInteraction);
            SelectedCommand = new DelegateCommand<object[]>(Selected);
            AddTitleCommand = new DelegateCommand(AddTitle);
            RemoveTitleCommand = new DelegateCommand(RemoveTitle);
            ChooseWindowCommand = new DelegateCommand(ChooseWindow);
            UnloadedCommand = new DelegateCommand(Unloaded);

            WindowPicker.Picked += title => { Title = title; };
        }

        public void OkInteraction()
        {
            Options.HotKeyModifier = Modifier.Value;
            Options.HotKey = HotKey;
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

        public void ChooseWindow()
        {
            WindowPicker.Start();
        }

        public void Unloaded()
        {
            WindowPicker.Stop();
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public bool HasErrors => _errors.HasErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            return _errors.GetErrors(propertyName);
        }

        private void OnErrorsChanged([CallerMemberName] string propertyName = null)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            ((DelegateCommand)OkCommand).RaiseCanExecuteChanged();
        }

        private void SetError(string message, [CallerMemberName] string propertyName = null)
        {
            _errors.SetErrors(propertyName, new [] {message});
        }

        private void ClearError([CallerMemberName] string propertyName = null)
        {
            _errors.ClearErrors(propertyName);
        }
    }
}