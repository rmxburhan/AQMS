﻿using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

namespace AQMS
{
    public partial class chartGases : UserControl
    {
        Form1 _form1;
        public chartGases(Form1 form)
        {
            InitializeComponent();
            _form1 = form;
        }

        public void addPoint(double[] y, string x)
        {
            for (int i = 0; i < y.Length; i++)
            {
                chart2.Series[i].Points.AddXY(x, y[i]);
            }
            refreshChart("");
        }
     
        private void button6_Click(object sender, EventArgs e)
        {
            refreshChart("halo");
            chart2.Series[0].Enabled = (chart2.Series[0].Enabled == true ? false : true);
            button6.BackColor = (chart2.Series[0].Enabled == true ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button6.FlatAppearance.MouseDownBackColor = (button6.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button6.FlatAppearance.MouseOverBackColor = (button6.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button6.ForeColor = (chart2.Series[0].Enabled == true ? Color.White : Color.Black);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            chart2.Series[1].Enabled = (chart2.Series[1].Enabled == true ? false : true);
            button5.BackColor = (chart2.Series[1].Enabled == true ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button5.FlatAppearance.MouseDownBackColor = (button5.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button5.FlatAppearance.MouseOverBackColor = (button5.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button5.ForeColor = (chart2.Series[1].Enabled == true ? Color.White : Color.Black);
            refreshChart("halo");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            chart2.Series[2].Enabled = (chart2.Series[2].Enabled == true ? false : true);
            button4.BackColor = (chart2.Series[2].Enabled == true ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button4.FlatAppearance.MouseDownBackColor = (button4.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button4.FlatAppearance.MouseOverBackColor = (button4.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button4.ForeColor = (chart2.Series[2].Enabled == true ? Color.White : Color.Black);
            refreshChart("halo");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            chart2.Series[3].Enabled = (chart2.Series[3].Enabled == true ? false : true);
            button3.BackColor = (chart2.Series[3].Enabled == true ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button3.FlatAppearance.MouseDownBackColor = (button3.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button3.FlatAppearance.MouseOverBackColor = (button3.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button3.ForeColor = (chart2.Series[3].Enabled == true ? Color.White : Color.Black);
            refreshChart("halo");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            chart2.Series[4].Enabled = (chart2.Series[4].Enabled == true ? false : true);
            button2.BackColor = (chart2.Series[4].Enabled == true ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button2.FlatAppearance.MouseDownBackColor = (button2.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button2.FlatAppearance.MouseOverBackColor = (button2.BackColor == Color.FromArgb(44, 69, 157) ? Color.FromArgb(44, 69, 157) : Color.LightGray);
            button2.ForeColor = (chart2.Series[4].Enabled == true ? Color.White : Color.Black);
            refreshChart("halo");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            chart2.Series[0].Enabled = true;
            chart2.Series[1].Enabled = true;
            chart2.Series[2].Enabled = true;
            chart2.Series[3].Enabled = true;
            chart2.Series[4].Enabled = true;
            button1.BackColor = Color.FromArgb(44, 69, 157);
            button1.ForeColor = Color.White;
            button2.BackColor = Color.FromArgb(44, 69, 157);
            button2.ForeColor = Color.White;
            button3.BackColor = Color.FromArgb(44, 69, 157);
            button3.ForeColor = Color.White;
            button4.BackColor = Color.FromArgb(44, 69, 157);
            button4.ForeColor = Color.White;
            button5.BackColor = Color.FromArgb(44, 69, 157);
            button5.ForeColor = Color.White;
            button6.BackColor = Color.FromArgb(44, 69, 157);
            button6.ForeColor = Color.White;
            button1.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 69, 157);
            button1.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 69, 157);
            button2.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 69, 157);
            button2.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 69, 157);
            button3.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 69, 157);
            button3.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 69, 157);
            button4.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 69, 157);
            button4.FlatAppearance.MouseOverBackColor= Color.FromArgb(44, 69, 157);
            button5.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 69, 157);
            button5.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 69, 157);
            button6.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 69, 157);
            button6.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 69, 157);
        }

        private void refreshChart(string halo)
        {
            if (chart2.Series[0].Points.Count > 0)
            {
                double[] value = new double[5];
                value[0] = (chart2.Series[0].Enabled == true ? Convert.ToDouble(chart2.Series[0].Points.FindMaxByValue().YValues[0]) : 0);
                value[1] = (chart2.Series[1].Enabled == true ? Convert.ToDouble(chart2.Series[1].Points.FindMaxByValue().YValues[0]) : 0);
                value[2] = (chart2.Series[2].Enabled == true ? Convert.ToDouble(chart2.Series[2].Points.FindMaxByValue().YValues[0]) : 0);
                value[3] = (chart2.Series[3].Enabled == true ? Convert.ToDouble(chart2.Series[3].Points.FindMaxByValue().YValues[0]) : 0);
                value[4] = (chart2.Series[4].Enabled == true ? Convert.ToDouble(chart2.Series[4].Points.FindMaxByValue().YValues[0]) : 0);

                double maxValue = 0;
                for (int i = 0; i < value.Length - 1; i++)
                {
                    if (maxValue < value[i])
                    {
                        maxValue = value[i];
                    }
                }
                Console.WriteLine("Max " + maxValue);
                chart2.ChartAreas[0].AxisY.Maximum = maxValue;
            }
         
            if (halo == "true")
            {
                if (!_form1.serialPortUtama.IsOpen)
                {
                    chart2.Series[0].Points.AddXY("0", 0);
                    chart2.Series[1].Points.AddXY("0", 0);
                    chart2.Series[2].Points.AddXY("0", 0);
                    chart2.Series[3].Points.AddXY("0", 0);
                    chart2.Series[4].Points.AddXY("0", 0);
                    chart2.Series[0].Points.RemoveAt(chart2.Series[0].Points.Count - 1);
                    chart2.Series[1].Points.RemoveAt(chart2.Series[1].Points.Count - 1);
                    chart2.Series[2].Points.RemoveAt(chart2.Series[2].Points.Count - 1);
                    chart2.Series[3].Points.RemoveAt(chart2.Series[3].Points.Count - 1);
                    chart2.Series[4].Points.RemoveAt(chart2.Series[4].Points.Count - 1);
                }
            }
            
        }
    }
}
