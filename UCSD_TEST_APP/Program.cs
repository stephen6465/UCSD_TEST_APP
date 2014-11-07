using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.Net;
using System.Web;
using System.Collections;
using System.IO;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace UCSD_TEST_APP
{
    class Program
    {
        
        public UCSDSettings t = new UCSDSettings();
      
        public System.Timers.Timer Timer2 = new System.Timers.Timer();
       
        static void Main(string[] args)
        {
           
            
            DateTime dt = DateTime.Now;

           
              System.Timers.Timer Timer2 = new System.Timers.Timer();
           
                 UCSDSettings t = new UCSDSettings();
             
                 if (!File.Exists("C:\\MVPORTAL_plugin\\MVPortal_PlugIn_config.XML"))
                 {
                     t.Serialize("C:\\MVPORTAL_plugin\\MVPortal_PlugIn_config.XML", t);
                 }
       
                 UCSDSettings t2 = t.Deserialize("C:\\MVPORTAL_plugin\\MVPortal_PlugIn_config.XML");
               
                 t2.Serialize("C:\\MVPORTAL_plugin\\MVPortal_PlugIn_config.XML", t2);

                 try
                 {
                     using (SqlConnection conn = new SqlConnection(t2.ViewConnStr))  //settings.DBConnectionString))
                     {
                         SqlDataReader dr = null;
                         conn.Open();

                         try
                         {
                             SqlCommand cmd = conn.CreateCommand();
                             cmd.CommandText = t2.Query_Deactivate; //settings.DeActivate QueryString First;
                             dr = cmd.ExecuteReader();
                         }
                         catch (Exception ex)
                         {


                         }
                         while (dr.Read())
                         {
                             String ssn = "";
                             bool bactive = true;
                             ssn = TryGetField(t2.SSNField, dr);

                             if (bactive)
                             {

                                 try
                                 {
                                     PortalAckorDeAckUser DeackUser = new PortalAckorDeAckUser(t2, ssn);
                                     String apiDeactive = "/api-client/user/deactivate";
                                     String UriRequestString = t2.Uri + apiDeactive;

                                     var httpWebRequest = (HttpWebRequest)WebRequest.Create(UriRequestString);
                                     httpWebRequest.ContentType = "application/json";
                                     httpWebRequest.Method = "POST";
                                     using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                                     {
                                         string jsonString = SerializeJSon<PortalAckorDeAckUser>(DeackUser);

                                         streamWriter.Write(jsonString);
                                         streamWriter.Flush();
                                         streamWriter.Close();
                                     }
                                     String result2 = "";
                                     var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                                     using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                                     {
                                         result2 = streamReader.ReadToEnd();
                                     }
                                 }
                                 catch (Exception ex)
                                 {   
                                     // 400 level error from deactivating 

                                 }
                             }
                         }
                     }
                 }

                 catch (Exception ex)
                 {

                 }
               
                //In this part we are now doing the get request first to get current info
                // Then we are doing a comparison to see if an update is needed or a activate
                // 

                try
                {
                    using (SqlConnection conn = new SqlConnection(t2.ViewConnStr))  //settings.DBConnectionString))
                    {
                        SqlDataReader dr = null;
                        conn.Open();

                        try
                        {
                            SqlCommand cmd = conn.CreateCommand();
                            cmd.CommandText = t2.Query_activate; //settings.Activate QueryString Second;
                            dr = cmd.ExecuteReader();
                        }
                        catch (Exception ex)
                        {
                            // Put those errors ex returned if this errors to the error log...

                        }
                        while (dr.Read())
                        {
                            String ssn1 = "";
                            String email1 = "";
                            bool bactive1 = true;
                            ssn1 = TryGetField(t2.SSNField, dr);
                            email1 = TryGetField(t2.EmailField, dr);
                            // active = TryGetField(t2.ActiveField, dr);
                            if (bactive1)
                            {

                                
                                PortalAckorDeAckUser AckUser = new PortalAckorDeAckUser(t2, ssn1);
                                PortalUpdateUser UpdateUser = new PortalUpdateUser(t2, ssn1, email1);
                                String apiDeactive = "/api-client/user/activate";
                                String apiUpdate = "/api-client/user";
                             
                                String apiGet = "/api-client/user/";
                                String UriRequestStringActivate = t2.Uri + apiDeactive;
                                String UriRequestStringUpdate = t2.Uri + apiUpdate;
                                String UriGetRequestString = t2.Uri + apiGet + ssn1 + "?appid=" + t2.ClientID;

                                var httpWebRequest = (HttpWebRequest)WebRequest.Create(UriGetRequestString);
                                httpWebRequest.ContentType = "application/json";
                                httpWebRequest.Method = "GET";
                                var obj1 = new PortalUser();
                                String result = "";
                                try  // try the get request to find the patient 
                                {
                                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                                    {
                                        result = streamReader.ReadToEnd();
                                    }
                                    obj1 = DeserializeJSon<PortalUser>(result);

                                    if (obj1.EmailAddress.ToString().Trim().ToUpper() != email1.ToString().Trim().ToUpper())
                                    {
                                        if (!String.IsNullOrEmpty(obj1.MRN))
                                        {
                                            var httpWebRequest3 = (HttpWebRequest)WebRequest.Create(UriRequestStringUpdate);
                                            httpWebRequest3.ContentType = "application/json";
                                            httpWebRequest3.Method = "POST";
                                            PortalUpdateUser UUser = new PortalUpdateUser(t2, ssn1, email1);
                                            using (var streamWriter = new StreamWriter(httpWebRequest3.GetRequestStream()))
                                            {
                                                string jsonString = SerializeJSon<PortalUpdateUser>(UUser);
                                                streamWriter.Write(jsonString);
                                                streamWriter.Flush();
                                                streamWriter.Close();
                                            }
                                        }
                                    }

                                    PortalAckorDeAckUser AckUser2 = new PortalAckorDeAckUser(t2, ssn1);
                                    var httpWebRequest2 = (HttpWebRequest)WebRequest.Create(UriRequestStringActivate);
                                    httpWebRequest2.ContentType = "application/json";
                                    httpWebRequest2.Method = "POST";
                                    try
                                    {
                                        using (var streamWriter = new StreamWriter(httpWebRequest2.GetRequestStream()))
                                        {
                                            string jsonString = SerializeJSon<PortalAckorDeAckUser>(AckUser2);
                                            streamWriter.Write(jsonString);
                                            streamWriter.Flush();
                                            streamWriter.Close();
                                        }
                                    }
                                    catch (Exception ex1)
                                    {
                                        // last 400 error message to account for! Activate 


                                    }


                                }
                                catch(Exception ex)
                                {
                                    // This will create here below and done 
                                    if (String.IsNullOrEmpty(obj1.MRN))
                                    {
                                        var httpWebRequest3 = (HttpWebRequest)WebRequest.Create(UriRequestStringUpdate);
                                        httpWebRequest3.ContentType = "application/json";
                                        httpWebRequest3.Method = "PUT";
                                        PortalUpdateUser UUser = new PortalUpdateUser(t2, ssn1, email1);
                                        try
                                        {
                                            using (var streamWriter = new StreamWriter(httpWebRequest3.GetRequestStream()))
                                            {
                                                string jsonString = SerializeJSon<PortalUpdateUser>(UUser);
                                                streamWriter.Write(jsonString);
                                                streamWriter.Flush();
                                                streamWriter.Close();
                                            }

                                        }
                                        catch(Exception ex1)
                                        {
                                            // 400 level error from creating the user server down probably
                                            //exit and record info
                                        }
                                    }
                                    else
                                    {
                                        PortalAckorDeAckUser AckUser2 = new PortalAckorDeAckUser(t2, ssn1);
                                        var httpWebRequest2 = (HttpWebRequest)WebRequest.Create(UriRequestStringActivate);
                                        httpWebRequest2.ContentType = "application/json";
                                        httpWebRequest2.Method = "POST";
                                        try
                                        {
                                            using (var streamWriter = new StreamWriter(httpWebRequest2.GetRequestStream()))
                                            {
                                                string jsonString = SerializeJSon<PortalAckorDeAckUser>(AckUser2);
                                                streamWriter.Write(jsonString);
                                                streamWriter.Flush();
                                                streamWriter.Close();
                                            }
                                        }
                                        catch(Exception ex1)
                                        {
                                            // last 400 error message to account for! Activate if get fails


                                        }

                                    }

                                }
                             }


                        }
                    }
                }

                catch (Exception ex)
                {

                    t2.Serialize("C:\\MVPORTAL_plugin\\MVPortal_PlugIn_config.XML", t2);
                }  
                t2.Serialize("C:\\MVPORTAL_plugin\\MVPortal_PlugIn_config.XML", t2);
            }
        
        internal static DataSet GetDataSet(String connectionString, String query)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        SqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = query;
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        return ds;
                    }
                    catch (Exception ex)
                    {
                        DataSet ds = new DataSet();
                        DataTable dt = new DataTable("errtable");
                        dt.Columns.Add("ErrorMessage", typeof(String));
                        DataRow dr = dt.NewRow();
                        dr["ErrorMessage"] = ex.ToString();
                        dt.Rows.Add(dr);
                        ds.Tables.Add(dt);
                        return ds;
                    }
                }
            }
            catch (Exception ex)
            {
                DataSet ds = new DataSet();
                DataTable dt = new DataTable("errtable");
                dt.Columns.Add("ErrorMessage", typeof(String));
                DataRow dr = dt.NewRow();
                dr["ErrorMessage"] = ex.ToString();
                dt.Rows.Add(dr);
                ds.Tables.Add(dt);
                return ds;
            }
        }

        private String GetID(OleDbConnection mvdata)
        {
            bool exists = true;
            String id = "";

            while (exists)
            {
                id = RndLongStr(9999999999);
                OleDbCommand ocmd = mvdata.CreateCommand();
                ocmd.CommandText = "select id from puser where id=?";
                ocmd.Parameters.Add("?id", OleDbType.Char).Value = id;
                OleDbDataReader odr = ocmd.ExecuteReader();
                exists = false;
                if (odr.Read()) exists = true;
                odr.Close();
            }
            return id;
        }

        private String RndLongStr(long max)
        {
            Random r = new Random();
            int i1 = r.Next(), i2 = r.Next();
            long l = (i1 << 32) | i2;
            while (l > max)
            {
                i1 = r.Next(); i2 = r.Next();
                l = (i1 << 32) | i2;
            }
            return l.ToString();
        }
        private static String TryGetField(String field, SqlDataReader dr)
        {
            if (String.IsNullOrEmpty(field)) return "";
            try
            {
                String s = dr[field].ToString();
                return s.Trim();
            }
            catch (Exception) { return ""; }
        }

        public static string SerializeJSon<T>(T t)
        {
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(T));
            DataContractJsonSerializerSettings s = new DataContractJsonSerializerSettings();
            ds.WriteObject(stream, t);
            string jsonString = Encoding.UTF8.GetString(stream.ToArray());
            stream.Close();
            return jsonString;
        }

        public static T DeserializeJSon<T>(string jsonString)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            T obj = (T)ser.ReadObject(stream);
            return obj;
        }
    }

    [Serializable()]
    [XmlRoot("UCSDSettings")]
    public class UCSDSettings
    {

        public UCSDSettings()
        {
            this.ActiveField = " ";
            this.ClientID = " ";
            this.EmailField = " ";
            this.LastRun = " ";
            this.MVDataPath = " ";
            this.Query_activate = " ";
            this.Query_Deactivate = " ";
            this.Server = " ";
            this.SQLPassword = " ";
            this.SQLUserID = " ";
            this.SSNField = " ";
            this.Uri = " ";
            this.ViewConnStr = " ";
            
        }

        [XmlElement("SQLServer")]
        public String Server { get; set; }

        [XmlElement("Query_Deactivate")]
        public String Query_Deactivate { get; set; }

        [XmlElement("ClientID")]
        public String ClientID { get; set; }

        [XmlElement("SQLPassword")]
        public String SQLPassword { get; set; }
        [XmlElement("SQLUserID")]
        public String SQLUserID { get; set; }

        [XmlElement("Query_activate")]
        public String Query_activate { get; set; }

        [XmlElement("LastRun")]
        public String LastRun { get; set; }

        [XmlElement("SSNField")]
        public String SSNField { get; set; }

        [XmlElement("EmailField")]
        public String EmailField { get; set; }

        [XmlElement("ActiveField")]
        public String ActiveField { get; set; }

        [XmlElement("ViewConnStr")]
        public String ViewConnStr { get; set; }

        [XmlElement("Uri")]
        public String Uri { get; set; }

        [XmlElement("MVDataPath")]
        public String MVDataPath { get; set; }

        public void Serialize(string file, UCSDSettings c)
        {
            System.Xml.Serialization.XmlSerializer xs
               = new System.Xml.Serialization.XmlSerializer(c.GetType());
            string path1 = Path.GetDirectoryName(file);
            if (!Directory.Exists(path1))
            {
                Directory.CreateDirectory(path1);
            }
            StreamWriter writer = File.CreateText(file);
            xs.Serialize(writer, c);
            writer.Flush();
            writer.Close();
        }
        public UCSDSettings Deserialize(string file)
        {
            System.Xml.Serialization.XmlSerializer xs
               = new System.Xml.Serialization.XmlSerializer(
                  typeof(UCSDSettings));
            StreamReader reader = File.OpenText(file);
            UCSDSettings c = (UCSDSettings)xs.Deserialize(reader);
            reader.Close();
            return c;
        }

    }

    [DataContract]
    public class PortalAckorDeAckUser
    {
        [DataMember]
        public string MRN { get; set; }
        [DataMember]
        public string Appid { get; set; }

        public PortalAckorDeAckUser(UCSDSettings t, String Ssn)
        {
            this.Appid = t.ClientID;
            this.MRN = Ssn;
        }

    }
    [DataContract]
    public class PortalUpdateUser
    {

        [DataMember]
        public string mrn { get; set; }
        [DataMember]
        public string appid { get; set; }

        [DataMember]
        public string email { get; set; }

        public PortalUpdateUser(UCSDSettings t, String Ssn, String email_addr)
        {
            this.appid = t.ClientID;
            this.mrn = Ssn;
            this.email = email_addr;
        }

    }

    [DataContract]
    public class PortalUser
    {
        [DataMember]
        public string Account { get; set; }
        [DataMember]
        public string AccountUserType { get; set; }
        [DataMember]
        public string Active { get; set; }
        [DataMember]
        public string PortalUserName { get; set; }
        [DataMember]
        public string EmailAddress { get; set; }
        [DataMember]
        public string MRN { get; set; }
    }


}

