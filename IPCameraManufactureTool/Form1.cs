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
using System.Net.Sockets;
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

        private static string ModelNumber;
        private static string SerialNumber = "";
        private static string WorkPath = @"C:\IPCameraConfig\";
        private static int configStatus = 0; // 0 not start, 1 configuring, 2 completed, others configuring with failures
        private static StreamWriter log;
        private static string CameraCurrentAddress = "";
        private static string CameraConfigAddress = "";
        private static string CameraOrigAddress = "";
        private static string CameraAuth = "";
        private static string CameraStreamUri = "rtsp://$(IP)/h264"; // This is a standard format
        private static string CameraFirmware = "";
        private static string[] Cgi;
        private static int TotalCgis = 0;

        static object lockObj = new object();

        public bool Log(string logContent = "")
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

        // Config the camera with a cgi command in format http:\\hostname_or_ip\cgi_xxx?param0_xxx, POST and Disget Auth are considered by keywords
        private string Config(string cmd, string Auth = "")
        {
            try
            {
                int index;
                Uri uri;
                HttpWebRequest request;

                if (cmd.Contains("POST "))
                {
                    cmd = cmd.Replace("POST ", "");  // get rid off the POST indicator
                    index = cmd.IndexOf('?');
                    string url = cmd.Substring(0, index);
                    string postData = cmd.Substring(index + 1);
                    byte[] bytes = Encoding.UTF8.GetBytes(postData);

                    uri = new Uri(url);
                    request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = bytes.Length;

                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    uri = new Uri(cmd);

                    request = (HttpWebRequest)WebRequest.Create(uri);
                }

                if (cmd.Substring(20).Contains(CameraCurrentAddress))
                    request.Timeout = 5000; // set time out to be 5s
                else
                    request.Timeout = 20000; // set time out to be 20s

                if (!String.IsNullOrEmpty(Auth))
                {
                    if (Auth.Contains("Digest "))
                    {
                        Auth = Auth.Replace("Digest ", "");
                        index = Auth.IndexOf(':');
                        string user = Auth.Substring(0, index);
                        string password = Auth.Substring(index + 1);

                        var credentialCache = new CredentialCache();
                        credentialCache.Add(
                          new Uri(uri.GetLeftPart(UriPartial.Authority)), // request url's host
                          "Digest",  // authentication type 
                          new NetworkCredential(user, password) // credentials 
                        );
                        request.Credentials = credentialCache;
                    }
                    else
                    {
                        request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(Auth));
                    }
                }

                WebResponse response = request.GetResponse();
                StreamReader inStream = new StreamReader(response.GetResponseStream());
                return inStream.ReadToEnd();
            }

            catch (HttpRequestException error)
            {
                if (cmd.Substring(20).Contains(CameraCurrentAddress))
                    return "IP address has been changed.";
                return "Error " + error.Message;
            }

            catch (WebException error)
            {
                return "Continuation " + error.Message;
            }
        }

        // background worker used to make all the configurations of a camera
        private void BackgroundConfig(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            configStatus = 1; // configuring

            try
            {
                string cgi = "";

                Log();
                Log("==");
                Log("Start configuring a new IP camera.");

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
                Log("Saved a proof image of the camera before the configuration as " + imageFile);

                string ln;
                bool CheckFirmware = false;
 
                for (int i = 0; i < TotalCgis; i++)
                {
                    // report grogress
                    worker.ReportProgress(i);

                    // Get a new command
                    ln = Cgi[i];
                    CheckFirmware = false;

                    // it is a new common cgi, which ends with a ?
                    if (ln.Substring(ln.Length-1,1) == "?")
                    {
                        cgi = ln;
                        continue;
                    }

                    // specify the authentication
                    if (ln.Contains("$Auth="))
                    {
                        CameraAuth = ln.Replace("$Auth=", "");
                        //CameraAuth = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(CameraAuth));
                        continue;
                    }

                    // it is a date and time setting
                    if (ln.Contains("$(T"))
                    {
                        // parse the date and time settings
                        DateTime dt = DateTime.Now;
                        string DT = dt.ToString("yyyy.MM.dd.hh.mm.ss");

                        ln = ln.Replace("$(T0)", DT);
                        ln = ln.Replace("$(Ty)", DT.Substring(0, 4));
                        ln = ln.Replace("$(TM)", DT.Substring(5, 2));
                        ln = ln.Replace("$(Td)", DT.Substring(8, 2));
                        ln = ln.Replace("$(Th)", DT.Substring(11, 2));
                        ln = ln.Replace("$(Tm)", DT.Substring(14, 2));
                        ln = ln.Replace("$(Ts)", DT.Substring(17, 2));
                    }
                    else if (ln.Contains("$WaitStream="))
                    {
                        // To stop the stream play
                        //worker.ReportProgress(-1);

                        Log();
                        ln = ln.Replace("$WaitStream=", "");
                        int s = 1; // default value for sleep is 1s 
                        if (!Int32.TryParse(ln, out s))
                            Log("Warning: Sleep time is not specified correct at line " + s.ToString());
                        Log(String.Format("Wait for the resume of the camera stream in {0} seconds.", s));

                        // wait for the stream to be resumed or timeout
                        do
                        {
                            worker.ReportProgress(1000 + s); // tell the handler to restart the play
                            Thread.Sleep(1000);  // sleep 1s first
                            s--;
                            if (s < 0)
                            {
                                Log("The camera play does not resume in " + ln + "s. Configuration failed.");
                                configStatus = 5;  // config error of stream play not resumed
                                return;
                            }
                        } while (!streamPlayerControl1.IsPlaying);

                        Log("The camera play has been resumed.");
                        continue;
                    }
                    else if (ln.Contains("$(SN)"))
                        ln = ln.Replace("$(SN)", SerialNumber);
                    else if (ln.Contains("$CheckFirmware"))
                    {
                        CheckFirmware = true;
                        ln = ln.Replace("$CheckFirmware", "");
                    }
                    else if (ln.Contains("$Sleep="))
                    {
                        ln = ln.Replace("$Sleep=","");
                        int s = 1; // default value for sleep is 1s 
                        if (!Int32.TryParse(ln, out s))
                            Log("Warning: Sleep time is not specified correct at line " + s.ToString());
                        Log(String.Format("Sleep for {0} seconds.", s));
                        Thread.Sleep(s * 1000);

                        continue;
                    }
                    else if (ln.Contains("$StopPlay"))
                    {
                        Log("Try to stop the playing of camera stream.");
                        worker.ReportProgress(-1);
                        Thread.Sleep(100);
                        continue;
                    }

                    ln = ln.Trim();
 
                    // check if it is an incompleted cgi
                    if (ln.Substring(0, 1) != "/")
                        ln = cgi + ln;
                    ln = "http://" + CameraCurrentAddress + ln;

                    // check if it is going to change the IP address
                    if (ln.Contains("$(IP)"))
                    {
                        // update the current camera address
                        CameraCurrentAddress = CameraConfigAddress + "0";
                        ln = ln.Replace("$(IP)", CameraCurrentAddress);
                    }

                    // config one line and read the results
                    Log();
                    if (ln.Contains("POST"))
                        Log("Send config command to camera using POST: " + ln.Replace("POST ", ""));
                    else
                        Log("Send config command to camera using GET: " + ln);

                    Console.WriteLine("Send config command to camera: " + ln);

                    // Configure the camera
                    ln = Config(ln, CameraAuth);

                    Console.WriteLine("Get response: " + ln.Replace("<br>", "\n"));
                    Log("Get response: " + ln);

                    if (ln.ToLower().Contains("error") || ln.ToLower().Contains("failed"))
                        configStatus = 4;  // Config error of getting exceptions

                    if (CheckFirmware)
                    {
                        Log();

                        if (ln.Contains(CameraFirmware))
                            Log("The camera has a correct firmware.");
                        else
                        {
                            Log("Error. The camera has a different firmware.");
                            configStatus = 3;  // Firmware does not match
                        }
                    }

                    if (configStatus > 1)
                    {
                        Log("Cannot complete the configuration because of a failure.");
                        return;
                    }
                }

                // save the image after configuration
                if (streamPlayerControl1.IsPlaying)
                {
                    picture = streamPlayerControl1.GetCurrentFrame();
                    imageFile = WorkPath + SerialNumber + @"\" + SerialNumber + "-1.jpg";
                    picture.Save(imageFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                    Log("Saved proof image from the camera after configuration as " + imageFile);
                }
                else
                {
                    Log("Cannot saved proof image from the camera after configuration.");
                    configStatus = 5;
                }
            }

            catch (HttpRequestException error)
            {
                Console.WriteLine("\nException Message : {0} ", error.Message);
                configStatus = 4;
            }

            catch (WebException error)
            {
                Console.WriteLine("\nException Message : {0}.", error.Message);
                configStatus = 4;
            }

            catch
            {
                Console.WriteLine("\nUnkown exception.");
                configStatus = 4;
            }
        }

        // This is called when the background worker report grogress
        private void ConfigProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage < 0)
            {
                labelOutput.Text = "Stop the playing of camera stream for critical parameters has changed." 
                    + Environment.NewLine
                    + "Waiting for the video stream to resume...";

                if (streamPlayerControl1.IsPlaying)
                {
                    streamPlayerControl1.Stop();
                    Log("Stop the stream playing.");
                }
                return;
            }

            if (e.ProgressPercentage >= TotalCgis)
            {
                if (streamPlayerControl1.IsPlaying)
                {
                    streamPlayerControl1.Stop();
                    Log("Find the camera stream is still playing. Stop it now.");
                }
                else
                {
                    Log("Trying to restart the playing of camera stream.");

                    // Make the display moving

                    labelOutput.Text = String.Format("Waiting for the camera stream to be resumed at new IP address in {0}s.", e.ProgressPercentage - 1000);

                    string str = CameraStreamUri.Replace("$(IP)", CameraCurrentAddress);
                    var uri = new Uri(str);
                    streamPlayerControl1.StartPlay(uri);
                }
                return;
            }

            labelOutput.Text = String.Format("Configuration in progress {0}/{1}...", e.ProgressPercentage, TotalCgis);
            progressBar1.Value = 100 * (e.ProgressPercentage + 1) / TotalCgis;
        }

        // This is called when the background configuration is completed
        private void ConfigCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (configStatus == 1)
            {
                labelOutput.Text = ModelNumber 
                    + " camera " 
                    + SerialNumber 
                    + " has been configured successfully."
                    + Environment.NewLine + Environment.NewLine 
                    + "Please unplug the camera.";
                Log("Configuration accomplished.");
                configStatus = 2;
            }
            else if (configStatus == 3)
            {
                labelOutput.Text = "This camera has a different firmware." 
                    + Environment.NewLine + Environment.NewLine 
                    + "Please burn the correct firmware before configuration.";
                Log("Configuration failed because of firmware incorrect.");
            }
            else if (configStatus == 4)
            {
                labelOutput.Text = "Encounter exceptions while cofiguration." 
                    + Environment.NewLine + Environment.NewLine 
                    + "Please contact a develop engineer.";
                Log("Configuration failed because of encountering exceptions.");
            }
            else if (configStatus == 5)
            {
                labelOutput.Text = "The video stream is not resumed in given time." 
                    + Environment.NewLine + Environment.NewLine 
                    + "Please try again or contact a develop engineer.";
                Log("Configuration failed because of the stream does not resumed.");
            }

            Log("==");
            log.Close();

            // Copy files to network shared folder when the configuration is completed successfully
            if (configStatus == 2)
            {
                string sourcePath = WorkPath + SerialNumber + @"\";
                string destPath = @"\\public\public\CopTrax IP Camera Files\" + SerialNumber + @"\";
                DirectoryInfo dir = new DirectoryInfo(sourcePath);

                try
                {
                    // Create the destnation directory if it does not exist
                    if (!Directory.Exists(destPath))
                        Directory.CreateDirectory(destPath);

                    // Copy all the files in local disk to network shared drive
                    FileInfo[] files = dir.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        string filePath = Path.Combine(destPath, file.Name);
                        file.CopyTo(filePath, true);  // copy and replace the original file
                    }

                    // Empty the serial number. This will invoke the method of SerialNumberInput
                    textBoxSerialNumber.Text = "";
                }

                catch
                {
                    labelOutput.Text = string.Format("Cannot copy the files in {0} to {1}. ", sourcePath, destPath) 
                        + Environment.NewLine + "You may have to copy them manually.";
                }
            }

            // beep and update the status 
            Console.Beep();
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

                //var reply = await p.SendPingAsync(ip, 500);
                var task = PingAsync(p, ip);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ContinueWith(t =>
            {
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
                // keep searching when there is no camera
                while (true)
                {                    
                    if (String.IsNullOrEmpty(CameraCurrentAddress))
                    {
                        // search for new camera when the camera address is empty
                        SearchCamera(CameraConfigAddress);  // search for camera in given range
                        worker.ReportProgress(1);  // report progress trying to setup the stream play
                    }
                    else if (!streamPlayerControl1.IsPlaying)
                    {
                        // has the camera address but the stream is not playing
                        Thread.Sleep(2000); // give extra 2s wait for the setup of stream play
                        worker.ReportProgress(1);  //report progress trying to setup the stream play
                    }

                    Thread.Sleep(2000);  // sleep to reduce CPU usage
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
            // doing nothing during configuration
            if (backgroundConfig.IsBusy)
                return;

            int progress = progressBar1.Value;

            // Check if the camera been found or not
            if (String.IsNullOrEmpty(CameraCurrentAddress))
            {
                labelOutput.Text = "Have not found any IP camera." 
                    + Environment.NewLine
                    + "Please plug in the IP camera and check the connection." 
                    + Environment.NewLine
                    + "Keep searching...";
                buttonConfig.Enabled = false;

                // grow the progress
                progress += 10;
                if (progress > 100)
                    progress = 0;
            }
            else
            {
                // Trying to start the camera stream playing
                if (!streamPlayerControl1.IsPlaying)
                {
                    // To start stream playing after find an IP address.
                    string str = CameraStreamUri.Replace("$(IP)", CameraCurrentAddress);
                    var uri = new Uri(str);
                    streamPlayerControl1.StartPlay(uri);
                    labelOutput.Text = "Find the camera at " 
                        + CameraCurrentAddress 
                        + Environment.NewLine
                        + "Start to play the stream from " 
                        + str;

                    if (backgroundConfig.IsBusy)
                        Log("Start to play the stream from " + str);
                }

                progress = 100;
            }

            // show the progress of searching
            progressBar1.Value = progress;
        }

        // this is called when IP camera searching has completed. In new design, this shall not be reached.
        private void SearchingCameraCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = 100;

            labelOutput.Text = "Error. The search for camera has been terminated.";
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
            buttonConfig.Enabled = !String.IsNullOrEmpty(SerialNumber) && streamPlayerControl1.IsPlaying;
            configStatus = 0;
        }

        // this is called when the model number changed
        private void ModelNumberChanged(object sender, EventArgs e)
        {
            // reset the flags
            configStatus = 0;

            // Check if the model number is in valid format, which shall be ddd-dddd-dd
            Regex rgx = new Regex(@"^[0-9]{3}-[0-9]{4}-[0-9]{2}");
            if (!rgx.IsMatch(comboBox1.Text))
                return;

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
                    if (ln.Contains("$End"))
                        break;

                    // specify the new ip address of the camera
                    if (ln.Contains("$IP="))
                    {
                        CameraConfigAddress = ln.Replace("$IP=", "");
                        continue;
                    }

                    // specify the original ip address of the camera
                    if (ln.Contains("$OriginIP="))
                    {
                        CameraOrigAddress = ln.Replace("$OriginIP=", "");
                        continue;
                    }

                    // specify the stream uri
                    if (ln.Contains("$Play="))
                    {
                        CameraStreamUri = ln.Replace("$Play=", "rtsp://");
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
                CameraCurrentAddress = "";
            }

            // Start the Searching of cameras
            CameraCurrentAddress = "";
            if (streamPlayerControl1.IsPlaying)
                streamPlayerControl1.Stop(); // the play shall be stopped whenever the camera address is cleared
            if (!SearchingCamera.IsBusy)
                SearchingCamera.RunWorkerAsync();  // This is the only place to launch background camera search

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
            // beep to indicate the camera is ok
            Console.Beep();

            // Enable the config button in case the Serial number is valid
            if (!backgroundConfig.IsBusy)
            {
                labelOutput.Text = "Playing stream from " 
                    + CameraCurrentAddress + ".";
                if (String.IsNullOrEmpty(SerialNumber))
                    labelOutput.Text += Environment.NewLine 
                        + "Please scan or input the serial number.";
                else
                    buttonConfig.Enabled = true;
                progressBar1.Value = 0;
            }
            else
                labelOutput.Text = "Camera stream from " + CameraCurrentAddress + " is resumed.";
        }

        // This is called when the stream cannot be played
        private void StreamFailed(object sender, WebEye.Controls.WinForms.StreamPlayerControl.StreamFailedEventArgs e)
        {
            if (configStatus == 1)
                return;

            if (configStatus > 1)
            {
                labelOutput.Text = "Please plug a new camera if you want to configure another camera.";
                configStatus = 0;
            }
            else if (configStatus == 0)
                labelOutput.Text = String.Format("The address {0} is not occupied by a {1} IP camera with error {2}.", 
                    CameraCurrentAddress, ModelNumber, e.ToString());

            progressBar1.Value = 0;
            buttonConfig.Enabled = false;
            comboBox1.Enabled = true;
            textBoxSerialNumber.Enabled = true;

            CameraCurrentAddress = "";
        }

        // This is called when the stream play is stopped, which typically indicates that the camera is unplugged or has its IP address changed
        private void StreamStopped(object sender, EventArgs e)
        {
            if (configStatus == 1)
                return;

            if (configStatus > 1)
                labelOutput.Text = "Please plug a new camera to be configured.";

            progressBar1.Value = 0;
            buttonConfig.Enabled = false;
            comboBox1.Enabled = true;
            textBoxSerialNumber.Enabled = true;
            this.ActiveControl = textBoxSerialNumber;
            configStatus = 0;
            CameraCurrentAddress = "";
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

            this.Text = Application.ProductName + ", Version " + Application.ProductVersion;
        }

        // to handle arp using system dll. This is used to get the MAC address
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);

        private void clickCameraPicture(object sender, EventArgs e)
        {
            if (backgroundConfig.IsBusy)
                return;

            configStatus = 0;
            CameraCurrentAddress = "";
            if (streamPlayerControl1.IsPlaying)
                streamPlayerControl1.Stop();
        }
    }
}
