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
using System.Diagnostics;
using System.IO;
using Prism.Mvvm;

namespace BurageSnap
{
    public class Main : BindableBase
    {
        private readonly Recorder _recorder;

        public Config Config { get; } = Config.Load();

        public Main()
        {
            _recorder = new Recorder(Config) {ReportCaptureResult = ReportCaptureResult};
        }

        public void OneShot()
        {
            try
            {
                _recorder.OneShot();
            }
            catch (CaptureError e)
            {
                CaptureResult = e.Summary;
                throw;
            }
        }

        public void StartCapture()
        {
            Capturing = true;
            try
            {
                _recorder.Start();
            }
            catch (CaptureError e)
            {
                CaptureResult = e.Summary;
                Capturing = false;
                throw;
            }
        }

        public void StopCapture()
        {
            _recorder.Stop();
        }

        public void SaveBuffer()
        {
            try
            {
                _recorder.SaveBuffer();
            }
            catch (CaptureError e)
            {
                CaptureResult = e.Summary;
                throw;
            }
            finally
            {
                Capturing = false;
            }
        }

        public void DiscardBuffer()
        {
            _recorder.DiscardBuffer();
            Capturing = false;
        }

        private string _windowTitle = "";

        public string WindowTitle
        {
            get { return _windowTitle; }
            set { SetProperty(ref _windowTitle, value); }
        }

        private string _captureResult = "00:00:00.000";

        public string CaptureResult
        {
            get { return _captureResult; }
            set { SetProperty(ref _captureResult, value); }
        }

        private bool _capturing;

        public bool Capturing
        {
            get { return _capturing; }
            set { SetProperty(ref _capturing, value); }
        }

        private void ReportCaptureResult(string title, DateTime time)
        {
            WindowTitle = title;
            CaptureResult = time.ToString("HH:mm:ss.fff");
        }

        public void OpenPictureFolder()
        {
            var dir = Config.Folder;
            if (Config.DailyFolder)
            {
                var now = DateTime.Now;
                dir = Path.Combine(Config.Folder, now.ToString(Recorder.DateFormat));
            }
            Directory.CreateDirectory(dir);
            Process.Start(dir);
        }
    }
}