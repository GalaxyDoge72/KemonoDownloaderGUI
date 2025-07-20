using System.Runtime.CompilerServices;

namespace KemonoDownloaderGUI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            providerText = new Label();
            providerListBox = new ListBox();
            logTextBox = new RichTextBox();
            usrIDLabel = new Label();
            idBox = new TextBox();
            postLimitText = new Label();
            postLimitBox = new TextBox();
            downloadButton = new Button();
            directoryLabel = new Label();
            directoryBox = new TextBox();
            fileDialogButton = new Button();
            checkedListBox1 = new CheckedListBox();
            downloadProgressBar = new ProgressBar();
            downloadProgressText = new Label();
            downloadedText = new Label();
            skipPostButton = new Button();
            speedText = new Label();
            filepathLimitText = new Label();
            filepathLimitBox = new TextBox();
            SuspendLayout();
            // 
            // providerText
            // 
            providerText.AutoSize = true;
            providerText.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            providerText.Location = new Point(12, 9);
            providerText.Name = "providerText";
            providerText.Size = new Size(72, 21);
            providerText.TabIndex = 0;
            providerText.Text = "Provider:";
            // 
            // providerListBox
            // 
            providerListBox.FormattingEnabled = true;
            providerListBox.Location = new Point(90, 12);
            providerListBox.Name = "providerListBox";
            providerListBox.Size = new Size(120, 94);
            providerListBox.TabIndex = 1;
            // 
            // logTextBox
            // 
            logTextBox.Location = new Point(12, 342);
            logTextBox.Name = "logTextBox";
            logTextBox.Size = new Size(776, 96);
            logTextBox.TabIndex = 2;
            logTextBox.Text = "";
            // 
            // usrIDLabel
            // 
            usrIDLabel.AutoSize = true;
            usrIDLabel.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            usrIDLabel.Location = new Point(216, 9);
            usrIDLabel.Name = "usrIDLabel";
            usrIDLabel.Size = new Size(28, 21);
            usrIDLabel.TabIndex = 3;
            usrIDLabel.Text = "ID:";
            // 
            // idBox
            // 
            idBox.Location = new Point(250, 9);
            idBox.Name = "idBox";
            idBox.Size = new Size(156, 23);
            idBox.TabIndex = 4;
            // 
            // postLimitText
            // 
            postLimitText.AutoSize = true;
            postLimitText.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            postLimitText.Location = new Point(412, 9);
            postLimitText.Name = "postLimitText";
            postLimitText.Size = new Size(81, 21);
            postLimitText.TabIndex = 5;
            postLimitText.Text = "Post Limit:";
            // 
            // postLimitBox
            // 
            postLimitBox.Location = new Point(499, 9);
            postLimitBox.Name = "postLimitBox";
            postLimitBox.Size = new Size(100, 23);
            postLimitBox.TabIndex = 6;
            // 
            // downloadButton
            // 
            downloadButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            downloadButton.Location = new Point(665, 12);
            downloadButton.Name = "downloadButton";
            downloadButton.Size = new Size(123, 44);
            downloadButton.TabIndex = 7;
            downloadButton.Text = "Download...";
            downloadButton.UseVisualStyleBackColor = true;
            downloadButton.Click += downloadButton_Click;
            // 
            // directoryLabel
            // 
            directoryLabel.AutoSize = true;
            directoryLabel.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            directoryLabel.Location = new Point(216, 35);
            directoryLabel.Name = "directoryLabel";
            directoryLabel.Size = new Size(77, 21);
            directoryLabel.TabIndex = 8;
            directoryLabel.Text = "Directory:";
            // 
            // directoryBox
            // 
            directoryBox.Location = new Point(299, 35);
            directoryBox.Name = "directoryBox";
            directoryBox.Size = new Size(100, 23);
            directoryBox.TabIndex = 9;
            // 
            // fileDialogButton
            // 
            fileDialogButton.Location = new Point(405, 33);
            fileDialogButton.Name = "fileDialogButton";
            fileDialogButton.Size = new Size(75, 23);
            fileDialogButton.TabIndex = 10;
            fileDialogButton.Text = "Browse...";
            fileDialogButton.UseVisualStyleBackColor = true;
            fileDialogButton.Click += fileDialogButton_Click;
            // 
            // checkedListBox1
            // 
            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Location = new Point(90, 112);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new Size(120, 94);
            checkedListBox1.TabIndex = 11;
            // 
            // downloadProgressBar
            // 
            downloadProgressBar.Location = new Point(688, 313);
            downloadProgressBar.Name = "downloadProgressBar";
            downloadProgressBar.Size = new Size(100, 23);
            downloadProgressBar.TabIndex = 12;
            // 
            // downloadProgressText
            // 
            downloadProgressText.AutoSize = true;
            downloadProgressText.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            downloadProgressText.Location = new Point(608, 313);
            downloadProgressText.Name = "downloadProgressText";
            downloadProgressText.Size = new Size(74, 21);
            downloadProgressText.TabIndex = 13;
            downloadProgressText.Text = "Progress:";
            // 
            // downloadedText
            // 
            downloadedText.AutoSize = true;
            downloadedText.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            downloadedText.Location = new Point(12, 313);
            downloadedText.Name = "downloadedText";
            downloadedText.Size = new Size(98, 21);
            downloadedText.TabIndex = 14;
            downloadedText.Text = "Downloaded";
            // 
            // skipPostButton
            // 
            skipPostButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            skipPostButton.Location = new Point(665, 62);
            skipPostButton.Name = "skipPostButton";
            skipPostButton.Size = new Size(123, 44);
            skipPostButton.TabIndex = 15;
            skipPostButton.Text = "Skip Post";
            skipPostButton.UseVisualStyleBackColor = true;
            skipPostButton.Click += skipPostButton_Click;
            // 
            // speedText
            // 
            speedText.AutoSize = true;
            speedText.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            speedText.Location = new Point(12, 292);
            speedText.Name = "speedText";
            speedText.Size = new Size(53, 21);
            speedText.TabIndex = 16;
            speedText.Text = "Speed";
            // 
            // filepathLimitText
            // 
            filepathLimitText.AutoSize = true;
            filepathLimitText.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            filepathLimitText.Location = new Point(221, 62);
            filepathLimitText.Name = "filepathLimitText";
            filepathLimitText.Size = new Size(178, 21);
            filepathLimitText.TabIndex = 18;
            filepathLimitText.Text = "Filepath Character Limit:";
            // 
            // filepathLimitBox
            // 
            filepathLimitBox.Location = new Point(405, 64);
            filepathLimitBox.Name = "filepathLimitBox";
            filepathLimitBox.Size = new Size(100, 23);
            filepathLimitBox.TabIndex = 19;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(filepathLimitBox);
            Controls.Add(filepathLimitText);
            Controls.Add(speedText);
            Controls.Add(skipPostButton);
            Controls.Add(downloadedText);
            Controls.Add(downloadProgressText);
            Controls.Add(downloadProgressBar);
            Controls.Add(checkedListBox1);
            Controls.Add(fileDialogButton);
            Controls.Add(directoryBox);
            Controls.Add(directoryLabel);
            Controls.Add(downloadButton);
            Controls.Add(postLimitBox);
            Controls.Add(postLimitText);
            Controls.Add(idBox);
            Controls.Add(usrIDLabel);
            Controls.Add(logTextBox);
            Controls.Add(providerListBox);
            Controls.Add(providerText);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "Form1";
            Text = "KemonoDownloader";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label providerText;
        private ListBox providerListBox;
        private RichTextBox logTextBox;
        private Label usrIDLabel;
        private TextBox idBox;
        private Label postLimitText;
        private TextBox postLimitBox;
        private Button downloadButton;
        private Label directoryLabel;
        private TextBox directoryBox;
        private Button fileDialogButton;
        private CheckedListBox checkedListBox1;
        private ProgressBar downloadProgressBar;
        private Label downloadProgressText;
        private Label downloadedText;
        private Button skipPostButton;
        private Label speedText;
        private Label filepathLimitText;
        private TextBox filepathLimitBox;
    }
}
