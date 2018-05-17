using Riss.Devices;
using System;
using System.Collections.Generic;
using System.Text;
using KSoftFingerPrintManager.Business;
using KSoftFingerPrintManager.IConvert;
using MySql.Data.MySqlClient;
using MySql.Data;
using System.Timers;
using System.Configuration;

namespace KSoftFingerPrintManager.Entity
{
    class Engine
    {
        private int DN;
        private String Password;
        private String Model;
        private int ConnectionModel;
        private String IPAddress;
        private int IPPort;
        private CommunicationType CommType;

        private Device device;
        private DeviceConnection deviceConnection;
        private DeviceCommEty deviceEty;
        private MySqlConnection databaseConnection = null;
        private String connectionString = "User Id=root;Password=;Server=localhost;Database=ksofterp_payroll";
        private String MaxWorkHours = ConfigurationManager.AppSettings["MaxWorkingHours"];
        private DateTime last_processed_entry;
        private bool shouldInsertLastEntry = false;


        public void Init(int DN, String Password, String Model, int ConnectionModel, String IPAddress, int IPPort, CommunicationType CommType)
        {
            this.DN = DN;
            this.Password = Password;
            this.Model = Model;
            this.ConnectionModel = ConnectionModel;
            this.IPAddress = IPAddress;
            this.IPPort = IPPort;
            this.CommType = CommType;
            
        }
        public void Run()
        {
            Logger.log("\n\n\n*************** Timer fired at " + DateTime.Now + " ****************");
            if(OpenConnection())
            {
                //TruncateTable();
                //logDB();
                FetchData();
                logDB();

                
            }
            else
            {

            }
        }


        private bool ProcessData(List<Record> recordList)
        {
            
            int no = 1;
            String time = String.Empty;
            foreach (Record record in recordList)
            {
                string type = ConvertObject.IOMode(record.Verify);
                string mode = ConvertObject.GLogType(record.Action);
                time = record.Clock.ToString("yyyy-MM-dd HH:mm:ss");
                String text =  no.ToString() +"|"+ record.DN.ToString()+"|" + record.DIN.ToString() + "|"+ type +"|" + mode +"|"+ time;
                
                if (type.Equals("Check in"))
                {
                    Logger.log(text);
                    if( AddToDatabase(record))
                    {
                        no++;
                        UpdateLastEntryTime(time);
                    }
                    
                }
            }
            //Util.UpdateTime(time);
            return true;
        }

        private void UpdateLastEntryTime(string time)
        {
            if (databaseConnection == null)
                databaseConnection = new MySqlConnection(connectionString);
            try
            {
                MySqlCommand command = databaseConnection.CreateCommand();
                if(shouldInsertLastEntry)
                {
                    command.CommandText = "insert into last_entry SET last_entry_time = '" + time + "'";
                    Logger.log("last entry time inserted");
                    shouldInsertLastEntry = false;
                }
                else
                {
                    command.CommandText = "UPDATE last_entry SET last_entry_time = '" + time + "' ORDER BY last_entry_time DESC LIMIT 1";
                    Logger.log("last entry time updated");
                }
                databaseConnection.Open();
                command.ExecuteNonQuery();
                databaseConnection.Close();
            }
            catch(Exception ex)
            {
                Logger.log(ex.Message);
            }
            
        }

