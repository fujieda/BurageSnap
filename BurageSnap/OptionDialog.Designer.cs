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

namespace BurageSnap
{
    partial class OptionDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionDialog));
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxFolder = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxInterval = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.checkBoxTopMost = new System.Windows.Forms.CheckBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.comboBoxWindowTitle = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.radioButtonJpg = new System.Windows.Forms.RadioButton();
            this.radioButtonPng = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxRingBuffer = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.checkBoxAnimationGif = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // textBoxFolder
            // 
            resources.ApplyResources(this.textBoxFolder, "textBoxFolder");
            this.textBoxFolder.Name = "textBoxFolder";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // textBoxInterval
            // 
            resources.ApplyResources(this.textBoxInterval, "textBoxInterval");
            this.textBoxInterval.Name = "textBoxInterval";
            this.textBoxInterval.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxInterval_Validating);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // buttonBrowse
            // 
            resources.ApplyResources(this.buttonBrowse, "buttonBrowse");
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // buttonOk
            // 
            resources.ApplyResources(this.buttonOk, "buttonOk");
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // checkBoxTopMost
            // 
            resources.ApplyResources(this.checkBoxTopMost, "checkBoxTopMost");
            this.checkBoxTopMost.Name = "checkBoxTopMost";
            this.checkBoxTopMost.UseVisualStyleBackColor = true;
            // 
            // comboBoxWindowTitle
            // 
            this.comboBoxWindowTitle.FormattingEnabled = true;
            resources.ApplyResources(this.comboBoxWindowTitle, "comboBoxWindowTitle");
            this.comboBoxWindowTitle.Name = "comboBoxWindowTitle";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // radioButtonJpg
            // 
            resources.ApplyResources(this.radioButtonJpg, "radioButtonJpg");
            this.radioButtonJpg.Name = "radioButtonJpg";
            this.radioButtonJpg.TabStop = true;
            this.radioButtonJpg.UseVisualStyleBackColor = true;
            // 
            // radioButtonPng
            // 
            resources.ApplyResources(this.radioButtonPng, "radioButtonPng");
            this.radioButtonPng.Name = "radioButtonPng";
            this.radioButtonPng.TabStop = true;
            this.radioButtonPng.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // textBoxRingBuffer
            // 
            resources.ApplyResources(this.textBoxRingBuffer, "textBoxRingBuffer");
            this.textBoxRingBuffer.Name = "textBoxRingBuffer";
            this.textBoxRingBuffer.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxRingBuffer_Validating);
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // checkBoxAnimationGif
            // 
            resources.ApplyResources(this.checkBoxAnimationGif, "checkBoxAnimationGif");
            this.checkBoxAnimationGif.Name = "checkBoxAnimationGif";
            this.checkBoxAnimationGif.UseVisualStyleBackColor = true;
            // 
            // OptionDialog
            // 
            this.AcceptButton = this.buttonOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.Controls.Add(this.checkBoxAnimationGif);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.textBoxRingBuffer);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.radioButtonPng);
            this.Controls.Add(this.radioButtonJpg);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.comboBoxWindowTitle);
            this.Controls.Add(this.checkBoxTopMost);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxFolder);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxInterval);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionDialog";
            this.ShowIcon = false;
            this.Load += new System.EventHandler(this.OptionDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxFolder;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxInterval;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.CheckBox checkBoxTopMost;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.ComboBox comboBoxWindowTitle;
        private System.Windows.Forms.RadioButton radioButtonPng;
        private System.Windows.Forms.RadioButton radioButtonJpg;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxRingBuffer;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox checkBoxAnimationGif;
    }
}