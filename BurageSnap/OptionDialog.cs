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
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using BurageSnap.Properties;

namespace BurageSnap
{
    public partial class OptionDialog : Form
    {
        private readonly Config _config;
        private readonly ErrorProvider _errorProvider = new ErrorProvider();

        public OptionDialog(Config config)
        {
            InitializeComponent();
            _config = config;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            var ringbuffer = int.Parse(textBoxRingBuffer.Text);
            if (checkBoxAnimationGif.Checked && ringbuffer == 0)
            {
                DialogResult = DialogResult.None;
                _errorProvider.SetError(textBoxRingBuffer,
                    Resources.OptionDialog_buttonOk_Click_Ring_buffer_for_animation_GIF);
                return;
            }
            _errorProvider.SetError(textBoxRingBuffer, "");
            _config.TopMost = checkBoxTopMost.Checked;
            _config.Interval = int.Parse(textBoxInterval.Text);
            _config.RingBuffer = ringbuffer;
            var title = comboBoxWindowTitle.Text;
            if (title != "")
            {
                comboBoxWindowTitle.Items.Remove(title);
                comboBoxWindowTitle.Items.Insert(0, title);
                for (var i = comboBoxWindowTitle.Items.Count; i > 10; i--)
                    comboBoxWindowTitle.Items.RemoveAt(10);
            }
            _config.TitleHistory = (from object item in comboBoxWindowTitle.Items select item.ToString()).ToArray();
            _config.Folder = textBoxFolder.Text;
            _config.DailyFolder = checkBoxDailyFolder.Checked;
            _config.Format = radioButtonJpg.Checked ? OutputFormat.Jpg : OutputFormat.Png;
            _config.AnimationGif = checkBoxAnimationGif.Checked;
        }

        private void OptionDialog_Load(object sender, EventArgs e)
        {
            Text = Application.ProductName + @" " +
                   string.Join(".", Application.ProductVersion.Split('.').Take(2)) +
                   Resources.OptionDialog_OptionDialog_Load_Options;
            checkBoxTopMost.Checked = _config.TopMost;
            textBoxInterval.Text = _config.Interval.ToString();
            textBoxRingBuffer.Text = _config.RingBuffer.ToString();
            comboBoxWindowTitle.Items.Clear();
            // ReSharper disable once CoVariantArrayConversion
            comboBoxWindowTitle.Items.AddRange(_config.TitleHistory);
            comboBoxWindowTitle.Text = _config.TitleHistory[0];
            textBoxFolder.Text = _config.Folder;
            textBoxFolder.Select(textBoxFolder.TextLength, 0);
            checkBoxDailyFolder.Checked = _config.DailyFolder;
            radioButtonJpg.Checked = _config.Format == OutputFormat.Jpg;
            radioButtonPng.Checked = _config.Format == OutputFormat.Png;
            checkBoxAnimationGif.Checked = _config.AnimationGif;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = textBoxFolder.Text;
            if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
                return;
            textBoxFolder.Text = folderBrowserDialog.SelectedPath;
            textBoxFolder.Select(textBoxFolder.TextLength, 0);
        }

        private void textBoxInterval_Validating(object sender, CancelEventArgs e)
        {
            int interval;
            if (int.TryParse(textBoxInterval.Text, out interval) && 10 <= interval && interval < 1000 * 1000)
            {
                _errorProvider.SetError(textBoxInterval, "");
                return;
            }
            e.Cancel = true;
            _errorProvider.SetError(textBoxInterval, Resources.OptionDialog_textBoxInterval_Validating_Interval);
        }

        private void textBoxRingBuffer_Validating(object sender, CancelEventArgs e)
        {
            int frames;
            if (int.TryParse(textBoxRingBuffer.Text, out frames) && 0 <= frames && frames <= 100)
            {
                _errorProvider.SetError(textBoxRingBuffer, "");
                return;
            }
            e.Cancel = true;
            _errorProvider.SetError(textBoxRingBuffer, Resources.OptionDialog_textBoxRingBuffer_Validating);
        }
    }
}