// Copyright (C) 2015 Kazuhiro Fujieda <fujieda@users.osdn.me>
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BurageSnap.Properties;

namespace BurageSnap
{
    public partial class FormMain : Form
    {
        private readonly Config _config;
        private readonly OptionDialog _optionDialog;
        private readonly ConfirmDialog _confirmDialog = new ConfirmDialog();
        private readonly Recorder _recorder;
        private bool _captureing;

        public FormMain()
        {
            InitializeComponent();
            _config = Config.Load();
            _optionDialog = new OptionDialog(_config);
            _recorder = new Recorder(_config) {ReportCaptureResult = ReportCaptureResult};
        }

        private void ReportCaptureResult(object obj)
        {
            BeginInvoke(new Action(() =>
            {
                if (obj is DateTime)
                {
                    var time = (DateTime)obj;
                    labelTimeStamp.Text = time.ToString("HH:mm:ss.fff");
                    if (time == DateTime.MinValue && _captureing)
                        AbortRecording();
                }
                // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                else if (obj is string)
                {
                    labelTimeStamp.Text = (string)obj;
                    if (_captureing)
                        AbortRecording();
                }
            }));
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            TopMost = _config.TopMost;
            checkBoxContinuous.Checked = _config.Continuous;
            if (_config.Location.IsEmpty)
                return;
            var newb = Bounds;
            newb.Location = _config.Location;
            if (IsVisibleOnAnyScreen(newb))
                Location = _config.Location;
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _config.Continuous = checkBoxContinuous.Checked;
            _config.Location = Location;
            _config.Save();
        }

        public static bool IsVisibleOnAnyScreen(Rectangle rect)
        {
            return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(rect));
        }

        private void buttonOption_Click(object sender, EventArgs e)
        {
            if (_optionDialog.ShowDialog(this) != DialogResult.OK)
                return;
            TopMost = _config.TopMost;
        }

        private void buttonCapture_Click(object sender, EventArgs e)
        {
            if (!checkBoxContinuous.Checked)
            {
                _recorder.OneShot();
                return;
            }
            if (!_captureing)
            {
                buttonCapture.Text = Resources.FormMain_buttonCapture_Click_Stop;
                checkBoxContinuous.Enabled = false;
                buttonOption.Enabled = false;
                _recorder.Start();
                _captureing = true;
            }
            else
            {
                _recorder.Stop();
                if (_config.RingBuffer > 0)
                {
                   if (_confirmDialog.ShowDialog(this) == DialogResult.Yes)
                        _recorder.SaveBuffer();
                   else
                        _recorder.DiscardBuffer();
                }
                AbortRecording();
            }
        }

        private void AbortRecording()
        {
            buttonCapture.Text = Resources.FormMain_buttonCapture_Click_Start;
            checkBoxContinuous.Enabled = true;
            buttonOption.Enabled = true;
            _captureing = false;
        }

        private void checkBoxContinuous_CheckedChanged(object sender, EventArgs e)
        {
            buttonCapture.Text = checkBoxContinuous.Checked
                ? Resources.FormMain_buttonCapture_Click_Start
                : Resources.FormMain_checkBoxContinuous_CheckedChanged_Capture;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var dir = _config.Folder;
            if (_config.DailyFolder)
            {
                var now = DateTime.Now;
                dir = Path.Combine(_config.Folder, now.ToString(Recorder.DateFormat));
            }
            Directory.CreateDirectory(dir);
            Process.Start(dir);
        }
    }
}