// Copyright (C) 2015 Kazuhiro Fujieda <fujieda@users.osdn.me>
//
// This program is part of BurageSnap.
//
// BurageSnap is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, see <http://www.gnu.org/licenses/>.

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
        private readonly Recorder _recorder;
        private bool _captureing;

        public FormMain()
        {
            InitializeComponent();
            _config = Config.Load();
            _optionDialog = new OptionDialog(_config);
            _recorder = new Recorder(_config) {ReportCaptureTime = ReportCaptureTime};
        }

        private void ReportCaptureTime(DateTime time)
        {
            BeginInvoke(new Action(() =>
            {
                labelTimeStamp.Text = time.ToString("HH:mm:ss.fff");
                if (time == DateTime.MinValue && _captureing)
                    buttonCapture.PerformClick();
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
                buttonCapture.Text = Resources.FormMain_buttonCapture_Click_Start;
                checkBoxContinuous.Enabled = true;
                buttonOption.Enabled = true;
                _captureing = false;
            }
        }

        private void checkBoxContinuous_CheckedChanged(object sender, EventArgs e)
        {
            buttonCapture.Text = checkBoxContinuous.Checked
                ? Resources.FormMain_buttonCapture_Click_Start
                : Resources.FormMain_checkBoxContinuous_CheckedChanged_Capture;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var dir = Path.Combine(_config.Folder, now.ToString(Recorder.DateFormat));
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch
            {
                return;
            }
            Process.Start(dir);
        }
    }
}