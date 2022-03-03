using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace ChatTextImport
{
    internal class Program
    {
        public string machineName { get; set; }

        //main entrance to application
        private static void Main(string[] args)
        {
            string LogEntry = @"----------- Start of log file" + " " + DateTime.Now + "-----------";

            Program program = new Program();

            program.machineName = Environment.MachineName;

            //this will put the current date of application start per run. This allows for easier readability.
            if (File.Exists("ChatTextImport Log.txt"))
            {
                using (StreamWriter file = new StreamWriter(("ChatTextImport Log.txt"), true))
                {
                    file.WriteLine(LogEntry);
                }
            }
            else
            {
                using (StreamWriter file = new StreamWriter(("ChatTextImport Log.txt"), true))
                {
                    file.WriteLine(LogEntry);
                    program.LogEntryWriter(LogEntry);
                }
            }

            program.SearchFolder(@"E:\Recordings\TwitchChatIngest");

            program.MoveFiles(@"E:\Recordings\TwitchChatIngest\", @"E:\Recordings\TwitchChatDelete\");
            program.DeleteFiles(@"E:\Recordings\TwitchChatDelete\");
        }

        #region Folder Search

        //searches through the predefined folder for files that are not empty
        public void SearchFolder(string location)
        {
            foreach (var file in Directory.GetFiles(location))
            {
                if (new FileInfo(file).Length == 0)
                {
                    MoveFiles(Path.GetDirectoryName(file), @"E:\Recordings\TwitchChatDelete\");

                    string LogEntry = DateTime.Now + " " + file + " was moved because it was empty";

                    LogEntryWriter(LogEntry);

                    
                }
                else
                {
                    SearchFileForName(file);
                }
            }
        }

        //searches through files for message specific user data
        private void SearchFileForMessage(string File)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(File), Path.GetFileName(File));
            int lineNumber = 1;
            try
            {
                using (StreamReader inputFile = new StreamReader(filePath))
                {
                    for (int i = 1; i <= lineNumber; i++)
                    {
                        string message = inputFile.ReadLine();

                        return;
                    }
                }
            }
            catch
            {
                SearchFileForMessage(File);
            }

            Console.ReadLine();
        }

        //searches through files for data on the db data
        private async void SearchFileForName(string File)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(File), Path.GetFileName(File));

            try
            {
                if (new FileInfo(filePath).Length == 0)
                {
                }
                else
                {
                    using (StreamReader inputFile = new StreamReader(filePath))
                    {
                        string message = inputFile.ReadLine().TrimStart('U', 'M', ':', ' ');
                        string name = inputFile.ReadLine().TrimStart('U', 'N', ':', ' ');
                        string mod = inputFile.ReadLine().TrimStart('I', 'M', ':', ' ');
                        string sub = inputFile.ReadLine().TrimStart('I', 'S', ':', ' ');
                        string SubTime = inputFile.ReadLine().TrimStart('S', 'L', ':', ' ', '/');
                        string VIP = inputFile.ReadLine().TrimStart('V', 'P', ':', ' ');
                        string Bit = inputFile.ReadLine().TrimStart('B', 't', ' ', ':');
                        string BitNumber = inputFile.ReadLine().TrimStart('B', 'N', ':', ' ', '/', 't', 's');
                        string Found = inputFile.ReadLine().TrimStart('f', 'N', ':', ' ');

                        InsertName(name, mod, sub, SubTime, VIP, Bit, BitNumber, Found);

                        InsertMessage(message);

                        InsertNameandMessage(name, message);

                        return;
                    }
                }
            }
            catch
            {
                SearchFileForName(File);
            }
        }

        //will search through files for user name and message
        private void SearchFileForBoth(string File)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(File), Path.GetFileName(File));
            int lineNumber = 1;
            try
            {
                using (StreamReader inputFile = new StreamReader(filePath))
                {
                    for (int i = 1; i <= lineNumber; i++)
                    {
                        string message = inputFile.ReadLine();
                        string name = inputFile.ReadLine();

                        return;
                    }
                }
            }
            catch
            {
                SearchFileForBoth(File);
            }

            Console.ReadLine();
        }

        #endregion Folder Search

        #region SQL Insert

        //will insert user message into local db
        private void InsertMessage(string message)
        {
            string connetionString;
            SqlDataReader rdr = null;
            SqlConnection cnn;
            connetionString = @"Data Source=" + machineName + "\\SQLEXPRESS;Initial Catalog=STREAMINGDB;Integrated Security=SSPI;";
            cnn = new SqlConnection(connetionString);
            cnn.Open();

            try
            {
                SqlCommand cmd = new SqlCommand("InsertUserMessage", cnn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@User_Message", message));

                rdr = cmd.ExecuteReader();
                rdr.Close();
            }
            catch (Exception e)
            {
                if (string.IsNullOrEmpty(message))
                {
                    string Message = "BLANK VALUE";
                    SqlCommand cmd = new SqlCommand("InsertUserMessage", cnn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@User_Message", Message));

                    rdr = cmd.ExecuteReader();
                    rdr.Close();
                }
                else
                {
                    string LogEntry1 = DateTime.Now + e.ToString();

                    LogEntryWriter(LogEntry1);
                }
            }

            cnn.Close();
        }

        //will insert user name into local db
        private void InsertName(string Name, string Mod, string Sub, string SubTime, string VIP, string Bit, string BitNumber, string Found)
        {
            string connetionString;
            SqlDataReader rdr = null;
            SqlConnection cnn;
            connetionString = @"Data Source=" + machineName + "\\SQLEXPRESS;Initial Catalog=STREAMINGDB;Integrated Security=SSPI;";
            cnn = new SqlConnection(connetionString);
            cnn.Open();

            try
            {
                SqlCommand cmd = new SqlCommand("InsertUserName", cnn);
                cmd.CommandType = CommandType.StoredProcedure;

                List<SqlParameter> prm = new List<SqlParameter>()
                {
                     new SqlParameter("@UserName", SqlDbType.NVarChar) {Value = Name},
                     new SqlParameter("@Is_Subscribed", SqlDbType.NVarChar) {Value = Sub},
                     new SqlParameter("@Sub_Length", SqlDbType.Int) {Value = SubTime},
                     new SqlParameter("@Is_Moderator", SqlDbType.NVarChar) {Value = Mod},
                     new SqlParameter("@Is_VIP", SqlDbType.NVarChar) {Value = VIP},
                     new SqlParameter("@Given_Bits", SqlDbType.NVarChar) {Value = Bit},
                     new SqlParameter("@Bit_Amount", SqlDbType.Int) {Value = BitNumber},
                     new SqlParameter("@Founder", SqlDbType.NVarChar) {Value = Found}
                };
                cmd.Parameters.AddRange(prm.ToArray());

                rdr = cmd.ExecuteReader();

                rdr.Close();
            }
            catch (Exception e)
            {
                string LogEntry1 = DateTime.Now + e.ToString();

                LogEntryWriter(LogEntry1);
            }

            cnn.Close();
        }

        //will insert user name and message into local db
        private void InsertNameandMessage(string Name, string Message)
        {
            string connetionString;
            SqlDataReader rdr = null;
            SqlConnection cnn;
            connetionString = @"Data Source=" + machineName + "\\SQLEXPRESS;Initial Catalog=STREAMINGDB;Integrated Security=SSPI;";
            cnn = new SqlConnection(connetionString);
            cnn.Open();

            try
            {
                SqlCommand cmd = new SqlCommand("InsertUserNameandMessage", cnn);
                cmd.CommandType = CommandType.StoredProcedure;

                List<SqlParameter> prm = new List<SqlParameter>()
                {
                     new SqlParameter("@User_Name", SqlDbType.NVarChar) {Value = Name},
                     new SqlParameter("@User_Message", SqlDbType.NVarChar) {Value = Message},
                     new SqlParameter("@User_ID", SqlDbType.Int) {Value = 0},
                     new SqlParameter("@Message_ID", SqlDbType.Int) {Value = 0}
                };
                cmd.Parameters.AddRange(prm.ToArray());

                rdr = cmd.ExecuteReader();

                rdr.Close();
            }
            catch (Exception e)
            {
                string LogEntry1 = DateTime.Now + e.ToString();

                LogEntryWriter(LogEntry1);
            }

            cnn.Close();
        }

        #endregion SQL Insert

        #region File Manipulation

        //will move files from one folder to another
        private void MoveFiles(string Start, string End)
        {
            foreach (var file in Directory.GetFiles(Start))
            {
                if (new FileInfo(file).Length == 0)
                {
                    MoveFile(Start, End);
                }
                else
                {
                    File.Move(Path.Combine(Start, Path.GetFileName(file)), Path.Combine(End, Path.GetFileName(file)));
                }
            }
        }

        //will move a file if it is blank
        private void MoveFile(string Start, string End)
        {
            foreach (var file in Directory.GetFiles(Start))
            {
                if (new FileInfo(file).Length == 0)
                {
                    File.Move(Path.Combine(Start, Path.GetFileName(file)), Path.Combine(End, Path.GetFileName(file)));
                }
            }
        }

        //delete all files found in a folder
        private void DeleteFiles(string location)
        {
            foreach (var file in Directory.GetFiles(location))
            {
                File.Delete(Path.Combine(location, file));
            }
        }

        #endregion File Manipulation

        #region Logging

        //This will write to a log file to keep between runs
        private void LogEntryWriter(string LogEntry)
        {
            try
            {
                using (TextWriter text_writer = new StreamWriter(("ChatTextImport Log.txt"), true))
                {
                    text_writer.WriteLine(LogEntry);
                }
            }
            catch (Exception e)
            {
                LogEntryWriter(LogEntry);
            }
        }

        #endregion Logging
    }
}