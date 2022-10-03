using AltoHttp;
using System;
using System.IO;
using System.Windows.Forms;

namespace AQMS
{
    public partial class DownloadPopup : Form
    {
        string _downloadFile, _version;
        public DownloadPopup(string downloadLink, string ver, string bugFixes)
        {
            InitializeComponent();
            _version = ver;
            _downloadFile = downloadLink;
            if (Properties.Settings.Default.imgaePath != "")
            {
                pictureBox2.ImageLocation = Application.StartupPath + Properties.Settings.Default.imgaePath;
            }
            richTextBox1.Text = bugFixes;
        }
        HttpDownloader httpDownloader;
        private void DownloadPopup_Load(object sender, EventArgs e)
        {
            labelVersion.Text = $"Update available v{_version}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if(MessageBox.Show("File akan didownload apakah anda yakin akan mendownload", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                {

                }
                if (txtPath.Text.Trim() != "")
                {
                    if (File.Exists(txtPath.Text + @"\" + Path.GetFileName(_downloadFile)))
                    {
                        File.Delete(txtPath.Text + @"\" + Path.GetFileName(_downloadFile));
                    }
                    httpDownloader = new HttpDownloader(_downloadFile, $"{txtPath.Text}\\{Path.GetFileName(_downloadFile)}");
                    httpDownloader.DownloadCompleted += HttpDownloader_DownloadCompleted;
                    httpDownloader.ProgressChanged += HttpDownloader_ProgressChanged;
                    httpDownloader.Start();
                }
                else
                {
                    MessageBox.Show("Folder location cannot be empty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HttpDownloader_ProgressChanged(object sender, AltoHttp.ProgressChangedEventArgs e)
        {
            progressBar1.Value = (int)e.Progress;
            labelStatus.Text = "Status : Downloading";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                txtPath.Text = fbd.SelectedPath;
            }
        }

        private void HttpDownloader_DownloadCompleted(object sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
           {
               labelStatus.Text = "Status : Completed";
           });
        }
    }
}
