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
using System.Timers;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Threading;
using MySql.Data.MySqlClient;

namespace ATCSImageProcessing
{
    public partial class Form1 : Form
    {
        MySqlConnection conn = new MySqlConnection("server=xxxxxxxx; user id=xxx; password=xxxx; database=xxxx");
        static BackgroundWorker m_oWorker;
        int errorcodebackground = 0;
        string errormessage = "";
        int countimageproccess = 0;
        String url = "http://admin:12345@atcslampung.ubl.ac.id:8080/Streaming/channels/1/picture?videoResolutionWidth=352&videoResolutionHeight=288";
      private static String[] urlList = {
                "http://admin:12345@atcslampung.ubl.ac.id:8080/Streaming/channels/1/picture?videoResolutionWidth=352&videoResolutionHeight=288",
                "http://admin:12345@atcslampung.ubl.ac.id:8080/Streaming/channels/2/picture?videoResolutionWidth=352&videoResolutionHeight=288",
                "http://admin:12345@atcslampung.ubl.ac.id:8080/Streaming/channels/3/picture?videoResolutionWidth=352&videoResolutionHeight=288"
                    };
        private static String[] pathList = {
                "ATCS/screenshoot/unila1/",
                "ATCS/screenshoot/unila2/",
                "ATCS/screenshoot/unila3/"
                    };

        private String[] tempNameList = {
                "unila1",
                "unila2",
                "unila3"
                    };
        private static Bitmap bitmap2;
        public Form1()
        {
            
            InitializeComponent();
            //Console.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmssfff"));

            m_oWorker = new BackgroundWorker();

            // Create a background worker thread that ReportsProgress &
            // SupportsCancellation
            // Hook up the appropriate events.
            m_oWorker.DoWork += new DoWorkEventHandler(m_oWorker_DoWork);
            m_oWorker.ProgressChanged += new ProgressChangedEventHandler
                    (m_oWorker_ProgressChanged);
            m_oWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (m_oWorker_RunWorkerCompleted);
            m_oWorker.WorkerReportsProgress = true;
            m_oWorker.WorkerSupportsCancellation = true;
        }
        private void m_oWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // The sender is the BackgroundWorker object we need it to
            // report progress and check for cancellation.
            //NOTE : Never play with the UI thread here...
            int y = 0;
            while (!m_oWorker.CancellationPending)
            {
                Thread.Sleep(500);

                for (int i = 0; i < urlList.Length; i++)
                {
                    m_oWorker.ReportProgress(20);
                    getimagefromurl(i,urlList[i], pathList[i], tempNameList[i]);
                    
                }
                


            }
            e.Cancel = true;
            errorcodebackground = 0;
            return;

        }

        private void m_oWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;

            if (e.ProgressPercentage == 20)
            {
                lblStatus.Text = "Processing......" + progressBar1.Value.ToString() + "% Success Get file from FTP ";
            }
            else if (e.ProgressPercentage == 50)
            {
                lblStatus.Text = "Processing......" + progressBar1.Value.ToString() + "% Success Proccessing file ";
            }
            else if (e.ProgressPercentage == 100)
            {
               
                lblStatus.Text = "Processing......" + progressBar1.Value.ToString() + "% Success Get Lot Status, Processing next Image ";
                pictureBox1.Image = bitmap2;
            }

        }

        private void m_oWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // The background process is complete. We need to inspect
            // our response to see if an error occurred, a cancel was
            // requested or if we completed successfully.  
            if (e.Cancelled)
            {
                lblStatus.Text = "Task Cancelled.";
            }

            // Check to see if an error occurred in the background process.

            else if (e.Error != null)
            {
                lblStatus.Text = "Error while performing background operation.";
            }
            else if (errorcodebackground > 99)
            {
                lblStatus.Text = errormessage;
            }
            else if (errorcodebackground == 0)
            {
                lblStatus.Text = "Success Processing";
            }
        }
        private void getimagefromurl(int cctv_code, String url, String path,String tempname)
        {
        
            System.Net.WebRequest request =System.Net.WebRequest.Create(url);
            request.Credentials = new NetworkCredential("xxxx", "xxxx");

           
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                     bitmap2 = new Bitmap(responseStream);
                }
            }

           
           
            m_oWorker.ReportProgress(50);
            uploadtoftp(cctv_code, bitmap2, path,tempname);
         
        }

        private void uploadtoftp(int cctv_code, Bitmap image,String path,String tempname)
        {
           
            string filename = tempname+ DateTime.Now.ToString("yyyyMMddHHmmssfff")+".jpg";
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://167.205.7.226:60328/" + path+filename);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential("xxxx", "xxxxxx");

            // Copy the contents of the file to the request stream.
            byte[] fileContents = ImageToByte2(image);
          
            request.ContentLength = fileContents.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);

            response.Close();
            uploadtodb(cctv_code,path, filename);
            m_oWorker.ReportProgress(100);
        }

        private void uploadtodb(int cctv_code, string path, string name)
        {
            using (conn)
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "INSERT INTO atcs_screenshot (path, name, cctv_code) VALUES('" + path + "', '" + name + "', " + cctv_code + ")";
                cmd.Prepare();
                cmd.ExecuteNonQuery();
                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
            }
        }

        public static byte[] ImageToByte2(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text.ToString() == "Stop")
            {
                //myTimer.Stop();
                button3.Text = "Start";
                if (m_oWorker.IsBusy)
                {

                    // Notify the worker thread that a cancel has been requested.

                    // The cancel will not actually happen until the thread in the

                    // DoWork checks the m_oWorker.CancellationPending flag. 

                    m_oWorker.CancelAsync();
                }
            }
            else
            {
                // myTimer.Start();
                button3.Text = "Stop";
                m_oWorker.RunWorkerAsync();
            }

        }
    }
}
