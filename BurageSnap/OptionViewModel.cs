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
            get => _notification;
            set
            {
                Options = (OptionContent)value.Content;
                JpegQuality = Options.JpegQuality.ToString();
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
            get => _options;
            set => SetProperty(ref _options, value);
        }

        private string _interval;

        public string Interval
        {
            get => _interval;
            set
            {
                SetProperty(ref _interval, value);
                if (!int.TryParse(_interval, out var result) || result < 10 || result > 1000 * 1000)
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
            get => _ringBuffer;
            set
            {
                SetProperty(ref _ringBuffer, value);
                if (!int.TryParse(value, out var result) || result < 0 || result > 100)
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

        private string _jpegQuality;

        public string JpegQuality
        {
            get => _jpegQuality;
            set
            {
                SetProperty(ref _jpegQuality, value);
                if (!int.TryParse(value, out var result) || result < 0 || result > 100)
                {
                    SetError(Resources.OptionView_Validate_jpeg_quality);
                }
                else
                {
                    ClearError();
                }
                _options.JpegQuality = result;
            }
        }

        private bool _animationGif;

        public bool AnimationGif
        {
            get => _animationGif;
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
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public IEnumerable<string> KeyList => GlobalHotKey.KeyList;

        private KeyModifier _modifier;

        public KeyModifier Modifier
        {
            get => _modifier;
            set => SetProperty(ref _modifier, value);
        }

        private string _hotKey;

        public string HotKey
        {
            get => _hotKey;
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
        public ICommand CancelCommand { get; }
        public ICommand SelectedCommand { get; }
        public ICommand AddTitleCommand { get; }
        public ICommand RemoveTitleCommand { get; }
        public ICommand ChooseWindowCommand { get; }
        public ICommand UnloadedCommand { get; }

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

        private void OkInteraction()
        {
            Options.HotKeyModifier = Modifier.Value;
            Options.HotKey = HotKey;
            ((IConfirmation)Notification).Confirmed = true;
            FinishInteraction();
        }

        private void CancelInteraction()
        {
            ((IConfirmation)Notification).Confirmed = false;
            FinishInteraction();
        }

        private void Selected(object[] args)
        {
            var title = args.FirstOrDefault() as string;
            if (title == null)
                return;
            Title = title;
        }

        private void AddTitle()
        {
            if (Options.WindowTitles.Contains(Title))
                return;
            Options.WindowTitles.Add(Title);
        }

        private void RemoveTitle()
        {
            Options.WindowTitles.Remove(Title);
        }

        private void ChooseWindow()
        {
            WindowPicker.Start();
        }

        private void Unloaded()
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
            _errors.SetErrors(propertyName, new[] {message});
        }

        private void ClearError([CallerMemberName] string propertyName = null)
        {
            _errors.ClearErrors(propertyName);
        }
    }
}