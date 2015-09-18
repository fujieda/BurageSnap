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
using System.Linq;
using System.Windows.Forms;

namespace BurageSnap
{
    public partial class OptionDialog : Form
    {
        private readonly Config _config;

        public OptionDialog(Config config)
        {
            InitializeComponent();
            _config = config;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            _config.TopMost = checkBoxTopMost.Checked;
            _config.Interval = int.Parse(textBoxInterval.Text);
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
        }

        private void OptionDialog_Load(object sender, EventArgs e)
        {
            checkBoxTopMost.Checked = _config.TopMost;
            textBoxInterval.Text = _config.Interval.ToString();
            comboBoxWindowTitle.Items.Clear();
            // ReSharper disable once CoVariantArrayConversion
            comboBoxWindowTitle.Items.AddRange(_config.TitleHistory);
            comboBoxWindowTitle.Text = _config.TitleHistory[0];
            textBoxFolder.Text = _config.Folder;
            textBoxFolder.Select(textBoxFolder.TextLength, 0);
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = textBoxFolder.Text;
            if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
                return;
            textBoxFolder.Text = folderBrowserDialog.SelectedPath;
            textBoxFolder.Select(textBoxFolder.TextLength, 0);
        }
    }
}