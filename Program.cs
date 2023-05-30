using System;
using System.Configuration;
using System.IO;
using System.Net.Mail;
using System.ComponentModel;

namespace LogFilesCleanupUtility
{
    class Program
    {
        //static bool mailSent = false;
        static string mailbody = string.Empty;
        static void Main(string[] args)
        {

            var path = ConfigurationManager.AppSettings["FilePath"].ToString().Split(';');
            string FileAge = ConfigurationManager.AppSettings["AgeOfFiles"];

            mailbody = DateTime.Now.ToString() + " Started Cleanup process..." + Environment.NewLine;
            mailbody = DateTime.Now.ToString() + " Deleted the following files:   " + Environment.NewLine;

            for (int t = 0; t < path.Length; t++)
            {
                string files = path[t].ToString();
                System.IO.DirectoryInfo di = new DirectoryInfo(files);
                var count = 0;
                try {
                    foreach (FileInfo file in di.GetFiles())
                    {
                         if (file.LastWriteTime < DateTime.Now.AddDays((-1) * Int32.Parse(FileAge))) { //can be in prod
                            file.Delete();
                            count++;
                        }
                    }
                    var msg = count.ToString() + " in " + files + " have been deleted";
                    LogWriter.LogMessageToFile(msg);
                    mailbody = mailbody + msg + Environment.NewLine;
                }
                catch (Exception err)
                {
                    throw err;
                }
                finally { }             
            }
            LogWriter.LogMessageToFile("Send email to TCMP ..." + DateTime.Now.ToString());
            mailbody = mailbody + DateTime.Now.ToString() + " Send email to TCMP ...";
            SendMail();
            return;
        }

        private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
            String token = (string)e.UserState;

            if (e.Cancelled)
            {
                Console.WriteLine("[{0}] Send canceled.", token);
            }
            if (e.Error != null)
            {
                Console.WriteLine("[{0}] {1}", token, e.Error.ToString());
            }
            else
            {
                LogWriter.LogMessageToFile(" Message sent.");
            }

            //mailSent = true;
        }

        private static void SendMail()
        {
            string ToPeople = ConfigurationManager.AppSettings["ToPeople"];
            string CCPeople = ConfigurationManager.AppSettings["CCPeople"];

            SmtpClient client = new SmtpClient("smtp.cdcr.ca.gov");
            MailAddress from = new MailAddress("FDCDBA3_TCMP@cdcr.ca.gov", "TCMP", System.Text.Encoding.UTF8);
            // Set destinations for the e-mail message.

            // Specify the message content.
            MailMessage message = new MailMessage();
            message.From = from;
            message.To.Add(ToPeople);
            message.CC.Add(CCPeople);
            message.Body = "logs cleaned up " + DateTime.Now + ":";

            // Include some non-ASCII characters in body and subject.
            message.Body = mailbody + Environment.NewLine;
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.Subject = "Log Files Cleanup!";
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            // Set the method that is called back when the send operation ends.
            client.SendCompleted += new
            SendCompletedEventHandler(SendCompletedCallback);
            // The userState can be any object that allows your callback 
            // method to identify this send operation.
            string userState = "Log Cleanup";
            client.SendAsync(message, userState);
            string answer = Console.ReadLine();
            // Clean up.
            message.Dispose();
            System.Environment.Exit(0);
            //return;
        }
    }
}