        private bool validateEntry(String dateTime)
        {
            DateTime currentEntry = Util.ConvertToDateTime(dateTime);
            DateTime lastProcessedEntry = last_processed_entry;

            TimeSpan diff = currentEntry - lastProcessedEntry;

            if(diff.TotalSeconds <= 0)
            {
                Logger.log("VALIDATE ENTRY - Same as last entry");
                return false;
            }
            return true;
        }
        private bool ComparableWithLastEntry(String lastDateTime, String currentDateTime)
        {
            DateTime curTime = Util.ConvertToDateTime(currentDateTime );
            DateTime lastTime = Util.ConvertToDateTime(lastDateTime);

            double diff = curTime.Subtract(lastTime).TotalHours;

            try
            {
                if (diff > Double.Parse(MaxWorkHours))
                {
                    Logger.log("VALIDATE ENTRY - More than 18 hours later - " + diff);
                    return false;
                }
                
            }
            catch (Exception ex)
            {
                Logger.log("VALIDATE ENTRY - Incorrect MaxWorkingHours configuration - " + MaxWorkHours);
                Logger.log(ex.Message);
            }
            Logger.log("VALIDATE ENTRY - " + diff);
            return true;
        }
        private bool GetLastEntryTime()
        {
            if (databaseConnection == null)
                databaseConnection = new MySqlConnection(connectionString);
            try
            {
                MySqlCommand command = databaseConnection.CreateCommand();
                command.CommandText = "SELECT last_entry_time from last_entry ORDER BY last_entry_time DESC LIMIT 1";
                databaseConnection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    String last_time = SafeGetString(reader, 0);
                    last_processed_entry = Convert.ToDateTime(last_time);
                    Logger.log("LAST ENTRY in DATABASE : " + last_time);
                    reader.Close();
                    databaseConnection.Close();
                }
                else
                {
                    last_processed_entry = Util.MinDateTime();
                    shouldInsertLastEntry = true;
                }
            }
            catch(Exception ex)
            {
                Logger.log(ex.Message);
                return false;
            }
            
