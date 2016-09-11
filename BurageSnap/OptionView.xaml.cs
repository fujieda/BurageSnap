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

using System.Windows;
using System.Windows.Forms;

namespace BurageSnap
{
    /// <summary>
    /// OptionView.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionView
    {
        private readonly FolderBrowserDialog _browserDialog = new FolderBrowserDialog();

        public OptionView()
        {
            InitializeComponent();
        }

        private void buttonFolderBrowser_Click(object sender, RoutedEventArgs e)
        {
            _browserDialog.SelectedPath = textBoxFolder.Text;
            if (_browserDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxFolder.Text = _browserDialog.SelectedPath;
                textBoxFolder.Focus();
                textBoxFolder.CaretIndex = textBoxFolder.Text.Length;
            }
        }
    }
}
