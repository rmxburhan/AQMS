using System;
using System.Windows.Forms;

namespace AQMS
{
    public partial class FrmQrLogin : Form
    {
        public FrmQrLogin()
        {
            InitializeComponent();
            if (Properties.Settings.Default.imgaePath != "")
            {
                logoApp.ImageLocation = Application.StartupPath + Properties.Settings.Default.imgaePath;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
