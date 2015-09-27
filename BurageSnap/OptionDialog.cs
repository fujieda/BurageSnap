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
            _config.Format = radioButtonJpg.Checked ? OutputFormat.Jpg : OutputFormat.Png;
            _config.AnimationGif = checkBoxAnimationGif.Checked;
        }

        private void OptionDialog_Load(object sender, EventArgs e)
        {
            checkBoxTopMost.Checked = _config.TopMost;
            textBoxInterval.Text = _config.Interval.ToString();
            textBoxRingBuffer.Text = _config.RingBuffer.ToString();
            comboBoxWindowTitle.Items.Clear();
            // ReSharper disable once CoVariantArrayConversion
            comboBoxWindowTitle.Items.AddRange(_config.TitleHistory);
            comboBoxWindowTitle.Text = _config.TitleHistory[0];
            textBoxFolder.Text = _config.Folder;
            textBoxFolder.Select(textBoxFolder.TextLength, 0);
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
            if (int.TryParse(textBoxInterval.Text, out interval) && 0 < interval && interval < 1000 * 1000)
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