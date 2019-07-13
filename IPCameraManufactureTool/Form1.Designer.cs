﻿namespace IPCameraManufactureTool
{
    partial class Form1
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
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.labelOutput = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.streamPlayerControl1 = new WebEye.Controls.WinForms.StreamPlayerControl.StreamPlayerControl();
            this.backgroundConfig = new System.ComponentModel.BackgroundWorker();
            this.SearchingCamera = new System.ComponentModel.BackgroundWorker();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "200-1301-00",
            "200-1301-10"});
            this.comboBox1.Location = new System.Drawing.Point(27, 11);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(2);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(113, 21);
            this.comboBox1.TabIndex = 0;
            this.comboBox1.Text = "200-1301-00";
            this.comboBox1.TextChanged += new System.EventHandler(this.ModelNumberChanged);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(27, 249);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(113, 20);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "GTA00000";
            this.textBox1.TextChanged += new System.EventHandler(this.SerialNumberInput);
            // 
            // labelOutput
            // 
            this.labelOutput.AutoSize = true;
            this.labelOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelOutput.Location = new System.Drawing.Point(165, 500);
            this.labelOutput.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelOutput.MaximumSize = new System.Drawing.Size(800, 100);
            this.labelOutput.Name = "labelOutput";
            this.labelOutput.Size = new System.Drawing.Size(46, 17);
            this.labelOutput.TabIndex = 2;
            this.labelOutput.Text = "label1";
            this.labelOutput.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // button1
            // 
            this.button1.AllowDrop = true;
            this.button1.AutoEllipsis = true;
            this.button1.Location = new System.Drawing.Point(27, 506);
            this.button1.Margin = new System.Windows.Forms.Padding(2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(67, 33);
            this.button1.TabIndex = 3;
            this.button1.Text = "Config";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.ButtonConfigClick);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(168, 11);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(2);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(820, 21);
            this.progressBar1.TabIndex = 4;
            // 
            // streamPlayerControl1
            // 
            this.streamPlayerControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.streamPlayerControl1.AutoSize = true;
            this.streamPlayerControl1.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.streamPlayerControl1.Location = new System.Drawing.Point(168, 39);
            this.streamPlayerControl1.Margin = new System.Windows.Forms.Padding(5);
            this.streamPlayerControl1.Name = "streamPlayerControl1";
            this.streamPlayerControl1.Size = new System.Drawing.Size(820, 461);
            this.streamPlayerControl1.TabIndex = 8;
            this.streamPlayerControl1.StreamStarted += new System.EventHandler(this.StreamStarted);
            this.streamPlayerControl1.StreamFailed += new System.EventHandler<WebEye.Controls.WinForms.StreamPlayerControl.StreamFailedEventArgs>(this.StreamFailed);
            // 
            // backgroundConfig
            // 
            this.backgroundConfig.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundConfig);
            this.backgroundConfig.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.ConfigProgressChanged);
            this.backgroundConfig.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.ConfigCompleted);
            // 
            // SearchingCamera
            // 
            this.SearchingCamera.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundSearchingCamera);
            this.SearchingCamera.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.SearchCameraProgressChanged);
            this.SearchingCamera.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.SearchingCameraCompleted);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 561);
            this.Controls.Add(this.streamPlayerControl1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.labelOutput);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.comboBox1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "IP Camera Configuration Tool";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label labelOutput;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private WebEye.Controls.WinForms.StreamPlayerControl.StreamPlayerControl streamPlayerControl1;
        private System.ComponentModel.BackgroundWorker backgroundConfig;
        private System.ComponentModel.BackgroundWorker SearchingCamera;
    }
}
