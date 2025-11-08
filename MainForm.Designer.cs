using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.IO;

namespace WindowsEventLogReporter
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private Label titleLabel;
        private PictureBox headerIcon;
        private ComboBox comboLogSource;
        private ComboBox comboLevel;
        private ComboBox comboRange;
        private Button btnPrint;
        private Label footerLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // --- Form Settings ---
            this.Text = "Windows Event Log Reporter";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 640;
            this.Height = 380;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            // Load embedded icon for title bar
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowsEventLogReporter.log.ico"))
            {
                if (stream != null)
                {
                    this.Icon = new Icon(stream);
                }
            }

            // --- Header Icon (top-left) ---
            headerIcon = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                Width = 48,
                Height = 48,
                Top = 15,
                Left = 30
            };
            Controls.Add(headerIcon);

            // Load the same icon image from embedded resource into PictureBox
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowsEventLogReporter.log.ico"))
            {
                if (stream != null)
                {
                    headerIcon.Image = new Icon(stream).ToBitmap();
                }
            }

            // --- Header Text ---
            titleLabel = new Label
            {
                Text = "Select Windows Logs to Export",
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Top = 25
            };
            Controls.Add(titleLabel);
            titleLabel.Left = headerIcon.Right + 15;

            // --- Log Source ---
            var lblSource = new Label
            {
                Text = "Log Source:",
                AutoSize = true,
                Top = 85,
                Left = 100,
                ForeColor = Color.White
            };
            Controls.Add(lblSource);

            comboLogSource = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = 220,
                Top = 80,
                Width = 220
            };
            comboLogSource.Items.AddRange(new object[] { "Application", "System" });
            comboLogSource.SelectedIndex = 0;
            Controls.Add(comboLogSource);

            // --- Level ---
            var lblLevel = new Label
            {
                Text = "Level:",
                AutoSize = true,
                Top = 130,
                Left = 100,
                ForeColor = Color.White
            };
            Controls.Add(lblLevel);

            comboLevel = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = 220,
                Top = 125,
                Width = 220
            };
            comboLevel.Items.AddRange(new object[] { "Critical", "Error", "Warning" });
            comboLevel.SelectedIndex = 1;
            Controls.Add(comboLevel);

            // --- Time Range ---
            var lblRange = new Label
            {
                Text = "Time Range:",
                AutoSize = true,
                Top = 175,
                Left = 100,
                ForeColor = Color.White
            };
            Controls.Add(lblRange);

            comboRange = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = 220,
                Top = 170,
                Width = 220
            };
            comboRange.Items.AddRange(new object[] { "Anytime", "Last hour", "Last 24 hours", "Last 7 days" });
            comboRange.SelectedIndex = 2;
            Controls.Add(comboRange);

            // --- Print Button ---
            btnPrint = new Button
            {
                Text = "Print",
                Width = 140,
                Height = 40,
                BackColor = Color.DimGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnPrint.FlatAppearance.BorderColor = Color.Gray;
            btnPrint.Left = (this.ClientSize.Width - btnPrint.Width) / 2;
            btnPrint.Top = 235;
            btnPrint.Anchor = AnchorStyles.None;
            btnPrint.Click += BtnPrint_Click;
            Controls.Add(btnPrint);

            // --- Footer Label ---
            footerLabel = new Label
            {
                Text = "Application Design by Noe Tovar-MBA 2025  |  Visit me at noetovar.com",
                AutoSize = false,
                Width = this.ClientSize.Width,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.White,
                Top = 310,
                Left = 0
            };
            Controls.Add(footerLabel);

            // --- Responsive centering ---
            this.Resize += (s, e) =>
            {
                btnPrint.Left = (this.ClientSize.Width - btnPrint.Width) / 2;
                footerLabel.Width = this.ClientSize.Width;
            };
        }
    }
}
