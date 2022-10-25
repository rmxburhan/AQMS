using GMap.NET.WindowsForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AQMS
{
    public partial class Form1 : Form
    {
        SQLiteConnection connection = new SQLiteConnection(@"Data Source=D:\Trusur\AQMS\aqms.db;Version=3;FailIfMissing=True;New=false;");
        SQLiteCommand command;
        int countDataReceived = 0;
        FrmLogin _login;
        public Form1()
        {
            InitializeComponent();
            setup();

        }

        public Form1(FrmLogin login)
        {
            InitializeComponent();
            _login = login;
            setup();
        }
        chartGases _chartGasses;
        void setup()
        {
            if (LoginStatus.role == "operator")
            {
                tabControl1.TabPages.Remove(settings);
            }
            if (LoginStatus.role == "client" || LoginStatus.role == "superuser" || LoginStatus.role == "teknisi")
            {
                if (!tabControl1.TabPages.Contains(settings))
                {
                    tabControl1.TabPages[4] = settings;
                    tabControl1.TabPages[5] = tabPage1;
                }
            }
            _chartGasses = new chartGases(this);
            this._chartGasses.Dock = System.Windows.Forms.DockStyle.Fill;
            panel40.Controls.Add(_chartGasses);

            txtPwmTrack.MaxLength = 3;
            txtBatteryThreshold.MaxLength = 3;
            //chart1.ChartAreas["ChartArea1"].AxisX.Minimum = 0;
            //chart1.ChartAreas["ChartArea1"].AxisX.Maximum = ChartLimit;
            //chart1.ChartAreas["ChartArea1"].AxisX.Interval = 1;
            //chart1.ChartAreas[0].AxisX.LabelStyle.Angle = 45;

            _chartGasses.chart2.ChartAreas["ChartArea1"].AxisX.Minimum = 0;
            _chartGasses.chart2.ChartAreas["ChartArea1"].AxisX.Maximum = ChartLimit;
            _chartGasses.chart2.ChartAreas["ChartArea1"].AxisX.Interval = 1;
            _chartGasses.chart2.ChartAreas[0].AxisX.LabelStyle.Angle = 45;

            chart3.ChartAreas[0].AxisY.Maximum = 500;
            chart2.ChartAreas["ChartArea1"].AxisX.Minimum = 0;
            chart2.ChartAreas["ChartArea1"].AxisX.Maximum = ChartLimit;
            chart2.ChartAreas["ChartArea1"].AxisX.Interval = 1;
            chart2.ChartAreas[0].AxisX.LabelStyle.Angle = 45;
            chart3.ChartAreas["ChartArea1"].AxisX.Minimum = 0;
            chart3.ChartAreas["ChartArea1"].AxisX.Maximum = ChartLimit;
            chart3.ChartAreas["ChartArea1"].AxisX.Interval = 1;
            chart3.ChartAreas[0].AxisX.LabelStyle.Angle = 45;
            chart3.Series[0].Points.AddXY("0:00", 0);

            

            string[] version = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
            label39.Text = $"v{version[0]}.{version[1]}.{version[2]}";

            if (LoginStatus.role != "superuser")
            {
                tanggalAktivasi.Text = Convert.ToDateTime(LoginStatus.tanggalAktivasi).ToString("yyyy-MM-dd");
                tanggalKadaluwarsa.Text = Convert.ToDateTime(LoginStatus.tanggalKedaluwarsa).ToString("yyyy-MM-dd");
                label1.Text = LoginStatus.sisaTanggal + " Hari lagi";
            }
        }

        private async void insertLog(string aktifitas)
        {
            try
            {
                Koneksi();
                using (command = new SQLiteCommand($"INSERT INTO tbl_log(nama_user, email, aktifitas, waktu) values('{LoginStatus.namaUser}', '{LoginStatus.email}',  '{aktifitas}', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}')", connection))
                {
                    await command.ExecuteNonQueryAsync();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSavePort_Click(object sender, EventArgs e)
        {
            if (cbxDaftarPort.Text.Trim() == "") { MessageBox.Show("Nama Port tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            Properties.Settings.Default.portAlat = cbxDaftarPort.SelectedItem.ToString().Trim();
            Properties.Settings.Default.Save();
            labelPortYangDigunakan.Text = $"Port yang digunakan : {Properties.Settings.Default.portAlat}";
            insertLog("Mengubah pengaturan port");
            MessageBox.Show("Port updated", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void debugState()
        {
            cbxDaftarPort.Enabled = false;
            btnSavePort.Enabled = false;
            btnSavePort.BackColor = Color.LightGray;
        }

        private void stopDebugState()
        {
            cbxDaftarPort.Enabled = true;
            btnSavePort.Enabled = true;
            btnSavePort.BackColor = Color.FromArgb(44, 69, 157);
        }

        private void btnDebug_Click(object sender, EventArgs e)
        {
            if (cbxDaftarPort.Text.Trim() == "")
            {
                MessageBox.Show("Nama port tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                if (serialPortUtama.IsOpen)
                {
                    MessageBox.Show("Stop mesin terlebih dahulu");
                    return;
                }
                debugState();
                timerRequest.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Debug failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                timerRequest.Stop();
            }
        }

        void getPortList(string tipe)
        {
            string[] ports = SerialPort.GetPortNames();
            if (tipe == "startup")
            {
                if (ports.Length > 0)
                {
                    cbxDaftarPort.Items.Clear();
                    cbxPortGps.Items.Clear();
                    cbxDaftarPort.Items.AddRange(ports);
                    cbxPortGps.Items.AddRange(ports);
                    cbxDaftarPort.SelectedIndex = 0;
                    cbxPortGps.SelectedIndex = 0;
                }
                else
                {
                    cbxPortGps.Items.Clear();
                    cbxDaftarPort.Items.Clear();
                    cbxDaftarPort.Text = "";
                    cbxPortGps.Text = "";
                }
            }
            else if (tipe == "run_time")
            {
                if (ports.Length > 0)
                {
                    bool halo = true;
                    if (serialPortUtama.IsOpen || serialPortGps.IsOpen)
                    {
                        for (int i = 0; i < ports.Length; i++)
                        {
                            if (!(ports[i] == serialPortUtama.PortName || ports[i] == serialPortGps.PortName))
                            {
                                halo = false;
                            }
                        }
                    }
                   
                    if (halo == false)
                    {
                        timerRequest.Stop();
                        if (serialPortUtama.IsOpen)
                        {
                            serialPortUtama.Close();
                        }
                        if (serialPortGps.IsOpen)
                        {
                            serialPortGps.Close();
                        }
                        disconnectState();
                    }
                    cbxDaftarPort.Items.Clear();
                    cbxDaftarPort.Items.AddRange(ports);
                    cbxDaftarPort.SelectedIndex = 0;
                    cbxPortGps.Items.Clear();
                    cbxPortGps.Items.AddRange(ports);
                    cbxPortGps.SelectedIndex = 0;
                }
                else
                {
                    disconnectState();
                    cbxDaftarPort.Items.Clear();
                    cbxDaftarPort.Text = "";
                    cbxPortGps.Items.Clear();
                    cbxPortGps.Text = "";
                }
            }
        }

        void connectState()
        {
            progressBar1.Value = 100;
            btnDisconnect.Enabled = true;
            btnDisconnect.BackColor = Color.Red;
            btnConnect.Enabled = false;
            btnConnect.BackColor = Color.LightGray;
            cbxDaftarPort.Enabled = false;
            panel10.Enabled = false;
            panel21.Enabled = false;
            panel13.Enabled = false;
            rbDataOtomatis.Enabled = false;
            rbDataManual.Enabled = false;
            labelStatus.Text = "ON";
            panel21.Enabled = false;
            panel22.Enabled = false;
            panel51.Enabled = false;
        }

        private void disconnectState()
        {
            progressBar1.Value = 0;
            btnDisconnect.Enabled = false;
            btnDisconnect.BackColor = Color.LightGray;
            btnConnect.Enabled = true;
            btnConnect.BackColor = Color.FromArgb(0, 192, 0);
            cbxDaftarPort.Enabled = true;
            panel13.Enabled = true;
            panel10.Enabled = true;
            rbDataOtomatis.Enabled = true;
            rbDataManual.Enabled = true;
            panel21.Enabled = true;
            labelStatus.Text = "OFF";
            panel21.Enabled = true;
            panel22.Enabled = true;
            panel51.Enabled = true;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 537:
                    try
                    {
                        getPortList("run_time");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    break;
            }
            base.WndProc(ref m);
        }
        private delegate void _serialPortUtama_DataReceived(string datas);
        private void serialPortUtama_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string datas = serialPortUtama.ReadLine();
                this.BeginInvoke(new _serialPortUtama_DataReceived(serial_DataReceived), new object[] { datas });
            }
            catch (Exception)
            {
            
            }
        }

        private async void serial_DataReceived(string datas)
        {
            try
            {
                labelRawSerial.Text = datas;
                string[] dataIn = datas.Trim().Split(',');
                Console.WriteLine(datas);
                #region Data cleansing
                if (dataIn[dataIn.Length - 1].Trim() != "*")
                    return;
                //if (dataIn[0] == "" || dataIn[1] == "" || dataIn[2] == "" || dataIn[3] == "" || dataIn[4] == "" || dataIn[5] == "" || dataIn[6] == "")
                //    return;
                try
                {
                    Convert.ToDouble(dataIn[0]);
                }
                catch { return; }
                #endregion
                if (timerRequest.Enabled == true)
                {
                    timerRequest.Stop();
                }
                Console.WriteLine("TMR1 " + timerRequest.Enabled);
                string teganganIn = dataIn[6];
                double persen_baterai = (Convert.ToDouble(teganganIn) / 12) * 100;
                string persen = String.Format("{0:0}", persen_baterai);
                labelBattery.Text = (persen_baterai > 100 ? "100" : Convert.ToInt32(persen).ToString() + "%");
                if ((Convert.ToDouble(teganganIn) / 12) * 100 <= Properties.Settings.Default.batteryThreshold)
                {
                    try
                    {
                        serialPortUtama.Write("setPWM,0,*");
                        popUpBaterai popup = new popUpBaterai("lowbat");
                        popup.Show();
                        timerRequest.Stop();
                        labelTunggu.Text = "Sedang melakukan charge";
                        progressBar2.Value = 0;
                        labelTunggu.Show();
                        progressBar2.Show();
                        btnDisconnect.Enabled = false;
                        labelSensorMenyala.Hide();
                        label15.Text = "ON";
                        serialPortUtama.Write("CHARGE,1,*");
                        timerWaktuCharge.Start();
                        return;
                    }
                    catch 
                    {
                        return;
                    }
                }

                #region zeroing if data minus
                dataIn[0] = (Convert.ToDouble(dataIn[0]) <= 0 ? "0" : dataIn[0]);
                dataIn[1] = (Convert.ToDouble(dataIn[1]) <= 0 ? "0" : dataIn[1]);
                dataIn[2] = (Convert.ToDouble(dataIn[2]) <= 0 ? "0" : dataIn[2]);
                dataIn[3] = (Convert.ToDouble(dataIn[3]) <= 0 ? "0" : dataIn[3]);
                dataIn[4] = (Convert.ToDouble(dataIn[4]) <= 0 ? "0" : dataIn[4]);
                #endregion

                pwm = Convert.ToInt32(Convert.ToDouble(dataIn[5]));
                float suhuSensor = float.Parse(dataIn[15]);
                float kelembabanSensor = float.Parse(dataIn[16]);
                float tekananSensor = float.Parse(dataIn[17]);

                string arusIn = dataIn[7];
                string dayaIn = dataIn[8];

                if (rbOtomatis.Checked)
                {
                    if (!(pwm > 73 && pwm < 80))
                    {
                        serialPortUtama.Write($"setPWM,30,*");
                    }
                }

                if (countDataReceived == 0)
                {
                    voltaseAwal = float.Parse(teganganIn);
                }

                countDataReceived++;
                voltaseAkhir = float.Parse(teganganIn);
                //Console.WriteLine("Voltase awal" + voltaseAwal);
                //Console.WriteLine("Voltase skhir" + voltaseAkhir);

                #region Conversi
                Task<double[]> tambahOffset = cleansingData(dataIn, suhuSensor, kelembabanSensor);
                tambahOffset.Wait();
                double[] finalData = await tambahOffset;
                Task<double[]> task = conversiPpmKeMicrogram(finalData);
                double[] ugM3 = await task;
                #endregion

                double no2 = Convert.ToDouble(String.Format("{0:0.000}",ugM3[0]));
                double so2 = Convert.ToDouble(String.Format("{0:0.000}", ugM3[1]));
                double o3 = Convert.ToDouble(String.Format("{0:0.000}",ugM3[2]));
                double co = Convert.ToDouble(String.Format("{0:0.000}",ugM3[3]));
                double hc = Convert.ToDouble(String.Format("{0:0.000}",ugM3[4]));

                string arahAnginIn = finalData[5].ToString();
                double kecepatanAnginIn = ((finalData[6] >= Convert.ToDouble(Properties.Settings.Default.bawahKecepatan)) && finalData[6]<= Convert.ToDouble(Properties.Settings.Default.atasKecepatan) ? Convert.ToDouble(finalData[6]) * 1.609344 : 0);
                double kelembabanIn = Convert.ToDouble(finalData[7]);
                double suhuIn = Convert.ToDouble(finalData[8]);
                string solarRadiasi = (Properties.Settings.Default.solarRadiasi == true ? finalData[9].ToString() : "0");

                double tekananUdaraIn = 0;
                Console.WriteLine("GPS" + serialPortGps.IsOpen);
                string ekor = dataIn[dataIn.Length - 1];

                double PM10 = 0;
                double PM25 = 0;

                DateTime time = DateTime.Now;
                string waktu = $"{time.ToString("yyyy-MM-dd")} {time.ToString("HH")}:{time.ToString("mm")}:{time.ToString("ss")}";

                #region Sql
                // ini adalah data yang sudah bersih
                insertData($"INSERT INTO tbl_data(nama_petugas, suhu, kelembaban, tekanan, arah_angin, kecepatan_angin, no2, o3, co, so2, hc, pm2_5, pm10, waktu, solar_radiasi, membraSensValid) values('{LoginStatus.namaUser}','{suhuIn}', '{kelembabanIn}', '{tekananUdaraIn}', '{arahAnginIn}', '{String.Format("{0:0.00}", kecepatanAnginIn)}', '{no2}', '{o3}', '{co}', '{so2}', '{hc}', '{PM25}', '{PM10}', '{waktu}', '{solarRadiasi}', '{(baca_membrasens == true ? "Valid" : "Invalid" )}')");
                // Data ini adalah data mentah yang belum ditambahkan offset
                insertData($"INSERT INTO raw_data(raw_data,nama_petugas,created_at,email,no2,so2,o3,co,hc) values('{datas}','{LoginStatus.namaUser}', '{time.ToString("yyyy-MM-dd HH:mm:ss")}','{LoginStatus.email}', '{dataIn[0]}', '{dataIn[1]}', '{dataIn[2]}', '{dataIn[3]}', '{dataIn[4]}')");
                #endregion

                #region Api Post
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        rawDataPost raw = new rawDataPost();
                        raw.device_id = LoginStatus.idNode;
                        raw.suhu = Convert.ToInt32(suhuIn);
                        raw.kelembapan = Convert.ToInt32(kelembabanIn);
                        raw.kecepatan_angin = Convert.ToInt32(kecepatanAnginIn);
                        raw.arah_angin = arahAnginIn;
                        raw.time = time.ToString("yyyy-MM-dd HH:mm:ss");
                        raw.tail = ekor;
                        raw.tekanan = Convert.ToInt32(tekananUdaraIn);
                        raw.no2 = finalData[0].ToString();
                        raw.so2 = finalData[1].ToString();
                        raw.o3 = finalData[2].ToString();
                        raw.co = finalData[3].ToString();
                        raw.nmhc = finalData[4].ToString();
                        raw.tegangan = teganganIn;
                        raw.arus = arusIn;
                        raw.daya = dayaIn;
                        raw.pm10 = Convert.ToInt32(PM10);
                        raw.pm25 = Convert.ToInt32(PM25);
                        raw.solar = solarRadiasi;
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", LoginStatus.token);
                        string url = $"{Properties.Settings.Default.apiUrl}/api/device_data";
                        var json = JsonConvert.SerializeObject(raw);
                        var data = new StringContent(json, Encoding.UTF8, "application/json");
                        using (var response = await client.PostAsync(url, data))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                var responseMessage = await response.Content.ReadAsStringAsync();
                                Console.WriteLine(responseMessage);
                            }
                            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                            {
                                var responseMessage = await response.Content.ReadAsStringAsync();
                                JObject keyValuePairs = JObject.Parse(responseMessage);
                                if (keyValuePairs["code"].ToString() == "ERR")
                                {
                                    if (!serialPortUtama.IsOpen)
                                    {
                                        serialPortUtama.Open();
                                    }
                                    if (serialPortUtama.IsOpen)
                                    {
                                        serialPortUtama.Write("setPWM,0,*");
                                        Thread.Sleep(500);
                                        serialPortUtama.Write("setPWM,0,*");
                                    }
                                    waktuAkhir = DateTime.Now;
                                    serialPortUtama.Close();
                                    _login.Show();
                                    MessageBox.Show(keyValuePairs["messages"].ToString(), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    this.Close();
                                    return;
                                }
                            }
                            else
                            {
                                var responseMessage = await response.Content.ReadAsStringAsync();
                                Console.WriteLine(responseMessage);
                            }
                        }
                    }
                    
                }
                catch (Exception)
                {
                }
                #endregion

                #region Show to chart
                chart2.Series[0].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(suhuIn));
                chart2.Series[1].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(kelembabanIn));
                chart2.Series[2].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(kecepatanAnginIn));
                chart2.Series[3].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(tekananUdaraIn));
                chart3.Series[0].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(PM25));

                if (baca_membrasens == true)
                {
                    if (_chartGasses.chart2.Series[0].Points.Count > ChartLimit)
                    {
                        _chartGasses.chart2.Series[0].Points.RemoveAt(0);
                        _chartGasses.chart2.Series[1].Points.RemoveAt(0);
                        _chartGasses.chart2.Series[2].Points.RemoveAt(0);
                        _chartGasses.chart2.Series[3].Points.RemoveAt(0);
                        _chartGasses.chart2.Series[4].Points.RemoveAt(0);
                    }

                    if (label116.Text == "ppm")
                    {
                        double[] point = new double[5];
                        point[0] = Convert.ToDouble(dataIn[0]);
                        point[1] = Convert.ToDouble(dataIn[1]);
                        point[2] = Convert.ToDouble(dataIn[2]);
                        point[3] = Convert.ToDouble(dataIn[3]);
                        point[4] = Convert.ToDouble(dataIn[4]);
                        _chartGasses.addPoint(point, time.ToString("HH:mm:ss"));
                    }
                    else if (label116.Text == "µg/m3")
                    {
                        double[] point = new double[5];
                        point[0] = Convert.ToDouble(no2);
                        point[1] = Convert.ToDouble(so2);
                        point[2] = Convert.ToDouble(o3);
                        point[3] = Convert.ToDouble(co);
                        point[4] = Convert.ToDouble(hc);
                        _chartGasses.addPoint(point, time.ToString("HH:mm:ss"));
                    }
                }

                if (chart2.Series[0].Points.Count > ChartLimit) { chart2.Series[0].Points.RemoveAt(0); chart2.Series[1].Points.RemoveAt(0); chart2.Series[2].Points.RemoveAt(0); chart2.Series[3].Points.RemoveAt(0); }

                #endregion

                showPPM(dataIn[0], dataIn[1], dataIn[2], dataIn[3], dataIn[4]);
                showValue(suhuIn.ToString(), kelembabanIn.ToString(), String.Format("{0:0.00}", kecepatanAnginIn), tekananUdaraIn.ToString(), arahAnginIn, no2.ToString(), o3.ToString(), co.ToString(), so2.ToString(), hc.ToString(), PM25.ToString(), PM10.ToString(), teganganIn, arusIn, dayaIn, datas, teganganIn, solarRadiasi);
                if (timerRequest.Enabled == false)
                {
                    timerRequest.Start();
                }
                Console.WriteLine("TMR2 " + timerRequest.Enabled);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                if (timerRequest.Enabled == false)
                {
                    timerRequest.Start();
                }
                Console.WriteLine("TMR3 " + timerRequest.Enabled);
            }
        }

        private void showPPM(string no2, string so2, string o3, string co, string hc)
        {
            ppmNO2.Text = String.Format("{0:0.000}", Convert.ToDouble(no2)) + " ppm";
            ppmSO2.Text = String.Format("{0:0.000}", Convert.ToDouble(so2)) + " ppm";
            ppmO3.Text = String.Format("{0:0.000}", Convert.ToDouble(o3)) + " ppm";
            ppmCO.Text = String.Format("{0:0.000}", Convert.ToDouble(co)) + " ppm";
            ppmHC.Text = String.Format("{0:0.000}", Convert.ToDouble(hc)) + " ppm";
        }

        class rawDataPost
        {
            public string device_id { get; set; }
            public int suhu { get; set; }
            public int kecepatan_angin { get; set; }
            public int tekanan { get; set; }
            public string arah_angin { get; set; }
            public int kelembapan { get; set; }
            public string no2 { get; set; }
            public string o3 { get; set; }
            public string co { get; set; }
            public string so2 { get; set; }
            public string nmhc { get; set; }
            public int pm25 { get; set; }
            public int pm10 { get; set; }
            public string solar { get; set; }
            //public string ppmNO2 { get; set; }
            //public string ppmSO2 { get; set; }
            //public string ppmO3 { get; set; }
            //public string ppmCO { get; set; }
            //public string ppmHC { get; set; }
            //public string ppmPM25 { get; set; }
            //public string ppmPM10 { get; set; }
            public string tegangan { get; set; }
            public string arus { get; set; }
            public string daya { get; set; }
            public string tail { get; set; }
            public string time { get; set; }
        }

        int ChartLimit = 40;

        private async Task<double[]> conversiPpmKeMicrogram(double[] nilai)
        {
            double[] hasil = new double[5];
            await Task.Run(() =>
            {
                hasil[0] = 0.0409 * (Convert.ToDouble(nilai[0]) * 1000) * 46.01;
                hasil[1] = 0.0409 * (Convert.ToDouble(nilai[1]) * 1000) * 64.07;
                hasil[2] = 0.0409 * (Convert.ToDouble(nilai[2]) * 1000) * 48;
                hasil[3] = 0.0409 * (Convert.ToDouble(nilai[3]) * 1000) * 28.01;
                hasil[4] = 0.0409 * (Convert.ToDouble(nilai[4]) * 1000) * 13.01;
            });
            return hasil;
        }
        bool baca_membrasens;
        int pwm;
        private async Task<double[]> cleansingData(string[] dataIn, float suhuSensor, float kelembabanSensor)
        {
            double[] finalData = new double[10];
            try
            {
                if ((suhuSensor > Properties.Settings.Default.bawahSuhu && suhuSensor < Properties.Settings.Default.atashSuhu) && (kelembabanSensor > Properties.Settings.Default.bawahKelembaban && kelembabanSensor < Properties.Settings.Default.atasKelembaban)/* && (tekananSensor > Properties.Settings.Default.bawahTekanan && tekananSensor < Properties.Settings.Default.atasTekanan)*/)
                {
                    finalData[0] = (Properties.Settings.Default.no2 == true ? (Convert.ToDouble(dataIn[0]) + Properties.Settings.Default.offsetNo2) : 0);
                    finalData[1] = (Properties.Settings.Default.so2 == true ? Convert.ToDouble(dataIn[1]) + Properties.Settings.Default.offsetSo2 : 0);
                    finalData[2] = (Properties.Settings.Default.o3 == true ? Convert.ToDouble(dataIn[2]) + Properties.Settings.Default.offsetO3 : 0);
                    finalData[3] = (Properties.Settings.Default.co == true ? Convert.ToDouble(dataIn[3]) + Properties.Settings.Default.offsetCo : 0);
                    finalData[4] = (Properties.Settings.Default.nmhc == true ? Convert.ToDouble(dataIn[4]) + Properties.Settings.Default.offsetNMHC : 0);
                    labelDataInvalid.Text = "";
                    baca_membrasens = true;
                }
                else
                {
                    finalData[0] = 0;
                    finalData[1] = 0;
                    finalData[2] = 0;
                    finalData[3] = 0;
                    finalData[4] = 0;
                    baca_membrasens = false;
                    labelDataInvalid.Text = "Data membrasens Invalid";
                    labelDataInvalid.ForeColor = Color.Red;
                }

                finalData[5] = (Properties.Settings.Default.arahAngin == true ? Convert.ToDouble(dataIn[9]) + Properties.Settings.Default.offsetArahAngin : 0);
                finalData[6] = (Properties.Settings.Default.kecepatanAngin == true ? Convert.ToDouble(dataIn[11]) + Properties.Settings.Default.offsetKecepatanAngin : 0);
                finalData[7] = (Properties.Settings.Default.kelembaban == true ? Convert.ToDouble(dataIn[12]) + Properties.Settings.Default.offsetKelembaban : 0);
                finalData[8] = (Properties.Settings.Default.suhu == true ? Convert.ToDouble(dataIn[13]) + Properties.Settings.Default.offsetSuhu : 0);
                finalData[9] = (Properties.Settings.Default.solarRadiasi == true ? Convert.ToDouble(dataIn[14]) + Properties.Settings.Default.offsetSolarRadiasi : 0);
                return finalData;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return finalData;
            }
        }
        float voltaseAwal, voltaseAkhir;
        DateTime waktuAwal, waktuAkhir;

        private void showValue(string suhu, string kelembaban, string kecepatan_angin, string tekanan, string arahangin, string no2, string o3, string co, string so2, string hc, string pm25, string pm10, string tegangan, string arus, string daya, string dataFromSerialPort, string teganganIn, string solarRadiasi)
        {
            //Show metereology
            labelSuhu.Text = suhu + " °C";
            labelKelembaban.Text = kelembaban + "%";
            labelKecepatanAngin.Text = kecepatan_angin + "km/h";
            //labelTekanan.Text = tekanan + " mbar";
            //string arahAngin = konversiDerajatKeArahAngin(arahangin);
            labelArahAngin.Text = arahangin + "° Dari utara";
            //Show gas
            labelNO2.Text = no2 + " μg/m3";
            labelO3.Text = o3 + " μg/m3";
            labelCO.Text = co + " μg/m3";
            labelSO2.Text = so2 + " μg/m3";
            labelHC.Text = hc + " μg/m3";

            labelPM25.Text = pm25 + " µg/m3";
            labelPM10.Text = pm10 + " µg/m3";

            labelTegangan.Text = tegangan + " V";
            labelArus.Text = arus + " mA";
            labelDaya.Text = daya + " W";

            labelSolarRadiasi.Text = solarRadiasi + " W/m²";

            labelRawData.Text = dataFromSerialPort.Trim();
            try
            {
                double bateraiThreshold = (double)(Properties.Settings.Default.batteryThreshold + 10) / 100;
                if (Convert.ToDouble(teganganIn) > (12 * (( bateraiThreshold > 100 ? 100 : bateraiThreshold))))
                {
                    panel44.BackColor = Color.LimeGreen;
                    beep = true;
                }
                else if (Convert.ToDouble(teganganIn) <= (12 * ((double)Properties.Settings.Default.batteryThreshold / 100)))
                {
                    panel44.BackColor = Color.Red;
                    if (beep == true)
                    {
                        beep = false;
                        for (int i = 0; i < 3; i++)
                        {
                            Console.Beep();
                        }
                    }
                }
                else if (Convert.ToDouble(teganganIn) < (12 * ((bateraiThreshold > 100 ? 100 : bateraiThreshold))))
                {
                    panel44.BackColor = Color.Yellow;
                    beep = true;
                }
                double persen_baterai = (Convert.ToDouble(teganganIn) / 12) * 100;
                labelBattery.Text = String.Format("{0:0}", (persen_baterai > 100 ? 100 : Convert.ToInt32(persen_baterai)) +  "%");
            }
            catch (Exception)
            {
            }
        }
        bool beep = true;
        private DataTable readDatabase(string query)
        {
            DataTable dt = new DataTable();
            try
            {
                Koneksi();
                command = new SQLiteCommand(query, connection);
                SQLiteDataAdapter da = new SQLiteDataAdapter(command);
                da.Fill(dt);
                connection.Close();
                return dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return dt;
        }
        private async void insertData(string v)
        {
            try
            {
                Koneksi();
                Console.WriteLine(v);
                command = new SQLiteCommand("PRAGMA foreign_keys=ON", connection);
                command.CommandText = v;
                await command.ExecuteNonQueryAsync();
                connection.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Koneksi()
        {
            try
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            //countDataReceived = 0;
            waktuAwal = DateTime.Now;
            if (Properties.Settings.Default.portAlat != "" && Properties.Settings.Default.portGps != "")
            {
                try
                {
                    if (rbOtomatis.Checked)
                    {
                        MessageBox.Show("Fitur dinonaktifkan");
                        return;
                    }
                    serialPortUtama.PortName = Properties.Settings.Default.portAlat;
                    serialPortUtama.BaudRate = 9600;
                    serialPortUtama.Open();

                    if (serialPortGps.IsOpen == false)
                    {
                        serialPortGps.PortName = Properties.Settings.Default.portGps;
                        serialPortGps.BaudRate = 9600;
                        serialPortGps.ReadTimeout = 1000;
                        var task = Task.Run(() =>
                        {
                            try
                            {
                                serialPortGps.Open();
                                serialPortGps.DiscardInBuffer();
                                serialPortGps.DiscardOutBuffer();
                                serialPortGps.DataReceived += serialPortGps_DataReceived;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error : {ex.Message}\nGPS tidak aktif lokasi akan diambil dari lokasi terakhir", "Port GPS error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        });
                        task.Wait();
                        if (serialPortGps.IsOpen)
                        {
                            timerRequestMap.Start();
                        }
                    }
                    if (serialPortUtama.IsOpen)
                    {
                        serialPortUtama.Write($"setPWM,{Properties.Settings.Default.pwmTrack},*");
                    }
                    if (serialPortUtama.IsOpen && rbDataOtomatis.Checked)
                    {
                        timerRequest.Start();
                    }
                    else if (serialPortUtama.IsOpen && rbDataManual.Checked)
                    {
                        timerRequest.Stop();
                        btnSendRequest.Enabled = true;
                    }
                    if (Properties.Settings.Default.reset == true)
                    {
                        timer2.Start();
                    }
                    else
                    {
                        timer2.Stop();
                    }
                    connectState();
                }
                catch (Exception ex)
                {
                    if (!serialPortUtama.IsOpen && serialPortGps.IsOpen)
                    {
                        serialPortGps.Close();
                    }
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                return;
            }
            MessageBox.Show("Silahkan isi port", "Port empty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        class raw
        {
            public string device_id { get; set; }
            public string baterai { get; set; }
        }

        private async void btnDisconnect_Click(object sender, EventArgs e)
        {
            //waktuAkhir = DateTime.Now;
            try
            {
                if (serialPortUtama.IsOpen)
                {
                    serialPortUtama.Close();
                    serialPortGps.Close();
                    serialPortGps.DataReceived -= serialPortGps_DataReceived;
                    btnSendRequest.Enabled = false;
                    disconnectState();
                    timerRequest.Stop();
                    timerRequestMap.Stop();
                    timer2.Stop();
                }
                insertData($"UPDATE tbl_perangkat SET portAlat = '{Properties.Settings.Default.portAlat}', portGps = '{Properties.Settings.Default.portGps}', lat = '{Properties.Settings.Default.lat}', lng = '{Properties.Settings.Default.lng}', pwmTrack = '{Properties.Settings.Default.pwmTrack}', pwmAuto = '{Properties.Settings.Default.pwmOtomatis}', timerCharge = '{Properties.Settings.Default.timerCharge}' WHERE id = '1';");
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        rawLat h = new rawLat() { device_id = LoginStatus.idNode, latitude = Properties.Settings.Default.lat, longitude = Properties.Settings.Default.lng };
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", LoginStatus.token);
                        string url = $"{Properties.Settings.Default.apiUrl}/api/device/sendlocation";
                        var json = JsonConvert.SerializeObject(h);
                        var asd = new StringContent(json, Encoding.UTF8, "application/json");
                        using (var response = await client.PostAsync(url, asd))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                var responseMessage = await response.Content.ReadAsStringAsync();
                                Console.WriteLine(responseMessage);
                                done = false;
                            }
                            else
                            {
                                var responseMessage = await response.Content.ReadAsStringAsync();
                                Console.WriteLine(responseMessage);
                            }
                        }
                    }
                }
                catch
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Disconnect failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //private string konversiDerajatKeArahAngin(string derajat)
        //{
        //    string result = "";
        //    float degree = float.Parse(derajat.Replace('.', ','));
        //    if ((degree > 348.75 && degree <= 360) || (degree >= 0 && degree < 11.26))
        //    {
        //        result = "N";
        //    }
        //    else if (degree > 11.25 && degree < 33.76)
        //    {
        //        result = "NNE";
        //    }
        //    else if (degree > 33.75 && degree < 58.26)
        //    {
        //        result = "NE";
        //    }
        //    else if (degree > 56.25 && degree < 78.76)
        //    {
        //        result = "ENE";
        //    }
        //    else if (degree > 78.75 && degree < 101.26)
        //    {
        //        result = "E";
        //    }
        //    else if (degree > 101.25 && degree < 123.76)
        //    {
        //        result = "ESE";
        //    }
        //    else if (degree > 123.75 && degree < 146.26)
        //    {
        //        result = "SE";
        //    }
        //    else if (degree > 146.25 && degree < 168.76)
        //    {
        //        result = "SSE";
        //    }
        //    else if (degree > 168.75 && degree < 191.26)
        //    {
        //        result = "S";
        //    }
        //    else if (degree > 191.25 && degree < 213.76)
        //    {
        //        result = "SSW";
        //    }
        //    else if (degree > 213.75 && degree < 236.26)
        //    {
        //        result = "SW";
        //    }
        //    else if (degree > 236.25 && degree < 258.76)
        //    {
        //        result = "WSW";
        //    }
        //    else if (degree > 258.75 && degree < 281.26)
        //    {
        //        result = "W";
        //    }
        //    else if (degree > 281.25 && degree < 303.76)
        //    {
        //        result = "WNW";
        //    }
        //    else if (degree > 303.75 && degree < 326.26)
        //    {
        //        result = "NW";
        //    }
        //    else if (degree > 326.25 && degree < 348.76)
        //    {
        //        result = "NWN";
        //    }
        //    return result;
        //}

        private void btnGenereateRawData_Click(object sender, EventArgs e)
        {
            string query = $"SELECT * FROM raw_data where created_at between '{dtpRawDataFrom.Value.ToString("yyyy-MM-dd")} {dateTimePicker2.Value.ToString("HH:mm:ss")}' AND '{dtpRawDataFrom.Value.ToString("yyyy-MM-dd")} {dateTimePicker1.Value.ToString("HH:mm:ss")}' ORDER BY id DESC";
            Console.WriteLine(query);
            DataTable dtRawData = readDatabase(query);
            entityRawDataBindingSource.DataSource = dtRawData;
        }

        private void btnUserLog_Click(object sender, EventArgs e)
        {
            string query = $"SELECT * FROM tbl_log WHERE waktu between '{dtpFromLog.Value.ToString("yyyy-MM-dd")}' AND '{dtpToLog.Value.ToString("yyyy-MM-dd")} 23:59:59' ORDER BY id DESC";
            DataTable dtLogUser = readDatabase(query);
            entityUserLogBindingSource.DataSource = dtLogUser;
        }

        CultureInfo ci = new CultureInfo("id-ID");

        private void timerDate_Tick(object sender, EventArgs e)
        {
            labelDateTime.Text = $"{DateTime.Now.ToString("D", ci)} {DateTime.Now.ToLongTimeString()}";
        }

        void firstSetup()
        {
            // Chart setup
            txtPwmTrack.Text = Properties.Settings.Default.pwmTrack.ToString();
            trackPwm.Value = Properties.Settings.Default.pwmTrack;
            //set gambar aplikasi
            if (Properties.Settings.Default.imgaePath != "")
            {
                pbApp.ImageLocation = Application.StartupPath + Properties.Settings.Default.imgaePath;
                logoApp.ImageLocation = Application.StartupPath + Properties.Settings.Default.imgaePath;
                pbAbout.ImageLocation = Application.StartupPath + Properties.Settings.Default.imgaePath;
            }

            if (Properties.Settings.Default.namaAplikasi != "")
            {
                txtNamaAplikasi.Text = Properties.Settings.Default.namaAplikasi;
                namaAplikasi.Text = Properties.Settings.Default.namaAplikasi;
            }

            // Set nama port yang digunakan
            if (Properties.Settings.Default.portAlat != "")
            {
                labelPortYangDigunakan.Text = $"Port yang digunakan : {Properties.Settings.Default.portAlat}";
            }

            numericUpDown1.Controls[0].Enabled = false;
            numericUpDown2.Controls[0].Enabled = false;
            labelLatitude.Text = Properties.Settings.Default.lat;
            labelLongitude.Text = Properties.Settings.Default.lng;
            numericUpDown1.Value = Convert.ToDecimal(Properties.Settings.Default.lat);
            numericUpDown2.Value = Convert.ToDecimal(Properties.Settings.Default.lng);

            if (Properties.Settings.Default.apiUrl == "")
            {
                Properties.Settings.Default.apiUrl = "http://103.139.192.125";
                Properties.Settings.Default.Save();
            }

            if (Properties.Settings.Default.batteryThreshold == 0)
            {
                Properties.Settings.Default.batteryThreshold = 75;
                Properties.Settings.Default.Save();
            }

            if (Properties.Settings.Default.portGps != "")
            {
                label28.Text = $"Port yang digunakan : {Properties.Settings.Default.portGps}";
            }
            if (Properties.Settings.Default.aboutUs != "")
            {
                labelAbout.Text = Properties.Settings.Default.aboutUs;
            }
            //offsetCO.Text = Properties.Settings.Default.offsetCo.ToString();
            //offsetNO2.Text = Properties.Settings.Default.offsetNo2.ToString();
            //offsetSO2.Text = Properties.Settings.Default.offsetSo2.ToString();
            //offsetO3.Text = Properties.Settings.Default.offsetO3.ToString();
            //offsetHC.Text = Properties.Settings.Default.offsetNMHC.ToString();
            //offsetPM10.Text = Properties.Settings.Default.offsetPM10.ToString();
            //offsetPM25.Text = Properties.Settings.Default.offsetPM25.ToString();
            txtBatteryThreshold.Text = Properties.Settings.Default.batteryThreshold.ToString();
            bawahSuhu.Value = Convert.ToDecimal(Properties.Settings.Default.bawahSuhu);
            atasSuhu.Value = Convert.ToDecimal(Properties.Settings.Default.atashSuhu);
            bawahKelembaban.Value = Convert.ToDecimal(Properties.Settings.Default.bawahKelembaban);
            atasKelembaban.Value = Convert.ToDecimal(Properties.Settings.Default.atasKelembaban);
            bawahTekanan.Value = Convert.ToDecimal(Properties.Settings.Default.bawahTekanan);
            atastekanan.Value = Convert.ToDecimal(Properties.Settings.Default.atasTekanan);
            txtInletUdara.Text = Properties.Settings.Default.inletUdara.ToString();
            txtPwmAuto.Text = Properties.Settings.Default.pwmOtomatis.ToString();
            numericUpDown3.Value = Convert.ToDecimal(Properties.Settings.Default.interval);
            numericUpDown4.Value = Convert.ToDecimal(Properties.Settings.Default.timerCharge);
            //textBox3.Text = LoginStatus.namaUser;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            getPortList("startup");
            disconnectState();
            timerDate.Start();
            firstSetup();
            setCheckBoxSetting();
            getCountSensorMenyala();
            pwmManual();
            stopDebugState();
            btnSendRequest.Enabled = false;
            btnConnect.Enabled = false;
            btnConnect.BackColor = Color.LightGray;
            timerWaktuTunggu.Start();
            cbxReset.SelectedIndex = (Properties.Settings.Default.reset == true ? 1 : 0);
            //textBox3.Text = LoginStatus.namaUser;
            //timerMap.Start();
        }

        private void btnSaveAppSetting_Clck(object sender, EventArgs e)
        {
            if (txtNamaAplikasi.Text != "")
            {
                Properties.Settings.Default.namaAplikasi = txtNamaAplikasi.Text;
                Properties.Settings.Default.Save();
                namaAplikasi.Text = Properties.Settings.Default.namaAplikasi;
                insertLog("Mengubah gambar dan nama aplikasi");
                MessageBox.Show("Data sudah update", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MessageBox.Show("Nama aplikasi tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            {
                OpenFileDialog openFileDialog = new OpenFileDialog()
                {
                    Filter = "Image Files (*.png; *.jpeg; *.jpg;)|*.png; *.jpg; *.jpeg;"
                };
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //var fileName = openFileDialog.FileName.Split('\\')[openFileDialog.FileName.Split('\\').Length - 1];
                    //var fileNameWithoutExtension = fileName.Split('.')[0];
                    if (File.Exists(Application.StartupPath + @"\images\logo.png"))
                    {
                        File.Delete(Application.StartupPath + @"\images\logo.png");
                    }
                    File.Copy(openFileDialog.FileName, Application.StartupPath + @"\images\logo.png");
                    Properties.Settings.Default.imgaePath = @"\images\logo.png";
                    Properties.Settings.Default.Save();
                    logoApp.ImageLocation = Application.StartupPath + Properties.Settings.Default.imgaePath;
                    pbApp.ImageLocation = Application.StartupPath + Properties.Settings.Default.imgaePath;
                    pbAbout.ImageLocation = Application.StartupPath + Properties.Settings.Default.imgaePath;
                }
            }
        }

        void setCheckBoxSetting()
        {
            checkBoxSuhu.Checked = Properties.Settings.Default.suhu;
            checkBoxKelembaban.Checked = Properties.Settings.Default.kelembaban;
            checkBoxTekanan.Checked = Properties.Settings.Default.tekanan;
            checkBoxKecepatanAngin.Checked = Properties.Settings.Default.kecepatanAngin;
            checkBoxArahAngin.Checked = Properties.Settings.Default.arahAngin;
            checkBoxNO2.Checked = Properties.Settings.Default.no2;
            checkBoxO3.Checked = Properties.Settings.Default.o3;
            checkBoxCO.Checked = Properties.Settings.Default.co;
            checkBoxSO2.Checked = Properties.Settings.Default.so2;
            checkBoxHC.Checked = Properties.Settings.Default.nmhc;
            checkBoxPM25.Checked = Properties.Settings.Default.pm25;
            checkBoxSolar.Checked = Properties.Settings.Default.solarRadiasi;
            checkBoxPM10.Checked = Properties.Settings.Default.pm10;
        }

        private void getCountSensorMenyala()
        {
            int sensorYangMenyala = 0;
            int jumlahSensor = 13;
            if (Properties.Settings.Default.suhu)
                sensorYangMenyala++;
            if (Properties.Settings.Default.kelembaban)
                sensorYangMenyala++;
            if (Properties.Settings.Default.tekanan)
                sensorYangMenyala++;
            if (Properties.Settings.Default.kecepatanAngin)
                sensorYangMenyala++;
            if (Properties.Settings.Default.arahAngin)
                sensorYangMenyala++;
            if (Properties.Settings.Default.no2)
                sensorYangMenyala++;
            if (Properties.Settings.Default.o3)
                sensorYangMenyala++;
            if (Properties.Settings.Default.co)
                sensorYangMenyala++;
            if (Properties.Settings.Default.so2)
                sensorYangMenyala++;
            if (Properties.Settings.Default.nmhc)
                sensorYangMenyala++;
            if (Properties.Settings.Default.pm10)
                sensorYangMenyala++;
            if (Properties.Settings.Default.pm25)
                sensorYangMenyala++;
            if (Properties.Settings.Default.solarRadiasi)
                sensorYangMenyala++;
            labelSensorMenyala.Text = $"Daftar sensor yang menyala {sensorYangMenyala} dari {jumlahSensor}";
            //return sensorYangMenyala;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Properties.Settings.Default.suhu = checkBoxSuhu.Checked;
                Properties.Settings.Default.kelembaban = checkBoxKelembaban.Checked;
                Properties.Settings.Default.tekanan = checkBoxTekanan.Checked;
                Properties.Settings.Default.kecepatanAngin = checkBoxKecepatanAngin.Checked;
                Properties.Settings.Default.arahAngin = checkBoxArahAngin.Checked;
                Properties.Settings.Default.no2 = checkBoxNO2.Checked;
                Properties.Settings.Default.o3 = checkBoxO3.Checked;
                Properties.Settings.Default.co = checkBoxCO.Checked;
                Properties.Settings.Default.so2 = checkBoxSO2.Checked;
                Properties.Settings.Default.nmhc = checkBoxHC.Checked;
                Properties.Settings.Default.pm25 = checkBoxPM25.Checked;
                Properties.Settings.Default.pm10 = checkBoxPM10.Checked;
                Properties.Settings.Default.solarRadiasi = checkBoxSolar.Checked;
                Properties.Settings.Default.batteryThreshold = Convert.ToInt32(txtBatteryThreshold.Text);
                Properties.Settings.Default.Save();
                getCountSensorMenyala();
                insertLog("Mengubah pengaturan sensor dan baterai");
                MessageBox.Show("Sensor updated", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        GMapControl map = new GMapControl();
        private void timerMap_Tick(object sender, EventArgs e)
        {
            //try
            //{
            //    string latitude = Properties.Settings.Default.lat;
            //    string longitude = Properties.Settings.Default.lng;
            //    string altitude = Properties.Settings.Default.alt;
            //    panelMap.Controls.Clear();
            //    map.Dock = DockStyle.Fill;
            //    panelMap.Controls.Add(map);
            //    map.DragButton = System.Windows.Forms.MouseButtons.Left;
            //    map.MapProvider = GMapProviders.OpenStreetMap;
            //    map.MinZoom = 5;
            //    map.MaxZoom = 20;
            //    map.Zoom = 13;
            //    PointLatLng point = new PointLatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
            //    map.Position = point;
            //    GMapMarker marker = new GMarkerGoogle(point, GMarkerGoogleType.red_dot);
            //    GMapOverlay markers = new GMapOverlay("markers");
            //    markers.Markers.Add(marker);
            //    map.Overlays.Clear();
            //    map.Overlays.Add(markers);

            //    labelLatitude.Text = latitude;
            //    labelLongitude.Text = longitude;
            //    labelAltitude.Text = altitude + " Mdpl";
            //    timerMap.Stop();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Map error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    timerMap.Stop();
            //}
        }

        private void trackPwm_Scroll(object sender, EventArgs e)
        {
            int valueScroll = Convert.ToInt32(trackPwm.Value);
            txtPwmTrack.Text = valueScroll.ToString();
        }

        private void txtPwmTrack_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
     (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }

        private void txtPwmTrack_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Convert.ToInt32(txtPwmTrack.Text) <= 100)
                {
                    trackPwm.Value = (Convert.ToDecimal(txtPwmTrack.Text) <= 0 ? 1 : Convert.ToDecimal(txtPwmTrack.Text));
                }
                else
                {
                    trackPwm.Value = 100;
                    txtPwmTrack.Text = "100";
                }
            }
            catch (Exception)
            {
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string query = $"SELECT * FROM log_perangkat WHERE tanggal between '{dtpFromZero.Value.ToString("yyyy-MM-dd")}' AND '{dtpToZero.Value.ToString("yyyy-MM-dd")}' ORDER BY id DESC";
            DataTable dtZeroLog = readDatabase(query);
            dgZeroLog.DataSource = dtZeroLog;
        }
        DataRow dataRow;
        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                string query = $"SELECT * FROM tbl_data WHERE waktu between '{tanggalReport.Value.ToString("yyyy-MM-dd")} {waktuFrom.Value.ToString("HH:mm:ss")}' AND '{tanggalReport.Value.ToString("yyyy-MM-dd")} {waktuTo.Value.ToString("HH:mm:ss")}'";
                DataTable dtReport = readDatabase(query);
                if (dtReport.Rows.Count > 0)
                {
                    chartWeather.Series[0].Points.Clear();
                    chartWeather.Series[1].Points.Clear();
                    chartWeather.Series[2].Points.Clear();
                    chartGas.Series[0].Points.Clear();
                    chartGas.Series[1].Points.Clear();
                    chartGas.Series[2].Points.Clear();
                    chartGas.Series[3].Points.Clear();
                    chartGas.Series[4].Points.Clear();
                    for (int i = 0; i < dtReport.Rows.Count; i++)
                    {
                        dataRow = dtReport.Rows[i];
                        double suhu = Convert.ToDouble(dataRow["suhu"].ToString());
                        double kelembaban = Convert.ToDouble(dataRow["kelembaban"].ToString());
                        double kecepatan_angin = Convert.ToDouble(dataRow["kecepatan_angin"].ToString());
                        double no2 = Convert.ToDouble(dataRow["no2"].ToString());
                        double o3 = Convert.ToDouble(dataRow["o3"].ToString());
                        double co = Convert.ToDouble(dataRow["co"].ToString());
                        double so2 = Convert.ToDouble(dataRow["so2"].ToString());
                        double hc = Convert.ToDouble(dataRow["hc"].ToString());
                        DateTime waktu = Convert.ToDateTime(dataRow["waktu"].ToString());
                        chartWeather.Series["Temperature"].Points.AddXY(waktu.ToString("HH:mm"), suhu);
                        chartWeather.Series["Humidity"].Points.AddXY(waktu.ToString("HH:mm"), kelembaban);
                        chartWeather.Series["Wind Speed"].Points.AddXY(waktu.ToString("HH:mm"), kecepatan_angin);
                        if (dataRow["membraSensValid"].ToString() == "Valid")
                        {
                            chartGas.Series["NO2"].Points.AddXY(waktu.ToString("HH:mm"), no2);
                            chartGas.Series["O3"].Points.AddXY(waktu.ToString("HH:mm"), o3);
                            chartGas.Series["CO"].Points.AddXY(waktu.ToString("HH:mm"), co);
                            chartGas.Series["SO2"].Points.AddXY(waktu.ToString("HH:mm"), so2);
                            chartGas.Series["HC"].Points.AddXY(waktu.ToString("HH:mm"), hc);
                        }
                    }
                }
                else
                {
                    chartGas.Series[0].Points.Clear();
                    chartGas.Series[1].Points.Clear();
                    chartGas.Series[2].Points.Clear();
                    chartGas.Series[3].Points.Clear();
                    chartGas.Series[4].Points.Clear();
                    chartWeather.Series[0].Points.Clear();
                    chartWeather.Series[1].Points.Clear();
                    chartWeather.Series[2].Points.Clear();
                }
                dgDataReport.DataSource = dtReport;
            }
            catch (Exception es)
            {
                MessageBox.Show(es.Message);
            }
        }

        private void pwmManual()
        {
            panelTrack.Enabled = true;
            panelTrack.BackColor = Color.FromArgb(23, 38, 88);
            panelPwmOtomatis.Enabled = false;
            panelPwmOtomatis.BackColor = Color.Gray;
            label23.BackColor = Color.FromArgb(44, 69, 157);
            label24.BackColor = Color.LightGray;
            btnSavePwmOtomatis.BackColor = Color.LightGray;
            btnMin.BackColor = Color.FromArgb(44, 69, 157);
            btnMax.BackColor = Color.FromArgb(44, 69, 157);
        }

        private void pwmOtomatis()
        {
            panelTrack.Enabled = false;
            panelTrack.BackColor = Color.Gray;
            panelPwmOtomatis.Enabled = true;
            panelPwmOtomatis.BackColor = Color.FromArgb(23, 38, 88);
            label23.BackColor = Color.LightGray;
            label24.BackColor = Color.FromArgb(44, 69, 157);
            btnSavePwmOtomatis.BackColor = Color.FromArgb(44, 69, 157);
            btnMin.BackColor = Color.LightGray;
            btnMax.BackColor = Color.LightGray;
        }


        private void rbManual_Click(object sender, EventArgs e)
        {
            if (rbManual.Checked == true)
            {
                pwmManual();
            }
        }

        private void rbOtomatis_Click(object sender, EventArgs e)
        {
            if (rbOtomatis.Checked == true)
            {
                pwmOtomatis();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                txtPwmTrack.Text = "1";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            try
            {
                txtPwmTrack.Text = "100";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCheckAll_Click(object sender, EventArgs e)
        {
            checkBoxSuhu.Checked = true;
            checkBoxKelembaban.Checked = true;
            checkBoxTekanan.Checked = true;
            checkBoxArahAngin.Checked = true;
            checkBoxKecepatanAngin.Checked = true;
            checkBoxNO2.Checked = true;
            checkBoxO3.Checked = true;
            checkBoxCO.Checked = true;
            checkBoxSO2.Checked = true;
            checkBoxHC.Checked = true;
            checkBoxPM25.Checked = true;
            checkBoxSolar.Checked = true;
            checkBoxPM10.Checked = true;
        }

        private void btnUncheckAll_Click(object sender, EventArgs e)
        {
            checkBoxSuhu.Checked = false;
            checkBoxKelembaban.Checked = false;
            checkBoxTekanan.Checked = false;
            checkBoxArahAngin.Checked = false;
            checkBoxKecepatanAngin.Checked = false;
            checkBoxNO2.Checked = false;
            checkBoxO3.Checked = false;
            checkBoxCO.Checked = false;
            checkBoxSO2.Checked = false;
            checkBoxHC.Checked = false;
            checkBoxPM25.Checked = false;
            checkBoxPM10.Checked = false;
            checkBoxSolar.Checked = false;
        }

        private void btnSavePwmOtomatis_Click(object sender, EventArgs e)
        {
            if (txtInletUdara.Text != "" || txtInputSensor1.Text != "" || txtPwmAuto.Text != "")
            {
                Properties.Settings.Default.inletUdara = long.Parse(txtInletUdara.Text);
                Properties.Settings.Default.inputSensor = long.Parse(txtInputSensor1.Text);
                Properties.Settings.Default.pwmOtomatis = long.Parse(txtPwmAuto.Text);
                Properties.Settings.Default.Save();
                insertLog("Mengubah pengaturan pwm otomatis");
                MessageBox.Show("Update pwm berhasil");
                return;
            }
            MessageBox.Show("Lengkapi semua data", "Field empty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void txtInputSensor_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtInletUdara_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtPwmAuto_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        string downloadFileUrl, versiApp, bugFixes;
        private void btnSendRequest_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPortUtama.IsOpen)
                {
                    if (serialPortUtama.BytesToWrite < 1)
                    {
                        serialPortUtama.Write("REQ,*");
                    }
                }
            }
            catch { return; }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            //chart1.Hide();
            _chartGasses.Hide();
            chart2.Show();
            chart3.Hide();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            //chart1.Show();
            _chartGasses.Show();
            chart2.Hide();
            chart3.Hide();
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            //chart1.Hide();
            _chartGasses.Hide();
            chart2.Hide();
            chart3.Show();
        }


        bool clicked = false;
        private void colorSlider1_Scroll(object sender, ScrollEventArgs e)
        {
            txtPwmTrack.Text = trackPwm.Value.ToString();
            if (clicked)
                return;
            Properties.Settings.Default.pwmTrack = trackPwm.Value;
            Properties.Settings.Default.Save();
            if (serialPortUtama.IsOpen)
            {
                serialPortUtama.Write($"setPWM,{trackPwm.Value},*");
            }
            Console.WriteLine(trackPwm.Value);
        }


        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtBatteryThreshold.Text == "")
                {
                    txtBatteryThreshold.Text = "1";
                }
                if (txtBatteryThreshold.Text != "")
                {
                    if (Convert.ToInt32(txtBatteryThreshold.Text) > 100)
                    {
                        txtBatteryThreshold.Text = "100";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        string datas;
        bool bacaMap = false;
        private delegate void ShowLatLng(string gpgga);
        private void serialPortGps_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (bacaMap == true)
            {
                try
                {
                    datas = serialPortGps.ReadLine();
                    //Console.WriteLine(datas);
                    string[] halo = datas.Split('\n');
                    string[] gpgga = halo[0].ToString().Split(',');
                    if (gpgga[0] == "$GPGGA")
                    {
                        this.BeginInvoke(new ShowLatLng(_serialPortGps_DataReceived), new object[] { halo[0].ToString() });
                        bacaMap = false;
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                }
            }
        }

        private async void _serialPortGps_DataReceived(string gpgga)
        {
            try
            {
                Console.WriteLine("Ini gpgga" + datas);
                labelRawGps.Text = datas;
                string[] data = gpgga.Split(',');
                if (data[0] != "$GPGGA")
                {
                    return;
                }
                double latitude = Convert.ToDouble(data[2]);
                double longitude = Convert.ToDouble(data[4]);
                int derajatLatitudeReal = (int)(latitude / 100);
                double detikLatitudeReal = latitude - derajatLatitudeReal * 100;

                double latitudeReal = derajatLatitudeReal + (detikLatitudeReal / 60);

                int derajatLongitudeReal = (int)(longitude / 100);
                double detikLongitudeReal = longitude - derajatLongitudeReal * 100;
                double longitudeReal = derajatLongitudeReal + (detikLongitudeReal / 60);

                if (data[3] == "S")
                {
                    latitudeResult = String.Format("{0:0.0000000}", Convert.ToDouble("-" + latitudeReal.ToString()));
                }
                else
                {
                    latitudeResult = String.Format("{0:0.0000000}", Convert.ToDouble(latitudeReal.ToString()));
                }
                if (data[5] == "W")
                {
                    longituderesult = String.Format("{0:0.0000000}", Convert.ToDouble("-" + longitudeReal.ToString()));
                }
                else
                {
                    longituderesult = String.Format("{0:0.0000000}", Convert.ToDouble(longitudeReal.ToString()));
                }

                Properties.Settings.Default.lat = latitudeResult;
                Properties.Settings.Default.lng = longituderesult;
                Properties.Settings.Default.Save();
                labelLatitude.Text = latitudeResult;
                labelLongitude.Text = longituderesult;
                if (done == true)
                {
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            rawLat h = new rawLat() { device_id = LoginStatus.idNode, latitude = latitudeResult, longitude = longituderesult };
                            client.DefaultRequestHeaders.Accept.Clear();
                            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", LoginStatus.token);
                            string url = $"{Properties.Settings.Default.apiUrl}/api/device/sendlocation";
                            var json = JsonConvert.SerializeObject(h);
                            var asd = new StringContent(json, Encoding.UTF8, "application/json");
                            using (var response = await client.PostAsync(url, asd))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    var responseMessage = await response.Content.ReadAsStringAsync();
                                    Console.WriteLine("GPS suc" + responseMessage);
                                    done = false;
                                }
                                else
                                {
                                    var responseMessage = await response.Content.ReadAsStringAsync();
                                    Console.WriteLine("GPS err" + responseMessage);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Write("GPs err" + ex.Message);
                    }
                }
                timerRequestMap.Interval = 60000;
            }
            catch
            {
                labelLatitude.Text = (latitudeResult.Trim() == "" ? Properties.Settings.Default.lat : latitudeResult);
                labelLongitude.Text = (longituderesult.Trim() == "" ? Properties.Settings.Default.lng : longituderesult);
                return;
            }
        }

        bool done = true;

        class rawLat
        {
            public string device_id { get; set; }
            public string latitude { get; set; }
            public string longitude { get; set; }
        }
        string latitudeResult = "";
        string longituderesult = "";

        private void timerRequestMap_Tick(object sender, EventArgs e)
        {
            bacaMap = true;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                if (cbxPortGps.Text != "")
                {
                    Properties.Settings.Default.lat = numericUpDown1.Value.ToString();
                    Properties.Settings.Default.lng = numericUpDown2.Value.ToString();
                    Properties.Settings.Default.Save();
                    Properties.Settings.Default.Save();
                    label28.Text = $"Port yang digunakan = {Properties.Settings.Default.portGps}";
                    insertLog("Mengubah pengaturan GPS");
                    MessageBox.Show("Update successfully!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                MessageBox.Show("Port tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        //private void button4_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (serialPortUtama.IsOpen)
        //        {
        //            MessageBox.Show("Stop mesin untuk melakukan kalibrasi", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            return;
        //        }
        //        serialPortCalibration.PortName = Properties.Settings.Default.portAlat;
        //        serialPortCalibration.BaudRate = 9600;
        //        serialPortCalibration.WriteTimeout = 1000;
        //        serialPortCalibration.ReadTimeout = 1000;
        //        serialPortCalibration.Open();
        //        serialPortCalibration.DataReceived += serialPortCalibration_DataReceived;
        //        serialPortCalibration.Write("debug,1,*");

        //        timerCalibration.Start();
        //        labelStart.Text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
        //        textBox3.Enabled = false;
        //        numericUpDown23.Enabled = false;
        //        button4.Enabled = false;
        //    }
        //    catch (Exception ez)
        //    {
        //        MessageBox.Show(ez.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    }
        //}

        int count = 0;
        //private void timerCalibration_Tick(object sender, EventArgs e)
        //{
        //    timerCalibration.Interval = 1000;
        //    if (count > numericUpDown23.Value - 1)
        //    {
        //        if (serialPortCalibration.IsOpen)
        //        {
        //            //serialPortCalibration.DataReceived -= serialPortCalibration_DataReceived;
        //            serialPortCalibration.Write("debug,0,*");
        //            serialPortCalibration.Close();
        //        }
        //        timerCalibration.Stop();
        //        textBox3.Enabled = true;
        //        numericUpDown23.Enabled = true;
        //        button4.Enabled = true;
        //        count = 0; 
        //        return;
        //    }
        //    count++;
        //    //label47.Text = $"Remaining   {numericUpDown23.Value - count}";
        //}

        private void trackPwm_MouseDown(object sender, MouseEventArgs e)
        {
            clicked = true;
        }

        private void trackPwm_MouseUp(object sender, MouseEventArgs e)
        {
            if (!clicked)
                return;
            clicked = false;
        }

        private delegate void SetText(string dataCalibration);
        private void serialPortCalibration_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = serialPortCalibration.ReadLine();
                this.BeginInvoke(new SetText(KalibrasiGas), new object[] { data });
            }
            catch
            {

                return;
            }
        }

        private void KalibrasiGas(string dataCalibration)
        {
            try
            {
                string[] datas = dataCalibration.Split(',');
                try
                {
                    Convert.ToDouble(datas[0]);
                }
                catch
                {
                    return;
                }
                //if (radioButton1.Checked)
                //{
                //    Properties.Settings.Default.offsetNo2 = 0 - Convert.ToDouble(datas[0]);
                //    Properties.Settings.Default.offsetSo2 = 0 - Convert.ToDouble(datas[1]);
                //    Properties.Settings.Default.offsetO3 = 0 - Convert.ToDouble(datas[2]);
                //    Properties.Settings.Default.offsetCo = 0 - Convert.ToDouble(datas[3]);
                //    Properties.Settings.Default.offsetNMHC = 0 - Convert.ToDouble(datas[4]);
                //}

                insertData($"INSERT INTO tbl_zero_calibration(NO2,SO2,O3,CO,NMHC,NO2_OFFSET,SO2_OFFSET,O3_OFFSET,CO_OFFSET,NMHC_OFFSET, waktu) values('{datas[0]}', '{datas[1]}', '{datas[2]}', '{datas[3]}', '{datas[4]}', '{Convert.ToDouble(datas[0]) + Properties.Settings.Default.offsetNo2}', '{Convert.ToDouble(datas[1]) + Properties.Settings.Default.offsetSo2}', '{Convert.ToDouble(datas[2]) + Properties.Settings.Default.offsetO3}', '{Convert.ToDouble(datas[3]) + Properties.Settings.Default.offsetCo}', '{Convert.ToDouble(datas[4]) + Properties.Settings.Default.offsetNMHC}', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}')");
                //labelDuration.Text = $"{numericUpDown23.Value}";
                //showCalibration(datas);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                Properties.Settings.Default.bawahSuhu = float.Parse(bawahSuhu.Value.ToString());
                Properties.Settings.Default.atashSuhu = float.Parse(atasKelembaban.Value.ToString());
                Properties.Settings.Default.bawahKelembaban = float.Parse(bawahKelembaban.Value.ToString());
                Properties.Settings.Default.atasKelembaban = float.Parse(atasKelembaban.Value.ToString());
                Properties.Settings.Default.bawahTekanan = float.Parse(bawahTekanan.Value.ToString());
                Properties.Settings.Default.atasTekanan = float.Parse(atastekanan.Value.ToString());
                Properties.Settings.Default.bawahKecepatan = float.Parse(bawahKecepatan.Value.ToString());
                Properties.Settings.Default.atasKecepatan = float.Parse(atasKecepatan.Value.ToString());
                Properties.Settings.Default.Save();
                MessageBox.Show("Data updated", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        void ShowPPM()
        {
            ppmNO2.Visible = true;
            ppmSO2.Visible = true;
            ppmO3.Visible = true;
            ppmCO.Visible = true;
            ppmHC.Visible = true;
        }
        void ShowMikro()
        {
            ppmNO2.Visible = false;
            ppmSO2.Visible = false;
            ppmO3.Visible = false;
            ppmCO.Visible = false;
            ppmHC.Visible = false;
        }
        private void button9_Click(object sender, EventArgs e)
        {
            if (label116.Text == "µg/m3")
            {
                label116.Text = "ppm";
                _chartGasses.chart2.Series[0].Points.Clear();
                _chartGasses.chart2.Series[1].Points.Clear();
                _chartGasses.chart2.Series[2].Points.Clear();
                _chartGasses.chart2.Series[3].Points.Clear();
                _chartGasses.chart2.Series[4].Points.Clear();
                _chartGasses.chart2.ChartAreas[0].AxisY.LabelStyle.Format = "{0:0.000} ppm";
                ShowPPM();
            }
            else if (label116.Text == "ppm")
            {
                label116.Text = "µg/m3";
                _chartGasses.chart2.Series[0].Points.Clear();
                _chartGasses.chart2.Series[1].Points.Clear();
                _chartGasses.chart2.Series[2].Points.Clear();
                _chartGasses.chart2.Series[3].Points.Clear();
                _chartGasses.chart2.Series[4].Points.Clear();
                _chartGasses.chart2.ChartAreas[0].AxisY.LabelStyle.Format = "{0} µg/m3";
                ShowMikro();
            }
        }

        private void button10_Click_1(object sender, EventArgs e)
        {
            chartGas.Show();
            chartWeather.Hide();
        }
        private void button11_Click(object sender, EventArgs e)
        {
            chartGas.Hide();
            chartWeather.Show();
        }
        int waktu = 10;
        private void timerWaktuTunggu_Tick(object sender, EventArgs e)
        {
            count++;
            progressBar2.Value = Convert.ToInt32(((float)count / waktu) * 100);
            Console.WriteLine(count);
            Console.WriteLine(((float)count / waktu) * 100);
            if (count == waktu)
            {
                btnConnect.Enabled = true;
                btnConnect.BackColor = Color.FromArgb(0, 192, 0);
                timerWaktuTunggu.Stop();
                labelSensorMenyala.Show();
                labelTunggu.Hide();
                progressBar2.Hide();
                return;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (serialPortUtama.IsOpen || serialPortGps.IsOpen)
            if (serialPortUtama.IsOpen || serialPortGps.IsOpen)
            {
                MessageBox.Show("Matikan engine terlebih dahulu", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            waktuAkhir = DateTime.Now;
            _login.Show();
            this.Close();
        }

        public void ShutDownSystem()
        {
            ManagementBaseObject mboShutdown = null;
            ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();

            // You can't shutdown without security privileges
            mcWin32.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject mboShutdownParams =
                     mcWin32.GetMethodParameters("Win32Shutdown");

            // Flag 1 means we want to shut down the system. Use "2" to reboot.
            mboShutdownParams["Flags"] = "1";
            mboShutdownParams["Reserved"] = "0";
            foreach (ManagementObject manObj in mcWin32.GetInstances())
            {
                mboShutdown = manObj.InvokeMethod("Win32Shutdown",
                                               mboShutdownParams, null);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (serialPortUtama.IsOpen || serialPortGps.IsOpen)
            {
                MessageBox.Show("Matikan engine terlebih dahulu", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Apakah anda ingin mematikan perangkat", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
            {
                return;
            }
            waktuAkhir = DateTime.Now;
            if (!serialPortUtama.IsOpen)
            {
                serialPortUtama.PortName = Properties.Settings.Default.portAlat;
                serialPortUtama.BaudRate = 9600;
                serialPortUtama.Open();
                //serialPortUtama.Close();
            }
            Thread.Sleep(4000);
            serialPortUtama.Write("CHARGE,0,*");
            Thread.Sleep(500);
            serialPortUtama.Write("setPWM,0,*");
            _login.Close();
            this.Close();
            ShutDownSystem();
        }
        string konsumsi_batery;
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (new StackTrace().GetFrames().Any(x => x.GetMethod().Name == "Close"))
            {
            }
            else
            {
                if (_login != null)
                {
                    _login.Close();
                }
                waktuAkhir = DateTime.Now;
            }
            onClosing();
        }
        public void onClosing()
        {
            TimeSpan span = waktuAkhir.Subtract(Convert.ToDateTime(LoginStatus.loginTime));
            konsumsi_batery = String.Format("{0:0.00}", ((float)(voltaseAwal - voltaseAkhir) / (span.TotalSeconds / 3600)));
            insertData($"UPDATE log_perangkat set logout_time = '{waktuAkhir.ToString("yyyy-MM-dd HH:mm:ss")}', konsumsi_baterai = '{konsumsi_batery} V/Jam' , durasi_penggunaan = '{String.Format("{0:0.00}", (double)(span.TotalMinutes / 60))} Jam' WHERE login_time = '{LoginStatus.loginTime}'; UPDATE tbl_perangkat SET portAlat = '{Properties.Settings.Default.portAlat}', portGps = '{Properties.Settings.Default.portGps}', lat = '{Properties.Settings.Default.lat}', lng = '{Properties.Settings.Default.lng}', pwmTrack = '{Properties.Settings.Default.pwmTrack}', pwmAuto = '{Properties.Settings.Default.pwmOtomatis}', timerCharge = '{Properties.Settings.Default.timerCharge}' WHERE id = '1';");
            var task = Task.Run(async () =>
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        raw h = new raw() { device_id = LoginStatus.idNode, baterai = $"{konsumsi_batery}"/* Convert.ToString((voltaseAkhir - voltaseAwal) / ((float)(span.Seconds / 3600f))) */};
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", LoginStatus.token);
                        string url = $"{Properties.Settings.Default.apiUrl}/api/logout";
                        var json = JsonConvert.SerializeObject(h);
                        var data = new StringContent(json, Encoding.UTF8, "application/json");
                        using (var response = await client.PostAsync(url, data))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                var responseMessage = await response.Content.ReadAsStringAsync();
                                Console.WriteLine(responseMessage);
                            }
                            else
                            {
                                var responseMessage = await response.Content.ReadAsStringAsync();
                                Console.WriteLine(responseMessage);
                            }
                        }
                    }
                }
                catch 
                {
                    return;
                }
            }); 
            task.Wait();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            //if (serialPortUtama.IsOpen)
            //{
            //    setCharge();
            //}
            //else
            //{
            //    serialPortUtama.Open();
            //    setCharge();
            //    serialPortUtama.Close();
            //    if (!timerWaktuTunggu.Enabled)
            //    {
            //        btnConnect.Enabled = true;
            //    }
            //}
        }


        void setCharge()
        {
            if (label15.Text == "OFF")
            {
                if (timerWaktuCharge.Enabled == false)
                {
                    serialPortUtama.Write("CHARGE,1,*");
                    label15.Text = "ON";
                }
            }
            else if(label15.Text == "ON")
            {
                serialPortUtama.Write("CHARGE,0,*");
                label15.Text = "OFF";
            }
        }
        int countCharge = 0;
        private void timerWaktuCharge_Tick(object sender, EventArgs e)
        {
            countCharge++;
            progressBar2.Value = Convert.ToInt32(((double)countCharge / (Properties.Settings.Default.timerCharge * 60 ) * 100));
            if ((Properties.Settings.Default.timerCharge * 60) == countCharge)
            {
                timerWaktuCharge.Stop();
                popUpBaterai popUp = new popUpBaterai("fullcharge");
                popUp.Show();
                labelTunggu.Hide();
                progressBar2.Hide();
                labelSensorMenyala.Show();
                countCharge = 0;
                btnDisconnect.Enabled = true;
                serialPortUtama.Write($"setPWM,{Properties.Settings.Default.pwmTrack},*");
                label15.Text = "ON"; 
                if (serialPortUtama.IsOpen)
                {
                    serialPortUtama.Write("CHARGE,0,*");
                    timerRequest.Start();
                }
                return;
            }

        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (numericUpDown3.Value >= 1 && numericUpDown3.Value <= 60)
                {
                    Properties.Settings.Default.interval = Convert.ToInt32(numericUpDown3.Value);
                    Properties.Settings.Default.Save();
                    insertLog("Melakukan pengaturan interval");
                    MessageBox.Show("Interval updated", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Interval pengambilan data minimal 1 menit dan maksimal 60 menit", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.timerCharge = Convert.ToInt32(numericUpDown4.Value);
            Properties.Settings.Default.reset = (cbxReset.SelectedIndex == 0 ? false : true);
            Properties.Settings.Default.Save();
            insertLog("Mengubah pengaturan timer waktu charging");
            MessageBox.Show("Data berhasil disimpan");
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (serialPortUtama.IsOpen && label15.Text != "ON")
            {
                serialPortUtama.Write("reset,*");
                Thread.Sleep(1000);
                serialPortUtama.Write($"setPWM,{Properties.Settings.Default.pwmTrack},*");
            }
        }

        private void btnSimpanGPS_Click(object sender, EventArgs e)
        {
            try
            {
                if (cbxPortGps.Text != "")
                {
                    Properties.Settings.Default.portGps = cbxPortGps.SelectedItem.ToString();
                    Properties.Settings.Default.lat = numericUpDown1.Value.ToString();
                    Properties.Settings.Default.lng = numericUpDown2.Value.ToString();
                    Properties.Settings.Default.Save();
                    label28.Text = $"Port yang digunakan : {Properties.Settings.Default.portGps}";
                    insertLog("Mengubah pengaturan GPS");
                    MessageBox.Show("Update successfully!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                MessageBox.Show("Port tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
        }

    
        private void timerRequest_Tick(object sender, EventArgs e)
        {
            timerRequest.Interval = Properties.Settings.Default.interval * 1000 * 60; 
            if (serialPortUtama.IsOpen)
            {
                if (serialPortUtama.BytesToWrite < 1)
                {
                    serialPortUtama.Write("REQ,*");
                }
            }
        }
        private async void btnCheckUpdates_Click(object sender, EventArgs e)
        {
            try
            {
                bool adaUpdate = false;
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri($"{Properties.Settings.Default.apiUrl}");
                    Console.WriteLine(Properties.Settings.Default.apiUrl);
                    using (HttpResponseMessage response = await client.GetAsync("api/ver"))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string data = await response.Content.ReadAsStringAsync();
                            if (data != null)
                            {
                                Console.WriteLine(data);
                                JObject keys = JObject.Parse(data);
                                string[] ver = Convert.ToString(keys["app_ver"]["version"]).Split('.');
                                downloadFileUrl = keys["link"].ToString();
                                versiApp = keys["app_ver"]["version"].ToString();
                                Properties.Settings.Default.aboutUs = keys["app_ver"]["description"].ToString();
                                Properties.Settings.Default.Save();
                                labelAbout.Text = Properties.Settings.Default.aboutUs;
                                bugFixes = keys["fixes"].ToString();
                                string[] version = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
                                int major = int.Parse(ver[0]);
                                int minor = int.Parse(ver[1]);
                                int build = int.Parse(ver[2]);
                                if (major > int.Parse(version[0]))
                                {
                                    adaUpdate = true;
                                }
                                else if (minor > int.Parse(version[1]))
                                {
                                    adaUpdate = true;
                                }
                                else if (build > int.Parse(version[2]))
                                {
                                    adaUpdate = true;
                                }
                                else
                                {
                                    adaUpdate = false;
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Server error", "Server error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                if (adaUpdate)
                {
                    if (MessageBox.Show("Update tersedia\nApakah anda ingin download", "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        new DownloadPopup(downloadFileUrl, versiApp, bugFixes).ShowDialog();
                    }
                }
                else
                {
                    MessageBox.Show("Tidak ada update", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}