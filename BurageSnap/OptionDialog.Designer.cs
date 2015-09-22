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
            this.components = new System.ComponentModel.Container();
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
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.label5 = new System.Windows.Forms.Label();
            this.radioButtonJpg = new System.Windows.Forms.RadioButton();
            this.radioButtonPng = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.errorProvider.SetError(this.label3, resources.GetString("label3.Error"));
            this.errorProvider.SetIconAlignment(this.label3, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("label3.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.label3, ((int)(resources.GetObject("label3.IconPadding"))));
            this.label3.Name = "label3";
            // 
            // textBoxFolder
            // 
            resources.ApplyResources(this.textBoxFolder, "textBoxFolder");
            this.errorProvider.SetError(this.textBoxFolder, resources.GetString("textBoxFolder.Error"));
            this.errorProvider.SetIconAlignment(this.textBoxFolder, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("textBoxFolder.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.textBoxFolder, ((int)(resources.GetObject("textBoxFolder.IconPadding"))));
            this.textBoxFolder.Name = "textBoxFolder";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.errorProvider.SetError(this.label4, resources.GetString("label4.Error"));
            this.errorProvider.SetIconAlignment(this.label4, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("label4.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.label4, ((int)(resources.GetObject("label4.IconPadding"))));
            this.label4.Name = "label4";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.errorProvider.SetError(this.label2, resources.GetString("label2.Error"));
            this.errorProvider.SetIconAlignment(this.label2, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("label2.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.label2, ((int)(resources.GetObject("label2.IconPadding"))));
            this.label2.Name = "label2";
            // 
            // textBoxInterval
            // 
            resources.ApplyResources(this.textBoxInterval, "textBoxInterval");
            this.errorProvider.SetError(this.textBoxInterval, resources.GetString("textBoxInterval.Error"));
            this.errorProvider.SetIconAlignment(this.textBoxInterval, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("textBoxInterval.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.textBoxInterval, ((int)(resources.GetObject("textBoxInterval.IconPadding"))));
            this.textBoxInterval.Name = "textBoxInterval";
            this.textBoxInterval.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxInterval_Validating);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.errorProvider.SetError(this.label1, resources.GetString("label1.Error"));
            this.errorProvider.SetIconAlignment(this.label1, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("label1.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.label1, ((int)(resources.GetObject("label1.IconPadding"))));
            this.label1.Name = "label1";
            // 
            // buttonBrowse
            // 
            resources.ApplyResources(this.buttonBrowse, "buttonBrowse");
            this.errorProvider.SetError(this.buttonBrowse, resources.GetString("buttonBrowse.Error"));
            this.errorProvider.SetIconAlignment(this.buttonBrowse, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("buttonBrowse.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.buttonBrowse, ((int)(resources.GetObject("buttonBrowse.IconPadding"))));
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // buttonOk
            // 
            resources.ApplyResources(this.buttonOk, "buttonOk");
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.errorProvider.SetError(this.buttonOk, resources.GetString("buttonOk.Error"));
            this.errorProvider.SetIconAlignment(this.buttonOk, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("buttonOk.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.buttonOk, ((int)(resources.GetObject("buttonOk.IconPadding"))));
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.errorProvider.SetError(this.buttonCancel, resources.GetString("buttonCancel.Error"));
            this.errorProvider.SetIconAlignment(this.buttonCancel, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("buttonCancel.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.buttonCancel, ((int)(resources.GetObject("buttonCancel.IconPadding"))));
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // checkBoxTopMost
            // 
            resources.ApplyResources(this.checkBoxTopMost, "checkBoxTopMost");
            this.errorProvider.SetError(this.checkBoxTopMost, resources.GetString("checkBoxTopMost.Error"));
            this.errorProvider.SetIconAlignment(this.checkBoxTopMost, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("checkBoxTopMost.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.checkBoxTopMost, ((int)(resources.GetObject("checkBoxTopMost.IconPadding"))));
            this.checkBoxTopMost.Name = "checkBoxTopMost";
            this.checkBoxTopMost.UseVisualStyleBackColor = true;
            // 
            // folderBrowserDialog
            // 
            resources.ApplyResources(this.folderBrowserDialog, "folderBrowserDialog");
            // 
            // comboBoxWindowTitle
            // 
            resources.ApplyResources(this.comboBoxWindowTitle, "comboBoxWindowTitle");
            this.errorProvider.SetError(this.comboBoxWindowTitle, resources.GetString("comboBoxWindowTitle.Error"));
            this.comboBoxWindowTitle.FormattingEnabled = true;
            this.errorProvider.SetIconAlignment(this.comboBoxWindowTitle, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("comboBoxWindowTitle.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.comboBoxWindowTitle, ((int)(resources.GetObject("comboBoxWindowTitle.IconPadding"))));
            this.comboBoxWindowTitle.Name = "comboBoxWindowTitle";
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            resources.ApplyResources(this.errorProvider, "errorProvider");
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.errorProvider.SetError(this.label5, resources.GetString("label5.Error"));
            this.errorProvider.SetIconAlignment(this.label5, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("label5.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.label5, ((int)(resources.GetObject("label5.IconPadding"))));
            this.label5.Name = "label5";
            // 
            // radioButtonJpg
            // 
            resources.ApplyResources(this.radioButtonJpg, "radioButtonJpg");
            this.errorProvider.SetError(this.radioButtonJpg, resources.GetString("radioButtonJpg.Error"));
            this.errorProvider.SetIconAlignment(this.radioButtonJpg, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("radioButtonJpg.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.radioButtonJpg, ((int)(resources.GetObject("radioButtonJpg.IconPadding"))));
            this.radioButtonJpg.Name = "radioButtonJpg";
            this.radioButtonJpg.TabStop = true;
            this.radioButtonJpg.UseVisualStyleBackColor = true;
            // 
            // radioButtonPng
            // 
            resources.ApplyResources(this.radioButtonPng, "radioButtonPng");
            this.errorProvider.SetError(this.radioButtonPng, resources.GetString("radioButtonPng.Error"));
            this.errorProvider.SetIconAlignment(this.radioButtonPng, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("radioButtonPng.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.radioButtonPng, ((int)(resources.GetObject("radioButtonPng.IconPadding"))));
            this.radioButtonPng.Name = "radioButtonPng";
            this.radioButtonPng.TabStop = true;
            this.radioButtonPng.UseVisualStyleBackColor = true;
            // 
            // OptionDialog
            // 
            this.AcceptButton = this.buttonOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
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
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
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
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.RadioButton radioButtonPng;
        private System.Windows.Forms.RadioButton radioButtonJpg;
        private System.Windows.Forms.Label label5;
    }
}