using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Web.Configuration;

namespace Lib.Service
{
    public class Service
    {
        string conn_str = WebConfigurationManager.ConnectionStrings["conn_string"].ConnectionString.ToString();
        string local_conn_str = WebConfigurationManager.ConnectionStrings["local_conn_string"].ConnectionString.ToString();
        string IsDebug = WebConfigurationManager.AppSettings["Debug"].ToString();
        string IsLocal = WebConfigurationManager.AppSettings["Local"].ToString();
        string csql = "";
        DataSet ds = new DataSet();
        //Log 記錄
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #region Mail
        public DataTable Mail(string From,string From_Name,string M_To,string M_CC,string subject,string body)
        {
            string err_msg = "";
            string c_msg = "";
            string status = "";
            string[] c_To;
            string[] c_CC;

            string str_Host = "";
            string str_Account = "";
            string str_Pwd = "";
            string str_Port = "";
            string str_SSL = "";
            int int_Port = 0;

            

            DataTable dt = new DataTable();
            DataRow dw;

            dt.Columns.Add("status", System.Type.GetType("System.String"));
            dt.Columns.Add("c_msg", System.Type.GetType("System.String"));
            dt.Columns.Add("err_msg", System.Type.GetType("System.String"));

            status = "N";

            SqlConnection conn = new SqlConnection(conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            //抓取Smtp Server 設定
            try
            {
                csql = "SELECT ms_id, _BD_ID, _BD_DT, _UPD_ID, _UPD_DT, Host, Port, Account, Pwd, SSL FROM Mail_Server";
                cmd.CommandText = csql;
                if (ds.Tables["m_server"] != null)
                {
                    ds.Tables["m_server"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "m_server");
                news_ada = null;

                if(ds.Tables["m_server"].Rows.Count > 0)
                {
                    str_Host = ds.Tables["m_server"].Rows[0]["Host"].ToString();
                    str_Account = ds.Tables["m_server"].Rows[0]["Account"].ToString();
                    str_Pwd = ds.Tables["m_server"].Rows[0]["Pwd"].ToString();
                    str_Port = ds.Tables["m_server"].Rows[0]["Port"].ToString();
                    str_SSL = ds.Tables["m_server"].Rows[0]["SSL"].ToString();
                    int_Port = Convert.ToInt32(str_Port);
                    //=====================================//
                    From = str_Account;
                }
                else
                {
                    str_Host = "";
                    str_Account = "";
                    str_Pwd = "";
                    str_Port = "";
                    str_SSL = "";
                    int_Port = 0;
                }
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
            }


            try
            {
                

                System.Net.Mail.MailMessage em = new System.Net.Mail.MailMessage();
                //寄件者
                em.From = new System.Net.Mail.MailAddress(From, From_Name, System.Text.Encoding.UTF8);
                //收件者
                if (M_To.Trim().Length > 0)
                {
                    c_To = M_To.Split(',');
                    for(int i = 0; i < c_To.Length; i++)
                    {
                        //em.To.Add(new System.Net.Mail.MailAddress("收件信箱"));
                        em.To.Add(new System.Net.Mail.MailAddress(c_To[i]));
                    }
                }

                //副本
                if(M_CC.Trim().Length > 0)
                {
                    c_CC = M_CC.Split(',');
                    for (int i = 0; i < c_CC.Length; i++)
                    {
                        //em.To.Add(new System.Net.Mail.MailAddress("收件信箱"));
                        em.CC.Add(new System.Net.Mail.MailAddress(c_CC[i]));
                    }
                }

                //em.Bcc.Add(new System.Net.Mail.MailAddress("gary661116@gmail.com"));
                //em.Bcc.Add(new System.Net.Mail.MailAddress("boy0039@gmail.com"));
                //boy0039@gmail.com
                //主題
                em.Subject = subject;
                em.SubjectEncoding = System.Text.Encoding.UTF8;
                //內容
                em.Body = body; 
                em.BodyEncoding = System.Text.Encoding.UTF8;
                em.IsBodyHtml = true;	  //信件內容是否使用HTML格式
                //----------------------------------------------------------------------------//
                //Mail Server 設定
                System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient();
                //=== Google 設定 ===//
                //登入帳號認證
                //smtp.Credentials = new System.Net.NetworkCredential("帳號", "密碼");
                //使用587 Port - google要設定
                //smtp.Port = 587;
                //smtp.EnableSsl = true;   //啟動SSL 
                //end of google設定
                //smtp.Host = "smtp.gmail.com";   //SMTP伺服器
                //=================//
                //=== 一般設定 ====//
                //smtp.Host = "msa.hinet.net";   //SMTP伺服器
                //===============//
                //=== IsCom (Office365 設定) == //
                //====登入帳號認證設定 == //
                smtp.UseDefaultCredentials = false;
                //smtp.Credentials = new System.Net.NetworkCredential("biztest@azuretestsandbox.onmicrosoft.com", "Suha7367");
                smtp.Credentials = new System.Net.NetworkCredential(str_Account, str_Pwd);
                //=====================//
                //=====Port 設定=====//
                //使用587 Port - Office365 要設定
                //smtp.Port = 587;
                smtp.Port = int_Port;
                //====================//
                //==== SSL 設定 =======//
                //smtp.EnableSsl = true;   //啟動SSL 
                //啟動SSL 
                if (str_SSL == "Y")
                {
                    smtp.EnableSsl = true;
                }
                else
                {
                    smtp.EnableSsl = false;
                }
                //=====================//
                //end of Office365設定
                //===== Host 設定 ======//
                //smtp.Host = "smtp.office365.com";   //SMTP伺服器
                smtp.Host = str_Host;   //SMTP伺服器
                //=====================//
                //----------------------------------------------------------------------------//
                smtp.Send(em);            //寄出
                status = "Y";
            }
            catch (Exception ex)
            {
                status = "N";
                err_msg = ex.Message;
            }
            finally
            {
                dw = dt.NewRow();
                dw["status"] = status;
                dw["c_msg"] = c_msg;
                dw["err_msg"] = err_msg;
                dt.Rows.Add(dw);
            }
            return dt;
        }
        #endregion Mail

        #region 連結字串取得 conn_string
        //連結字串取得
        public string conn_string()
        {
            string connstr = "";
            if (IsLocal == "On")
            {
                connstr = local_conn_str;
            }
            else
            {
                connstr = conn_str;
            }
            return connstr;
        }
        #endregion 連結字串取得 conn_string
    }
}