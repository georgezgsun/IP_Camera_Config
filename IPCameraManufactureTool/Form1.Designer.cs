namespace IPCameraManufactureTool
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.textBoxSerialNumber = new System.Windows.Forms.TextBox();
            this.labelOutput = new System.Windows.Forms.Label();
            this.buttonConfig = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.streamPlayerControl1 = new WebEye.Controls.WinForms.StreamPlayerControl.StreamPlayerControl();
            this.backgroundConfig = new System.ComponentModel.BackgroundWorker();
            this.SearchingCamera = new System.ComponentModel.BackgroundWorker();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBoxCameraModel = new System.Windows.Forms.PictureBox();
            this.labelCameraDescription = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxCameraModel)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "200-1301-00",
            "200-1301-10"});
            this.comboBox1.Location = new System.Drawing.Point(863, 11);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(2);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(113, 21);
            this.comboBox1.TabIndex = 0;
            this.comboBox1.Text = "200-1301-00";
            this.comboBox1.TextChanged += new System.EventHandler(this.ModelNumberChanged);
            // 
            // textBoxSerialNumber
            // 
            this.textBoxSerialNumber.Location = new System.Drawing.Point(863, 221);
            this.textBoxSerialNumber.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxSerialNumber.Name = "textBoxSerialNumber";
            this.textBoxSerialNumber.Size = new System.Drawing.Size(113, 20);
            this.textBoxSerialNumber.TabIndex = 1;
            this.textBoxSerialNumber.Text = "GTA00000";
            this.textBoxSerialNumber.TextChanged += new System.EventHandler(this.SerialNumberInput);
            // 
            // labelOutput
            // 
            this.labelOutput.AutoSize = true;
            this.labelOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelOutput.Location = new System.Drawing.Point(860, 310);
            this.labelOutput.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelOutput.MaximumSize = new System.Drawing.Size(140, 290);
            this.labelOutput.Name = "labelOutput";
            this.labelOutput.Size = new System.Drawing.Size(46, 17);
            this.labelOutput.TabIndex = 2;
            this.labelOutput.Text = "label1";
            this.labelOutput.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // buttonConfig
            // 
            this.buttonConfig.AllowDrop = true;
            this.buttonConfig.AutoEllipsis = true;
            this.buttonConfig.Location = new System.Drawing.Point(909, 506);
            this.buttonConfig.Margin = new System.Windows.Forms.Padding(2);
            this.buttonConfig.Name = "buttonConfig";
            this.buttonConfig.Size = new System.Drawing.Size(67, 33);
            this.buttonConfig.TabIndex = 3;
            this.buttonConfig.Text = "Config";
            this.buttonConfig.UseVisualStyleBackColor = true;
            this.buttonConfig.Click += new System.EventHandler(this.ButtonConfigClick);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(15, 518);
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
            this.streamPlayerControl1.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.streamPlayerControl1.Location = new System.Drawing.Point(15, 14);
            this.streamPlayerControl1.Margin = new System.Windows.Forms.Padding(5);
            this.streamPlayerControl1.Name = "streamPlayerControl1";
            this.streamPlayerControl1.Size = new System.Drawing.Size(820, 486);
            this.streamPlayerControl1.TabIndex = 8;
            this.streamPlayerControl1.StreamStarted += new System.EventHandler(this.StreamStarted);
            this.streamPlayerControl1.StreamStopped += new System.EventHandler(this.StreamStopped);
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
            this.SearchingCamera.WorkerReportsProgress = true;
            this.SearchingCamera.WorkerSupportsCancellation = true;
            this.SearchingCamera.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundSearchingCamera);
            this.SearchingCamera.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.SearchCameraProgressChanged);
            this.SearchingCamera.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.SearchingCameraCompleted);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(863, 246);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(113, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 9;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBoxCameraModel
            // 
            this.pictureBoxCameraModel.Location = new System.Drawing.Point(863, 95);
            this.pictureBoxCameraModel.Name = "pictureBoxCameraModel";
            this.pictureBoxCameraModel.Size = new System.Drawing.Size(113, 113);
            this.pictureBoxCameraModel.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxCameraModel.TabIndex = 10;
            this.pictureBoxCameraModel.TabStop = false;
            this.pictureBoxCameraModel.Click += new System.EventHandler(this.clickCameraPicture);
            // 
            // labelCameraDescription
            // 
            this.labelCameraDescription.AutoSize = true;
            this.labelCameraDescription.Location = new System.Drawing.Point(860, 45);
            this.labelCameraDescription.MaximumSize = new System.Drawing.Size(140, 0);
            this.labelCameraDescription.Name = "labelCameraDescription";
            this.labelCameraDescription.Size = new System.Drawing.Size(102, 13);
            this.labelCameraDescription.TabIndex = 11;
            this.labelCameraDescription.Text = "Camera descriptions";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 561);
            this.Controls.Add(this.labelCameraDescription);
            this.Controls.Add(this.pictureBoxCameraModel);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.streamPlayerControl1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.buttonConfig);
            this.Controls.Add(this.labelOutput);
            this.Controls.Add(this.textBoxSerialNumber);
            this.Controls.Add(this.comboBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "IP Camera Configuration Tool";
            this.Load += new System.EventHandler(this.loadConfigForm);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxCameraModel)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TextBox textBoxSerialNumber;
        private System.Windows.Forms.Label labelOutput;
        private System.Windows.Forms.Button buttonConfig;
        private System.Windows.Forms.ProgressBar progressBar1;
        private WebEye.Controls.WinForms.StreamPlayerControl.StreamPlayerControl streamPlayerControl1;
        private System.ComponentModel.BackgroundWorker backgroundConfig;
        private System.ComponentModel.BackgroundWorker SearchingCamera;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBoxCameraModel;
        private System.Windows.Forms.Label labelCameraDescription;
    }
}