            return true;
        }
        private bool AddToDatabase(Record record)
        {
            if (databaseConnection == null)
                databaseConnection = new MySqlConnection(connectionString);

            String emp_id = record.DIN.ToString();
            //String date = record.Clock.ToString("yyyy-MM-dd");
            String dateTime = record.Clock.ToString("yyyy-MM-dd HH:mm:ss");

            if(!validateEntry(dateTime))
            {
                return false;
            }

            try
            {
                MySqlCommand command = databaseConnection.CreateCommand();
                command.CommandText = "SELECT time_in, time_out from attendance WHERE emp_id = " + emp_id + " ORDER BY time_in DESC LIMIT 1";
                databaseConnection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                                
                if (reader.Read())
                {
                    String last_time_in = SafeGetString(reader, 0);
                    String last_time_out = SafeGetString(reader, 1);
                    String date_in = Convert.ToDateTime(last_time_in).ToString("yyyy-MM-dd HH:mm:sss");
                    
                    Logger.log("LAST ENTRY TIMES : " + date_in  + "-" + dateTime);

                    if (last_time_out.Equals(String.Empty))
                    {
                        reader.Close();

                        if(ComparableWithLastEntry(date_in, dateTime))
                        {
                            command.CommandText = "UPDATE attendance SET time_out = '" + dateTime + "' WHERE emp_id = " + emp_id + " ORDER BY time_in DESC LIMIT 1";
                            command.ExecuteNonQuery();
                            Logger.log("update time_out");
                        }
                        else
                        {
                            command.CommandText = "INSERT INTO attendance VALUES (" + emp_id + ", '" + dateTime + "', NULL)";
                            command.ExecuteNonQuery();
                            Logger.log("insert time_in");
                        }
                        
                    }
                    else
                    {
                        reader.Close();
                        command.CommandText = "INSERT INTO attendance VALUES (" + emp_id + ", '" + dateTime + "', NULL)";
                        command.ExecuteNonQuery();
                        Logger.log("insert time_in");
                    }
                }
                else
                {
                    reader.Close();
                    command.CommandText = "INSERT INTO attendance VALUES (" + emp_id + ", '" + dateTime + "', NULL)";
                    command.ExecuteNonQuery();
                    Logger.log("insert time_in");
                }
                databaseConnection.Close();

            }
            catch( Exception ex)
            {
                databaseConnection.Close();
                Logger.log(ex.Message);
                return false;
            }
            return true;
        }
        public static string SafeGetString(MySqlDataReader reader, int colIndex)
        {
            if (!reader.IsDBNull(colIndex))
                return reader.GetString(colIndex);
            return string.Empty;
        }
        private void logDB()
        {
            if (databaseConnection == null)
                databaseConnection = new MySqlConnection(connectionString);

            try
            {
                MySqlCommand command = databaseConnection.CreateCommand();
                command.CommandText = "SELECT emp_id, time_in, time_out from attendance";
                databaseConnection.Open();
                MySqlDataReader reader = command.ExecuteReader();

                Logger.log("========================Database========================");

                while (reader.Read())
                {
                    String text = SafeGetString(reader, 0) + "\t\t|\t" + SafeGetString(reader, 1) + "\t|\t" + SafeGetString(reader, 2);
                    Logger.log(text);                    
                }
                reader.Close();
                databaseConnection.Close();

            }
            catch (Exception ex)
            {
                Logger.log(ex.Message);
            }

        }
        private bool FetchData()
        {
            object extraProperty = new object();
            object extraData = new object();
            extraData = Global.DeviceBusy;

            try
            {
                if(GetLastEntryTime() == false)
                {
                    Logger.log("Database exception : Abort fetching data");
                    return false;
                }
                List<DateTime> dtList = GetDateTimeList();
                bool result = deviceConnection.SetProperty(DeviceProperty.Enable, extraProperty, device, extraData);
                extraProperty = false;
                extraData = dtList;
                result = deviceConnection.GetProperty(DeviceProperty.AttRecordsCount, extraProperty, ref device,
                    ref extraData);
                if (false == result)
                {
                    Logger.log("Get All Glog Fail");
                    return false;
                }

                int recordCount = (int)extraData;
                if (0 == recordCount)
                {
                    Logger.log("Get All Glog Fail : zero fetch count");
                    return false;
                }

                List<bool> boolList = new List<bool>();
                boolList.Add(false);
                boolList.Add(false);//清除新日志标记，false则不清除
                extraProperty = boolList;
                extraData = dtList;
                result = deviceConnection.GetProperty(DeviceProperty.AttRecords, extraProperty, ref device, ref extraData);
                if (result)
                {
                    List<Record> recordList = (List<Record>)extraData;
                    return ProcessData(recordList);
                }
                else
                {
                    Logger.log("Get All Glog Fail");
                }
            }
            catch (Exception ex)
            {
                Logger.log("Get All Glog Fail : Exception");
                Logger.log(ex.Message);
            }
            finally
            {
                extraData = Global.DeviceIdle;
                deviceConnection.SetProperty(DeviceProperty.Enable, extraProperty, device, extraData);
            }
            return false;
        }

        private List<DateTime> GetDateTimeList()
        {
            List<DateTime> dtList = new List<DateTime>();
            DateTime min = last_processed_entry;
            DateTime max = InitData.MaxDateTime;
            dtList.Add(min);
            dtList.Add(max);
            Logger.log("TIME INTERVAL : " + min + " - " + max);
            return dtList;
        }

        private bool OpenConnection()
        {
            try
            {
                device = new Device();

                device.DN = DN;
                device.Password = Password;
                device.Model = Model;
                device.ConnectionModel = ConnectionModel;

                device.IpAddress = IPAddress;
                device.IpPort = IPPort;
                device.CommunicationType = CommType;

                deviceConnection = DeviceConnection.CreateConnection(ref device);

                if (deviceConnection.Open() > 0)
                {
                    deviceEty = new DeviceCommEty();
                    deviceEty.Device = device;
                    deviceEty.DeviceConnection = deviceConnection;
                    Logger.log("Connection successful");
                    return true;
                }
                else
                {
                    Logger.log("Cannot open device connection");
                    return false;                    
                    //MessageBox.Show("Connect Device Fail", "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.log("Cannot create connection");
                Logger.log(ex.Message);
                return false;
                //MessageBox.Show(ex.Message, "Error1", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
