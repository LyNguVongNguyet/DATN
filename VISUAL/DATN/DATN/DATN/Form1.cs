using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;
using System.IO.Ports;
using System.Xml;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.ML;
using Emgu.Util;
using Emgu.CV.CvEnum;
using System.Threading;
using tesseract;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Reflection;
using WMPLib;
using System.Diagnostics;

namespace DATN
{
    public partial class Form1 : Form
    {
        #region Khởi tạo
        //Khai bao biến camera
        private FilterInfoCollection cameras;
        private VideoCaptureDevice cam;
        List<Image<Bgr, byte>> PlateImagesList = new List<Image<Bgr, byte>>();
        Image Plate_Draw;
        List<string> PlateTextList = new List<string>();
        List<Rectangle> listRect = new List<Rectangle>();
        PictureBox[] box = new PictureBox[12];
        public TesseractProcessor full_tesseract = null;
        public TesseractProcessor ch_tesseract = null;
        public TesseractProcessor num_tesseract = null;
        private string m_path = Application.StartupPath + @"\data\";
        private List<string> lstimages = new List<string>();
        private const string m_lang = "eng";
        //Khai báo biến RFID
        string InputDataUID = string.Empty;
        delegate void SetTextCallback(string text);
        //Khai báo biến vị trí
        string InputDataVitri = string.Empty;
        delegate void SetTextCallback1(string text2);
        public int max_id = 0;
        //Khai báo biến CSDL
        SqlConnection Connect = new SqlConnection(@"Data Source=LAPTOP-550DG89Q;Initial Catalog=BAI_DO_XE;Integrated Security=True");
        int counter = 3;
        #endregion
        public Form1()
        {
            InitializeComponent();
            #region Cập nhật thông tin
            serialPort2.DataReceived += new SerialDataReceivedEventHandler(UIDReceive);
            serialPort1.DataReceived += new SerialDataReceivedEventHandler(VitriReceive);
            //Thêm Thông Tin Camera
            cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo info in cameras)
            {
                comboBox1.Items.Add(info.Name); //thêm tên camera vào comboBox để lựa chọn
            }
            //Thêm Thông Tin Cổng COM
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comboBox2.Items.Add(port); //thêm tên cổng kết nối vào comboBox để lựa chọn
                comboBox3.Items.Add(port);
            }
            #endregion
            #region Nhận diện số từ ảnh dùng thư viện Tesseract
            full_tesseract = new TesseractProcessor();
            bool succeed = full_tesseract.Init(m_path, m_lang, 3);
            if (!succeed)
            {
                MessageBox.Show("Tesseract initialization failed. The application will exit.");
                Application.Exit();
            }
            full_tesseract.SetVariable("tessedit_char_whitelist", "ABCDEFHKLMNPRSTVXY1234567890").ToString();
            ch_tesseract = new TesseractProcessor();
            succeed = ch_tesseract.Init(m_path, m_lang, 3);
            if (!succeed)
            {
                MessageBox.Show("Tesseract initialization failed. The application will exit.");
                Application.Exit();
            }
            ch_tesseract.SetVariable("tessedit_char_whitelist", "ABCDEFHKLMNPRSTUVXY").ToString();
            num_tesseract = new TesseractProcessor();
            succeed = num_tesseract.Init(m_path, m_lang, 3);
            if (!succeed)
            {
                MessageBox.Show("Tesseract initialization failed. The application will exit.");
                Application.Exit();
            }
            num_tesseract.SetVariable("tessedit_char_whitelist", "1234567890").ToString();
            m_path = System.Environment.CurrentDirectory + "\\";
            for (int i = 0; i < box.Length; i++)
            {
                box[i] = new PictureBox();
            }
            #endregion            
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            CamOFF.Enabled = false;
            ArduinoOFF.Enabled = false;
            Arduino2OFF.Enabled = false;
        }
        #region Kết nối database
        private void START_Click(object sender, EventArgs e)
        {
            Connect.Open();
            LoadDataCARIN();
            LoadDataCAROUT();
            LoadDataID();
            START.Enabled = false;
        }
        private void STOP_Click(object sender, EventArgs e)
        {
            DialogResult Close = MessageBox.Show("XÁC NHẬN ĐÓNG CHƯƠNG TRÌNH ?", "Cảnh Báo", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (Close == DialogResult.OK)
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Write("1");
                }
                if (serialPort1.IsOpen) serialPort1.Close();
                if (serialPort2.IsOpen) serialPort2.Close();
                if (cam != null && cam.IsRunning) cam.Stop();
                Connect.Close();
                Application.Exit();
            }
        }
        #endregion
        #region Kết nối Camera
        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == "")
            {
                //Thông báo khi chưa chọn Camera mà nhưng đã ấn nút 'kết nối'
                MessageBox.Show("Vui Lòng Chọn Camera", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                //Quá trình kết nối camera
                cam = new VideoCaptureDevice(cameras[comboBox1.SelectedIndex].MonikerString);
                cam.NewFrame += Cam_NewFrame;
                cam.Start();
                MessageBox.Show("Kết Nối Camera Thành Công", "Thông Báo", MessageBoxButtons.OK);
                CamOFF.Enabled = true; //Cho phép tác động vào nút ngắt kết nối
                CamON.Enabled = false;
                comboBox1.Enabled = false;  //không cho phép thay đổi camera giữa quá trình kết nối
            }

        }
        private void Cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            pictureBox1.Image = bitmap; //hiển thị hình ảnh thu được từ camera lên pictureBox
        }
        //Ngắt kết nối camera
        private void button2_Click(object sender, EventArgs e)
        {
            if (cam != null && cam.IsRunning)
            {
                //Quá trình ngắt kết nối camera
                cam.Stop();
                MessageBox.Show("Đã Ngắt Kết Nối Camera", "Thông Báo", MessageBoxButtons.OK);
                pictureBox1.Image = null; //xóa hình ảnh từ pictureBox
                CamOFF.Enabled = false; //không cho phép ấn nút ngắt kết nối
                comboBox1.Enabled = true;//Cho phép tác động vào comboBox để thay đổi camera
                CamON.Enabled = true;
            }
        }
        #endregion
        #region Kết Nối Arduino 1
        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox2.Text == "")
            {
                //Thông báo khi chưa chọn cổng kết nối mà nhưng đã ấn nút 'kết nối'
                MessageBox.Show("Vui Lòng Chọn Cổng Kết Nối", "Cảnh Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                //Quá trình khi kết nối thành công
                serialPort2.PortName = comboBox2.Text;
                if (serialPort2.IsOpen)
                {
                    serialPort2.Close();
                }
                serialPort2.Open();
                MessageBox.Show("Kết Nối Arduino Thành Công", "Thông Báo", MessageBoxButtons.OK);
                comboBox2.Enabled = false; //không cho phép thay đổi cổng giữa quá trình kết nối
                ArduinoOFF.Enabled = true; //Cho phép tác động vào nút ngắt kết nối
                ArduinoON.Enabled = false;
            }
        }
        //Ngắt kết nối Arduino 1
        private void button4_Click(object sender, EventArgs e)
        {
            //Quá trình ngắt kết nối cổng COM
            serialPort2.Close();
            MessageBox.Show("Đã Ngắt Kết Nối Arduino", "Thông Báo", MessageBoxButtons.OK);
            comboBox2.Enabled = true; //Cho phép tác động vào comboBox để thay đổi cổng
            ArduinoOFF.Enabled = false;
            ArduinoON.Enabled = true;
        }
        #endregion
        #region  Kết Nối Arduino 2
        private void Arduino2ON_Click(object sender, EventArgs e)
        {
            if (comboBox3.Text == "")
            {
                //Thông báo khi chưa chọn cổng kết nối mà nhưng đã ấn nút 'kết nối'
                MessageBox.Show("Vui Lòng Chọn Cổng Kết Nối", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (comboBox3.Text == comboBox2.Text)
            {
                MessageBox.Show("Cổng đã được sử dụng.\nHãy chọn cổng khác!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                //Quá trình khi kết nối thành công
                serialPort1.PortName = comboBox3.Text;
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                }
                serialPort1.Open();
                serialPort1.Write("t");
                MessageBox.Show("Kết Nối Arduino Thành Công", "Thông Báo", MessageBoxButtons.OK);
                comboBox3.Enabled = false; //không cho phép thay đổi cổng giữa quá trình kết nối
                Arduino2ON.Enabled = false;
                Arduino2OFF.Enabled = true; //Cho phép tác động vào nút ngắt kết nối                
            }
        }
        ////Ngắt kết nối Arduino 2
        private void Arduino2OFF_Click(object sender, EventArgs e)
        {
            //Quá trình ngắt kết nối cổng COM
            serialPort1.Close();
            MessageBox.Show("Đã Ngắt Kết Nối Arduino", "Thông Báo", MessageBoxButtons.OK);
            comboBox3.Enabled = true; //Cho phép tác động vào comboBox để thay đổi cổng
            Arduino2OFF.Enabled = false;
            Arduino2ON.Enabled = true;
        }
        #endregion
        #region Nhận, xử lí thông tin vị trí để xe
        private void VitriReceive(object obj, SerialDataReceivedEventArgs e)
        {
            InputDataVitri = serialPort1.ReadExisting(); //Đọc dữ liệu nhận được từ cổng kết nối
            this.Invoke(new EventHandler(SetTextVitri));
        }
        private void SetTextVitri(object sender, EventArgs e)
        {
            textVT.Text += InputDataVitri.Trim();

        }
        #endregion
        #region Xử lí quẹt thẻ
        //Nhận, xử lí thông tin khi quẹt thẻ
        private void UIDReceive(object obj, SerialDataReceivedEventArgs e)
        {
            InputDataUID = serialPort2.ReadExisting(); //Đọc dữ liệu nhận được từ cổng kết nối
            this.Invoke(new EventHandler(SetTextUID));
        }

        private int find_max(List<int> all_ids)
        {
            int max_id1 = 0;
            for (int i = 0; i < all_ids.Count; i++)
            {
                if (all_ids[i] >= max_id1)
                {
                    max_id1 = all_ids[i];
                }
            }
            return max_id1;
        }
        private void SetTextUID(object sender, EventArgs e)
        {
            textBox1.Text += InputDataUID.Trim();
            string mathe = textBox1.Text;
            #endregion
        #region Kiểm tra thẻ hệ thống
            if (!string.IsNullOrEmpty(textBox1.Text))
            {                
                bool ktID = false;


                List<int> tat_ca_vi_tri = new List<int>();
                for (int i = 0; i < dataCARIN.Rows.Count; i++)
                {
                    tat_ca_vi_tri.Add(Int32.Parse(dataCARIN.Rows[i].Cells["Vị_trí"].Value?.ToString()));
                }
                for (int i = 0; i < dataID.Rows.Count; i++)
                {
                    // Console.WriteLine(dataCARIN.Rows[i].Cells["Vị_trí"].Value?.ToString());
                    if (textBox1.Text == dataID.Rows[i].Cells["ID"].Value?.ToString())
                    {
                        ktID = true;
                    }
                }
                //max_id = find_max(tat_ca_vi_tri) + 1;
                // textVT.Text = max_id.ToString();
               // Console.WriteLine(max_id);
                if (ktID == false)
                {
                    DialogResult tt = MessageBox.Show("Thẻ không có trong hệ thống\nThêm thẻ?", "Thông Báo", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (tt == DialogResult.Yes)
                    {
                        string sqlInsert = "INSERT INTO QUAN_LY_THE VALUES ('" + mathe +  "')";
                        SqlCommand cmd = new SqlCommand(sqlInsert, Connect);
                        cmd.ExecuteNonQuery();
                        textBox1.Text = null;
                        LoadDataID();
                    }
                    else
                    {
                        textBox1.Text = null;
                    }
                }
                else
                {
                    bool ktXE = false;
                    for (int i = 0; i < dataCARIN.Rows.Count; i++)
                    {
                        if (textBox1.Text == dataCARIN.Rows[i].Cells["Mã_Thẻ"].Value?.ToString())
                        {
                            ktXE = true;
                        }
                    }
                    #endregion
        #region Nhận diện biển số vào
                    if (ktXE == false)
                    {
                        textBox1.Text = null;
                        pictureBox2.Image = null;
                        pictureBox3.Image = null;
                        pictureBoxBSR.Image = null;
                        pictureBoxKTR.Image = null;
                        pictureBoxBSR.Image = null;
                        pictureBoxKTR.Image = null;
                        textVT.Text = null;
                        textIDOUT.Text = null;
                        textBSR.Text = null;
                        textTI2.Text = null;
                        textTO.Text = null;
                        textP.Text = null;
                        textT.Text = null;
                        textIDIN.Text = mathe;
                        //nhận diện biển số xe vào
                        serialPort1.Write("s");
                        Image xxx = pictureBox2.Image;
                        pictureBox2.Image = (Bitmap)pictureBox1.Image.Clone();
                        pictureBox1.Image.Save(@"D:\DATN_CARPARK\VISUAL\DATN\DATN\DATN\bin\Debug\picture1.jpg");
                        Image temp1;
                        string temp2, temp3;
                        // Reconize_in(@"D:\DATN_CARPARK\main\CSDL\CARIN\" + mathe + "_" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".Jpeg", out temp1, out temp2, out temp3);
                        // xxx = temp1;
                        string temp_content = detect_in("picture1.jpg");
                        if (temp_content == "")
                        {
                            textBSV.Text = "Không nhận dạng được";
                        }
                        else
                        {
                            textBSV.Text = Regex.Replace(temp_content, @"\s", "");
                        }
                        textTI.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"); // hiển thị thời gian scan thẻ (thời gian xe vào)
                        pictureBox2.Image.Save(@"D:\DATN_CARPARK\main\CSDL\CARIN\" + mathe + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".Jpeg");
                        pictureBoxBSV.Image.Save(@"D:\DATN_CARPARK\main\CSDL\BSV\" + mathe + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".Jpeg");
                        pictureBoxKTV.Image.Save(@"D:\DATN_CARPARK\main\CSDL\KTV\" + mathe + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".Jpeg");
                        string sqlispicture = "INSERT INTO Picture VALUES('" + mathe + "','" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + "')";
                        SqlCommand cmdispicture = new SqlCommand(sqlispicture, Connect);
                        cmdispicture.ExecuteNonQuery();

                        try
                        {
                            
                        }
                        catch (Exception ex)
                        {
                            
                        }
                        
                        #endregion
        #region Điểu khiển cho xe vào
                        DialogResult carin = MessageBox.Show("Bạn có muốn cho xe vào ?", "Thông Báo",MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                        if (carin == DialogResult.OK)
                        {
                            
                            int i = dataCARIN.Rows.Count;
                            if(i==5)
                            {
                                // textVT.Text = max_id.ToString();
                                string sqliscarin = "INSERT INTO QUAN_LY_XE_VAO VALUES('" + mathe + "','" + textVT.Text + "','" + textBSV.Text + "','" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "')";
                                SqlCommand cmdin = new SqlCommand(sqliscarin, Connect);
                                cmdin.ExecuteNonQuery();
                                LoadDataCARIN();
                            }
                            else
                            {
                                // textVT.Text = max_id.ToString();
                                serialPort1.Write("g"); //Điều khiển nâng xe lên
                                WindowsMediaPlayer sound = new WindowsMediaPlayer();
                                sound.URL = @"D:\DATN_CARPARK\main\Visual\MP3\warning.mp3";
                                sound.controls.play();
                                string sqliscarin = "INSERT INTO QUAN_LY_XE_VAO VALUES('" + mathe + "','" + textVT.Text + "','" + textBSV.Text + "','" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "')";
                                SqlCommand cmdin = new SqlCommand(sqliscarin, Connect);
                                cmdin.ExecuteNonQuery();
                                LoadDataCARIN();
                            }
                        }
                    }
                    #endregion
        #region Xử lí thông tin xe ra
                    else
                    {
                        textIDIN.Text = null;
                        textBox1.Text = null;
                        textIDOUT.Text = mathe;
                        textTO.Text = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"); // hiển thị thời gian scan thẻ (thời gian xe ra)
                        string sqlmott = "SELECT * FROM QUAN_LY_XE_VAO WHERE Mã_Thẻ = '" + mathe + "'";
                        SqlDataAdapter drmott = new SqlDataAdapter(sqlmott, Connect);
                        DataTable dtmott = new DataTable();
                        drmott.Fill(dtmott);
                        textIDIN.Text = dtmott.Rows[0][0].ToString();
                        textVT.Text   = dtmott.Rows[0][1].ToString();
                        textBSV.Text  = dtmott.Rows[0][2].ToString();
                        textTI2.Text  = dtmott.Rows[0][3].ToString();

                        string sqlmopic = "SELECT * FROM Picture WHERE Mã_Thẻ = '" + textIDIN.Text + "'";
                        SqlDataAdapter drmopic = new SqlDataAdapter(sqlmopic, Connect);
                        DataTable dtmopic = new DataTable();
                        drmopic.Fill(dtmopic);
                        pictureBox2.Image = new Bitmap(Image.FromFile(@"D:\DATN_CARPARK\main\CSDL\CARIN\" + mathe + dtmopic.Rows[0][1] + ".Jpeg"));
                        pictureBoxBSV.Image = new Bitmap(Image.FromFile(@"D:\DATN_CARPARK\main\CSDL\BSV\" + mathe + dtmopic.Rows[0][1] + ".Jpeg"));
                        pictureBoxKTV.Image = new Bitmap(Image.FromFile(@"D:\DATN_CARPARK\main\CSDL\KTV\" + mathe + dtmopic.Rows[0][1] + ".Jpeg"));
                        DateTime startdate = DateTime.Parse(textTI2.Text);
                        DateTime stopdate = DateTime.Parse(textTO.Text);
                        TimeSpan time = new TimeSpan();
                        time = stopdate - startdate;
                        if (time.Days.ToString() == "0" && time.Hours.ToString() == "0")
                        {
                            textT.Text = time.Minutes.ToString() + " phút ";
                        }
                        else if (time.Days.ToString() == "0")
                        {
                            textT.Text = time.Hours.ToString() + " giờ " + time.Minutes.ToString() + " phút ";
                        }
                        else
                        {
                            textT.Text = time.Days.ToString() + " ngày " + time.Hours.ToString() + " giờ " + time.Minutes.ToString() + " phút ";
                        }
                        double days = (int)time.TotalDays;
                        var hours = (int)time.TotalHours;
                        var minutes = (int)time.TotalMinutes;
                        if (minutes <= 0) minutes = 1;
                        double sotien = minutes * 500;
                        textP.Text = sotien.ToString();                        
                        serialPort1.Write(textVT.Text); //Gọi tầng chứa xe cần lấy                        
                        Thread.Sleep(15000);
                        #endregion
        #region Nhận diện biển số xe ra
                        pictureBox3.Image = (Bitmap)pictureBox1.Image.Clone();
                        pictureBox1.Image.Save(@"D:\DATN_CARPARK\VISUAL\DATN\DATN\DATN\bin\Debug\picture1.jpg");

                        Image temp4;
                        string temp5, temp6;
                        // Reconize_out(@"D:\DATN_CARPARK\main\CSDL\CAROUT\" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".jpg", out temp4, out temp5, out temp6);
                        // Image yyy = temp4;
                        string temp_content = detect_out("picture1.jpg");

                        if (temp_content == "")
                        {
                            textBSR.Text = "Không nhận dạng được";
                        }
                        else
                        {
                            textBSR.Text = Regex.Replace(temp_content, @"\s", ""); // Cắt bỏ khoảng trắng
                        }
                        pictureBox3.Image.Save(@"D:\DATN_CARPARK\main\CSDL\CAROUT\" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".Jpeg");
                        pictureBoxBSR.Image.Save(@"D:\DATN_CARPARK\main\CSDL\BSR\" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".Jpeg");
                        pictureBoxKTR.Image.Save(@"D:\DATN_CARPARK\main\CSDL\KTR\" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".Jpeg");
                        bool ktbs = false;
                        if (textBSR.Text == textBSV.Text) //Trùng biển số
                        {
                            ktbs = true;
                            WindowsMediaPlayer sound2 = new WindowsMediaPlayer();
                            sound2.URL = @"D:\DATN_CARPARK\main\Visual\MP3\moixera.mp3";
                            sound2.controls.play();
                            string sqlInsertcarout = "INSERT INTO QUAN_LY_XE_RA VALUES('" + mathe + "', '" + textBSR.Text + "','" + textTI2.Text + "' ,'" + textTO.Text + "','" + textP.Text + "')";
                            SqlCommand cmdcarout = new SqlCommand(sqlInsertcarout, Connect);
                            cmdcarout.ExecuteNonQuery();
                            LoadDataCAROUT();
                            string sqldltcarin = "DELETE FROM QUAN_LY_XE_VAO WHERE Mã_Thẻ = '" + mathe + "'";
                            SqlCommand cmddltcarin = new SqlCommand(sqldltcarin, Connect);
                            cmddltcarin.ExecuteNonQuery();
                            LoadDataCARIN();
                            string sqldltpicture = "DELETE FROM Picture WHERE Mã_Thẻ = '" + mathe + "'";
                            SqlCommand cmddltpicture = new SqlCommand(sqldltpicture, Connect);
                            cmddltpicture.ExecuteNonQuery();
                        }
                        if (ktbs == false)
                        {
                            DialogResult ktr = MessageBox.Show("Biển số xe không trùng.\n Bạn có muốn cho xe ra ? ", "Thông Báo", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (ktr == DialogResult.Yes)
                            {
                                WindowsMediaPlayer sound3 = new WindowsMediaPlayer();
                                sound3.URL = @"D:\DATN_CARPARK\main\Visual\MP3\moixera.mp3";
                                sound3.controls.play();
                                string sqlInsertcarout = "INSERT INTO QUAN_LY_XE_RA VALUES('" + mathe + "', '" + textBSR.Text + "','" + textTI2.Text + "' ,'" + textTO.Text + "','" + textP.Text + "')";
                                SqlCommand cmdcarout = new SqlCommand(sqlInsertcarout, Connect);
                                cmdcarout.ExecuteNonQuery();
                                LoadDataCAROUT();
                                string sqldltcarin = "DELETE FROM QUAN_LY_XE_VAO WHERE Mã_Thẻ = '" + mathe + "'";
                                SqlCommand cmddltcarin = new SqlCommand(sqldltcarin, Connect);
                                cmddltcarin.ExecuteNonQuery();
                                LoadDataCARIN();
                                string sqldltpicture = "DELETE FROM Picture WHERE Mã_Thẻ = '" + mathe + "'";
                                SqlCommand cmddltpicture = new SqlCommand(sqldltpicture, Connect);
                                cmddltpicture.ExecuteNonQuery();
                            }
                            else
                            {
                                int i = dataCARIN.Rows.Count;
                                if (i != 6 && textVT.Text != "6")
                                {                                 
                                    serialPort1.Write("g"); //Điều khiển nâng xe lên
                                    WindowsMediaPlayer sound4 = new WindowsMediaPlayer();
                                    sound4.URL = @"D:\DATN_CARPARK\main\Visual\MP3\warning.mp3";
                                    sound4.controls.play();
                                }
                            }
                        }                       
                    }
                }
            }
        }
        #endregion
        #region Gọi xe mất thẻ
        private void GOIXE_Click(object sender, EventArgs e)
        {
            if (textPass.Text == "666666")
            {
                counter = 3;
                if (textGOIXE.Text == "")
                {
                    MessageBox.Show("Vui Lòng nhập tầng cần gọi", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    textBox1.Text = null;
                    pictureBox2.Image = null;
                    pictureBox3.Image = null;
                    pictureBoxBSR.Image = null;
                    pictureBoxKTV.Image = null;
                    pictureBoxBSV.Image = null;
                    pictureBoxKTR.Image = null;
                    pictureBoxBSR.Image = null;
                    pictureBoxKTR.Image = null;
                    textVT.Text = null;
                    textIDOUT.Text = null;
                    textBSR.Text = null;
                    textTI2.Text = null;
                    textTO.Text = null;
                    textP.Text = null;
                    textT.Text = null;
                    textPass.Text = null;                    
                    textTO.Text = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"); // hiển thị thời gian xe ra
                    string sqlmott2 = "SELECT * FROM QUAN_LY_XE_VAO WHERE Vị_trí = '" + textGOIXE.Text + "'";
                    SqlDataAdapter drmott2 = new SqlDataAdapter(sqlmott2, Connect);
                    DataTable dtmott2 = new DataTable();
                    drmott2.Fill(dtmott2);
                    textIDIN.Text = dtmott2.Rows[0][0].ToString();
                    textVT.Text   = dtmott2.Rows[0][1].ToString();
                    textBSV.Text  = dtmott2.Rows[0][2].ToString();
                    textTI2.Text  = dtmott2.Rows[0][3].ToString();
                    textIDOUT.Text = "Mất thẻ";
                    DateTime startdate = DateTime.Parse(textTI2.Text);
                    DateTime stopdate = DateTime.Parse(textTO.Text);
                    TimeSpan time = new TimeSpan();
                    time = stopdate - startdate;
                    if (time.Days.ToString() == "0" && time.Hours.ToString() == "0")
                    {
                        textT.Text = time.Minutes.ToString() + " phút ";
                    }
                    else if (time.Days.ToString() == "0")
                    {
                        textT.Text = time.Hours.ToString() + " giờ " + time.Minutes.ToString() + " phút ";
                    }
                    else
                    {
                        textT.Text = time.Days.ToString() + " ngày " + time.Hours.ToString() + " giờ " + time.Minutes.ToString() + " phút ";
                    }
                    double days = (int)time.TotalDays;
                    var hours = (int)time.TotalHours;
                    var minutes = (int)time.TotalMinutes;
                    if (minutes <= 0) minutes = 1;
                    double sotien = minutes * 500 + 200000;
                    textP.Text = sotien.ToString();                    
                    serialPort1.Write(textGOIXE.Text); //Gọi tầng chứa xe cần lấy                    
                    Thread.Sleep(15000);
                    pictureBoxBSR.Image = (Bitmap)pictureBox1.Image.Clone();
                    pictureBox1.Image.Save(@"D:\DATN_CARPARK\main\CSDL\CAROUT\" + "Mất Thẻ" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".Jpeg");
                    WindowsMediaPlayer sound6 = new WindowsMediaPlayer();
                    sound6.URL = @"D:\DATN_CARPARK\main\Visual\MP3\moixera.mp3";
                    sound6.controls.play();
                    string sqlInsertcarout2 = "INSERT INTO QUAN_LY_XE_RA VALUES('" + "Losing Card" + "', '" + textBSV.Text + "','" + textTI2.Text + "' ,'" + textTO.Text + "','" + textP.Text + "')";
                    SqlCommand cmdcarout2 = new SqlCommand(sqlInsertcarout2, Connect);
                    cmdcarout2.ExecuteNonQuery();
                    LoadDataCAROUT();
                    string sqldltcarin2 = "DELETE FROM QUAN_LY_XE_VAO WHERE Vị_trí = '" + textGOIXE.Text + "'";
                    SqlCommand cmddltcarin2 = new SqlCommand(sqldltcarin2, Connect);
                    cmddltcarin2.ExecuteNonQuery();
                    LoadDataCARIN();
                    string sqldltpicture2 = "DELETE FROM Picture WHERE Mã_thẻ = '" + textIDIN.Text + "'";
                    SqlCommand cmddltpicture2 = new SqlCommand(sqldltpicture2, Connect);
                    cmddltpicture2.ExecuteNonQuery();
                    string sqldltthe = "DELETE FROM QUAN_LY_THE WHERE ID = '" + textIDIN.Text + "'";
                    SqlCommand cmddltthe = new SqlCommand(sqldltthe, Connect);
                    cmddltthe.ExecuteNonQuery();
                    LoadDataID();
                    textGOIXE.Text = null;
                }
            }
            else if (textPass.Text == "")
            {
                MessageBox.Show("Vui Lòng nhập mật khẩu", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                counter--;
                textPass.Text = null;
                MessageBox.Show("Sai mật khẩu!\n Còn " + counter + " lần thử!", "Cảnh Báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (counter == 0)
                {
                    Thread.Sleep(20000);
                    counter = 3;
                }
            }        
        }
        #endregion
        #region Nhận dạng biển vào
        public void ProcessImageIn(string urlImage)
        {
            System.Diagnostics.Debug.WriteLine("ProcessImage");
            PlateImagesList.Clear();
            PlateTextList.Clear();
            Bitmap imagein = new Bitmap(pictureBox2.Image);
            FindLicensePlateIn(imagein, out Plate_Draw);
        }
        private string Ocr_In(Bitmap image_s, bool isFull, bool isNum = false)
        {
            System.Diagnostics.Debug.WriteLine("ORC BIT MAP IN");
            string temp = "";
            Image<Gray, byte> src = new Image<Gray, byte>(image_s);
            double ratio = 1;
            while (true)
            {
                ratio = (double)CvInvoke.cvCountNonZero(src) / (src.Width * src.Height);
                if (ratio > 0.5) break;
                src = src.Dilate(2);
            }
            Bitmap image = src.ToBitmap();

            TesseractProcessor ocr;
            if (isFull)
                ocr = full_tesseract;
            else if (isNum)
                ocr = num_tesseract;
            else
                ocr = ch_tesseract;
            int cou = 0;
            ocr.Clear();
            ocr.ClearAdaptiveClassifier();
            temp = ocr.Apply(image);
            while (temp.Length > 3)
            {
                Image<Gray, byte> temp2 = new Image<Gray, byte>(image);
                temp2 = temp2.Erode(2);
                image = temp2.ToBitmap();
                ocr.Clear();
                ocr.ClearAdaptiveClassifier();
                temp = ocr.Apply(image);
                cou++;
                if (cou > 10)
                {
                    temp = "";
                    break;
                }
            }
            return temp;
        }
        public void FindLicensePlateIn(Bitmap image, out Image plateDraw)
        {
            System.Diagnostics.Debug.WriteLine("Find license plate 4");
            plateDraw = null;
            Image<Bgr, byte> frame;
            bool isface = false;
            Bitmap src;
            Image dst = image;
            HaarCascade haar = new HaarCascade(Application.StartupPath + "\\output-hv-33-x25.xml");
            System.Diagnostics.Debug.WriteLine("Find license plate 4 - 676");
            for (float i = 0; i <= 20; i = i + 3)
            {
                for (float s = -1; s <= 1 && s + i != 1; s += 2)
                {
                    src = RotateImage(dst, i * s);
                    PlateImagesList.Clear();
                    frame = new Image<Bgr, byte>(src);
                    using (Image<Gray, byte> grayframe = new Image<Gray, byte>(src))
                    {
                        var faces = grayframe.DetectHaarCascade(haar, 1.1, 8, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(0, 0))[0];
                        foreach (var face in faces)
                        {
                            Image<Bgr, byte> tmp = frame.Copy();
                            tmp.ROI = face.rect;
                            frame.Draw(face.rect, new Bgr(Color.Blue), 2);
                            PlateImagesList.Add(tmp);
                            isface = true;
                        }
                        if (isface)
                        {
                            Image<Bgr, byte> showimg = frame.Clone();
                            if (PlateImagesList.Count > 1)
                            {
                                for (int k = 1; k < PlateImagesList.Count; k++)
                                {
                                    if (PlateImagesList[0].Width < PlateImagesList[k].Width)
                                    {
                                        PlateImagesList[0] = PlateImagesList[k];
                                    }
                                }
                            }
                            PlateImagesList[0] = PlateImagesList[0].Resize(400, 400, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
                            return;
                        }
                    }
                }
            }
        }
        public static Bitmap RotateImage(Image image, float angle)
        {
            System.Diagnostics.Debug.WriteLine("Rotate image return bitmap");
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            PointF offset = new PointF((float)image.Width / 2, (float)image.Height / 2);
            //Tạo một bitmap trống mới để giữ hình ảnh xoay
            Bitmap rotatedBmp = new Bitmap(image.Width, image.Height);
            rotatedBmp.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            //Tạo một đối tượng đồ họa từ bitmap trống
            Graphics g = Graphics.FromImage(rotatedBmp);
            //Đặt điểm xoay ở giữa hình ảnh
            g.TranslateTransform(offset.X, offset.Y);
            //Xoay ảnh
            g.RotateTransform(angle);
            //Di chuyển hình ảnh trở lại
            g.TranslateTransform(-offset.X, -offset.Y);
            //vẽ qua hình ảnh vào đối tượng đồ họa
            g.DrawImage(image, new PointF(0, 0));
            return rotatedBmp;
        }
        private void Reconize_in(string link, out Image hinhbienso, out string bienso, out string bienso_text)
        {
            System.Diagnostics.Debug.WriteLine("Reconize");
            for (int i = 0; i < box.Length; i++)
            {
                this.Controls.Remove(box[i]);
            }
            hinhbienso = null;
            bienso = "";
            bienso_text = "";
            ProcessImageIn(link);
            if (PlateImagesList.Count != 0)
            {
                Image<Bgr, byte> src = new Image<Bgr, byte>(PlateImagesList[0].ToBitmap());
                Bitmap grayframe;
                var con = new FindContours();
                Bitmap color;
                int c = con.IdentifyContours(src.ToBitmap(), 50, false, out grayframe, out color, out listRect);
                //int z = con.count;
                pictureBoxBSV.Image = color;
                hinhbienso = Plate_Draw;
                pictureBoxKTV.Image = grayframe;
                Image<Gray, byte> dst = new Image<Gray, byte>(grayframe);
                grayframe = dst.ToBitmap();
                string zz = "";

                //lọc và sắp xếp số
                List<Bitmap> bmp = new List<Bitmap>();
                List<int> erode = new List<int>();
                List<Rectangle> up = new List<Rectangle>();
                List<Rectangle> dow = new List<Rectangle>();
                int up_y = 0, dow_y = 0;
                bool flag_up = false;
                int di = 0;
                if (listRect == null) return;
                for (int i = 0; i < listRect.Count; i++)
                {
                    Bitmap ch = grayframe.Clone(listRect[i], grayframe.PixelFormat);
                    int cou = 0;
                    full_tesseract.Clear();
                    full_tesseract.ClearAdaptiveClassifier();
                    string temp = full_tesseract.Apply(ch);
                    while (temp.Length > 3)
                    {
                        Image<Gray, byte> temp2 = new Image<Gray, byte>(ch);
                        temp2 = temp2.Erode(2);
                        ch = temp2.ToBitmap();
                        full_tesseract.Clear();
                        full_tesseract.ClearAdaptiveClassifier();
                        temp = full_tesseract.Apply(ch);
                        cou++;
                        if (cou > 10)
                        {
                            listRect.RemoveAt(i);
                            i--;
                            di = 0;
                            break;
                        }
                        di = cou;
                    }
                }
                for (int i = 0; i < listRect.Count; i++)
                {
                    for (int j = i; j < listRect.Count; j++)
                    {
                        if (listRect[i].Y > listRect[j].Y + 100)
                        {
                            flag_up = true;
                            up_y = listRect[j].Y;
                            dow_y = listRect[i].Y;
                            break;
                        }
                        else if (listRect[j].Y > listRect[i].Y + 100)
                        {
                            flag_up = true;
                            up_y = listRect[i].Y;
                            dow_y = listRect[j].Y;
                            break;
                        }
                        if (flag_up == true) break;
                    }
                }
                for (int i = 0; i < listRect.Count; i++)
                {
                    if (listRect[i].Y < up_y + 50 && listRect[i].Y > up_y - 50)
                    {
                        up.Add(listRect[i]);
                    }
                    else if (listRect[i].Y < dow_y + 50 && listRect[i].Y > dow_y - 50)
                    {
                        dow.Add(listRect[i]);
                    }
                }
                if (flag_up == false) dow = listRect;
                for (int i = 0; i < up.Count; i++)
                {
                    for (int j = i; j < up.Count; j++)
                    {
                        if (up[i].X > up[j].X)
                        {
                            Rectangle w = up[i];
                            up[i] = up[j];
                            up[j] = w;
                        }
                    }
                }
                for (int i = 0; i < dow.Count; i++)
                {
                    for (int j = i; j < dow.Count; j++)
                    {
                        if (dow[i].X > dow[j].X)
                        {
                            Rectangle w = dow[i];
                            dow[i] = dow[j];
                            dow[j] = w;
                        }
                    }
                }
                int x = 12;
                int c_x = 0;
                for (int i = 0; i < up.Count; i++)
                {
                    Bitmap ch = grayframe.Clone(up[i], grayframe.PixelFormat);
                    Bitmap o = ch;
                    //ch = con.Erodetion(ch);
                    string temp;
                    if (i < 2)
                    {
                        temp = Ocr_In(ch, false, true); // Nhận diện số
                    }
                    else
                    {
                        temp = Ocr_In(ch, false, false);// Nhận diện chữ
                    }
                    zz += temp;
                    box[i].Location = new System.Drawing.Point(x + i * 50, 290);
                    box[i].Size = new Size(50, 100);
                    box[i].SizeMode = PictureBoxSizeMode.StretchImage;
                    box[i].Image = ch;
                    box[i].Update();
                    //this.Controls.Add(box[i]);
                    c_x++;
                }
                zz += "\r\n";
                for (int i = 0; i < dow.Count; i++)
                {
                    Bitmap ch = grayframe.Clone(dow[i], grayframe.PixelFormat);
                    //ch = con.Erodetion(ch);
                    string temp = Ocr_In(ch, false, true); // Nhận diện số
                    zz += temp;
                    box[i + c_x].Location = new System.Drawing.Point(x + i * 50, 290);
                    box[i + c_x].Size = new Size(50, 100);
                    box[i + c_x].SizeMode = PictureBoxSizeMode.StretchImage;
                    box[i + c_x].Image = ch;
                    box[i + c_x].Update();
                    //this.Controls.Add(box[i + c_x]);
                }
                bienso = zz.Replace("\n", "");
                bienso = bienso.Replace("\r", "");
                bienso_text = zz;
            }
        }
        #endregion
        #region Nhận dạng biển ra
        public void ProcessImageOut(string urlImage)
        {
            System.Diagnostics.Debug.WriteLine("ProcessImage");
            PlateImagesList.Clear();
            PlateTextList.Clear();
            Bitmap imageout = new Bitmap(pictureBox3.Image);
            FindLicensePlateOut(imageout, out Plate_Draw);
        }
        private string Ocr_Out(Bitmap image_s, bool isFull, bool isNum = false)
        {
            System.Diagnostics.Debug.WriteLine("ORC BIT MAP OUT");
            string temp = "";
            Image<Gray, byte> src = new Image<Gray, byte>(image_s);
            double ratio = 1;
            while (true)
            {
                ratio = (double)CvInvoke.cvCountNonZero(src) / (src.Width * src.Height);
                if (ratio > 0.5) break;
                src = src.Dilate(2);
            }
            Bitmap image = src.ToBitmap();
            TesseractProcessor ocr;
            if (isFull)
                ocr = full_tesseract;
            else if (isNum)
                ocr = num_tesseract;
            else
                ocr = ch_tesseract;
            int cou = 0;
            ocr.Clear();
            ocr.ClearAdaptiveClassifier();
            temp = ocr.Apply(image);
            while (temp.Length > 3)
            {
                Image<Gray, byte> temp2 = new Image<Gray, byte>(image);
                temp2 = temp2.Erode(2);
                image = temp2.ToBitmap();
                ocr.Clear();
                ocr.ClearAdaptiveClassifier();
                temp = ocr.Apply(image);
                cou++;
                if (cou > 10)
                {
                    temp = "";
                    break;
                }
            }
            return temp;
        }
        public void FindLicensePlateOut(Bitmap image1, out Image plateDraw)
        {
            System.Diagnostics.Debug.WriteLine("Find license plate 4");
            plateDraw = null;
            Image<Bgr, byte> frame;
            bool isface = false;
            Bitmap src;
            Image dst1 = image1;
            HaarCascade haar = new HaarCascade(Application.StartupPath + "\\output-hv-33-x25.xml");

            System.Diagnostics.Debug.WriteLine("Find license plate 4 - 676");
            for (float i = 0; i <= 20; i = i + 3)
            {
                for (float s = -1; s <= 1 && s + i != 1; s += 2)
                {
                    src = RotateImageOut(dst1, i * s); PlateImagesList.Clear();
                    frame = new Image<Bgr, byte>(src);
                    using (Image<Gray, byte> grayframe = new Image<Gray, byte>(src))
                    {
                        var faces = grayframe.DetectHaarCascade(haar, 1.1, 8, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(0, 0))[0];
                        foreach (var face in faces)
                        {
                            Image<Bgr, byte> tmp = frame.Copy();
                            tmp.ROI = face.rect;
                            frame.Draw(face.rect, new Bgr(Color.Blue), 2);
                            PlateImagesList.Add(tmp);
                            isface = true;
                        }
                        if (isface)
                        {
                            Image<Bgr, byte> showimg = frame.Clone();
                            if (PlateImagesList.Count > 1)
                            {
                                for (int k = 1; k < PlateImagesList.Count; k++)
                                {
                                    if (PlateImagesList[0].Width < PlateImagesList[k].Width)
                                    {
                                        PlateImagesList[0] = PlateImagesList[k];
                                    }
                                }
                            }
                            PlateImagesList[0] = PlateImagesList[0].Resize(400, 400, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
                            return;
                        }
                    }
                }
            }
        }
        public static Bitmap RotateImageOut(Image image, float angle)
        {
            System.Diagnostics.Debug.WriteLine("Rotate image return bitmap");
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            PointF offset = new PointF((float)image.Width / 2, (float)image.Height / 2);
            //Tạo một bitmap trống mới để giữ hình ảnh xoay
            Bitmap rotatedBmp = new Bitmap(image.Width, image.Height);
            rotatedBmp.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            //Tạo một đối tượng đồ họa từ bitmap trống
            Graphics g = Graphics.FromImage(rotatedBmp);
            //Đặt điểm xoay ở giữa hình ảnh
            g.TranslateTransform(offset.X, offset.Y);
            //Xoay ảnh
            g.RotateTransform(angle);
            //Di chuyển hình ảnh trở lại
            g.TranslateTransform(-offset.X, -offset.Y);
            //vẽ qua hình ảnh vào đối tượng đồ họa
            g.DrawImage(image, new PointF(0, 0));
            return rotatedBmp;
        }
        private void Reconize_out(string link1, out Image hinhbienso1, out string bienso1, out string bienso_text1)
        {
            System.Diagnostics.Debug.WriteLine("Reconize");
            for (int i = 0; i < box.Length; i++)
            {
                this.Controls.Remove(box[i]);
            }
            hinhbienso1 = null;
            bienso1 = "";
            bienso_text1 = "";
            ProcessImageOut(link1);
            if (PlateImagesList.Count != 0)
            {
                Image<Bgr, byte> src = new Image<Bgr, byte>(PlateImagesList[0].ToBitmap());
                Bitmap grayframe;
                FindContours con = new FindContours();
                Bitmap color;
                int c = con.IdentifyContours(src.ToBitmap(), 50, false, out grayframe, out color, out listRect);
                //int z = con.count;
                pictureBoxBSR.Image = color;
                hinhbienso1 = Plate_Draw;
                pictureBoxKTR.Image = grayframe;
                Image<Gray, byte> dst = new Image<Gray, byte>(grayframe);
                grayframe = dst.ToBitmap();
                string zz = "";

                //lọc và sắp xếp số
                List<Bitmap> bmp = new List<Bitmap>();
                List<int> erode = new List<int>();
                List<Rectangle> up = new List<Rectangle>();
                List<Rectangle> dow = new List<Rectangle>();
                int up_y = 0, dow_y = 0;
                bool flag_up = false;
                int di = 0;
                if (listRect == null) return;
                for (int i = 0; i < listRect.Count; i++)
                {
                    Bitmap ch = grayframe.Clone(listRect[i],
                   grayframe.PixelFormat);
                    int cou = 0;
                    full_tesseract.Clear();
                    full_tesseract.ClearAdaptiveClassifier();
                    string temp = full_tesseract.Apply(ch);
                    while (temp.Length > 3)
                    {
                        Image<Gray, byte> temp2 = new Image<Gray, byte>(ch);
                        temp2 = temp2.Erode(2);
                        ch = temp2.ToBitmap();
                        full_tesseract.Clear();
                        full_tesseract.ClearAdaptiveClassifier();
                        temp = full_tesseract.Apply(ch);
                        cou++;
                        if (cou > 10)
                        {
                            listRect.RemoveAt(i);
                            i--;
                            di = 0;
                            break;
                        }
                        di = cou;
                    }
                }
                for (int i = 0; i < listRect.Count; i++)
                {
                    for (int j = i; j < listRect.Count; j++)
                    {
                        if (listRect[i].Y > listRect[j].Y + 100)
                        {
                            flag_up = true;
                            up_y = listRect[j].Y;
                            dow_y = listRect[i].Y;
                            break;
                        }
                        else if (listRect[j].Y > listRect[i].Y + 100)
                        {
                            flag_up = true;
                            up_y = listRect[i].Y;
                            dow_y = listRect[j].Y;
                            break;
                        }
                        if (flag_up == true) break;
                    }
                }
                for (int i = 0; i < listRect.Count; i++)
                {
                    if (listRect[i].Y < up_y + 50 && listRect[i].Y > up_y - 50)
                    {
                        up.Add(listRect[i]);
                    }
                    else if (listRect[i].Y < dow_y + 50 && listRect[i].Y > dow_y - 50)
                    {
                        dow.Add(listRect[i]);
                    }
                }
                if (flag_up == false) dow = listRect;
                for (int i = 0; i < up.Count; i++)
                {
                    for (int j = i; j < up.Count; j++)
                    {
                        if (up[i].X > up[j].X)
                        {
                            Rectangle w = up[i];
                            up[i] = up[j];
                            up[j] = w;
                        }
                    }
                }
                for (int i = 0; i < dow.Count; i++)
                {
                    for (int j = i; j < dow.Count; j++)
                    {
                        if (dow[i].X > dow[j].X)
                        {
                            Rectangle w = dow[i];
                            dow[i] = dow[j];
                            dow[j] = w;
                        }
                    }
                }
                int x = 12;
                int c_x = 0;
                for (int i = 0; i < up.Count; i++)
                {
                    Bitmap ch = grayframe.Clone(up[i],
                   grayframe.PixelFormat);
                    Bitmap o = ch;
                    string temp;
                    if (i < 2)
                    {
                        temp = Ocr_Out(ch, false, true); // nhan dien so
                    }
                    else
                    {
                        temp = Ocr_Out(ch, false, false);// nhan dien chu
                    }
                    zz += temp;
                    box[i].Location = new System.Drawing.Point(x + i * 50, 290);
                    box[i].Size = new Size(50, 100);
                    box[i].SizeMode = PictureBoxSizeMode.StretchImage;
                    box[i].Image = ch;
                    box[i].Update();
                    c_x++;
                }
                zz += "\r\n";
                for (int i = 0; i < dow.Count; i++)
                {
                    Bitmap ch = grayframe.Clone(dow[i],
                   grayframe.PixelFormat);
                    //ch = con.Erodetion(ch);
                    string temp = Ocr_Out(ch, false, true); // nhan dien so
                    zz += temp;
                    box[i + c_x].Location = new System.Drawing.Point(x + i *
                   50, 390);
                    box[i + c_x].Size = new Size(50, 100);
                    box[i + c_x].SizeMode = PictureBoxSizeMode.StretchImage;
                    box[i + c_x].Image = ch;
                    box[i + c_x].Update();
                    //this.Controls.Add(box[i + c_x]);
                }
                bienso1 = zz.Replace("\n", "");
                bienso1 = bienso1.Replace("\r", "");
                bienso_text1 = zz;
            }
        }
        #endregion
        #region Load thông tin cơ sở dữ liệu
        public void LoadDataCARIN()
        {
            string sqlSelectID = "SELECT * FROM QUAN_LY_XE_VAO";
            SqlCommand cmd = new SqlCommand(sqlSelectID, Connect);
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            dataCARIN.DataSource = dt;
        }
        public void LoadDataCAROUT()
        {
            string sqlSelectID = "SELECT * FROM QUAN_LY_XE_RA";
            SqlCommand cmd = new SqlCommand(sqlSelectID, Connect);
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            dataCAROUT.DataSource = dt;
        }
        public void LoadDataID()
        {
            string sqlSelectID = "SELECT * FROM QUAN_LY_THE";
            SqlCommand cmd = new SqlCommand(sqlSelectID, Connect);
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            dataID.DataSource = dt;
        }
        #endregion
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                textPass.UseSystemPasswordChar = false;
            }
            else
            {
                textPass.UseSystemPasswordChar = true;
            }
        }
        public string detect_in(string imagePath)
        {
            string content = "";
            string filePath = "result_reg.txt";

            string sourceFilePath = imagePath;
            string destinationFilePath = "test.jpg";

            //try
            //{
            //    if (File.Exists(destinationFilePath))
            //    {
            //        File.Delete(destinationFilePath);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Lỗi: " + ex.Message);
            //}
            // pictureBoxBSV.Dispose();
            // pictureBoxKTV.Dispose();

            pictureBoxBSV.Image = (Bitmap)pictureBox1.Image.Clone();
            pictureBoxKTV.Image = (Bitmap)pictureBox1.Image.Clone();

            Thread.Sleep(500);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

            }
            catch (Exception ex)
            {
            }

            Console.WriteLine("Running Bat");
            call_bat_file("run.bat");

            int i = 0;
            while (i < 10 && !File.Exists(filePath))
            {
                i += 1;
                Thread.Sleep(200);
                Console.WriteLine("Running");
            }
            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    content = reader.ReadToEnd();
                    Console.WriteLine(content);
                    // pictureBoxBSR.Image = Image.FromFile("cropped_image.jpg");
                    textBSR.Text = content;

                }


                string[] fileArray = Directory.GetFiles(@"D:\DATN_CARPARK\main\CSDL\KTV\");

                pictureBoxBSV.Image = Image.FromFile("cropped_image" + (fileArray.Length).ToString() + ".jpg");
                pictureBoxKTV.Image = Image.FromFile("cropped_image" + (fileArray.Length).ToString() + ".jpg");

                textBSV.Text = content;
            }
            else
            {
                textBSV.Text = "Khong nhan duoc";
            }

            
            return content;
        }

        public string detect_out(string imagePath)
        {
            string content = "";
            string filePath = "result_reg.txt";

            string sourceFilePath = imagePath;
            string destinationFilePath = "test.jpg";

            //try
            //{
            //    if (File.Exists(destinationFilePath))
            //    {
            //        File.Delete(destinationFilePath);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Lỗi: " + ex.Message);
            //}
            // pictureBoxBSR.Dispose();
            // pictureBoxKTR.Dispose();

            pictureBoxBSR.Image = (Bitmap) pictureBox1.Image.Clone();
            pictureBoxKTR.Image = (Bitmap) pictureBox1.Image.Clone();


            Thread.Sleep(500);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

            }
            catch (Exception ex)
            {
            }

            //while (File.Exists("cropped_image_copy.jpg"))
            //{
            //    try
            //    {
            //        if (File.Exists(filePath))
            //        {
            //            File.Delete(filePath);
            //        }
            //        File.Delete("cropped_image_copy.jpg");
            //        Thread.Sleep(500);

            //    }
            //    catch (Exception ex)
            //    {
            //    }
            //}
            Console.WriteLine("Running Bat");
            call_bat_file("run_out.bat");

            while (!File.Exists(filePath))
            {
                Thread.Sleep(100);
                Console.WriteLine("Running");
            }

            using (StreamReader reader = new StreamReader(filePath))
            {
                content = reader.ReadToEnd();
                Console.WriteLine(content);
                // pictureBoxBSR.Image = Image.FromFile("cropped_image.jpg");
                textBSR.Text = content;

            }

            string source_ = "cropped_image.Jpeg";
            string des_ = "cropped_image_copy.Jpeg";

            //try
            //{
            //    File.Copy(source_, des_);
            //}
            //catch (Exception ex)
            //{
            //}
            Thread.Sleep(200);

            string[] fileArray = Directory.GetFiles(@"D:\DATN_CARPARK\main\CSDL\KTR");

            pictureBoxBSR.Image = Image.FromFile("cropped_image" + (fileArray.Length).ToString() + ".jpg");
            pictureBoxKTR.Image = Image.FromFile("cropped_image" + (fileArray.Length).ToString() + ".jpg");

            textBSR.Text = content;
            return content;
        }



        public void call_bat_file(string batchFilePath)
        {
            try
            {
                // Khởi tạo một quá trình (process) để chạy tệp .bat
                Process process = new Process();

                // Thiết lập các thuộc tính của quá trình
                process.StartInfo.FileName = batchFilePath;
                process.StartInfo.Verb = "runas";
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                // Chạy tệp .bat
                process.Start();
                process.WaitForExit();

                // Kiểm tra mã thoát của quá trình (0 có nghĩa là thành công)
                int exitCode = process.ExitCode;

            }
            catch (Exception ex)
            {
            }
        }
        private void groupBox8_Enter(object sender, EventArgs e)
        {}
        private void pictureBox4_Click(object sender, EventArgs e)
        {}

        private void button1_Click_1(object sender, EventArgs e)
        {
            call_bat_file("run.bat");
        }

        private void textVT_TextChanged(object sender, EventArgs e)
        {

        }

        //private void button1_Click_1(object sender, EventArgs e)
        //{
        //    detect_("picture1.jpg");
        //}
    }
}
