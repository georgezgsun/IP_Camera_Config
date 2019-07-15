using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace IPCameraManufactureTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            backgroundConfig.WorkerReportsProgress = true;
            SearchingCamera.WorkerReportsProgress = true;
        }

        private string ModelNumber;
        private string SerialNumber = "";
        private string WorkPath = @"C:\IPCameraConfig\";
        private bool configSuccess = false;
        private bool CameraGood = false;
        private StreamWriter log;
        private string CameraAddress;
        private string CameraAuth = "";
        private string CameraStreamUri = "rtsp://$IP/h264";
        private int progressMax = 100;
        private int progressConfig = 0;

        static object lockObj = new object();
        private string CameraAddressOrig = "192.168.0.100";

        public bool Log(string logContent)
        {
            if (String.IsNullOrEmpty(logContent))
            {
                log.WriteLine();
                return true;
            }

            if (logContent.Substring(0, 2) == "==")
            {
                for (int i = 0; i < 100; i++)
                    log.Write("=");
                log.WriteLine();
                return true;
            }

            DateTime currentDateTime = DateTime.Now; // log the date and time
            log.Write(currentDateTime.ToString("yyyy-MMM-dd HH:mm:ss "));
            log.WriteLine(logContent);
            log.Flush();
            return true;
        }

        private void ConfigProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            //configSuccess = !String.IsNullOrEmpty(CameraAddress);
        }

        private void ConfigCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (configSuccess)
            {
                labelOutput.Text = ModelNumber + " camera " + SerialNumber + " has been configured successfully." + Environment.NewLine
                    + Environment.NewLine + "Please unplug the camera." + Environment.NewLine;

                // save the image after configuration
                Bitmap image0 = streamPlayerControl1.GetCurrentFrame();
                string imageFile = WorkPath + SerialNumber + @"\" + SerialNumber + "-1.jpg";
                image0.Save(imageFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                Log("Saved proof image from the camera after configuration as " + imageFile);

                Log("Configuration accomplished.");
            }
            else
            {
                labelOutput.Text = "Cannot configure this " + ModelNumber + " IP camera."
                    + Environment.NewLine + "Please double check the cable connection and try again." + Environment.NewLine;
                Log("Configuration for a " + ModelNumber + " camera failed.");
            }

            Log("==");
            log.Close();
            Console.Beep();

            comboBox1.Enabled = true;
            textBoxSerialNumber.Enabled = true;
            buttonConfig.Enabled = false;
            this.ActiveControl = textBoxSerialNumber;
        }

        public bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        private async void SearchCamera(string baseIP)
        {
            var tasks = new List<Task>();
            string ip;

            for (int i = -1; i < 10; i++)
            {
                Ping p = new Ping();
                if (i < 0)
                    ip = CameraAddressOrig;
                else
                    ip = baseIP + i.ToString();
                var task = PingAsync(p, ip);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ContinueWith(t =>
            {
                //labelOutput.Invoke((MethodInvoker)delegate
                //{
                //    //labelOutput.Text = "Found!";
                //});
            });
        }

        private async Task PingAsync(Ping ping, string ip)
        {
            var reply = await ping.SendPingAsync(ip, 500);

            if (reply.Status == IPStatus.Success)
            {
                lock (lockObj)
                {
                    if (String.IsNullOrEmpty(CameraAddress))
                        CameraAddress = ip;
                }
            }
        }

        private void BackgroundSearchingCamera(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            try
            {
                //string[] addr = { "192.168.0.100", "10.25.50.0", "10.25.50.1", "10.25.50.2", "10.25.50.3", "10.25.50.4" };
                ////CameraAddress = "";
                //if (!String.IsNullOrEmpty(CameraAddress))
                //    return;
                //CameraGood = false;
                //progressMax = addr.Length + 1;
                //progressConfig = 0;

                //foreach (string a in addr)
                //{
                //    progressConfig++;
                //    if (progressConfig > progressMax)
                //        progressConfig = progressMax;
                //    //worker.ReportProgress(progressConfig * 100 / progressMax);

                //    if (!String.IsNullOrEmpty(CameraAddress))
                //        break;

                //    labelOutput.Invoke((MethodInvoker)delegate
                //    {
                //        labelOutput.Text = "Searching camera at " + a + Environment.NewLine;
                //    });

                //    if (PingHost(a))
                //    {
                //        CameraAddress = a;
                //        progressConfig = progressMax;
                //    }
                //}
                while (true)
                {
                    // Keep search in case have not found any camera
                    if (String.IsNullOrEmpty(CameraAddress))
                    {
                        SearchCamera("10.25.50.");
                        // report the progress
                        if (String.IsNullOrEmpty(CameraAddress))
                            worker.ReportProgress(1);
                        else
                            worker.ReportProgress(100);
                    }

                    Thread.Sleep(1000);
                }
            }

            catch (HttpRequestException error)
            {
                Console.WriteLine("\nException Message :{0} ", error.Message);
            }
        }

        private void SearchCameraProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Do nothing during configuration
            if (backgroundConfig.IsBusy)
                return;

            int progress = progressBar1.Value;
            // Check if the camera found or not
            if (String.IsNullOrEmpty(CameraAddress))
            //if (e.ProgressPercentage < 100)
            {
                labelOutput.Text = "Have not found any IP camera." + Environment.NewLine
                    + "Please plug in the IP camera and check the connection" + Environment.NewLine
                    + "Keep searching...";
                buttonConfig.Enabled = false;

                // grow the progress
                progress += 10;
                if (progress >= 100)
                    progress = 10;
            }
            else
            {
                // Play the camera strem
                if (!streamPlayerControl1.IsPlaying)
                {
                    string str = CameraStreamUri.Replace("$IP", CameraAddress);
                    var uri = new Uri(str);
                    streamPlayerControl1.StartPlay(uri);
                    labelOutput.Text = "Find the camera at " + CameraAddress + Environment.NewLine
                        + "Try to play the stream from " + str;
                }

                Console.Beep();
                progress = 100;
            }

            // show the progress of searching
            progressBar1.Value = progress;
        }

        private void SearchingCameraCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = 100;

            if (String.IsNullOrEmpty(CameraAddress))
            {
                labelOutput.Text = "Cannot find any camera so far." + Environment.NewLine + "Keeps searching...";
                buttonConfig.Enabled = false;
                //SearchCamera.RunWorkerAsync();
            }
            else
            {
                labelOutput.Text = "Found camera at " + CameraAddress + Environment.NewLine
                    + "Trying to display the video stream from rtsp://" + CameraAddress + "/h264";
                var uri = new Uri("rtsp://" + CameraAddress + "/h264");
                streamPlayerControl1.StartPlay(uri);
            }

            this.ActiveControl = textBoxSerialNumber;
        }

        private string Config(string cmd)
        {
            string responseBody;

            try
            {
                Console.WriteLine("Send command: " + cmd);
                Log("Send command: " + cmd);
                WebRequest request = WebRequest.Create("http://" + CameraAddress + cmd);
                if (!String.IsNullOrEmpty(CameraAuth))
                    request.Headers.Add("Autheorization", "Basic " + CameraAuth);
                WebResponse response = request.GetResponse();
                StreamReader inStream = new StreamReader(response.GetResponseStream());
                responseBody = inStream.ReadToEnd();
            }

            catch (HttpRequestException error)
            {
                responseBody = error.Message;
                Console.WriteLine("\nException Message :{0} ", responseBody);
                configSuccess = false;
            }

            Console.WriteLine("Get response: " + responseBody.Replace("<br>", "\n"));
            Log("Get response: " + responseBody.Replace("<br>", "\r\n"));

            return responseBody;
        }

        private void BackgroundConfig(object sender, DoWorkEventArgs e)
        {
            try
            {
                string newAddress = "10.25.50.0";
                //CameraAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes("root:root"));
                progressConfig = 0;
                progressMax = 100;

                string configPath = WorkPath + ModelNumber + ".cfg";
                Log("");
                Log("==");
                Log("Start configure IP camera.");
                Log("The configurations are read from " + configPath);
                string cgi = "";

                IPAddress hostIPAddress = IPAddress.Parse(CameraAddress);
                byte[] macAddr = new byte[6];
                int macAddrLen = macAddr.Length;
                int r = SendARP(BitConverter.ToInt32(hostIPAddress.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen);

                string[] str = new string[macAddrLen];
                for (int i = 0; i < macAddrLen; i++)
                    str[i] = macAddr[i].ToString("x2");
                Log(string.Join(":", str));

                using (StreamReader conf = new StreamReader(configPath))
                {
                    string ln;
                    while ((ln = conf.ReadLine()) != null)
                    {
                        progressBar1.Invoke((MethodInvoker)delegate
                        {
                            progressConfig++;
                            if (progressConfig > progressMax)
                                progressBar1.Value = 100;
                            else
                                progressBar1.Value = progressConfig * 100 / progressMax;
                        });

                        if (ln.Contains("//"))
                            ln = System.Text.RegularExpressions.Regex.Replace(ln, "//.*", ""); // delete those comments in the line
                        ln = ln.Trim();  // trim leading and trailing whitespace

                        // an empty line or total lines specification
                        if (ln.Length < 5)
                        {
                            if (ln == "$END")
                            {
                                progressConfig = progressMax;
                                break;
                            }

                            // total lines
                            if (ln.Length > 0)
                                progressMax = System.Convert.ToInt32(ln);
                            continue;
                        }

                        // specify a new cgi
                        if (ln.Contains('/'))
                        {
                            cgi = ln;
                            continue;
                        }

                        // specify the authentication
                        if (ln.Contains(':'))
                        {
                            CameraAuth = ln;
                            continue;
                        }

                        // specify the new ip address
                        if (ln.Contains("$IP="))
                        {
                            newAddress = ln.Replace("$IP=", "");
                            continue;
                        }

                        if (ln.Contains("$T"))
                        {
                            DateTime dt = DateTime.Now;
                            ln = ln.Replace("$T", dt.ToString("yyyy.MM.dd.hh.mm.ss"));
                        }
                        else if (ln.Contains("$S"))
                            ln = ln.Replace("$S", SerialNumber);
                        else if (ln.Contains("$IP"))
                        {
                            ln = ln.Replace("$IP", newAddress);
                            Config(cgi + ln);
                            CameraAddress = newAddress;
                            System.Threading.Thread.Sleep(1000);
                            continue;
                        }

                        Config(cgi + ln);
                    }
                    conf.Close();
                }
            }

            catch (HttpRequestException error)
            {
                Console.WriteLine("\nException Message :{0} ", error.Message);
            }

        }

        private void SerialNumberInput(object sender, EventArgs e)
        {
            if (textBoxSerialNumber.Text.Length == 8)
            {
                SerialNumber = textBoxSerialNumber.Text;
            }
            else
                SerialNumber = "";

            buttonConfig.Enabled = !(String.IsNullOrEmpty(SerialNumber) || String.IsNullOrEmpty(CameraAddress)) && CameraGood; 

        }

        private void ModelNumberChanged(object sender, EventArgs e)
        {
            ModelNumber = comboBox1.Text;
            CameraAddress = "";
            CameraGood = false;

            if (!SearchingCamera.IsBusy)
                SearchingCamera.RunWorkerAsync();
            this.ActiveControl = textBoxSerialNumber;
        }

        private void ButtonConfigClick(object sender, EventArgs e)
        {
            Directory.CreateDirectory(WorkPath + SerialNumber);
            string logPath = WorkPath + SerialNumber + @"\" + SerialNumber + ".log";
            log = new StreamWriter(logPath, true);  // Open the log file in  append mode

            if (!backgroundConfig.IsBusy)
            {
                comboBox1.Enabled = false;
                textBoxSerialNumber.Enabled = false;
                buttonConfig.Enabled = false;

                // save the image before configuration
                Bitmap image0 = streamPlayerControl1.GetCurrentFrame();
                string imageFile = WorkPath + SerialNumber + @"\" + SerialNumber + "-0.jpg";
                image0.Save(imageFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                Log("Saved proof image from the camera before configuration as " + imageFile);

                configSuccess = true;
                backgroundConfig.RunWorkerAsync();
            }
        }

        private void StreamStarted(object sender, EventArgs e)
        {
            labelOutput.Text = "Camera stream from " + CameraAddress + " is playing well.";
            CameraGood = true;

            // Enable the config button in case the Serial number is valid
            if (!backgroundConfig.IsBusy)
            {
                buttonConfig.Enabled = !String.IsNullOrEmpty(SerialNumber);
                progressBar1.Value = 0;
            }
            else
                buttonConfig.Enabled = false;
        }

        private void StreamFailed(object sender, WebEye.Controls.WinForms.StreamPlayerControl.StreamFailedEventArgs e)
        {
            if (configSuccess)
                labelOutput.Text = "Please plug a new camera if you want to configure another camera.";

            progressBar1.Value = 0;
            buttonConfig.Enabled = false;
            comboBox1.Enabled = true;
            textBoxSerialNumber.Enabled = true;

            CameraAddress = "";
            CameraGood = false;
        }

        // using the load form to update the combo box list according to the  
        private void loadConfigForm(object sender, EventArgs e)
        {
            // Searches the camera config files in the target folder and add them to the combo list
            string[] filePaths = Directory.GetFiles(WorkPath, "*.cfg");
            if (filePaths.Length > 0)
            {
                comboBox1.Items.Clear();
                foreach (string a in filePaths)
                    comboBox1.Items.Add(Path.GetFileNameWithoutExtension(a));
            }
            comboBox1.SelectedItem = comboBox1.Items[0];
        }

        // to handle arp using system dll
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);

        // to handle the unplug of IP camera while playing
        private void StreamStopped(object sender, EventArgs e)
        {
            if (configSuccess)
                labelOutput.Text = "Please plug a new camera if you want to configure another camera.";

            progressBar1.Value = 0;
            buttonConfig.Enabled = false;
            comboBox1.Enabled = true;
            textBoxSerialNumber.Enabled = true;
            this.ActiveControl = textBoxSerialNumber;

            CameraAddress = "";
            CameraGood = false;
        }

        private void QRCodeImageClicked(object sender, EventArgs e)
        {
            SearchCamera("10.0.0.");
        }
    }
}
