using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AQMS
{
    public partial class LiteVersion : Form
    {
        SQLiteConnection connection = new SQLiteConnection(@"Data Source=D:\Trusur\AQMS\aqms.db;Version=3;FailIfMissing=True;New=false;");
        SQLiteCommand command;
        int countDataReceived = 0;
        int ChartLimit = 50;
        public LiteVersion()
        {
            InitializeComponent();
        }
        private delegate void _serialPortUtama_DataReceived(string datas);

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                string datas = serialPortUtama.ReadLine();
                this.BeginInvoke(new _serialPortUtama_DataReceived(serial_DataReceived), new object[] { datas });
            }
            catch
            {
                return;
            }
        }
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
                }
                else
                {
                    finalData[0] = 0;
                    finalData[1] = 0;
                    finalData[2] = 0;
                    finalData[3] = 0;
                    finalData[4] = 0;
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
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return finalData;
            }
        }
        int pwm;
        double voltaseAwal, voltaseAkhir;

        private async Task<double[]> conversiPpmKeMicrogram(double[] nilai)
        {
            double[] hasil = new double[5];
            await Task.Run(() =>
            {
                hasil[0] = 0.0409 * (Convert.ToDouble(nilai[0]) * 1000) * 46.01;
                hasil[1] = 0.0409 * (Convert.ToDouble(nilai[1]) * 1000) * 64.07;
                hasil[2] = 0.0409 * (Convert.ToDouble(nilai[2]) * 1000) * 48;
                hasil[3] = 0.0409 * (Convert.ToDouble(nilai[3]) * 1000) * 28.01;
                hasil[4] = 0.0409 * (Convert.ToDouble(nilai[4]) * 1000) * 13.87;
            });
            return hasil;
        }
        private async void serial_DataReceived(string datas)
        {
            try
            {
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
                    Convert.ToDouble(dataIn[1]);
                }
                catch { return; }
                #endregion
                string sensorId = "NODE202297001";
                pwm = Convert.ToInt32(Convert.ToDouble(dataIn[5]));
                float suhuSensor = float.Parse(dataIn[15]);
                float kelembabanSensor = float.Parse(dataIn[16]);
                float tekananSensor = float.Parse(dataIn[17]);
                bool baca_membrasens = (suhuSensor > Properties.Settings.Default.bawahSuhu && suhuSensor < Properties.Settings.Default.atashSuhu) && (kelembabanSensor > Properties.Settings.Default.bawahKelembaban && kelembabanSensor < Properties.Settings.Default.atasKelembaban);

                string teganganIn = dataIn[6];
                string arusIn = dataIn[7];
                string dayaIn = dataIn[8];
                if (countDataReceived == 0)
                {
                    voltaseAwal = float.Parse(teganganIn);
                }
                voltaseAkhir = float.Parse(teganganIn);
                #region Conversi
                Task<double[]> tambahOffset = cleansingData(dataIn, suhuSensor, kelembabanSensor);
                tambahOffset.Wait();
                double[] finalData = await tambahOffset;
                Task<double[]> task = conversiPpmKeMicrogram(finalData);
                double[] ugM3 = await task;
                #endregion


                int no2 = Convert.ToInt32(ugM3[0]);
                int so2 = Convert.ToInt32(ugM3[1]);
                int o3 = Convert.ToInt32(ugM3[2]);
                int co = Convert.ToInt32(ugM3[3]);
                int hc = Convert.ToInt32(ugM3[4]);

                string arahAnginIn = finalData[5].ToString();
                double kecepatanAnginIn = Convert.ToDouble(finalData[6]) * 1.609344;
                double kelembabanIn = Convert.ToDouble(finalData[7]);
                double suhuIn = Convert.ToDouble(finalData[8]);
                string solarRadiasi = dataIn[14];
                double tekananUdaraIn = 0;
                Console.WriteLine("GPS" + serialPortGps.IsOpen);
                string ekor = dataIn[dataIn.Length - 1];

                double PM10 = 0;
                double PM25 = 0;

                DateTime time = DateTime.Now;
                string waktu = $"{time.ToString("yyyy-MM-dd")} {time.ToString("HH")}:{time.ToString("mm")}:{time.ToString("ss")}";
                #region Sql
                // ini adalah data yang sudah bersih
                insertData($"INSERT INTO tbl_data(nama_petugas, suhu, kelembaban, tekanan, arah_angin, kecepatan_angin, no2, o3, co, so2, hc, pm2_5, pm10, waktu) values('{LoginStatus.namaUser}','{suhuIn}', '{kelembabanIn}', '{tekananUdaraIn}', '{arahAnginIn}', '{String.Format("{0:0.00}", kecepatanAnginIn)}', '{no2}', '{o3}', '{co}', '{so2}', '{hc}', '{PM25}', '{PM10}', '{waktu}')");
                // Data ini adalah data mentah yang belum ditambahkan offset
                insertData($"INSERT INTO raw_data(raw_data,nama_petugas,created_at,email,no2,so2,o3,co,hc) values('{datas}','{LoginStatus.namaUser}', '{time.ToString("yyyy-MM-dd HH:mm:ss")}','{LoginStatus.email}', '{dataIn[0]}', '{dataIn[1]}', '{dataIn[2]}', '{dataIn[3]}', '{dataIn[4]}')");
                #endregion

                #region Api Post
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        rawDataPost raw = new rawDataPost();
                        raw.device_id = sensorId;
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
                chart3.Series[0].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(PM25));

                chart2.Series[3].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(tekananUdaraIn));
                if (baca_membrasens == true)
                {
                    chart1.Series[0].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(no2));
                    chart1.Series[1].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(so2));
                    chart1.Series[2].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(o3));
                    chart1.Series[3].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(co));
                    chart1.Series[4].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(hc));

                    //chart1.Series[9].Points.AddXY(time.ToString("HH:mm:ss"), Convert.ToInt32(PM25));
                    if (chart1.Series[0].Points.Count > ChartLimit)
                    {
                        chart1.Series[0].Points.RemoveAt(0);
                        chart1.Series[1].Points.RemoveAt(0);
                        chart1.Series[2].Points.RemoveAt(0);
                        chart1.Series[3].Points.RemoveAt(0);
                        chart1.Series[4].Points.RemoveAt(0);
                    }
                }

                if (chart2.Series[0].Points.Count > ChartLimit) { chart2.Series[0].Points.RemoveAt(0); chart2.Series[1].Points.RemoveAt(0); chart2.Series[2].Points.RemoveAt(0); chart2.Series[3].Points.RemoveAt(0); }
                #endregion

                showPPM(dataIn[0], dataIn[1], dataIn[2], dataIn[3], dataIn[4]);
                //showCalibration(dataIn);
                showValue(suhuIn.ToString(), kelembabanIn.ToString(), String.Format("{0:0.00}", kecepatanAnginIn), tekananUdaraIn.ToString(), arahAnginIn, no2.ToString(), o3.ToString(), co.ToString(), so2.ToString(), hc.ToString(), PM25.ToString(), PM10.ToString(), teganganIn, arusIn, dayaIn, datas, teganganIn, solarRadiasi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void showPPM(string no2, string so2, string o3, string co, string hc)
        {
            ppmNO2.Text = no2 + " ppm";
            ppmSO2.Text = so2 + " ppm";
            ppmO3.Text = o3 + " ppm";
            ppmCO.Text = co + " ppm";
            ppmHC.Text = hc + " ppm";
        }

        private void showValue(string suhu, string kelembaban, string kecepatan_angin, string tekanan, string arahangin, string no2, string o3, string co, string so2, string hc, string pm25, string pm10, string tegangan, string arus, string daya, string dataFromSerialPort, string teganganIn, string solarRadiasi)
        {
            //Show metereology
            labelSuhu.Text = suhu + " °C";
            labelKelembaban.Text = kelembaban + "%";
            labelKecepatanAngin.Text = kecepatan_angin + "km/h";
            labelTekanan.Text = tekanan + " mbar";
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

            //labelRawData.Text = dataFromSerialPort.Trim();
            try
            {
                if (Convert.ToDouble(teganganIn) > (Convert.ToDouble(teganganIn) * (Properties.Settings.Default.batteryThreshold / 100)))
                {
                    panel44.BackColor = Color.Green;
                }
                else if (Convert.ToDouble(teganganIn) <= (Convert.ToDouble(teganganIn) * (Properties.Settings.Default.batteryThreshold / 100)))
                {
                    panel44.BackColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "asd");
            }
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

        private void btnUploadImage_Click(object sender, EventArgs e)
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

        private void btnSaveAppSetting_Click(object sender, EventArgs e)
        {
            if (txtNamaAplikasi.Text != "")
            {
                Properties.Settings.Default.namaAplikasi = txtNamaAplikasi.Text;
                Properties.Settings.Default.Save();
                namaAplikasi.Text = Properties.Settings.Default.namaAplikasi;
                insertLog("Mengubah gambar dan nama aplikasi");
                return;
            }
            MessageBox.Show("Nama aplikasi tidak boleh kosong", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        string downloadFileUrl, versiApp, bugFixes;

        private void LiteVersion_Load(object sender, EventArgs e)
        {
        }

        private void startEngine()
        {

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
                                JObject keys = JObject.Parse(data);
                                string[] ver = Convert.ToString(keys["app_ver"]["version"]).Split('.');
                                downloadFileUrl = keys["link"].ToString();
                                versiApp = keys["app_ver"]["version"].ToString();
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
