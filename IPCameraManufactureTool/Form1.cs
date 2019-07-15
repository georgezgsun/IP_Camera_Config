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
using System.Text.RegularExpressions;

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
        private int configStatus = 0; // 0 not start, 1 configuring, 2 configuring with failures, 3 completed
        //private bool configSuccess = false;
        private bool configCompleted = false;
        private bool CameraGood = false;
        private StreamWriter log;
        private string CameraCurrentAddress = "";
        private string CameraConfigAddress = "10.25.50.";
        private string CameraOrigAddress = "192.168.0.100";
        private string CameraAuth = "";
        private string CameraStreamUri = "rtsp://$IP/h264";
        private string CameraFirmware = "";
        private string[] Cgi;
        private int TotalCgis = 0;

        static object lockObj = new object();

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

        // Config the camera with a cgi command specified in cmd
        private string Config(string cmd)
        {
            string responseBody;
            bool CheckFirmware = false;
            bool IPAddreesChanged = false;

            try
            {
                if (cmd.Contains("$CheckFirmware"))
                {
                    CheckFirmware = true;
                    cmd = cmd.Replace("$CheckFirmware", "");
                }
                else if (cmd.Contains("$IP"))
                {
                    cmd = cmd.Replace("$IP", CameraConfigAddress + "0"); // new IP address need to append a 0 
                    IPAddreesChanged = true;
                }

                // Get rid of the spaces at the header and tailer of the command
                cmd = cmd.Trim();
                Console.WriteLine("Send command: " + cmd);
                Log("Send command: " + cmd);
                WebRequest request = WebRequest.Create("http://" + CameraCurrentAddress + cmd);
                if (!String.IsNullOrEmpty(CameraAuth))
                    request.Headers.Add("Autheorization", "Basic " + CameraAuth);
                WebResponse response = request.GetResponse();
                StreamReader inStream = new StreamReader(response.GetResponseStream());
                responseBody = inStream.ReadToEnd();

                if (IPAddreesChanged)
                    CameraCurrentAddress = CameraConfigAddress + "0";
            }

            catch (HttpRequestException error)
            {
                responseBody = error.Message;
                Console.WriteLine("\nException Message :{0} ", responseBody);
                //configSuccess = false;
                configStatus = 2;
            }

            Console.WriteLine("Get response: " + responseBody.Replace("<br>", "\n"));
            Log("Get response: " + responseBody.Replace("<br>", "\r\n"));

            if (CheckFirmware)
            {
                if (responseBody.Contains(CameraFirmware))
                    Log("The camera has a correct firmware.");
                else
                {
                    Log("Error. The camera has a different firmware.");
                    //configSuccess = false;
                    configStatus = 2;
                }
            }

            return responseBody;
        }

        // background worker used to make all the configurations of a camera
        private void BackgroundConfig(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //configSuccess = true;
            configCompleted = false;
            configStatus = 1; // configuring

            try
            {
                string cgi = "";
                //CameraAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes("root:root"));

                Log("");
                Log("==");
                Log("Start configure IP camera.");

                // Get the MAC address of the camera using arp
                IPAddress hostIPAddress = IPAddress.Parse(CameraCurrentAddress);
                byte[] macAddr = new byte[6];
                int macAddrLen = macAddr.Length;
                int r = SendARP(BitConverter.ToInt32(hostIPAddress.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen);

                // convert the MAC address in format xx:xx:xx:xx:xx:xx
                string[] str = new string[macAddrLen];
                for (int i = 0; i < macAddrLen; i++)
                    str[i] = macAddr[i].ToString("x2");
                Log("The camera MAC address is " + string.Join(":", str));

                // save the image before configuration
                Bitmap picture = streamPlayerControl1.GetCurrentFrame();
                string imageFile = WorkPath + SerialNumber + @"\" + SerialNumber + "-0.jpg";
                picture.Save(imageFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                Log("Saved proof image of the camera before the configuration as " + imageFile);

                string ln;
                for (int i = 0; i < TotalCgis; i++)
                {
                    // report grogress
                    worker.ReportProgress(100 * (i+1) / TotalCgis);

                    // Get a new command
                    ln = Cgi[i];

                    // it is a new cgi
                    if (ln.Contains('?'))
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

                    if (ln.Contains("$T"))
                    {
                        // parse the date and time settings
                        DateTime dt = DateTime.Now;
                        string DT = dt.ToString("yyyy.MM.dd.hh.mm.ss");

                        ln = ln.Replace("$T0", DT);
                        ln = ln.Replace("$Ty", DT.Substring(0, 4));
                        ln = ln.Replace("$TM", DT.Substring(5, 2));
                        ln = ln.Replace("$Td", DT.Substring(8, 2));
                        ln = ln.Replace("$Th", DT.Substring(11, 2));
                        ln = ln.Replace("$Tm", DT.Substring(14, 2));
                        ln = ln.Replace("$Ts", DT.Substring(17, 2));
                    }
                    else if (ln.Contains("$Sleep="))
                    {
                        // To stop the stream play
                        worker.ReportProgress(200);

                        ln = ln.Replace("$Sleep=", "");
                        int s = 1;
                        if (!Int32.TryParse(ln, out s))
                            Log("Warning: Sleep time is not specified correct at line " + i.ToString());
                        Log(String.Format("Sleep for {0} seconds", s));
                        Log("");

                        // Sleep s seconds
                        Thread.Sleep(s * 1000);

                        // Restart the play of camera stream
                        worker.ReportProgress(300);
                        continue;
                    }
                    else if (ln.Contains("$SN"))
                        ln = ln.Replace("$SN", SerialNumber);

                    Config(cgi + ln);
                }

                // save the image after configuration
                // TODO wait for the camera to resume
                picture = streamPlayerControl1.GetCurrentFrame();
                imageFile = WorkPath + SerialNumber + @"\" + SerialNumber + "-1.jpg";
                picture.Save(imageFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                Log("Saved proof image from the camera after configuration as " + imageFile);

                if (configStatus == 1)
                    Log("Configuration accomplished.");
                else
                    Log("Configuration failed.");

                Log("==");
                log.Close();
            }

            catch (HttpRequestException error)
            {
                Console.WriteLine("\nException Message :{0} ", error.Message);
            }

        }

        // This is called when the background worker report grogress
        private void ConfigProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage <= 100)
                progressBar1.Value = e.ProgressPercentage;
            else if (e.ProgressPercentage == 200)
            {
                labelOutput.Text = "Stop the play of camera stream.";
                streamPlayerControl1.Stop();
            }
            else if (e.ProgressPercentage == 300)
            {
                string str = CameraStreamUri.Replace("$IP", CameraCurrentAddress);
                var uri = new Uri(str);
                streamPlayerControl1.StartPlay(uri);
                labelOutput.Text = "Try to resume the play of stream from " + str;
            }
            //configSuccess = !String.IsNullOrEmpty(CameraCurrentAddress);
        }

        // This is called when the background configuration is completed
        private void ConfigCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.Beep();
            configCompleted = true;

            if (configStatus == 1)
                labelOutput.Text = ModelNumber + " camera " + SerialNumber + " has been configured successfully." + Environment.NewLine
                    + Environment.NewLine + "Please unplug the camera." + Environment.NewLine;
            else
                labelOutput.Text = "Cannot configure this " + ModelNumber + " IP camera."
                    + Environment.NewLine + "Please double check the cable connection and try again." + Environment.NewLine;
            configStatus = 3;

            comboBox1.Enabled = true;
            textBoxSerialNumber.Enabled = true;
            buttonConfig.Enabled = false;
            this.ActiveControl = textBoxSerialNumber;
        }

        // Ping the host in synchronouse mode
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

        // search the IP camera in async mode
        private async void SearchCamera(string baseIP)
        {
            var tasks = new List<Task>();
            string ip;

            for (int i = -1; i < 10; i++)
            {
                Ping p = new Ping();
                if (i < 0)
                    ip = CameraOrigAddress;
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

        // the async task that ping a host
        private async Task PingAsync(Ping ping, string ip)
        {
            var reply = await ping.SendPingAsync(ip, 500);

            if (reply.Status == IPStatus.Success)
            {
                lock (lockObj)
                {
                    if (String.IsNullOrEmpty(CameraCurrentAddress))
                        CameraCurrentAddress = ip;
                }
            }
        }

        // background worker for searching IP cameras
        private void BackgroundSearchingCamera(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            try
            {
                //string[] addr = { "192.168.0.100", "10.25.50.0", "10.25.50.1", "10.25.50.2", "10.25.50.3", "10.25.50.4" };
                ////CameraCurrentAddress = "";
                //if (!String.IsNullOrEmpty(CameraCurrentAddress))
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

                //    if (!String.IsNullOrEmpty(CameraCurrentAddress))
                //        break;

                //    labelOutput.Invoke((MethodInvoker)delegate
                //    {
                //        labelOutput.Text = "Searching camera at " + a + Environment.NewLine;
                //    });

                //    if (PingHost(a))
                //    {
                //        CameraCurrentAddress = a;
                //        progressConfig = progressMax;
                //    }
                //}
                while (true)
                {
                    // Keep search in case have not found any camera
                    if (String.IsNullOrEmpty(CameraCurrentAddress))
                    {
                        SearchCamera("10.25.50.");
                        // report the progress
                        if (String.IsNullOrEmpty(CameraCurrentAddress))
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

        // this is called when IP camera searching makes a progress
        private void SearchCameraProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Do nothing during configuration
            if (backgroundConfig.IsBusy)
                return;

            int progress = progressBar1.Value;
            // Check if the camera found or not
            if (String.IsNullOrEmpty(CameraCurrentAddress))
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
                    string str = CameraStreamUri.Replace("$IP", CameraCurrentAddress);
                    var uri = new Uri(str);
                    streamPlayerControl1.StartPlay(uri);
                    labelOutput.Text = "Find the camera at " + CameraCurrentAddress + Environment.NewLine
                        + "Try to play the stream from " + str;
                }

                Console.Beep();
                progress = 100;
            }

            // show the progress of searching
            progressBar1.Value = progress;
        }

        // this is called when IP camera searching has completed. In new design, the searching will nevere end.
        private void SearchingCameraCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = 100;

            if (String.IsNullOrEmpty(CameraCurrentAddress))
            {
                labelOutput.Text = "Cannot find any camera so far." + Environment.NewLine + "Keeps searching...";
                buttonConfig.Enabled = false;
                //SearchCamera.RunWorkerAsync();
            }
            else
            {
                labelOutput.Text = "Found camera at " + CameraCurrentAddress + Environment.NewLine
                    + "Trying to display the video stream from rtsp://" + CameraCurrentAddress + "/h264";
                var uri = new Uri("rtsp://" + CameraCurrentAddress + "/h264");
                streamPlayerControl1.StartPlay(uri);
            }

            this.ActiveControl = textBoxSerialNumber;
        }

        // this is called when the serial number changed
        private void SerialNumberInput(object sender, EventArgs e)
        {
            // Check if the Serial number has a valid format, which starts with 3 letters and followed by 5 digits
            Regex rgx = new Regex(@"^[a-zA-Z]{3}[0-9]{5}");
            if ((textBoxSerialNumber.Text.Length == 8) && rgx.IsMatch(textBoxSerialNumber.Text))
                SerialNumber = textBoxSerialNumber.Text;
            else
                SerialNumber = "";

            // update the config button
            buttonConfig.Enabled = !(String.IsNullOrEmpty(SerialNumber) || String.IsNullOrEmpty(CameraCurrentAddress)) && CameraGood;
            configStatus = 0;
        }

        // this is called when the model number changed
        private void ModelNumberChanged(object sender, EventArgs e)
        {
            // reset the flags
            //configSuccess = false;
            configStatus = 0;
            CameraCurrentAddress = "";
            CameraGood = false;

            // get the model number
            ModelNumber = comboBox1.Text;
            pictureBoxCameraModel.Image = Image.FromFile(WorkPath + ModelNumber + ".jpg");

            // reset the GUI
            buttonConfig.Enabled = false;
            progressBar1.Value = 0;

            // read the configuration file and update the configures
            Cgi = new string[255];
            TotalCgis = 0;
            string configPath = WorkPath + ModelNumber + ".cfg";
            using (StreamReader conf = new StreamReader(configPath))
            {
                string ln;
                while ((ln = conf.ReadLine()) != null)
                {
                    if (ln.Contains("//"))
                        ln = Regex.Replace(ln, "//.*", ""); // delete those comments in the line
                    ln = ln.Trim();  // trim leading and trailing whitespace

                    // skip those empty lines
                    if (String.IsNullOrEmpty(ln))
                        continue;

                    // End of config
                    if (ln.Contains("$END"))
                        break;

                    // specify the new ip address of the camera
                    if (ln.Contains("$IP="))
                    {
                        CameraConfigAddress = ln.Replace("$IP=", "");
                        continue;
                    }

                    // specify the original ip address of the camera
                    if (ln.Contains("$BaseIP="))
                    {
                        CameraOrigAddress = ln.Replace("$BaseIP=", "");
                        continue;
                    }

                    // specify the stream uri
                    if (ln.Contains("rtsp:"))
                    {
                        CameraStreamUri = ln.Replace("rtsp:", "rtsp://");
                        continue;
                    }

                    // specify the firmware version
                    if (ln.Contains("$Firmware="))
                    {
                        CameraFirmware = ln.Replace("$Firmware=", "");
                        continue;
                    }

                    // specify the firmware version
                    if (ln.Contains("$Description="))
                    {
                        labelCameraDescription.Text = ln.Replace("$Description=", "");
                        continue;
                    }

                    Cgi[TotalCgis++] = ln;
                }
                conf.Close();
            }

            // Start the Searching of cameras
            if (!SearchingCamera.IsBusy)
                SearchingCamera.RunWorkerAsync();

            this.ActiveControl = textBoxSerialNumber;
        }

        // This is called when the config button is clicked
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

                backgroundConfig.RunWorkerAsync();
            }
        }

        // This is called when the stream play starts successfully
        private void StreamStarted(object sender, EventArgs e)
        {
            labelOutput.Text = "Camera stream from " + CameraCurrentAddress + " is playing well.";
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

        // This is called when the stream cannot be played
        private void StreamFailed(object sender, WebEye.Controls.WinForms.StreamPlayerControl.StreamFailedEventArgs e)
        {
            if (configStatus == 3)
            {
                labelOutput.Text = "Please plug a new camera if you want to configure another camera.";
                configStatus = 0;
            }
            else if (configStatus == 1)
            {
                labelOutput.Text = "Cannot play the stream from the camera now.";
                configStatus = 2;
            }
            else if (configStatus == 0)
                labelOutput.Text = String.Format("The address {0} is not occupied by a {1} IP camera.", CameraCurrentAddress, ModelNumber);

            progressBar1.Value = 0;
            buttonConfig.Enabled = false;
            comboBox1.Enabled = true;
            textBoxSerialNumber.Enabled = true;

            CameraCurrentAddress = "";
            CameraGood = false;
        }

        // This is called when the stream play is stopped, which typically indicates that the camera is unplugged or has its IP address changed
        private void StreamStopped(object sender, EventArgs e)
        {
            if (configStatus == 3)
            {
                labelOutput.Text = "Please plug a new camera if you want to configure another camera.";

                progressBar1.Value = 0;
                buttonConfig.Enabled = false;
                comboBox1.Enabled = true;
                textBoxSerialNumber.Enabled = true;
                this.ActiveControl = textBoxSerialNumber;
                CameraCurrentAddress = "";
                CameraGood = false;
                return;
            }

            if (configStatus == 0)
            {
                CameraCurrentAddress = "";
                CameraGood = false;
                return;
            }

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

        // to handle arp using system dll. This is used to get the MAC address
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);
    }
}
