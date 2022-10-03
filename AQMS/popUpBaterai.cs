using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AQMS
{
    public partial class popUpBaterai : Form
    {
        int timer = 30;
        public popUpBaterai(string tipe)
        {
            InitializeComponent();
            if (tipe == "fullcharge")
            {
                label1.Text = "Baterai sudah selesai dicharge";
                label2.Text = "Sekarang anda dapat melakukan collect data";
                button1.FlatAppearance.MouseDownBackColor = Color.Green;
                button1.FlatAppearance.MouseOverBackColor = Color.Green;
                this.BackColor = Color.Green;
                button1.BackColor = Color.Green;
            }
            else if(tipe == "lowbat")
            {
                label1.Text = "Baterai sudah lemah aplikasi akan masuk kedalam mode charge";
                label2.Text = "Aplikasi tidak dapat melakukan collect data saat dalam mode charge";
                button1.FlatAppearance.MouseDownBackColor = Color.Red;
                button1.FlatAppearance.MouseOverBackColor = Color.Red;
                this.BackColor = Color.Red;
            }
        }

        private void popUpBaterai_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }
        
        int count = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            count++;
            label3.Text = $"Autoclose : {timer - count} Detik";
            if (count == timer)
            {
                timer1.Stop();
                this.Close();
                return;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
