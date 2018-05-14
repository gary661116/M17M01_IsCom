using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Web.Configuration;

namespace Lib.Service
{
    public class DBService
    {
        string IsDebug = WebConfigurationManager.AppSettings["Debug"].ToString();
        string csql = "";

        DataSet ds = new DataSet();

        Service CService = new Service();

        //Log 記錄
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        #region 最新消息 News

        #region 消息資料抓取 News_List
        public DataTable News_List(ref string err_msg, string news_id = "", string sort = "", string status = "", string title_query = "", string start_date = "", string end_date = "", string is_index = "")
        {
            DataTable dt = new DataTable();

            SqlConnection conn = new SqlConnection(CService.conn_string());

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            string[] Array_news_id;
            string[] Array_title_query;

            try
            {
                Array_news_id = news_id.Split(',');
                Array_title_query = title_query.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select distinct "
                     + "  a1.n_id, a1.n_title, convert(nvarchar(10),a1.n_date,23) as n_date, a1.n_url, a1.n_desc, a1.n_memo "
                     + ", a1.is_index, a1.sort, a1.status "
                     + "from "
                     + "   news a1 "
                     + "where "
                     + "  a1.status <> 'D' ";

                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (news_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.n_id in (";
                    for (int i = 0; i < Array_news_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_news_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.n_title like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                if (start_date.Trim().Length > 0)
                {

                    csql = csql + "and a1.n_date >= @start_date ";
                }

                if (end_date.Trim().Length > 0)
                {

                    csql = csql + "and a1.n_date <= @end_date ";
                }

                if (is_index.Trim().Length > 0)
                {
                    csql = csql + "and a1.is_index = @is_index ";
                }

                csql = csql + ")a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.n_date desc ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }

                if (start_date.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@start_date", start_date);
                }

                if (end_date.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@end_date", end_date);
                }

                if (news_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_news_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_news_id" + i.ToString(), Array_news_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                if (is_index.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@is_index", is_index);
                }
                //--------------------------------------------------------------//

                if (ds.Tables["news"] != null)
                {
                    ds.Tables["news"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "news");
                news_ada = null;

                dt = ds.Tables["news"];

            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region 消息資料新增 News_Insert
        public string News_Insert(string n_title = "", string n_date = "", string n_desc = "", string is_show = "", string is_index = "", string sort = "",string n_memo = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from News";
                cmd.CommandText = csql;

                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//

                csql = @"insert into News(n_title,n_date,n_desc,is_index,sort,status,n_memo) "
                     + "values(@n_title,@n_date,@n_desc,@is_index,@sort,@is_show,@n_memo)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@n_title", n_title);
                cmd.Parameters.AddWithValue("@n_date", n_date);
                cmd.Parameters.AddWithValue("@n_desc", n_desc);
                cmd.Parameters.AddWithValue("@is_index", is_index);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);
                cmd.Parameters.AddWithValue("@n_memo", n_memo);

                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 消息資料更新 News_Update
        //更新內容
        public string News_Update(string n_id = "", string n_title = "", string n_date = "", string n_desc = "", string is_show = "", string is_index = "", string sort = "",string n_memo = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  news "
                     + "set "
                     + "  n_title = @n_title "
                     + ", n_date = @n_date "
                     + ", n_desc = @n_desc "
                     + ", status = @is_show "
                     + ", is_index = @is_index "
                     + ", sort = @sort "
                     + ", n_memo = @n_memo "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  n_id = @n_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@n_id", n_id);
                cmd.Parameters.AddWithValue("@n_title", n_title);
                cmd.Parameters.AddWithValue("@n_date", n_date);
                cmd.Parameters.AddWithValue("@n_desc", n_desc);
                cmd.Parameters.AddWithValue("@is_index", is_index);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);
                cmd.Parameters.AddWithValue("@n_memo", n_memo);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 消息資料刪除 News_Del
        public string News_Del(string n_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from "
                     + "  news "
                     + "where "
                     + "  n_id = @n_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@n_id", n_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion
        #endregion

        #region 首頁影片資料 Video_List
        public DataTable Video_List()
        {
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            csql = "select "
                 + "  a1.* "
                 + "from "
                 + "  Advertisement a1 "
                 + "where "
                 + "  ad_title = 'img' ";

            cmd.CommandText = csql;

            if (ds.Tables["video"] != null)
            {
                ds.Tables["video"].Clear();
            }

            SqlDataAdapter video_ada = new SqlDataAdapter();
            video_ada.SelectCommand = cmd;
            video_ada.Fill(ds, "video");
            video_ada = null;

            cmd = null;

            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            conn = null;

            return ds.Tables["video"];
        }
        #endregion

        #region 首頁影片資料更新 Video_Update
        public string Video_Update(string mv)
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = "update "
                     + "  Advertisement "
                     + "set "
                     + "  ad_mv = @mv "
                     + "where "
                     + "  ad_id = 1";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@mv", mv);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 關於我們 Com_List
        //Com_List("AboutUs", lang)
        public DataTable Com_List(ref string err_msg, string category = "", string lang = "")
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "  Company_Info a1 "
                     + "where "
                     + "    a1.category = @category "
                     + "and a1.lang = @lang";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@category", category);
                cmd.Parameters.AddWithValue("@lang", lang);

                if (ds.Tables["com_info"] != null)
                {
                    ds.Tables["com_info"].Clear();
                }

                SqlDataAdapter com_info_ada = new SqlDataAdapter();
                com_info_ada.SelectCommand = cmd;
                com_info_ada.Fill(ds, "com_info");
                com_info_ada = null;

                dt = ds.Tables["com_info"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region 關於我們更新 Com_Update
        //DB.Com_Update("AboutUs", lang, com_desc);
        public string Com_Update(string category = "", string lang = "", string com_desc = "")
        {
            string c_msg = "";
            string err_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //檢查是否有資料
                DataTable d_com_info = Com_List(ref err_msg, category, lang);
                if (d_com_info.Rows.Count > 0)
                {
                    csql = "update "
                         + "  Company_Info "
                         + "set "
                         + "  com_desc = @com_desc "
                         + "where "
                         + "    lang = @lang "
                         + "and category = @category";
                }
                else
                {
                    csql = "insert into "
                         + "Company_Info(com_desc, lang, category) "
                         + "Values(@com_desc,@lang,@category) ";
                }

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@com_desc", com_desc);
                cmd.Parameters.AddWithValue("@lang", lang);
                cmd.Parameters.AddWithValue("@category", category);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 圖片新增 Img_Insert
        public string Img_Insert(string img_no = "", string img_file = "", string img_sty = "",string c_dbf = "")
        {
            string c_msg = "";
            string dbf_name = "";
            dbf_name = c_dbf + "_Img";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into " + dbf_name + "(img_no, img_file, img_sty) "
                     + "values(@img_no ,@img_file ,@img_sty)";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.Parameters.AddWithValue("@img_file", img_file);
                cmd.Parameters.AddWithValue("@img_sty", img_sty);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 圖片刪除 Img_Delete
        public string Img_Delete(string img_id = "",string c_dbf = "Activity")
        {
            string c_msg = "";
            string dbf_name = c_dbf + "_Img";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from " + dbf_name + " where img_id = @img_id ";

                cmd.CommandText = csql;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_id", img_id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 圖片更新 Img_Update
        public string Img_Update(string img_id = "", string img_file = "",string img_desc = "",string is_index="",string c_dbf="Activity")
        {
            string c_msg = "";
            string dbf_name = "";
            string c_update = "";

            dbf_name = c_dbf + "_Img";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;


            try
            {
                if(img_file.Trim().Length > 0)
                {
                    if(c_update.Trim().Length > 0)
                    {
                        c_update += ",";
                    }
                    c_update += " img_file = @img_file ";
                }

                if (img_desc.Trim().Length > 0)
                {
                    if (c_update.Trim().Length > 0)
                    {
                        c_update += ",";
                    }
                    c_update += " img_desc = @img_desc ";
                }

                if(is_index.Trim().Length > 0)
                {
                    if (c_update.Trim().Length > 0)
                    {
                        c_update += ",";
                    }
                    c_update += " is_index = @is_index ";
                }

                csql = @"update "
                     + "  " + dbf_name + " "
                     + "set "
                     + c_update + " "
                     + "where "
                     + "  img_id = @img_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                
                cmd.Parameters.AddWithValue("@img_id", img_id);
                if (img_file.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@img_file", img_file);
                }

                if (img_desc.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@img_desc", img_desc);
                }

                if (is_index.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@is_index", is_index);
                }
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 圖片陳列 Img_List
        public DataTable Img_List(ref string err_msg, string img_no = "", string img_sty = "", string c_dbf = "")
        {
            DataSet dt = new DataSet();
            DataTable d_t = new DataTable();

            string dbf_name = "";
            string[] cimg_no;
            string str_img_no = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                dbf_name = c_dbf + "_Img";
                cimg_no = img_no.Split(',');

                for (int i = 0; i < cimg_no.Length; i++)
                {
                    if (i > 0)
                    {
                        str_img_no = str_img_no + ",";
                    }
                    str_img_no = str_img_no + "'" + cimg_no[i] + "'";
                }



                csql = "select * from " + dbf_name + " where status = 'Y' and img_no in (";
                for (int i = 0; i < cimg_no.Length; i++)
                {
                    if (i > 0)
                    {
                        csql = csql + ",";
                    }
                    csql = csql + "@str_img_no" + i.ToString() + " ";
                }
                csql = csql + ") ";
                if (img_sty.Trim().Length > 0)
                {
                    csql = csql + "and img_sty= @img_sty ";
                }
                csql = csql + "order by ";
                csql = csql + "  img_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                if (img_sty.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@img_sty", img_sty);
                }

                for (int i = 0; i < cimg_no.Length; i++)
                {
                    cmd.Parameters.AddWithValue("@str_img_no" + i.ToString(), cimg_no[i]);
                }


                if (dt.Tables["img"] != null)
                {
                    dt.Tables["img"].Clear();
                }

                SqlDataAdapter img_ada = new SqlDataAdapter();
                img_ada.SelectCommand = cmd;
                img_ada.Fill(dt, "img");
                img_ada = null;

                d_t = dt.Tables["img"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
                dt = null;
            }

            return d_t;
        }
        #endregion

        #region 使用者資訊 User_Info
        public DataTable User_Info(ref string err_msg, string account = "")
        {
            DataSet dt = new DataSet();
            DataTable d_t = new DataTable();

            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            try
            {
                csql = @"select "
                     + "  * "
                     + "from "
                     + "  member "
                     + "where "
                     + "   status <> 'D' "
                     + "and account = @account "
                     + "order by "
                     + "  _SEQ_ID ";



                if (dt.Tables["user_info"] != null)
                {
                    dt.Tables["user_info"].Clear();
                }

                SqlDataAdapter user_info_ada = new SqlDataAdapter();

                cmd.CommandText = csql;
                ////定義parameter型別
                cmd.Parameters.Clear();
                //CMD.Parameters.AddWithValue(@account, account);
                cmd.Parameters.Add("@account", SqlDbType.NVarChar, 15).Value = account; //(參數,宣考型態,長度)

                user_info_ada.SelectCommand = cmd;
                user_info_ada.Fill(dt, "user_info");
                user_info_ada = null;

                d_t = dt.Tables["user_info"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
                dt = null;
            }

            return d_t;
        }
        #endregion

        #region 變更密碼 User_Update
        public string User_Update(string account = "", string pwd = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  member "
                     + "set "
                     + "  pwd = @pwd "
                     + "where "
                     + "  account = @account ";


                cmd.CommandText = csql;

                ////定義parameter型別
                cmd.Parameters.Clear();

                cmd.Parameters.Add("@account", SqlDbType.NVarChar, 30).Value = account; //(參數,宣考型態,長度)
                cmd.Parameters.Add("@pwd", SqlDbType.NVarChar, 30).Value = pwd; //(參數,宣考型態,長度)

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 首頁廣告圖片陳列 Ad_Img_List
        public DataTable Ad_Img_List(ref string err_msg, string img_no = "",string status="")
        {
            DataSet dt = new DataSet();
            DataTable d_t = new DataTable();
            
            string[] cimg_no;
            string str_img_no = "";
            //if(img_no == "")
            //{
            //    imgno_count = -1;
            //}
            //else
            //{
            //    imgno_count = 0;
            //}

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;



            try
            {
                cimg_no = img_no.Split(',');
                for (int i = 0; i < cimg_no.Length; i++)
                {
                    if (i > 0)
                    {
                        str_img_no = str_img_no + ",";
                    }
                    str_img_no = str_img_no + "'" + cimg_no[i] + "'";
                }

                csql = "select * from Advertisement where ad_title = 'img' ";
                if (status.Trim().Length > 0)
                {
                    csql += "and status = @status ";
                }
                //if(imgno_count == 0)
                //{
                //    csql = csql + "and ad_no in (";
                //    for (int i = 0; i < cimg_no.Length; i++)
                //    {
                //        if (i > 0)
                //        {
                //            csql = csql + ",";
                //        }
                //        csql = csql + "@str_img_no" + i.ToString() + " ";
                //    }
                //    csql = csql + ") ";
                //}

                cmd.CommandText = csql;
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@status", status);
                }

                //if(imgno_count == 0)
                //{
                //    cmd.Parameters.Clear();
                //    for (int i = 0; i < cimg_no.Length; i++)
                //    {
                //        cmd.Parameters.AddWithValue("@str_img_no" + i.ToString(), cimg_no[i]);
                //    }
                //}


                if (dt.Tables["img"] != null)
                {
                    dt.Tables["img"].Clear();
                }

                SqlDataAdapter scenic_ada = new SqlDataAdapter();
                scenic_ada.SelectCommand = cmd;
                scenic_ada.Fill(dt, "img");
                scenic_ada = null;

                d_t = dt.Tables["img"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
                dt = null;
            }

            return d_t;
        }
        #endregion

        #region 首頁廣告圖片新增 Ad_Img_Insert
        public string Ad_Img_Insert(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into Advertisement(ad_title,ad_no, ad_img) "
                     + "values('img',@img_no ,@img_file)";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.Parameters.AddWithValue("@img_file", img_file);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 首頁廣告圖片刪除 Ad_Img_Delete
        public string Ad_Img_Delete(string img_id = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from Advertisement where ad_id = @img_id ";

                cmd.CommandText = csql;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_id", img_id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 首頁 廣告圖片更新 Ad_Img_Update
        public string Ad_Img_Update(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  Advertisement "
                     + "set "
                     + "  ad_img = @img_file "
                     + "where "
                     + "  ad_no = @img_no ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_file", img_file);
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 首頁 廣告圖片說明 & 狀態更新 Ad_Update
        public string Ad_Update(string ad_id = "",string ad_memo = "",string ad_status = "",string ad_url = "", string ad_sort = "")
        {
            string c_msg = "";
            string c_sql = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                if(ad_memo.Trim().Length > 0)
                {
                    if(c_sql.Trim().Length > 0)
                    {
                        c_sql += ",";
                    }
                    c_sql += "ad_memo = @ad_memo ";
                }

                if (ad_url.Trim().Length > 0)
                {
                    if (c_sql.Trim().Length > 0)
                    {
                        c_sql += ",";
                    }
                    c_sql += "ad_url = @ad_url ";
                }

                if (ad_sort.Trim().Length > 0)
                {
                    if (c_sql.Trim().Length > 0)
                    {
                        c_sql += ",";
                    }
                    c_sql += "sort = @sort ";
                }

                if (ad_status.Trim().Length > 0)
                {
                    if (c_sql.Trim().Length > 0)
                    {
                        c_sql += ",";
                    }
                    c_sql += "status = @ad_status ";
                }

                if(c_sql.Trim().Length > 0)
                {
                    csql = "update "
                         + "  Advertisement "
                         + "set "
                         + c_sql
                         + "where "
                         + "  ad_id = @ad_id";
                }


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                if (ad_memo.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@ad_memo", ad_memo);
                }

                if (ad_url.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@ad_url", ad_url);
                }

                if (ad_sort.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@sort", ad_sort);
                }

                if (ad_status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@ad_status", ad_status);
                }
                cmd.Parameters.AddWithValue("@ad_id", ad_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 產品
        
        #region 產品資料 Prod_List
        public DataTable Prod_List(ref string err_msg, string prod_id = "", string sort = "", string status = "", string title_query = "", string cateb_id = "", string cates_id = "")
        {
            DataTable dt = new DataTable();

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            string[] Array_prod_id;
            string[] Array_title_query;
            string[] Array_cateb_id;
            string[] Array_cates_id;

            try
            {
                Array_prod_id = prod_id.Split(',');
                Array_title_query = title_query.Split(',');
                Array_cateb_id = cateb_id.Split(',');
                Array_cates_id = cates_id.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select "
                     + "  a1.prod_id, a1.cate_b_id, a1.cate_s_id, a1.prod_name, a1.prod_memo "
                     + ", a1.prod_desc, a1.sort, a1.status "
                     + ", a2.cate_name as cate_b_name,a3.cate_name as cate_s_name "
                     + ", a4.img_id, a4.img_file, a4.img_desc "
                     + "from "
                     + "   prod a1 "
                     + "left join Prod_Cate_B a2 on a1.cate_b_id = a2.cate_b_id "
                     + "left join Prod_Cate_S a3 on a1.cate_s_id = a3.cate_s_id "
                     + "left join prod_img a4 on Convert(nvarchar,a1.prod_id) = a4.img_no "
                     + "where "
                     + "  a1.status <> 'D' ";


                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (prod_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.prod_id in (";
                    for (int i = 0; i < Array_prod_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_prod_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (cateb_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.cate_b_id in (";
                    for (int i = 0; i < Array_cateb_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_cateb_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (cates_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.cate_s_id in (";
                    for (int i = 0; i < Array_cates_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_cates_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.prod_name like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ") a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.sort ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }


                if (cateb_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_cateb_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_cateb_id" + i.ToString(), Array_cateb_id[i]);
                    }
                }

                if (cates_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_cates_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_cates_id" + i.ToString(), Array_cates_id[i]);
                    }
                }

                if (prod_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_prod_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_prod_id" + i.ToString(), Array_prod_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["prod"] != null)
                {
                    ds.Tables["prod"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "prod");
                news_ada = null;

                dt = ds.Tables["prod"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region 產品資料新增 Prod_Insert
        //prod_name, manure_no, manure_info, manure_ingredients , manure_trait , prod_desc , lang, show, sort
        public string Prod_Insert(string cateb_id = "",string cates_id = "", string prod_name = "", string prod_desc = "", string show = "", string sort = "", string img_no = "")
        {
            string c_msg = "";
            string prod_id = "";

            if(cateb_id == null)
            {
                cateb_id = "0";
            }
            else if(cateb_id.Trim().Length == 0)
            {
                cateb_id = "0";
            }

            if(cates_id == null)
            {
                cates_id = "0";
            }
            else if(cates_id.Trim().Length == 0)
            {
                cates_id = "0";
            }

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from Prod where cate_b_id = @cateb_id and cate_s_id = @cates_id";
                cmd.CommandText = csql;
                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cateb_id", cateb_id);
                cmd.Parameters.AddWithValue("@cates_id", cates_id);
                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//

                csql = @"insert into Prod(cate_b_id,cate_s_id, prod_name, prod_desc, sort ,status) "
                     + "values(@cateb_id,@cates_id,@prod_name,@prod_desc,@sort,@status)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cateb_id", cateb_id);
                cmd.Parameters.AddWithValue("@cates_id", cates_id);
                cmd.Parameters.AddWithValue("@prod_name", prod_name);
                cmd.Parameters.AddWithValue("@prod_desc", prod_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();

                //抓取其編號
                csql = @"select distinct "
                     + "  prod_id "
                     + "from "
                     + "   Prod "
                     + "where "
                     + "    cate_b_id = @cateb_id "
                     + "and cate_s_id = @cates_id "
                     + "and prod_name = @prod_name "
                     + "and prod_desc = @prod_desc "
                     + "and sort = @sort "
                     + "and status = @status ";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cateb_id", cateb_id);
                cmd.Parameters.AddWithValue("@cates_id", cates_id);
                cmd.Parameters.AddWithValue("@prod_name", prod_name);
                cmd.Parameters.AddWithValue("@prod_desc", prod_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                if (ds.Tables["chk_prod"] != null)
                {
                    ds.Tables["chk_prod"].Clear();
                }

                SqlDataAdapter prod_chk_ada = new SqlDataAdapter();
                prod_chk_ada.SelectCommand = cmd;
                prod_chk_ada.Fill(ds, "chk_prod");
                prod_chk_ada = null;

                if (ds.Tables["chk_prod"].Rows.Count > 0)
                {
                    prod_id = ds.Tables["chk_prod"].Rows[0]["prod_id"].ToString();
                    if (img_no.Trim().Length > 0)
                    {
                        csql = @"update "
                             + "  prod_img "
                             + "set "
                             + "  img_no = @prod_id "
                             + "where "
                             + "  img_no = @img_no ";

                        cmd.CommandText = csql;

                        ////讓ADO.NET自行判斷型別轉換
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@prod_id", prod_id);
                        cmd.Parameters.AddWithValue("@img_no", img_no);

                        cmd.ExecuteNonQuery();
                    }
                }


            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 產品資料更新 Prod_Update
        //更新內容
        public string Prod_Update(string prod_id = "", string cateb_id = "", string cates_id = "", string prod_name = "", string prod_desc = "", string show = "", string sort = "")
        {
            string c_msg = "";

            if (cateb_id == null)
            {
                cateb_id = "0";
            }
            else if (cateb_id.Trim().Length == 0)
            {
                cateb_id = "0";
            }

            if (cates_id == null)
            {
                cates_id = "0";
            }
            else if (cates_id.Trim().Length == 0)
            {
                cates_id = "0";
            }

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  prod "
                     + "set "
                     + "  cate_b_id = @cateb_id "
                     + ", cate_s_id = @cates_id "
                     + ", prod_name = @prod_name "
                     + ", prod_desc = @prod_desc "
                     + ", status = @status "
                     + ", sort = @sort "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  prod_id = @prod_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@prod_id", prod_id);
                cmd.Parameters.AddWithValue("@cateb_id", cateb_id);
                cmd.Parameters.AddWithValue("@cates_id", cates_id);
                cmd.Parameters.AddWithValue("@prod_name", prod_name);
                cmd.Parameters.AddWithValue("@prod_desc", prod_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 產品資料刪除 Prod_Del
        public string Prod_Del(string prod_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //刪除產品圖片
                csql = "delete from "
                     + "  Prod_Img "
                     + "where "
                     + "  img_no = @prod_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@prod_id", prod_id);

                cmd.ExecuteNonQuery();

                //產品刪除
                csql = @"delete from "
                     + "  prod "
                     + "where "
                     + "  prod_id = @prod_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@prod_id", prod_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 產品圖片

        #region 產品資料圖片陳列 Prod_Img_List
        public DataTable Prod_Img_List(ref string err_msg, string img_no = "")
        {
            DataSet dt = new DataSet();
            DataTable d_t = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            string[] cimg_no;
            string str_img_no = "";

            cimg_no = img_no.Split(',');

            for (int i = 0; i < cimg_no.Length; i++)
            {
                if (i > 0)
                {
                    str_img_no = str_img_no + ",";
                }
                str_img_no = str_img_no + "'" + cimg_no[i] + "'";
            }

            try
            {
                csql = "select * from prod_img where status = 'Y' ";
                if (img_no != "ALL")
                {
                    csql = csql + "and img_no in (";
                    for (int i = 0; i < cimg_no.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_img_no" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                for (int i = 0; i < cimg_no.Length; i++)
                {
                    cmd.Parameters.AddWithValue("@str_img_no" + i.ToString(), cimg_no[i]);
                }


                if (dt.Tables["img"] != null)
                {
                    dt.Tables["img"].Clear();
                }

                SqlDataAdapter scenic_ada = new SqlDataAdapter();
                scenic_ada.SelectCommand = cmd;
                scenic_ada.Fill(dt, "img");
                scenic_ada = null;

                d_t = dt.Tables["img"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
                dt = null;
            }

            return d_t;
        }
        #endregion

        #region 產品資料圖片刪除 Prod_Img_Delete
        public string Prod_Img_Delete(string img_id = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from prod_img where img_id = @img_id ";

                cmd.CommandText = csql;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_id", img_id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 產品資料圖片更新 Prod_Img_Update
        public string Prod_Img_Update(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  prod_img "
                     + "set "
                     + "  img_file = @img_file "
                     + "where "
                     + "  img_no = @img_no ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_file", img_file);
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 產品資料圖片新增 Prod_Img_Insert
        public string Prod_Img_Insert(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into prod_img(img_no, img_file) "
                     + "values(@img_no ,@img_file)";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.Parameters.AddWithValue("@img_file", img_file);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 產品大類
        #region 產品大類 Prod_CateB_List
        public DataTable Prod_CateB_List(ref string err_msg, string prod_cate_id = "", string sort = "", string status = "", string title_query = "")
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            string[] Array_prod_cate_id;
            string[] Array_title_query;

            try
            {
                Array_prod_cate_id = prod_cate_id.Split(',');
                Array_title_query = title_query.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select distinct "
                     + "  a1.cate_b_id, a1.cate_name, a1.cate_desc "
                     + ", a1.sort, a1.status "
                     + ", a4.img_id, a4.img_file, a4.img_desc "
                     + "from "
                     + "   Prod_Cate_B a1 "
                     + "left join prod_cate_img a4 on Convert(nvarchar,a1.cate_b_id) = a4.img_no and a4.img_sty = 'B' "
                     + "where "
                     + "  a1.status <> 'D' ";

                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (prod_cate_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.cate_b_id in (";
                    for (int i = 0; i < Array_prod_cate_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_prod_cate_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.cate_name like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ")a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.sort desc ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }

                if (prod_cate_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_prod_cate_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_prod_cate_id" + i.ToString(), Array_prod_cate_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["prod_cate_b"] != null)
                {
                    ds.Tables["prod_cate_b"].Clear();
                }

                SqlDataAdapter prod_cate_b_ada = new SqlDataAdapter();
                prod_cate_b_ada.SelectCommand = cmd;
                prod_cate_b_ada.Fill(ds, "prod_cate_b");
                prod_cate_b_ada = null;

                dt = ds.Tables["prod_cate_b"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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
            
            return dt;
        }
        #endregion

        #region 產品大類新增 Prod_CateB_Insert
        public string Prod_CateB_Insert(string cate_name = "", string cate_desc = "", string is_show = "", string sort = "", string img_no = "")
        {
            string c_msg = "";
            string cate_id = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from Prod_Cate_B";
                cmd.CommandText = csql;

                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//

                csql = @"insert into Prod_Cate_B(cate_name,cate_desc,sort,status) "
                     + "values(@cate_name,@cate_desc,@sort,@is_show)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_name);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);

                cmd.ExecuteNonQuery();

                //抓取其編號
                csql = @"select distinct "
                     + "  cate_b_id "
                     + "from "
                     + "   Prod_Cate_B "
                     + "where "
                     + "    cate_name = @cate_name "
                     + "and cate_desc = @cate_desc "
                     + "and sort = @sort "
                     + "and status = @is_show ";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);

                if (ds.Tables["chk_prodCate_B"] != null)
                {
                    ds.Tables["chk_prodCate_B"].Clear();
                }

                SqlDataAdapter prod_chk_ada = new SqlDataAdapter();
                prod_chk_ada.SelectCommand = cmd;
                prod_chk_ada.Fill(ds, "chk_prodCate_B");
                prod_chk_ada = null;

                if (ds.Tables["chk_prodCate_B"].Rows.Count > 0)
                {
                    cate_id = ds.Tables["chk_prodCate_B"].Rows[0]["cate_b_id"].ToString();
                    if (img_no.Trim().Length > 0)
                    {
                        csql = @"update "
                             + "  prod_cate_img "
                             + "set "
                             + "  img_no = @cate_id "
                             + "where "
                             + "  img_no = @img_no ";

                        cmd.CommandText = csql;

                        ////讓ADO.NET自行判斷型別轉換
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@cate_id", cate_id);
                        cmd.Parameters.AddWithValue("@img_no", img_no);

                        cmd.ExecuteNonQuery();
                    }
                }


            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 產品大類更新 Prod_CateB_Update
        //更新內容
        public string Prod_CateB_Update(string cate_id = "", string cate_name = "", string cate_desc = "", string is_show = "", string sort = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  Prod_Cate_B "
                     + "set "
                     + "  cate_name = @cate_name "
                     + ", cate_desc = @cate_desc "
                     + ", status = @is_show "
                     + ", sort = @sort "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  cate_b_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 產品大類刪除 Prod_CateB_Del
        public string Prod_CateB_Del(string cate_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //刪除產品圖片
                csql = "delete from "
                     + "  Prod_Img "
                     + "where "
                     + "  img_no in ("
                     + "    select "
                     + "      Convert(nvarchar,a1.prod_id) "
                     + "    from "
                     + "      prod a1 "
                     + "    where "
                     + "      a1.cate_b_id = @cate_id "
                     + "  ) ";


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();

                //刪除產品
                csql = "delete from "
                     + "  Prod "
                     + "where "
                     + "  cate_b_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();

                //刪除產品小類圖片
                csql = "delete from "
                     + "  Prod_Cate_Img "
                     + "where "
                     + "    img_sty = 'S' "
                     + "and img_no in ("
                     + "    select "
                     + "      Convert(nvarchar,a1.cate_s_id) "
                     + "    from "
                     + "      prod_cate_s a1 "
                     + "    where "
                     + "      a1.cate_b_id = @cate_id "
                     + "  ) ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();

                //刪除產品小類
                csql = @"delete from "
                     + "  Prod_Cate_S "
                     + "where "
                     + "  cate_b_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();

                //刪除產品大類圖片
                csql = "delete from "
                     + "  Prod_Cate_Img "
                     + "where "
                     + "    img_sty = 'B' "
                     + "and img_no in ("
                     + "    select "
                     + "      Convert(nvarchar,a1.cate_b_id) "
                     + "    from "
                     + "      prod_cate_b a1 "
                     + "    where "
                     + "      a1.cate_b_id = @cate_id "
                     + "  ) ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                //刪除產品大類
                csql = @"delete from "
                     + "  Prod_Cate_B "
                     + "where "
                     + "  cate_b_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion
        #endregion

        #region 產品小類
        #region 產品小類陳列 Prod_CateS_List
        public DataTable Prod_CateS_List(ref string err_msg, string prod_cate_id = "", string sort = "", string status = "", string title_query = "",string prod_cateb_id = "")
        {
            DataTable dt = new DataTable();

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            string[] Array_prod_cate_id;
            string[] Array_title_query;
            string[] Array_prod_cateb_id;

            try
            {
                Array_prod_cate_id = prod_cate_id.Split(',');
                Array_title_query = title_query.Split(',');
                Array_prod_cateb_id = prod_cateb_id.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select distinct "
                     + "  a1.cate_s_id, a1.cate_name, a1.cate_desc "
                     + ", a1.sort, a1.status "
                     + ", a2.cate_b_id, a2.cate_name as cate_b_name "
                     + ", a2.sort as cate_b_sort "
                     + ", a4.img_id, a4.img_file, a4.img_desc "
                     + "from "
                     + "   Prod_Cate_S a1 "
                     + "inner join Prod_Cate_B a2 on a1.cate_b_id = a2.cate_b_id ";
                if (prod_cateb_id.Trim().Length > 0)
                {
                    csql = csql + " and a2.cate_b_id in (";
                    for (int i = 0; i < Array_prod_cateb_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_prod_cateb_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }
                csql += "left join prod_cate_img a4 on Convert(nvarchar,a1.cate_s_id) = a4.img_no and a4.img_sty = 'S' "
                     + "where "
                     + "  a1.status <> 'D' ";

                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (prod_cate_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.cate_s_id in (";
                    for (int i = 0; i < Array_prod_cate_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_prod_cate_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.cate_name like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ")a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.cate_b_sort, a1.sort ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }

                if (prod_cate_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_prod_cate_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_prod_cate_id" + i.ToString(), Array_prod_cate_id[i]);
                    }
                }

                if (prod_cateb_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_prod_cateb_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_prod_cateb_id" + i.ToString(), Array_prod_cateb_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["prod_cate_s"] != null)
                {
                    ds.Tables["prod_cate_s"].Clear();
                }

                SqlDataAdapter prod_cate_s_ada = new SqlDataAdapter();
                prod_cate_s_ada.SelectCommand = cmd;
                prod_cate_s_ada.Fill(ds, "prod_cate_s");
                prod_cate_s_ada = null;
                dt = ds.Tables["prod_cate_s"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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
            
            return dt;
        }
        #endregion

        #region 產品小類新增 Prod_CateS_Insert
        public string Prod_CateS_Insert(string cate_name = "", string cate_desc = "", string is_show = "", string sort = "", string img_no = "",string cateb_id = "")
        {
            string c_msg = "";
            string cate_id = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from Prod_Cate_S where cate_b_id = @cate_b_id";
                cmd.CommandText = csql;
                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_b_id", cateb_id);
                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//

                csql = @"insert into Prod_Cate_S(cate_name,cate_desc,sort,status,cate_b_id) "
                     + "values(@cate_name,@cate_desc,@sort,@is_show,@cate_b_id)";
                
                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_name);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);
                cmd.Parameters.AddWithValue("@cate_b_id", cateb_id);

                cmd.ExecuteNonQuery();

                //抓取其編號
                csql = @"select distinct "
                     + "  cate_s_id "
                     + "from "
                     + "   Prod_Cate_S "
                     + "where "
                     + "    cate_name = @cate_name "
                     + "and cate_desc = @cate_desc "
                     + "and sort = @sort "
                     + "and status = @is_show "
                     + "and cate_b_id = @cate_b_id ";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_desc);
                //cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);
                cmd.Parameters.AddWithValue("@cate_b_id", cateb_id);

                if (ds.Tables["chk_prodCate_S"] != null)
                {
                    ds.Tables["chk_prodCate_S"].Clear();
                }

                SqlDataAdapter prod_chk_ada = new SqlDataAdapter();
                prod_chk_ada.SelectCommand = cmd;
                prod_chk_ada.Fill(ds, "chk_prodCate_S");
                prod_chk_ada = null;

                if (ds.Tables["chk_prodCate_S"].Rows.Count > 0)
                {
                    cate_id = ds.Tables["chk_prodCate_S"].Rows[0]["cate_s_id"].ToString();
                    if (img_no.Trim().Length > 0)
                    {
                        csql = @"update "
                             + "  prod_cate_img "
                             + "set "
                             + "  img_no = @cate_id "
                             + "where "
                             + "  img_no = @img_no ";

                        cmd.CommandText = csql;

                        ////讓ADO.NET自行判斷型別轉換
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@cate_id", cate_id);
                        cmd.Parameters.AddWithValue("@img_no", img_no);

                        cmd.ExecuteNonQuery();
                    }
                }


            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 產品小類更新 Prod_CateS_Update
        //更新內容
        public string Prod_CateS_Update(string cate_id = "", string cate_name = "", string cate_desc = "", string is_show = "", string sort = "",string cateb_id = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  Prod_Cate_S "
                     + "set "
                     + "  cate_name = @cate_name "
                     + ", cate_desc = @cate_desc "
                     + ", status = @is_show "
                     + ", sort = @sort "
                     + ", cate_b_id = @cateb_id "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  cate_s_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);
                cmd.Parameters.AddWithValue("@cateb_id", cateb_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 產品小類刪除 Prod_CateS_Del
        public string Prod_CateS_Del(string cate_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {

                //刪除產品圖片
                csql = "delete from "
                     + "  Prod_Img "
                     + "where "
                     + "  img_no in ("
                     + "    select "
                     + "      Convert(nvarchar,a1.prod_id) "
                     + "    from "
                     + "      prod a1 "
                     + "    where "
                     + "      a1.cate_s_id = @cate_id "
                     + "  ) ";


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();

                //刪除產品
                csql = "delete from "
                     + "  Prod "
                     + "where "
                     + "  cate_s_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();

                //刪除產品小類圖片
                csql = "delete from "
                     + "  Prod_Cate_Img "
                     + "where "
                     + "    img_sty = 'S' "
                     + "and img_no in ("
                     + "    select "
                     + "      Convert(nvarchar,a1.cate_s_id) "
                     + "    from "
                     + "      prod_cate_s a1 "
                     + "    where "
                     + "      a1.cate_s_id = @cate_id "
                     + "  ) ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();

                //刪除產品小類
                csql = @"delete from "
                     + "  Prod_Cate_S "
                     + "where "
                     + "  cate_s_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 產品類別圖片
        #region 產品類別圖片陳列 Prod_Cate_Img_List
        public DataTable Prod_Cate_Img_List(ref string err_msg, string img_no = "",string img_sty = "")
        {
            DataSet dt = new DataSet();
            DataTable d_t = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();
            string[] cimg_no;
            string str_img_no = "";

            try
            {
                cimg_no = img_no.Split(',');

                for (int i = 0; i < cimg_no.Length; i++)
                {
                    if (i > 0)
                    {
                        str_img_no = str_img_no + ",";
                    }
                    str_img_no = str_img_no + "'" + cimg_no[i] + "'";
                }

                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }


                cmd.Connection = conn;

                csql = "select * from prod_cate_img where status = 'Y' ";
                if (img_sty.Trim().Length > 0)
                {
                    csql = csql + "and img_sty = @img_sty ";
                }

                if (img_no != "ALL")
                {
                    csql = csql + "and img_no in (";
                    for (int i = 0; i < cimg_no.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_img_no" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                for (int i = 0; i < cimg_no.Length; i++)
                {
                    cmd.Parameters.AddWithValue("@str_img_no" + i.ToString(), cimg_no[i]);
                }

                if (img_sty.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@img_sty", img_sty);
                }

                if (dt.Tables["prod_cate_img"] != null)
                {
                    dt.Tables["prod_cate_img"].Clear();
                }

                SqlDataAdapter scenic_ada = new SqlDataAdapter();
                scenic_ada.SelectCommand = cmd;
                scenic_ada.Fill(dt, "prod_cate_img");
                scenic_ada = null;

                d_t = dt.Tables["prod_cate_img"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
                dt = null;
            }

            return d_t;
        }
        #endregion

        #region 產品類別圖片刪除 Prod_Cate_Img_Delete
        public string Prod_Cate_Img_Delete(string img_id = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from prod_cate_img where img_id = @img_id ";

                cmd.CommandText = csql;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_id", img_id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 產品類別圖片更新 Prod_Cate_Img_Update
        public string Prod_Cate_Img_Update(string img_no = "", string img_file = "", string img_sty = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  prod_cate_img "
                     + "set "
                     + "  img_file = @img_file "
                     + "where "
                     + "  img_no = @img_no ";

                if(img_sty.Trim().Length > 0)
                {
                    csql = csql + " and img_sty = @img_sty";
                }

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_file", img_file);
                cmd.Parameters.AddWithValue("@img_no", img_no);
                if(img_sty.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@img_sty", img_sty);
                }
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 產品類別圖片新增 Prod_Cate_Img_Insert
        public string Prod_Cate_Img_Insert(string img_no = "", string img_file = "",string img_sty = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into prod_cate_img(img_no, img_file, img_sty) "
                     + "values(@img_no ,@img_file, @img_sty)";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.Parameters.AddWithValue("@img_file", img_file);
                cmd.Parameters.AddWithValue("@img_sty", img_sty);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion
        #endregion

        #region 成交案例 Proj

        #region 成交案例抓取 Proj_List
        public DataTable Proj_List(ref string err_msg, string cproj_id = "", string sort = "", string status = "", string title_query = "", string start_date = "", string end_date = "", string is_index = "")
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();
            string[] Array_proj_id;
            string[] Array_title_query;

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                cmd.Connection = conn;

                Array_proj_id = cproj_id.Split(',');
                Array_title_query = title_query.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select distinct "
                     + "  a1.proj_id, a1.proj_title, convert(nvarchar(10),a1.proj_date,23) as proj_date, a1.proj_url, a1.proj_desc, a1.proj_memo "
                     + ", a1.is_index, a1.sort, a1.status "
                     + "from "
                     + "   proj a1 "
                     + "where "
                     + "  a1.status <> 'D' ";

                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (cproj_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.proj_id in (";
                    for (int i = 0; i < Array_proj_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_proj_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.proj_title like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                if (start_date.Trim().Length > 0)
                {

                    csql = csql + "and a1.proj_date >= @start_date ";
                }

                if (end_date.Trim().Length > 0)
                {

                    csql = csql + "and a1.proj_date <= @end_date ";
                }

                if (is_index.Trim().Length > 0)
                {
                    csql = csql + "and a1.is_index = @is_index ";
                }

                csql = csql + ")a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.proj_date desc ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }

                if (start_date.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@start_date", start_date);
                }

                if (end_date.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@end_date", end_date);
                }

                if (cproj_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_proj_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_proj_id" + i.ToString(), Array_proj_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                if (is_index.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@is_index", is_index);
                }
                //--------------------------------------------------------------//

                if (ds.Tables["proj"] != null)
                {
                    ds.Tables["proj"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "proj");
                news_ada = null;

                dt = ds.Tables["proj"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region 成交案例新增 Proj_Insert
        public string Proj_Insert(string proj_title = "", string proj_date = "", string proj_desc = "", string is_show = "", string is_index = "", string sort = "", string proj_memo = "",string proj_url = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from Proj";
                cmd.CommandText = csql;
              
                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//

                csql = @"insert into Proj(proj_title,proj_date,proj_desc,is_index,sort,status,proj_memo,proj_url) "
                     + "values(@proj_title,@proj_date,@proj_desc,@is_index,@sort,@is_show,@proj_memo,@proj_url)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@proj_title", proj_title);
                cmd.Parameters.AddWithValue("@proj_date", proj_date);
                cmd.Parameters.AddWithValue("@proj_desc", proj_desc);
                cmd.Parameters.AddWithValue("@is_index", is_index);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);
                cmd.Parameters.AddWithValue("@proj_memo", proj_memo);
                cmd.Parameters.AddWithValue("@proj_url", proj_url);


                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 成交案例更新 Proj_Update
        //更新內容
        public string Proj_Update(string proj_id = "", string proj_title = "", string proj_date = "", string proj_desc = "", string is_show = "", string is_index = "", string sort = "", string proj_memo = "",string proj_url = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  proj "
                     + "set "
                     + "  proj_title = @proj_title "
                     + ", proj_date = @proj_date "
                     + ", proj_desc = @proj_desc "
                     + ", status = @is_show "
                     + ", is_index = @is_index "
                     + ", sort = @sort "
                     + ", proj_memo = @proj_memo "
                     + ", proj_url = @proj_url "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  proj_id = @proj_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@proj_id", proj_id);
                cmd.Parameters.AddWithValue("@proj_title", proj_title);
                cmd.Parameters.AddWithValue("@proj_date", proj_date);
                cmd.Parameters.AddWithValue("@proj_desc", proj_desc);
                cmd.Parameters.AddWithValue("@is_index", is_index);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);
                cmd.Parameters.AddWithValue("@proj_memo", proj_memo);
                cmd.Parameters.AddWithValue("@proj_url", proj_memo);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 成交案例資料刪除 Proj_Del
        public string Proj_Del(string proj_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from "
                     + "  proj "
                     + "where "
                     + "  proj_id = @proj_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@proj_id", proj_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 里程碑

        #region 里程碑資料 His_List
        public DataTable His_List(ref string err_msg, string his_id = "", string sort = "", string status = "", string title_query = "")
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }


                cmd.Connection = conn;

                string[] Array_his_id;
                string[] Array_title_query;

                Array_his_id = his_id.Split(',');
                Array_title_query = title_query.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select "
                     + "  a1.his_id, a1.his_title, a1.his_ym, a1.his_desc, a1.sort, a1.status  "
                     + ", a4.img_id, a4.img_file, a4.img_desc "
                     + "from "
                     + "   his a1 "
                     + "left join his_img a4 on Convert(nvarchar,a1.his_id) = a4.img_no "
                     + "where "
                     + "  a1.status <> 'D' ";


                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (his_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.his_id in (";
                    for (int i = 0; i < Array_his_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_his_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.his_title like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ") a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.sort desc";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }


                if (his_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_his_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_his_id" + i.ToString(), Array_his_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["his"] != null)
                {
                    ds.Tables["his"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "his");
                news_ada = null;

                dt = ds.Tables["his"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region 里程碑資料新增 His_Insert
        //prod_name, manure_no, manure_info, manure_ingredients , manure_trait , prod_desc , lang, show, sort
        public string His_Insert(string his_ym = "",string his_title = "", string his_desc = "", string show = "", string sort = "", string img_no = "")
        {
            string c_msg = "";
            string his_id = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from His";
                cmd.CommandText = csql;

                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//

                csql = @"insert into His(his_ym,his_title, his_desc, sort ,status) "
                     + "values(@his_ym,@his_title,@his_desc,@sort,@status)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@his_ym", his_ym);
                cmd.Parameters.AddWithValue("@his_title", his_title);
                cmd.Parameters.AddWithValue("@his_desc", his_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();

                //抓取其編號
                csql = @"select distinct "
                     + "  his_id "
                     + "from "
                     + "   His "
                     + "where "
                     + "    his_ym = @his_ym "
                     + "and his_title = @his_title "
                     + "and his_desc = @his_desc "
                     + "and sort = @sort "
                     + "and status = @status ";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@his_ym", his_ym);
                cmd.Parameters.AddWithValue("@his_title", his_title);
                cmd.Parameters.AddWithValue("@his_desc", his_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                if (ds.Tables["chk_his"] != null)
                {
                    ds.Tables["chk_his"].Clear();
                }

                SqlDataAdapter prod_chk_ada = new SqlDataAdapter();
                prod_chk_ada.SelectCommand = cmd;
                prod_chk_ada.Fill(ds, "chk_his");
                prod_chk_ada = null;

                if (ds.Tables["chk_his"].Rows.Count > 0)
                {
                    his_id = ds.Tables["chk_his"].Rows[0]["his_id"].ToString();
                    if (img_no.Trim().Length > 0)
                    {
                        csql = @"update "
                             + "  his_img "
                             + "set "
                             + "  img_no = @his_id "
                             + "where "
                             + "  img_no = @img_no ";

                        cmd.CommandText = csql;

                        ////讓ADO.NET自行判斷型別轉換
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@his_id", his_id);
                        cmd.Parameters.AddWithValue("@img_no", img_no);

                        cmd.ExecuteNonQuery();
                    }
                }


            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 里程碑資料更新 His_Update
        //更新內容
        public string His_Update(string his_id = "", string his_ym = "", string his_title = "", string his_desc = "", string show = "", string sort = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  his "
                     + "set "
                     + "  his_ym = @his_ym "
                     + ", his_title = @his_title "
                     + ", his_desc = @his_desc "
                     + ", status = @status "
                     + ", sort = @sort "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  his_id = @his_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@his_id", his_id);
                cmd.Parameters.AddWithValue("@his_ym", his_ym);
                cmd.Parameters.AddWithValue("@his_title", his_title);
                cmd.Parameters.AddWithValue("@his_desc", his_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 里程碑資料刪除 His_Del
        public string His_Del(string his_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //刪除圖片
                csql = "delete from "
                     + "  His_Img "
                     + "where "
                     + "  img_no = @his_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@his_id", his_id);

                cmd.ExecuteNonQuery();


                csql = @"delete from "
                     + "  his "
                     + "where "
                     + "  his_id = @his_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@his_id", his_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 里程碑資料圖片陳列 His_Img_List
        public DataTable His_Img_List(ref string err_msg, string img_no = "")
        {
            DataSet dt = new DataSet();
            DataTable d_t = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();
            string[] cimg_no;
            string str_img_no = "";

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                cmd.Connection = conn;

                cimg_no = img_no.Split(',');

                for (int i = 0; i < cimg_no.Length; i++)
                {
                    if (i > 0)
                    {
                        str_img_no = str_img_no + ",";
                    }
                    str_img_no = str_img_no + "'" + cimg_no[i] + "'";
                }

                csql = "select * from his_img where status = 'Y' ";
                if (img_no != "ALL")
                {
                    csql = csql + "and img_no in (";
                    for (int i = 0; i < cimg_no.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_img_no" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                for (int i = 0; i < cimg_no.Length; i++)
                {
                    cmd.Parameters.AddWithValue("@str_img_no" + i.ToString(), cimg_no[i]);
                }


                if (dt.Tables["img"] != null)
                {
                    dt.Tables["img"].Clear();
                }

                SqlDataAdapter scenic_ada = new SqlDataAdapter();
                scenic_ada.SelectCommand = cmd;
                scenic_ada.Fill(dt, "img");
                scenic_ada = null;

                d_t = dt.Tables["img"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
                dt = null;
            }
            
            return d_t;
        }
        #endregion

        #region  里程碑資料圖片刪除 His_Img_Delete
        public string His_Img_Delete(string img_id = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from his_img where img_id = @img_id ";

                cmd.CommandText = csql;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_id", img_id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 里程碑資料圖片更新 His_Img_Update
        public string His_Img_Update(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  his_img "
                     + "set "
                     + "  img_file = @img_file "
                     + "where "
                     + "  img_no = @img_no ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_file", img_file);
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 里程碑資料圖片新增 His_Img_Insert
        public string His_Img_Insert(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into his_img(img_no, img_file) "
                     + "values(@img_no ,@img_file)";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.Parameters.AddWithValue("@img_file", img_file);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 小圖廣告類別
        #region 小圖廣告類別 ADS_Cate_List
        public DataTable ADS_Cate_List(ref string err_msg, string cate_id = "", string sort = "", string status = "", string title_query = "")
        {
            DataTable dt = new DataTable();

            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }


                cmd.Connection = conn;

                string[] Array_cate_id;
                string[] Array_title_query;

                Array_cate_id = cate_id.Split(',');
                Array_title_query = title_query.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select distinct "
                     + "  a1.cate_id, a1.cate_name, a1.cate_desc "
                     + ", a1.sort, a1.status "
                     + ", a4.img_id, a4.img_file, a4.img_desc "
                     + "from "
                     + "   ADS_Cate a1 "
                     + "left join ADS_cate_img a4 on Convert(nvarchar,a1.cate_id) = a4.img_no "
                     + "where "
                     + "  a1.status <> 'D' ";

                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (cate_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.cate_id in (";
                    for (int i = 0; i < Array_cate_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_cate_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.cate_name like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ")a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.sort desc ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }

                if (cate_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_cate_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_cate_id" + i.ToString(), Array_cate_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["ADS_cate"] != null)
                {
                    ds.Tables["ADS_cate"].Clear();
                }

                SqlDataAdapter ADS_cate_ada = new SqlDataAdapter();
                ADS_cate_ada.SelectCommand = cmd;
                ADS_cate_ada.Fill(ds, "ADS_cate");
                ADS_cate_ada = null;
                dt = ds.Tables["ADS_cate"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region 小圖廣告類別新增 ADS_Cate_Insert
        public string ADS_Cate_Insert(string cate_name = "", string cate_desc = "", string is_show = "", string sort = "", string img_no = "")
        {
            string c_msg = "";
            string cate_id = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into ADS_Cate(cate_name,cate_desc,sort,status) "
                     + "values(@cate_name,@cate_desc,@sort,@is_show)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_name);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);

                cmd.ExecuteNonQuery();

                //抓取其編號
                csql = @"select distinct "
                     + "  cate_id "
                     + "from "
                     + "   ADS_Cate "
                     + "where "
                     + "    cate_name = @cate_name "
                     + "and cate_desc = @cate_desc "
                     + "and sort = @sort "
                     + "and status = @is_show ";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);

                if (ds.Tables["chk_ADS_Cate"] != null)
                {
                    ds.Tables["chk_ADS_Cate"].Clear();
                }

                SqlDataAdapter prod_chk_ada = new SqlDataAdapter();
                prod_chk_ada.SelectCommand = cmd;
                prod_chk_ada.Fill(ds, "chk_ADS_Cate");
                prod_chk_ada = null;

                if (ds.Tables["chk_ADS_Cate"].Rows.Count > 0)
                {
                    cate_id = ds.Tables["chk_ADS_Cate"].Rows[0]["cate_id"].ToString();
                    if (img_no.Trim().Length > 0)
                    {
                        csql = @"update "
                             + "  ADS_Cate_img "
                             + "set "
                             + "  img_no = @cate_id "
                             + "where "
                             + "  img_no = @img_no ";

                        cmd.CommandText = csql;

                        ////讓ADO.NET自行判斷型別轉換
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@cate_id", cate_id);
                        cmd.Parameters.AddWithValue("@img_no", img_no);

                        cmd.ExecuteNonQuery();
                    }
                }


            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 小圖廣告類別更新 ADS_Cate_Update
        //更新內容
        public string ADS_Cate_Update(string cate_id = "", string cate_name = "", string cate_desc = "", string is_show = "", string sort = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  ADS_Cate "
                     + "set "
                     + "  cate_name = @cate_name "
                     + ", cate_desc = @cate_desc "
                     + ", status = @is_show "
                     + ", sort = @sort "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  cate_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 小圖廣告類別刪除 ADS_Cate_Del
        public string ADS_Cate_Del(string cate_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from "
                     + "  ADS_Cate "
                     + "where "
                     + "  cate_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion
        #endregion

        #region 小圖廣告類別圖片

        #region 小圖廣告類別圖片陳列 ADS_Cate_Img_List
        public DataTable ADS_Cate_Img_List(ref string err_msg, string img_no = "")
        {
            DataSet dt = new DataSet();
            DataTable d_t = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();

            string[] cimg_no;
            string str_img_no = "";

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                cmd.Connection = conn;

                cimg_no = img_no.Split(',');

                for (int i = 0; i < cimg_no.Length; i++)
                {
                    if (i > 0)
                    {
                        str_img_no = str_img_no + ",";
                    }
                    str_img_no = str_img_no + "'" + cimg_no[i] + "'";
                }

                csql = "select * from ADS_Cate_Img where status = 'Y' ";
                if (img_no != "ALL")
                {
                    csql = csql + "and img_no in (";
                    for (int i = 0; i < cimg_no.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_img_no" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                for (int i = 0; i < cimg_no.Length; i++)
                {
                    cmd.Parameters.AddWithValue("@str_img_no" + i.ToString(), cimg_no[i]);
                }


                if (dt.Tables["img"] != null)
                {
                    dt.Tables["img"].Clear();
                }

                SqlDataAdapter scenic_ada = new SqlDataAdapter();
                scenic_ada.SelectCommand = cmd;
                scenic_ada.Fill(dt, "img");
                scenic_ada = null;

                d_t = dt.Tables["img"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
                dt = null;
            }

            return d_t;
        }
        #endregion

        #region 小圖廣告類別圖片刪除 ADS_Cate_Img_Delete
        public string ADS_Cate_Img_Delete(string img_id = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from ADS_Cate_img where img_id = @img_id ";

                cmd.CommandText = csql;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_id", img_id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 小圖廣告類別圖片更新 ADS_Cate_Img_Update
        public string ADS_Cate_Img_Update(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  ADS_Cate_img "
                     + "set "
                     + "  img_file = @img_file "
                     + "where "
                     + "  img_no = @img_no ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_file", img_file);
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 小圖廣告類別圖片新增 ADS_Cate_Img_Insert
        public string ADS_Cate_Img_Insert(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into ADS_Cate_img(img_no, img_file) "
                     + "values(@img_no ,@img_file)";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.Parameters.AddWithValue("@img_file", img_file);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 小圖廣告

        #region 小圖廣告 ADS_List
        public DataTable ADS_List(ref string err_msg, string ad_id = "", string sort = "", string status = "", string title_query = "", string cate_id = "")
        {
            DataTable dt = new DataTable();

            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();

            string[] Array_ad_id;
            string[] Array_title_query;
            string[] Array_cate_id;

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

           
            cmd.Connection = conn;

            try
            {
                Array_ad_id = ad_id.Split(',');
                Array_title_query = title_query.Split(',');
                Array_cate_id = cate_id.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select "
                     + "  a1.ad_id, a1.cate_id, a1.ad_title, a1.ad_url "
                     + ", a1.sort, a1.status "
                     + ", a2.cate_name "
                     + ", a4.img_id, a4.img_file, a4.img_desc "
                     + "from "
                     + "   ADS a1 "
                     + "left join ADS_Cate a2 on a1.cate_id = a2.cate_id "
                     + "left join ADS_img a4 on Convert(nvarchar,a1.ad_id) = a4.img_no "
                     + "where "
                     + "  a1.status <> 'D' ";


                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (ad_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.ad_id in (";
                    for (int i = 0; i < Array_ad_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_ad_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (cate_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.cate_id in (";
                    for (int i = 0; i < Array_cate_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_cate_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.ad_title like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ") a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.cate_id,a1.sort ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }


                if (cate_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_cate_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_cate_id" + i.ToString(), Array_cate_id[i]);
                    }
                }

                if (ad_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_ad_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_ad_id" + i.ToString(), Array_ad_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["ADS"] != null)
                {
                    ds.Tables["ADS"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "ADS");
                news_ada = null;

                dt = ds.Tables["ADS"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region 小圖廣告資料新增 ADS_Insert
        //prod_name, manure_no, manure_info, manure_ingredients , manure_trait , prod_desc , lang, show, sort
        public string ADS_Insert(string cate_id = "", string ad_title = "", string ad_url = "", string show = "", string sort = "", string img_no = "")
        {
            string c_msg = "";
            string ad_id = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into ADS(cate_id, ad_title, ad_url, sort ,status) "
                     + "values(@cate_id,@ad_title,@ad_url,@sort,@status)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@ad_title", ad_title);
                cmd.Parameters.AddWithValue("@ad_url", ad_url);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();

                //抓取其編號
                csql = @"select distinct "
                     + "  ad_id "
                     + "from "
                     + "   ADS "
                     + "where "
                     + "    cate_id = @cate_id "
                     + "and ad_title = @ad_title "
                     + "and ad_url = @ad_url "
                     + "and sort = @sort "
                     + "and status = @status ";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@ad_title", ad_title);
                cmd.Parameters.AddWithValue("@ad_url", ad_url);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                if (ds.Tables["chk_ads"] != null)
                {
                    ds.Tables["chk_ads"].Clear();
                }

                SqlDataAdapter prod_chk_ada = new SqlDataAdapter();
                prod_chk_ada.SelectCommand = cmd;
                prod_chk_ada.Fill(ds, "chk_ads");
                prod_chk_ada = null;

                if (ds.Tables["chk_ads"].Rows.Count > 0)
                {
                    ad_id = ds.Tables["chk_ads"].Rows[0]["ad_id"].ToString();
                    if (img_no.Trim().Length > 0)
                    {
                        csql = @"update "
                             + "  ADS_img "
                             + "set "
                             + "  img_no = @ad_id "
                             + "where "
                             + "  img_no = @img_no ";

                        cmd.CommandText = csql;

                        ////讓ADO.NET自行判斷型別轉換
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@ad_id", ad_id);
                        cmd.Parameters.AddWithValue("@img_no", img_no);

                        cmd.ExecuteNonQuery();
                    }
                }


            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 小圖廣告資料更新 ADS_Update
        //更新內容
        public string ADS_Update(string ad_id = "", string cate_id = "", string ad_title = "", string ad_url = "", string show = "", string sort = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  ADS "
                     + "set "
                     + "  cate_id = @cate_id "
                     + ", ad_title = @ad_title "
                     + ", ad_url = @ad_url "
                     + ", status = @status "
                     + ", sort = @sort "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  ad_id = @ad_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@ad_id", ad_id);
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@ad_title", ad_title);
                cmd.Parameters.AddWithValue("@ad_url", ad_url);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 小圖廣告資料刪除 ADS_Del
        public string ADS_Del(string ad_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from "
                     + "  ADS "
                     + "where "
                     + "  ad_id = @ad_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@ad_id", ad_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 小圖廣告圖片

        #region 小圖廣告圖片陳列 ADS_Cate_Img_List
        public DataTable ADS_Img_List(ref string err_msg, string img_no = "")
        {
            DataSet dt = new DataSet();
            DataTable d_t = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();
            string[] cimg_no;
            string str_img_no = "";

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                
                cmd.Connection = conn;
                
                cimg_no = img_no.Split(',');

                for (int i = 0; i < cimg_no.Length; i++)
                {
                    if (i > 0)
                    {
                        str_img_no = str_img_no + ",";
                    }
                    str_img_no = str_img_no + "'" + cimg_no[i] + "'";
                }
                
                csql = "select * from ADS_Img where status = 'Y' ";
                if (img_no != "ALL")
                {
                    csql = csql + "and img_no in (";
                    for (int i = 0; i < cimg_no.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_img_no" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                for (int i = 0; i < cimg_no.Length; i++)
                {
                    cmd.Parameters.AddWithValue("@str_img_no" + i.ToString(), cimg_no[i]);
                }


                if (dt.Tables["img"] != null)
                {
                    dt.Tables["img"].Clear();
                }

                SqlDataAdapter scenic_ada = new SqlDataAdapter();
                scenic_ada.SelectCommand = cmd;
                scenic_ada.Fill(dt, "img");
                scenic_ada = null;

                d_t = dt.Tables["img"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
                dt = null;
            }

            return d_t;
        }
        #endregion

        #region 小圖廣告圖片刪除 ADS_Img_Delete
        public string ADS_Img_Delete(string img_id = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from ADS_img where img_id = @img_id ";

                cmd.CommandText = csql;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_id", img_id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 小圖廣告圖片更新 ADS_Img_Update
        public string ADS_Img_Update(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  ADS_img "
                     + "set "
                     + "  img_file = @img_file "
                     + "where "
                     + "  img_no = @img_no ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_file", img_file);
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 小圖廣告圖片新增 ADS_Img_Insert
        public string ADS_Img_Insert(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into ADS_img(img_no, img_file) "
                     + "values(@img_no ,@img_file)";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.Parameters.AddWithValue("@img_file", img_file);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region FAQ

        #region FAQ資料 Faq_List
        public DataTable Faq_List(ref string err_msg, string faq_id = "", string sort = "", string status = "", string title_query = "")
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();

            string[] Array_faq_id;
            string[] Array_title_query;

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;

                Array_faq_id = faq_id.Split(',');
                Array_title_query = title_query.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select "
                     + "  a1.faq_id, a1.faq_title, a1.faq_desc, a1.sort, a1.status  "
                     + "from "
                     + "   faq a1 "
                     + "where "
                     + "  a1.status <> 'D' ";

                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (faq_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.faq_id in (";
                    for (int i = 0; i < Array_faq_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_faq_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.faq_title like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ") a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.sort ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }


                if (faq_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_faq_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_faq_id" + i.ToString(), Array_faq_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["faq"] != null)
                {
                    ds.Tables["faq"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "faq");
                news_ada = null;

                dt = ds.Tables["faq"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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
            
            return dt;
        }
        #endregion

        #region Faq資料新增 Faq_Insert
        //prod_name, manure_no, manure_info, manure_ingredients , manure_trait , prod_desc , lang, show, sort
        public string Faq_Insert(string faq_title = "", string faq_desc = "", string show = "", string sort = "")
        {
            string c_msg = "";
            //string faq_id = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from faq";
                cmd.CommandText = csql;
               
                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//

                csql = @"insert into Faq(faq_title, faq_desc, sort ,status) "
                     + "values(@faq_title,@faq_desc,@sort,@status)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@faq_title", faq_title);
                cmd.Parameters.AddWithValue("@faq_desc", faq_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region Faq資料更新 Faq_Update
        //更新內容
        public string Faq_Update(string faq_id = "", string faq_title = "", string faq_desc = "", string show = "", string sort = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  faq "
                     + "set "
                     + "  faq_title = @faq_title "
                     + ", faq_desc = @faq_desc "
                     + ", status = @status "
                     + ", sort = @sort "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  faq_id = @faq_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@faq_id", faq_id);
                cmd.Parameters.AddWithValue("@faq_title", faq_title);
                cmd.Parameters.AddWithValue("@faq_desc", faq_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region Faq資料刪除 Faq_Del
        public string Faq_Del(string faq_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from "
                     + "  faq "
                     + "where "
                     + "  faq_id = @faq_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@faq_id", faq_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 職工福利 Staff

        #region 職工福利資料 Staff_List
        public DataTable Staff_List(ref string err_msg, string s_id = "", string sort = "", string status = "", string title_query = "")
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();

            string[] Array_s_id;
            string[] Array_title_query;

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;

                Array_s_id = s_id.Split(',');
                Array_title_query = title_query.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select "
                     + "  a1.s_id, a1.s_title, a1.s_desc, a1.sort, a1.status  "
                     + "from "
                     + "   Staff a1 "
                     + "where "
                     + "  a1.status <> 'D' ";


                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (s_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.s_id in (";
                    for (int i = 0; i < Array_s_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_s_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.s_title like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ") a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.sort ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }


                if (s_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_s_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_s_id" + i.ToString(), Array_s_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["staff"] != null)
                {
                    ds.Tables["staff"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "staff");
                news_ada = null;

                dt = ds.Tables["staff"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region 職工福利資料新增 Staff_Insert
        //prod_name, manure_no, manure_info, manure_ingredients , manure_trait , prod_desc , lang, show, sort
        public string Staff_Insert(string s_title = "", string s_desc = "", string show = "", string sort = "")
        {
            string c_msg = "";
            //string s_id = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from staff";
                cmd.CommandText = csql;

                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//

                csql = @"insert into Staff(s_title, s_desc, sort ,status) "
                     + "values(@s_title,@s_desc,@sort,@status)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@s_title", s_title);
                cmd.Parameters.AddWithValue("@s_desc", s_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 職工福利資料更新 Staff_Update
        //更新內容
        public string Staff_Update(string s_id = "", string s_title = "", string s_desc = "", string show = "", string sort = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  Staff "
                     + "set "
                     + "  s_title = @s_title "
                     + ", s_desc = @s_desc "
                     + ", status = @status "
                     + ", sort = @sort "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  s_id = @s_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@s_id", s_id);
                cmd.Parameters.AddWithValue("@s_title", s_title);
                cmd.Parameters.AddWithValue("@s_desc", s_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 職工福利資料刪除 Staff_Del
        public string Staff_Del(string s_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from "
                     + "  Staff "
                     + "where "
                     + "  s_id = @s_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@s_id", s_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 合作夥伴類別
        #region 合作夥伴類別 Partner_Cate_List
        public DataTable Partner_Cate_List(ref string err_msg, string cate_id = "", string sort = "", string status = "", string title_query = "")
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();

            string[] Array_cate_id;
            string[] Array_title_query;

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;

                Array_cate_id = cate_id.Split(',');
                Array_title_query = title_query.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select distinct "
                     + "  a1.cate_id, a1.cate_name, a1.cate_desc "
                     + ", a1.sort, a1.status "
                     //+ ", a4.img_id, a4.img_file, a4.img_desc "
                     + "from "
                     + "   Partner_Cate a1 "
                     //+ "left join Partner_cate_img a4 on Convert(nvarchar,a1.cate_id) = a4.img_no "
                     + "where "
                     + "  a1.status <> 'D' ";

                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (cate_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.cate_id in (";
                    for (int i = 0; i < Array_cate_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_cate_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.cate_name like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ")a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.sort ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }

                if (cate_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_cate_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_cate_id" + i.ToString(), Array_cate_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["Partner_cate"] != null)
                {
                    ds.Tables["Partner_cate"].Clear();
                }

                SqlDataAdapter ADS_cate_ada = new SqlDataAdapter();
                ADS_cate_ada.SelectCommand = cmd;
                ADS_cate_ada.Fill(ds, "Partner_cate");
                ADS_cate_ada = null;

                dt = ds.Tables["Partner_cate"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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
            
            return dt;
        }
        #endregion

        #region 合作夥伴類別新增 Partner_Cate_Insert
        public string Partner_Cate_Insert(string cate_name = "", string cate_desc = "", string is_show = "", string sort = "", string img_no = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from Partner_Cate";
                cmd.CommandText = csql;

                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//

                csql = @"insert into Partner_Cate(cate_name,cate_desc,sort,status) "
                     + "values(@cate_name,@cate_desc,@sort,@is_show)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_name);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);

                cmd.ExecuteNonQuery();

                ////抓取其編號
                //csql = @"select distinct "
                //     + "  cate_id "
                //     + "from "
                //     + "   Partner_Cate "
                //     + "where "
                //     + "    cate_name = @cate_name "
                //     + "and cate_desc = @cate_desc "
                //     + "and sort = @sort "
                //     + "and status = @is_show ";

                //cmd.CommandText = csql;

                //////讓ADO.NET自行判斷型別轉換
                //cmd.Parameters.Clear();
                //cmd.Parameters.AddWithValue("@cate_name", cate_name);
                //cmd.Parameters.AddWithValue("@cate_desc", cate_desc);
                //cmd.Parameters.AddWithValue("@sort", sort);
                //cmd.Parameters.AddWithValue("@is_show", is_show);

                //if (ds.Tables["chk_Partner_Cate"] != null)
                //{
                //    ds.Tables["chk_Partner_Cate"].Clear();
                //}

                //SqlDataAdapter prod_chk_ada = new SqlDataAdapter();
                //prod_chk_ada.SelectCommand = cmd;
                //prod_chk_ada.Fill(ds, "chk_Partner_Cate");
                //prod_chk_ada = null;

                //if (ds.Tables["chk_Partner_Cate"].Rows.Count > 0)
                //{
                //    cate_id = ds.Tables["chk_Partner_Cate"].Rows[0]["cate_id"].ToString();
                //    if (img_no.Trim().Length > 0)
                //    {
                //        csql = @"update "
                //             + "  Partner_Cate_img "
                //             + "set "
                //             + "  img_no = @cate_id "
                //             + "where "
                //             + "  img_no = @img_no ";

                //        cmd.CommandText = csql;

                //        ////讓ADO.NET自行判斷型別轉換
                //        cmd.Parameters.Clear();
                //        cmd.Parameters.AddWithValue("@cate_id", cate_id);
                //        cmd.Parameters.AddWithValue("@img_no", img_no);

                //        cmd.ExecuteNonQuery();
                //    }
                //}


            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 合作夥伴類別更新 Partner_Cate_Update
        //更新內容
        public string Partner_Cate_Update(string cate_id = "", string cate_name = "", string cate_desc = "", string is_show = "", string sort = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  Partner_Cate "
                     + "set "
                     + "  cate_name = @cate_name "
                     + ", cate_desc = @cate_desc "
                     + ", status = @is_show "
                     + ", sort = @sort "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  cate_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@cate_name", cate_name);
                cmd.Parameters.AddWithValue("@cate_desc", cate_desc);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@is_show", is_show);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 合作夥伴類別刪除 Partner_Cate_Del
        public string Partner_Cate_Del(string cate_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //刪除合作夥伴圖片
                csql = "delete from "
                     + "  Partner_Img "
                     + "where "
                     + "  img_no in ("
                     + "    select "
                     + "      Convert(nvarchar,a1.part_id) "
                     + "    from "
                     + "      Partner a1 "
                     + "    where "
                     + "      a1.cate_id = @cate_id "
                     + "  ) ";


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();
                //刪除合作夥伴資料
                csql = "delete from "
                     + "  Partner "
                     + "where "
                     + "  cate_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();

                //刪除合作夥伴類別資料
                csql = @"delete from "
                     + "  Partner_Cate "
                     + "where "
                     + "  cate_id = @cate_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion
        #endregion

        #region 合作夥伴

        #region 合作夥伴 Partner_List
        public DataTable Partner_List(ref string err_msg, string part_id = "", string sort = "", string status = "", string title_query = "", string cate_id = "")
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();

            string[] Array_part_id;
            string[] Array_title_query;
            string[] Array_cate_id;

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;

                Array_part_id = part_id.Split(',');
                Array_title_query = title_query.Split(',');
                Array_cate_id = cate_id.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select "
                     + "  a1.part_id, a1.cate_id, a1.part_title, a1.part_url "
                     + ", a1.sort, a1.status "
                     + ", a2.cate_name "
                     + ", a4.img_id, a4.img_file, a4.img_desc "
                     + "from "
                     + "   Partner a1 "
                     + "left join Partner_Cate a2 on a1.cate_id = a2.cate_id "
                     + "left join Partner_img a4 on Convert(nvarchar,a1.part_id) = a4.img_no "
                     + "where "
                     + "  a1.status <> 'D' ";


                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (part_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.part_id in (";
                    for (int i = 0; i < Array_part_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_part_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (cate_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.cate_id in (";
                    for (int i = 0; i < Array_cate_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_cate_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.part_title like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                csql = csql + ") a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.cate_id,a1.sort ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }


                if (cate_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_cate_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_cate_id" + i.ToString(), Array_cate_id[i]);
                    }
                }

                if (part_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_part_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_part_id" + i.ToString(), Array_part_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["Partner"] != null)
                {
                    ds.Tables["Partner"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "Partner");
                news_ada = null;

                dt = ds.Tables["Partner"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region 合作夥伴資料新增 Partner_Insert
        //prod_name, manure_no, manure_info, manure_ingredients , manure_trait , prod_desc , lang, show, sort
        public string Partner_Insert(string cate_id = "", string part_title = "", string part_url = "", string show = "", string sort = "", string img_no = "")
        {
            string c_msg = "";
            string part_id = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from Partner where cate_id=@cate_id";
                cmd.CommandText = csql;
                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//


                csql = @"insert into Partner(cate_id, part_title, part_url, sort ,status) "
                     + "values(@cate_id,@part_title,@part_url,@sort,@status)";
                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@part_title", part_title);
                cmd.Parameters.AddWithValue("@part_url", part_url);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();

                //抓取其編號
                csql = @"select distinct "
                     + "  part_id "
                     + "from "
                     + "   Partner "
                     + "where "
                     + "    cate_id = @cate_id "
                     + "and part_title = @part_title "
                     + "and part_url = @part_url "
                     + "and sort = @sort "
                     + "and status = @status ";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@part_title", part_title);
                cmd.Parameters.AddWithValue("@part_url", part_url);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                if (ds.Tables["chk_partner"] != null)
                {
                    ds.Tables["chk_partner"].Clear();
                }

                SqlDataAdapter prod_chk_ada = new SqlDataAdapter();
                prod_chk_ada.SelectCommand = cmd;
                prod_chk_ada.Fill(ds, "chk_partner");
                prod_chk_ada = null;

                if (ds.Tables["chk_partner"].Rows.Count > 0)
                {
                    part_id = ds.Tables["chk_partner"].Rows[0]["part_id"].ToString();
                    if (img_no.Trim().Length > 0)
                    {
                        csql = @"update "
                             + "  Partner_img "
                             + "set "
                             + "  img_no = @part_id "
                             + "where "
                             + "  img_no = @img_no ";

                        cmd.CommandText = csql;

                        ////讓ADO.NET自行判斷型別轉換
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@part_id", part_id);
                        cmd.Parameters.AddWithValue("@img_no", img_no);

                        cmd.ExecuteNonQuery();
                    }
                }


            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 合作夥伴資料更新 Partner_Update
        //更新內容
        public string Partner_Update(string part_id = "", string cate_id = "", string part_title = "", string part_url = "", string show = "", string sort = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  Partner "
                     + "set "
                     + "  cate_id = @cate_id "
                     + ", part_title = @part_title "
                     + ", part_url = @part_url "
                     + ", status = @status "
                     + ", sort = @sort "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  part_id = @part_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@part_id", part_id);
                cmd.Parameters.AddWithValue("@cate_id", cate_id);
                cmd.Parameters.AddWithValue("@part_title", part_title);
                cmd.Parameters.AddWithValue("@part_url", part_url);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region 合作夥伴資料刪除 Partner_Del
        public string Partner_Del(string part_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //刪除圖片
                csql = "delete from "
                     + "  Partner_Img "
                     + "where "
                     + "  img_no = @part_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@part_id", part_id);

                cmd.ExecuteNonQuery();

                //刪除合作夥伴
                csql = @"delete from "
                     + "  Partner "
                     + "where "
                     + "  part_id = @part_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@part_id", part_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region 合作夥伴圖片

        #region 合作夥伴圖片陳列 Partner_Img_List
        public DataTable Partner_Img_List(ref string err_msg,string img_no = "")
        {
            DataSet dt = new DataSet();
            DataTable d_t = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();

            string[] cimg_no;
            string str_img_no = "";

            try
            {
                cimg_no = img_no.Split(',');

                for (int i = 0; i < cimg_no.Length; i++)
                {
                    if (i > 0)
                    {
                        str_img_no = str_img_no + ",";
                    }
                    str_img_no = str_img_no + "'" + cimg_no[i] + "'";
                }

                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }


                cmd.Connection = conn;

                csql = "select * from Partner_Img where status = 'Y' ";
                if (img_no != "ALL")
                {
                    csql = csql + "and img_no in (";
                    for (int i = 0; i < cimg_no.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_img_no" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }


                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                for (int i = 0; i < cimg_no.Length; i++)
                {
                    cmd.Parameters.AddWithValue("@str_img_no" + i.ToString(), cimg_no[i]);
                }


                if (dt.Tables["img"] != null)
                {
                    dt.Tables["img"].Clear();
                }

                SqlDataAdapter scenic_ada = new SqlDataAdapter();
                scenic_ada.SelectCommand = cmd;
                scenic_ada.Fill(dt, "img");
                scenic_ada = null;

                d_t = dt.Tables["img"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                cmd = null;
                conn = null;
                dt = null;
            }

            return d_t;
        }
        #endregion

        #region 合作夥伴圖片刪除 Partner_Img_Delete
        public string Partner_Img_Delete(string img_id = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"delete from Partner_img where img_id = @img_id ";

                cmd.CommandText = csql;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_id", img_id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 合作夥伴圖片更新 Partner_Img_Update
        public string Partner_Img_Update(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  Partner_img "
                     + "set "
                     + "  img_file = @img_file "
                     + "where "
                     + "  img_no = @img_no ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_file", img_file);
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region 合作夥伴圖片新增 Partner_Img_Insert
        public string Partner_Img_Insert(string img_no = "", string img_file = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"insert into Partner_img(img_no, img_file) "
                     + "values(@img_no ,@img_file)";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@img_no", img_no);
                cmd.Parameters.AddWithValue("@img_file", img_file);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion

        #region Footer

        #region Footer陳列 Foot_List
        public DataTable Foot_List(ref string err_msg, string foot_id = "", string sort = "", string status = "", string title_query = "", string foot_url = "")
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(CService.conn_string());
            SqlCommand cmd = new SqlCommand();
            string[] Array_foot_id;
            string[] Array_title_query;
            string[] Array_foot_url;

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;

                Array_foot_id = foot_id.Split(',');
                Array_title_query = title_query.Split(',');
                Array_foot_url = foot_url.Split(',');

                csql = "select "
                     + "  a1.* "
                     + "from "
                     + "("
                     + "select "
                     + "  a1.foot_id, a1.foot_title, a1.foot_url, a1.is_blank "
                     + ", a1.sort, a1.status "
                     + "from "
                     + "   Foot a1 "
                     + "where "
                     + "  a1.status <> 'D' ";


                if (status.Trim().Length > 0)
                {
                    csql = csql + " and a1.status = @status ";
                }

                if (foot_id.Trim().Length > 0)
                {
                    csql = csql + " and a1.foot_id in (";
                    for (int i = 0; i < Array_foot_id.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_foot_id" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                if (title_query.Trim().Length > 0)
                {
                    csql = csql + " and (";
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + " or ";
                        }
                        csql = csql + " a1.foot_title like @str_title_query" + i.ToString() + " ";
                    }
                    csql = csql + ") ";
                }

                if (foot_url.Trim().Length > 0)
                {
                    csql = csql + " and a1.foot_url in (";
                    for (int i = 0; i < Array_foot_url.Length; i++)
                    {
                        if (i > 0)
                        {
                            csql = csql + ",";
                        }
                        csql = csql + "@str_foot_url" + i.ToString();
                    }
                    csql = csql + ") ";
                }

                csql = csql + ") a1 ";

                if (sort.Trim().Length > 0)
                {
                    csql = csql + " order by " + sort + " ";
                }
                else
                {
                    csql = csql + " order by a1.sort ";
                }

                cmd.CommandText = csql;

                //---------------------------------------------------------------//
                cmd.Parameters.Clear();
                if (status.Trim().Length > 0)
                {
                    cmd.Parameters.AddWithValue("@status", status);
                }

                if (foot_id.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_foot_id.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_foot_id" + i.ToString(), Array_foot_id[i]);
                    }
                }

                if (title_query.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_title_query.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_title_query" + i.ToString(), "%" + Array_title_query[i] + "%");
                    }
                }

                if (foot_url.Trim().Length > 0)
                {
                    for (int i = 0; i < Array_foot_url.Length; i++)
                    {
                        cmd.Parameters.AddWithValue("@str_foot_url" + i.ToString(), Array_foot_url[i]);
                    }
                }

                //--------------------------------------------------------------//

                if (ds.Tables["Footer"] != null)
                {
                    ds.Tables["Footer"].Clear();
                }

                SqlDataAdapter news_ada = new SqlDataAdapter();
                news_ada.SelectCommand = cmd;
                news_ada.Fill(ds, "Footer");
                news_ada = null;
                dt = ds.Tables["Footer"];
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(ex.Message);
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

            return dt;
        }
        #endregion

        #region Footer資料新增 Foot_Insert
        public string Foot_Insert(string foot_title = "", string foot_url = "", string show = "", string sort = "",string is_blank = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                //========抓取sort==============================================================//
                csql = "select (max(sort) + 1) as sort from Foot";
                cmd.CommandText = csql;
                if (ds.Tables["chk_sort"] != null)
                {
                    ds.Tables["chk_sort"].Clear();
                }

                SqlDataAdapter chk_sort_ada = new SqlDataAdapter();
                chk_sort_ada.SelectCommand = cmd;
                chk_sort_ada.Fill(ds, "chk_sort");
                chk_sort_ada = null;
                if (ds.Tables["chk_sort"].Rows.Count > 0)
                {
                    sort = ds.Tables["chk_sort"].Rows[0]["sort"].ToString();
                }
                else
                {
                    sort = "0";
                }
                //===============================================================================//
                csql = @"insert into Foot(foot_title, foot_url, sort ,status,is_blank) "
                     + "values(@foot_title,@foot_url,@sort,@status,@is_blank)";

                cmd.CommandText = csql;

                ////讓ADO.NET自行判斷型別轉換
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@foot_title", foot_title);
                cmd.Parameters.AddWithValue("@foot_url", foot_url);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);
                cmd.Parameters.AddWithValue("@is_blank", is_blank);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #region Foot_Menu資料更新 Foot_Update
        //更新內容
        public string Foot_Update(string foot_id = "", string foot_title = "", string foot_url = "", string show = "", string sort = "", string is_blank = "")
        {
            string c_msg = "";
            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
                csql = @"update "
                     + "  Foot "
                     + "set "
                     + "  foot_title = @foot_title "
                     + ", foot_url = @foot_url "
                     + ", status = @status "
                     + ", sort = @sort "
                     + ", is_blank = @is_blank "
                     + ", _UPD_ID = 'System' "
                     + ", _UPD_DT = getdate() "
                     + "where "
                     + "  foot_id = @foot_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@foot_id", foot_id);
                cmd.Parameters.AddWithValue("@foot_title", foot_title);
                cmd.Parameters.AddWithValue("@foot_url", foot_url);
                cmd.Parameters.AddWithValue("@sort", sort);
                cmd.Parameters.AddWithValue("@status", show);
                cmd.Parameters.AddWithValue("@is_blank", is_blank);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;

        }
        #endregion

        #region Foot資料刪除 Foot_Del
        public string Foot_Del(string foot_id = "")
        {
            string c_msg = "";

            SqlConnection conn = new SqlConnection(CService.conn_string());
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            try
            {
               
                //刪除Foot
                csql = @"delete from "
                     + "  Foot "
                     + "where "
                     + "  foot_id = @foot_id ";

                cmd.CommandText = csql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@foot_id", foot_id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                c_msg = ex.Message;
                logger.Error(ex.Message);
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

            return c_msg;
        }
        #endregion

        #endregion
    }
}