using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AQMS
{
    public partial class FrmLogin : Form
    {
        SQLiteConnection connection = new SQLiteConnection(@"Data Source=D:\Trusur\AQMS\aqms.db;Version=3;FailIfMissing=True;New=false;");
        SQLiteCommand command;
        TextBox text1;
        public FrmLogin()
        {
            InitializeComponent();
            if (Properties.Settings.Default.imgaePath != "")
            {
                logoApp.ImageLocation = Application.StartupPath + Properties.Settings.Default.imgaePath;
            }
            txtPassword.GotFocus += OnFocus;
            txtUsername.GotFocus += On1Focus;
            txtPassword.LostFocus += OnDefocus;
            txtUsername.LostFocus += On1Defocus;
        }
        private void OnFocus(object sender, EventArgs e)
        {
            var arrProcs = Process.GetProcessesByName("osk");
            if (arrProcs.Length != 0)
            {
                panel1.Location = new System.Drawing.Point(242, -150);
                text1 = (TextBox)sender;
                return;
            }
            else
            {
                Thread.Sleep(200);
                panel1.Location = new System.Drawing.Point(242, -150);
            }
        }
        private void On1Focus(object sender, EventArgs e)
        {
            var arrProcs = Process.GetProcessesByName("osk");
            if (arrProcs.Length != 0)
            {
                panel1.Location = new System.Drawing.Point(242, -150);
                return;
            }
            else
            {
                Thread.Sleep(200);
                panel1.Location = new System.Drawing.Point(242, -150);
            }
            text1 = (TextBox)sender;
        }

        private void OnDefocus(object sender, EventArgs e)
        {
            panel1.Location = new System.Drawing.Point(242, 55);
            text1 = null;
        }
        private void On1Defocus(object sender, EventArgs e)
        {
            panel1.Location = new System.Drawing.Point(242, 55);
            text1 = null;
        }

        class loginRaw
        {
            public string email { get; set; }
            public string password { get; set; }
            public string device_id { get; set; }
        };

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

        private void insertData(string v)
        {
            try
            {
                Koneksi();
                using (command = new SQLiteCommand(v, connection))
                {
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void FrmLogin_Load(object sender, EventArgs e)
        {
            timer1.Start();
            if (Properties.Settings.Default.apiUrl == "")
            {
                Properties.Settings.Default.apiUrl = "http://103.139.192.125";
                Properties.Settings.Default.Save();
            }
            DataTable dt = readDatabase($"SELECT * FROM tbl_perangkat WHERE id = '1'");
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                Properties.Settings.Default.portAlat = dr["portAlat"].ToString();
                Properties.Settings.Default.portGps = dr["portGps"].ToString();
                Properties.Settings.Default.lat = dr["lat"].ToString();
                Properties.Settings.Default.lng = dr["lng"].ToString();
                Properties.Settings.Default.pwmTrack = Convert.ToInt32(dr["pwmTrack"].ToString());
                Properties.Settings.Default.pwmOtomatis = Convert.ToInt32(dr["pwmAuto"].ToString());
                LoginStatus.idNode = dr["id_node"].ToString();
                Properties.Settings.Default.Save();
            }
        }

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

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = (txtPassword.UseSystemPasswordChar == true ? txtPassword.UseSystemPasswordChar = false : txtPassword.UseSystemPasswordChar = true);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var arrProcs = Process.GetProcessesByName("osk");
            if (arrProcs.Length == 0)
            {
                panel1.Location = new System.Drawing.Point(242, 55);
            }
        }

        private async void btnLogin_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (txtUsername.Text != "" && txtPassword.Text != "")
                {
                    btnLogin.Enabled = false;
                    loginRaw login = new loginRaw();
                    login.device_id = LoginStatus.idNode;
                    login.email = txtUsername.Text;
                    login.password = txtPassword.Text;
                    using (HttpClient client = new HttpClient())
                    {
                        string url = $"{Properties.Settings.Default.apiUrl}/api/login";
                        var json = JsonConvert.SerializeObject(login);
                        Console.WriteLine(json.ToString());
                        var data = new StringContent(json, Encoding.UTF8, "application/json");
                        //client.DefaultRequestHeaders.Accept.Clear();
                        //client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token");
                        using (var response = await client.PostAsync(url, data))
                        {
                            string message = await response.Content.ReadAsStringAsync();
                            if (response.IsSuccessStatusCode)
                            {
                                Console.WriteLine(message);
                                JObject jObject = JObject.Parse(message);
                                if (Convert.ToBoolean(jObject["success"].ToString()) == true)
                                {
                                    LoginStatus.namaUser = jObject["profile"]["name"].ToString();
                                    LoginStatus.email = jObject["profile"]["email"].ToString();
                                    LoginStatus.role = jObject["profile"]["role"].ToString();
                                    LoginStatus.token = jObject["token"].ToString();
                                    LoginStatus.loginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    if (LoginStatus.role != "superuser")
                                    {
                                        LoginStatus.tanggalAktivasi = jObject["device"]["activation_date"].ToString();
                                        LoginStatus.tanggalKedaluwarsa = jObject["device"]["expires_date"].ToString();
                                        TimeSpan span = Convert.ToDateTime(LoginStatus.tanggalKedaluwarsa).Subtract(Convert.ToDateTime(LoginStatus.tanggalAktivasi));
                                        LoginStatus.sisaTanggal = span.TotalDays.ToString();
                                    } else if (LoginStatus.role == "superuser")
                                    {
                                        MessageBox.Show("Tidak diizinkan login dengan akun tersebut", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        btnLogin.Enabled = true;
                                        return;
                                    }  
                                    string query = $"INSERT INTO tbl_log( nama_user, email, aktifitas, waktu) values('{LoginStatus.namaUser}','{LoginStatus.email}', 'Login', '{LoginStatus.loginTime}')";
                                    insertData(query);
                                    insertData($"INSERT INTO log_perangkat(nama_user,login_time,tanggal) values('{LoginStatus.namaUser}', '{LoginStatus.loginTime}', '{DateTime.Now.ToString("yyyy-MM-dd")}')");
                                    //getLogId($"select * from tbl_user_log ORDER BY id DESC limit 1");
                                    Form1 frm = new Form1(this);
                                    this.Hide();
                                    frm.ShowDialog();
                                    txtUsername.Text = "";
                                    txtPassword.Text = "";
                                    btnLogin.Enabled = true;
                                }
                                else if (Convert.ToBoolean(jObject["success"].ToString()) == false)
                                {
                                    MessageBox.Show(jObject["message"].ToString(), "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    btnLogin.Enabled = true;
                                }
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                            {
                                Console.WriteLine(message);
                                MessageBox.Show("Server error", "Server error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                btnLogin.Enabled = true;
                                return;
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                Console.WriteLine(message);
                                JObject keys = JObject.Parse(message);
                                MessageBox.Show($"Username atau password salah\n{keys["messages"].ToString()}", "Login fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                btnLogin.Enabled = true;
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Username dan password tidak boleh kosong", "Field empty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLogin.Enabled = true;
            }
        }

        private void pictureBox2_Click_1(object sender, EventArgs e)
        {

        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
