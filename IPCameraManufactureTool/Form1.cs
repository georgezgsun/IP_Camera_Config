using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;

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
        private bool configSuccess = true;
        private bool CameraGood = false;
        private StreamWriter log;
        private string CameraAddress;
        private string CameraAuth = "";
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
        }

        private void ConfigCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (configSuccess)
            {
                labelOutput.Text = ModelNumber + " camera " + SerialNumber + " has been configured successfully." + Environment.NewLine
                    + Environment.NewLine + "Please unplug the camera." + Environment.NewLine;

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

            comboBox1.Enabled = true;
            textBox1.Enabled = true;
            button1.Enabled = false;
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
            CameraAddress = "";

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
                labelOutput.Text = "";
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
            try
            {
                string[] addr = { "192.168.0.100", "10.25.50.0", "10.25.50.1", "10.25.50.2", "10.25.50.3", "10.25.50.4" };
                CameraAddress = "";
                CameraGood = false;
                progressMax = addr.Length + 1;
                progressConfig = 0;

                foreach (string a in addr)
                {
                    progressConfig++;
                    progressBar1.Invoke((MethodInvoker)delegate
                    {
                        if (progressMax < 1)
                            progressMax = 1;
                        if (progressConfig > progressMax)
                            progressConfig = progressMax;
                        progressBar1.Value = progressConfig * 100 / progressMax;
                    });

                    if (!String.IsNullOrEmpty(CameraAddress))
                        break;

                    labelOutput.Invoke((MethodInvoker)delegate
                    {
                        labelOutput.Text = "Searching camera at " + a + Environment.NewLine;
                    });

                    if (PingHost(a))
                    {
                        CameraAddress = a;
                        progressConfig = progressMax;

                        var uri = new Uri("rtsp://" + a + "/h264");
                        streamPlayerControl1.StartPlay(uri);
                    }
                }

            }

            catch (HttpRequestException error)
            {
                Console.WriteLine("\nException Message :{0} ", error.Message);
            }
        }

        private void SearchingCameraCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (String.IsNullOrEmpty(CameraAddress))
            {
                // invoke search camera again
                //BackgroundSearchingCamera(sender, null);
                labelOutput.Text = "Cannot find any a camera so far." + Environment.NewLine + "Keeps searching...";
            }
            else
            {
                labelOutput.Text = "Found camera at " + CameraAddress + Environment.NewLine
    + "Trying to display the video stream from rtsp://" + CameraAddress + "/h264";
            }
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
            comboBox1.Enabled = false;
            textBox1.Enabled = false;
            button1.Enabled = false;

            try
            {
                string newAddress = "10.25.50.0";
                //CameraAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes("root:root"));
                progressConfig = 0;
                progressMax = 100;

                string logPath = @"C:\temp\" + SerialNumber + ".log";
                string configPath = @"C:\temp\" + ModelNumber + ".txt";
                log = new StreamWriter(logPath, true);  // Open the log file in  append mode
                Log("");
                Log("==");
                Log("Start configure IP camera.");
                Log("The configurations are read from " + configPath);
                string cgi = "";

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
            if (textBox1.Text.Length == 8)
            {
                SerialNumber = textBox1.Text;
            }
            else
                SerialNumber = "";

            button1.Enabled = !(String.IsNullOrEmpty(SerialNumber) || String.IsNullOrEmpty(CameraAddress)) && CameraGood; 

        }

        private void ModelNumberChanged(object sender, EventArgs e)
        {
            ModelNumber = comboBox1.Text;
            CameraAddress = "";
            if (SearchingCamera.IsBusy)
                SearchingCamera.CancelAsync();
            SearchingCamera.RunWorkerAsync();
        }

        private void SearchCameraProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void ButtonConfigClick(object sender, EventArgs e)
        {
            if (!backgroundConfig.IsBusy)
            {
                backgroundConfig.RunWorkerAsync();
                button1.Enabled = false;
            }
        }

        private void StreamStarted(object sender, EventArgs e)
        {
            labelOutput.Text = "Camera at rtsp://" + CameraAddress + " is playing well.";
            CameraGood = true;
        }

        private void StreamFailed(object sender, WebEye.Controls.WinForms.StreamPlayerControl.StreamFailedEventArgs e)
        {
            if (configSuccess)
            {
                labelOutput.Text = "Please plug a new camera if you want to configure another camera.";
                CameraAddress = "";

                // invoke search camera
                BackgroundSearchingCamera(sender, null);
            }

            button1.Enabled = false;
            comboBox1.Enabled = true;
            textBox1.Enabled = true;

            CameraGood = false;
        }

    }
}
