using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lib.Service;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
/*Json.NET相關的命名空間*/
using Newtonsoft.Json;


namespace OutWeb.Controllers
{
    public class ManageController : Controller
    {
        DBService DB = new DBService();
        //Log 記錄
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ManageController()
        {
            ViewBag.IsFirstPage = false;
        }

        // GET: Manage
        // 後台首頁-導向Login
        public ActionResult Index()
        {
            if (Convert.ToString(Session["IsLogined"]) == "Y")
            {
                return RedirectToAction("News_List");
            }
            else
            {
                return View("Login");
            }
        }

        // 登入頁
        public ActionResult Login()
        {
            return View();
        }

        //登入檢查
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login_Chk(string account, string pwd, string ValidCode)
        {
            DataTable user_info;
            string cmsg = "";
            string err_msg = "";
            try
            {
                if (string.IsNullOrWhiteSpace(account))
                {
                    if (cmsg.Trim().Length > 0)
                    {
                        cmsg = cmsg + "\n";
                    }
                    cmsg = cmsg + "請輸入帳號";
                }

                if (string.IsNullOrWhiteSpace(pwd))
                {
                    if (cmsg.Trim().Length > 0)
                    {
                        cmsg = cmsg + "\n";
                    }
                    cmsg = cmsg + "請輸入密碼";
                }

                if (cmsg.Trim().Length == 0)
                {
                    //比對驗證碼
                    if (ValidCode != Session["ValidateCode"].ToString())
                    {
                        if (cmsg.Trim().Length > 0)
                        {
                            cmsg = cmsg + "\n";
                        }
                        cmsg = cmsg + "驗證碼不正確";
                    }
                    else
                    {
                        //抓取user資料
                        user_info = DB.User_Info(ref err_msg, account);
                        //驗證使用者有無資料
                        if (user_info.Rows.Count == 0)
                        {
                            if (cmsg.Trim().Length > 0)
                            {
                                cmsg = cmsg + "\n";
                            }
                            cmsg = cmsg + "無此帳號，請再確認。";
                        }
                        else
                        {
                            if (user_info.Rows[0]["status"].ToString() == "N")
                            {
                                if (cmsg.Trim().Length > 0)
                                {
                                    cmsg = cmsg + "\n";
                                }
                                cmsg = cmsg + "此帳號停用，請再確認。";
                            }
                            else
                            {
                                if (pwd == user_info.Rows[0]["pwd"].ToString())
                                {
                                    //輸入值
                                    Session["IsLogined"] = "Y";
                                    Session["Account"] = account;
                                    Session["Account_Name"] = user_info.Rows[0]["account_name"].ToString();
                                    Session["Rank"] = user_info.Rows[0]["rank"].ToString();
                                }
                                else
                                {
                                    if (cmsg.Trim().Length > 0)
                                    {
                                        cmsg = cmsg + "\n";
                                    }
                                    cmsg = cmsg + "密碼錯誤，請重新輸入。";
                                }
                            }
                        }
                    }
                }

                if (cmsg.Trim().Length > 0)
                {
                    TempData["message"] = cmsg;
                    return View("Login");
                }
                else
                {
                    return RedirectToAction("News_List");
                }

            }
            catch(Exception ex)
            {
                logger.Error(ex.Message);
                return View("Login");
            }
        }

        //後台登出
        public ActionResult Logout()
        {
            //清除 Session();
            Session.Remove("IsLogined");
            Session.Remove("Account");
            Session.Remove("Account_Name");
            Session.Remove("Rank");

            return Redirect(Url.Content("~/Manage"));
        }


        #region 最新消息

        #region 消息陳列 News_List
        public ActionResult News_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string txt_start_date = "", string txt_end_date = "", string txt_show = "", string txt_index = "")
        {
            //定義變數
            string c_sort = "";
            DataTable dt;
            string err_msg = "";
           
            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取消息資料
            dt = DB.News_List(ref err_msg, "", c_sort, txt_show, txt_title_query, txt_start_date, txt_end_date,txt_index);
           
            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_start_date"] = txt_start_date;
            ViewData["txt_end_date"] = txt_end_date;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 最新消息新增 News_Add
        public ActionResult News_Add()
        {
            string err_msg = "";
            
            DataTable d_img = DB.News_Img_List(ref err_msg, "");
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "add";
            return View("News_Data");
        }
        #endregion

        #region 最新消息修改 News_Edit
        public ActionResult News_Edit(string n_id = "")
        {
            string err_msg = "";
            DataTable d_news = DB.News_List(ref err_msg, n_id);
            DataTable d_img = DB.News_Img_List(ref err_msg, n_id);

            ViewData["d_img"] = d_img;
            ViewData["d_news"] = d_news;
            ViewData["action_sty"] = "edit";

            return View("News_Data");
        }
        #endregion

        #region 最新消息刪除 News_Del
        public ActionResult News_Del(string n_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.News_Del(n_id);
            return RedirectToAction("News_List");
        }
        #endregion

        #region 最新消息儲存 News_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult News_Save(string action_sty, string n_id, string n_title, string n_date, string n_desc, string show, string hot, string sort, string n_memo,string img_no,string n_url)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.News_Insert(n_title, n_date, n_desc, show, hot, sort,n_memo, n_url, img_no);
                    break;
                case "edit":
                    DB.News_Update(n_id, n_title, n_date, n_desc, show, hot, sort,n_memo, n_url);
                    break;
            }

            return RedirectToAction("News_List");
        }

        #endregion

        #endregion

        #region 成交案例

        #region 成交案例陳列 Proj_List
        public ActionResult Proj_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string txt_start_date = "", string txt_end_date = "", string txt_show = "", string txt_index = "")
        {
            //定義變數
            string c_sort = "";
            DataTable dt;
            string err_msg = "";

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取消息資料
            dt = DB.Proj_List(ref err_msg, "", c_sort, txt_show, txt_title_query, txt_start_date, txt_end_date, txt_index);

            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_start_date"] = txt_start_date;
            ViewData["txt_end_date"] = txt_end_date;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 成交案例新增 Proj_Add
        public ActionResult Proj_Add()
        {
            ViewData["action_sty"] = "add";

            return View("Proj_Data");
        }
        #endregion

        #region 成交案例修改 Proj_Edit
        public ActionResult Proj_Edit(string proj_id = "")
        {
            string err_msg = "";
            DataTable d_proj = DB.Proj_List(ref err_msg, proj_id);
            ViewData["d_proj"] = d_proj;
            ViewData["action_sty"] = "edit";

            return View("Proj_Data");
        }
        #endregion

        #region 成交案例刪除 Proj_Del
        public ActionResult Proj_Del(string proj_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Proj_Del(proj_id);
            return RedirectToAction("Proj_List");
        }
        #endregion

        #region 成交案例儲存 Proj_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Proj_Save(string action_sty, string proj_id, string proj_title, string proj_date, string proj_desc, string show, string hot, string sort, string proj_memo)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Proj_Insert(proj_title, proj_date, proj_desc, show, hot, sort, proj_memo);
                    break;
                case "edit":
                    DB.Proj_Update(proj_id, proj_title, proj_date, proj_desc, show, hot, sort, proj_memo);
                    break;
            }

            return RedirectToAction("Proj_List");
        }

        #endregion

        #endregion

        #region 大圖廣告
        public ActionResult Advertisement()
        {
            string err_msg = "";
            DataTable d_video = DB.Ad_Img_List(ref err_msg, "", "","","sort");
            ViewData["d_video"] = d_video;
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Advertisement_Update(string[] img_id,string[] img_desc, string[] img_url, string[] img_sort, string img_status, string img_count)
        {
            int i_count = 0;
            string str_img_desc = "";
            string str_img_id = "";
            string str_status = "";
            string str_img_url = "";
            string str_img_sort = "";

            string[] arr_status;

            arr_status = img_status.Split(',');
           
            i_count = Convert.ToInt32(img_count);

            if (arr_status.Count() < i_count) // 因為刪除選擇取消，還是會跑，但是沒有狀態資料，會造成陣列出錯
            {
                return RedirectToAction("Advertisement");
            }

            //Activity_Img
            str_img_id = "";
            str_img_desc = "";
            str_status = "";

            for (int i = 0; i < i_count; i++)
            {
                str_img_id = img_id[i];
                str_img_desc = img_desc[i];
                str_status = arr_status[i];
                str_img_url = img_url[i];
                str_img_sort = img_sort[i];

                DB.Ad_Update(str_img_id,str_img_desc,str_status,str_img_url,str_img_sort);
            }
            return RedirectToAction("Advertisement");
        }
        #endregion

        #region 修改密碼 ChangePW
        // 修改密碼
        public ActionResult ChangePW()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ChangePW(string now_pwd, string new_pwd, string chk_new_pwd)
        {
            string Account = Convert.ToString(Session["Account"]);
            string cmsg = "";
            string err_msg = "";
            DataTable user_info;
            //抓取資料
            user_info = DB.User_Info(ref err_msg, Account);
            //檢查登入密碼是否正確
            if (now_pwd == user_info.Rows[0]["pwd"].ToString())
            {
                DB.User_Update(Account, new_pwd);
            }
            else
            {
                if (cmsg.Trim().Length > 0)
                {
                    cmsg = cmsg + "\n";
                }
                cmsg = cmsg + "密碼錯誤，請重新輸入。";
            }

            if (cmsg.Trim().Length == 0)
            {
                cmsg = "密碼變更成功，下次登入請輸入新密碼!!!";
            }

            TempData["message"] = cmsg;
            return View();

        }
        #endregion

        #region 驗證碼 GetValidateCode
        //設置將生成的驗證碼存入Session，並輸出驗證碼圖片
        [AllowAnonymous]
        public ActionResult GetValidateCode()
        {
            ValidateCode vCode = new ValidateCode();
            string code = vCode.CreateValidateCode(5);
            Session["ValidateCode"] = code;
            byte[] bytes = vCode.CreateValidateGraphic(code);
            return File(bytes, @"image/jpeg");
        }
        #endregion

        #region 圖片上傳 Upload
        public ActionResult Upload(string img_no, string img_sta, string img_cate, string img_sty = "")
        {
            DataTable img_file;
            DataTable chk_file;

            HttpFileCollection hfc = System.Web.HttpContext.Current.Request.Files;
            string imgPath = "";
            string filename = "";
            string file_name = "";
            string file_path = "../Images/";
            string str_return = "";
            string[] files;
            string chk_sty = "";
            string pre_filename = "";
            int files_count = 0;
            //string cmsg = "";
            string err_msg = "";

            
            if (hfc.Count > 0)
            {
                file_name = hfc[0].FileName;
                files = file_name.Split('\\');
                files_count = files.Count();
                filename = files[files_count - 1];

                imgPath = file_path + img_no + "_" + img_sta + "_" + filename;
                string PhysicalPath = Server.MapPath(imgPath);
                hfc[0].SaveAs(PhysicalPath);
            }
            switch (img_cate)
            {
                case "Prod":
                    chk_file = DB.Prod_Img_List(ref err_msg, img_no);
                    break;
                case "Ad":
                    chk_file = DB.Ad_Img_List(ref err_msg, img_no);
                    break;
                case "Prod_Cate":
                    chk_file = DB.Prod_Cate_Img_List(ref err_msg, img_no, img_sty);
                    break;
                case "ADS_Cate":
                    chk_file = DB.ADS_Cate_Img_List(ref err_msg, img_no);
                    break;
                case "ADS":
                    chk_file = DB.ADS_Img_List(ref err_msg, img_no);
                    break;
                case "His":
                    chk_file = DB.His_Img_List(ref err_msg, img_no);
                    break;
                case "Partner":
                    chk_file = DB.Partner_Img_List(ref err_msg, img_no);
                    break;
                case "Microsoft":
                    chk_file = DB.Microsoft_Img_List(ref err_msg, img_no);
                    break;
                case "News":
                    chk_file = DB.News_Img_List(ref err_msg, img_no);
                    break;
                case "Classic":
                    chk_file = DB.Classic_Img_List(ref err_msg, img_no);
                    break;
                default:
                    chk_file = DB.Img_List(ref err_msg, img_no, img_sta);
                    break;
            }


            chk_sty = "add";
            if (img_sta == "S")
            {
                if (chk_file.Rows.Count > 0)
                {
                    pre_filename = file_path + chk_file.Rows[0]["img_file"].ToString();

                    chk_sty = "update";
                }
            }

            switch (chk_sty)
            {
                case "add": //加入到資料庫
                    switch (img_cate)
                    {
                        case "Prod":
                            DB.Prod_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "Ad":
                            //解除數量限制
                            //if (chk_file.Rows.Count < 5)
                            //{
                                DB.Ad_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename);
                            //}
                            break;
                        case "Prod_Cate":
                            DB.Prod_Cate_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename,img_sty);
                            break;
                        case "ADS_Cate":
                            DB.ADS_Cate_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "ADS":
                            DB.ADS_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "His":
                            DB.His_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "Partner":
                            DB.Partner_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "Microsoft":
                            DB.Microsoft_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "News":
                            DB.News_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "Classic":
                            DB.Classic_Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        default:
                            //OverlookDB.Img_Insert(img_no, img_no + "_" + img_sta + "_" + filename, img_sta);
                            break;
                    }

                    break;
                case "update":
                    switch (img_cate)
                    {
                        case "Prod":
                            DB.Prod_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "Ad":
                            DB.Ad_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "Prod_Cate":
                            DB.Prod_Cate_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename,img_sty);
                            break;
                        case "ADS_Cate":
                            DB.ADS_Cate_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "ADS":
                            DB.ADS_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "His":
                            DB.His_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "Partner":
                            DB.Partner_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "Microsoft":
                            DB.Microsoft_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "News":
                            DB.News_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        case "Classic":
                            DB.Classic_Img_Update(img_no, img_no + "_" + img_sta + "_" + filename);
                            break;
                        default:
                            //OverlookDB.Img_Update(chk_file.Rows[0]["_SEQ_ID"].ToString(), img_no + "_" + img_sta + "_" + filename);
                            break;
                    }

                    //刪除原本檔案
                    if (pre_filename == "")
                    {
                        string Pre_Path = Server.MapPath(pre_filename);

                        // Delete a file by using File class static method...
                        if (System.IO.File.Exists(Pre_Path))
                        {
                            // Use a try block to catch IOExceptions, to
                            // handle the case of the file already being
                            // opened by another process.
                            try
                            {
                                System.IO.File.Delete(Pre_Path);
                            }
                            catch (System.IO.IOException e)
                            {
                                str_return = str_return + e.Message;
                            }
                        }
                    }


                    break;
            }

            //抓取資料
            switch (img_cate)
            {
                case "Prod":
                    img_file = DB.Prod_Img_List(ref err_msg, img_no);
                    break;
                case "Ad":
                    img_file = DB.Ad_Img_List(ref err_msg, img_no);
                    break;
                case "Prod_Cate":
                    img_file = DB.Prod_Cate_Img_List(ref err_msg, img_no, img_sty);
                    break;
                case "ADS_Cate":
                    img_file = DB.ADS_Cate_Img_List(ref err_msg, img_no);
                    break;
                case "ADS":
                    img_file = DB.ADS_Img_List(ref err_msg, img_no);
                    break;
                case "His":
                    img_file = DB.His_Img_List(ref err_msg, img_no);
                    break;
                case "Partner":
                    img_file = DB.Partner_Img_List(ref err_msg, img_no);
                    break;
                case "Microsoft":
                    img_file = DB.Microsoft_Img_List(ref err_msg, img_no);
                    break;
                case "News":
                    img_file = DB.News_Img_List(ref err_msg, img_no);
                    break;
                case "Classic":
                    img_file = DB.Classic_Img_List(ref err_msg, img_no);
                    break;
                default:
                    img_file = DB.Img_List(ref err_msg, img_no, img_sta);
                    break;
            }

            str_return = JsonConvert.SerializeObject(img_file, Newtonsoft.Json.Formatting.Indented);

            return Content(str_return);


        }
        #endregion

        #region 圖片刪除 Img_Del
        public ActionResult Img_Del(string img_id, string img_sta, string img_no, string img_cate)
        {
            string str_return = "";
            DataTable img_file;
            DataTable chk_file;
            string filename = "";
            string file_path = "../Images/";
            string imgPath = "";
            string err_msg = "";

            switch (img_cate)
            {
                case "Prod":
                    chk_file = DB.Prod_Img_List(ref err_msg, img_no);
                    break;
                case "Ad":
                    chk_file = DB.Ad_Img_List(ref err_msg, img_no);
                    break;
                case "ADS":
                    chk_file = DB.ADS_Img_List(ref err_msg, img_no);
                    break;
                case "ADS_Cate":
                    chk_file = DB.ADS_Cate_Img_List(ref err_msg, img_no);
                    break;
                case "His":
                    chk_file = DB.His_Img_List(ref err_msg, img_no);
                    break;
                case "Partner":
                    chk_file = DB.Partner_Img_List(ref err_msg, img_no);
                    break;
                case "Microsoft":
                    chk_file = DB.Microsoft_Img_List(ref err_msg, img_no);
                    break;
                case "News":
                    chk_file = DB.News_Img_List(ref err_msg, img_no);
                    break;
                case "Classic":
                    chk_file = DB.Classic_Img_List(ref err_msg, img_no);
                    break;
                default:
                    chk_file = DB.Img_List(ref err_msg, img_no, img_sta);
                    break;
            }

            filename = "";

            if (chk_file.Rows.Count > 0)
            {
                for (int i = 0; i < chk_file.Rows.Count; i++)
                {
                    if (img_cate == "Ad" | img_cate == "Microsoft")
                    {
                        if (img_id == chk_file.Rows[i]["ad_id"].ToString())
                        {
                            filename = chk_file.Rows[i]["ad_img"].ToString();
                            break;
                        }
                    }
                    else
                    {
                        if (img_id == chk_file.Rows[i]["img_id"].ToString())
                        {
                            filename = chk_file.Rows[i]["img_file"].ToString();
                            break;
                        }
                    }

                }
            }

            imgPath = file_path + filename;

            string PhysicalPath = Server.MapPath(imgPath);

            // Delete a file by using File class static method...
            if (System.IO.File.Exists(PhysicalPath))
            {
                // Use a try block to catch IOExceptions, to
                // handle the case of the file already being
                // opened by another process.
                try
                {
                    System.IO.File.Delete(PhysicalPath);
                }
                catch (System.IO.IOException e)
                {
                    str_return = str_return + e.Message;
                }
            }

            switch (img_cate)
            {
                case "Prod":
                    DB.Prod_Img_Delete(img_id);
                    break;
                case "Ad":
                    DB.Ad_Img_Delete(img_id);
                    break;
                case "ADS":
                    DB.ADS_Img_Delete(img_id);
                    break;
                case "ADS_Cate":
                    DB.ADS_Cate_Img_Delete(img_id);
                    break;
                case "His":
                    DB.His_Img_Delete(img_id);
                    break;
                case "Partner":
                    DB.Partner_Img_Delete(img_id);
                    break;
                case "Microsoft":
                    DB.Microsoft_Img_Delete(img_id);
                    break;
                case "News":
                    DB.News_Img_Delete(img_id);
                    break;
                case "Classic":
                    DB.Classic_Img_Delete(img_id);
                    break;
                default:
                    //DB.Img_Delete(img_id);
                    break;
            }

            //抓取資料
            switch (img_cate)
            {
                case "Prod":
                    img_file = DB.Prod_Img_List(ref err_msg, img_no);
                    break;
                case "Ad":
                    img_file = DB.Ad_Img_List(ref err_msg, img_no);
                    break;
                case "ADS":
                    img_file = DB.ADS_Img_List(ref err_msg, img_no);
                    break;
                case "ADS_Cate":
                    img_file = DB.ADS_Cate_Img_List(ref err_msg, img_no);
                    break;
                case "His":
                    img_file = DB.His_Img_List(ref err_msg, img_no);
                    break;
                case "Partner":
                    img_file = DB.Partner_Img_List(ref err_msg, img_no);
                    break;
                case "Microsoft":
                    img_file = DB.Microsoft_Img_List(ref err_msg, img_no);
                    break;
                case "News":
                    img_file = DB.News_Img_List(ref err_msg, img_no);
                    break;
                case "Classic":
                    img_file = DB.Classic_Img_List(ref err_msg, img_no);
                    break;
                default:
                    img_file = DB.Img_List(ref err_msg, img_no, img_sta);
                    break;
            }
            str_return = JsonConvert.SerializeObject(img_file, Newtonsoft.Json.Formatting.Indented);
            return Content(str_return);
        }
        #endregion

        #region ckeditor上傳圖片
        /// <summary>
        /// ckeditor上傳圖片
        /// </summary>
        /// <param name="upload">預設參數叫upload</param>
        /// <param name="CKEditorFuncNum"></param>
        /// <param name="CKEditor"></param>
        /// <param name="langCode"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UploadPicture(HttpPostedFileBase upload, string CKEditorFuncNum, string CKEditor, string langCode)
        {
            string result = "";
            if (upload != null && upload.ContentLength > 0)
            {
                //儲存圖片至Server
                upload.SaveAs(Server.MapPath("~/Images/" + upload.FileName));


                var imageUrl = Url.Content("~/Images/" + upload.FileName);

                var vMessage = string.Empty;

                result = @"<html><body><script>window.parent.CKEDITOR.tools.callFunction(" + CKEditorFuncNum + ", \"" + imageUrl + "\", \"" + vMessage + "\");</script></body></html>";

            }

            return Content(result);
        }
        #endregion

        #region 產品類別(大)
        #region 產品類別(大)_陳列 Prod_CateB_List()
        public ActionResult Prod_CateB_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string txt_show = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取產品類別(大)資料
            dt = DB.Prod_CateB_List(ref err_msg, "", c_sort, txt_show, txt_title_query);
            //抓取產品類別(大)_圖片
            

            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 產品類別(大)_新增 Prod_CateB_Add
        public ActionResult Prod_CateB_Add()
        {
            DataTable d_img;
            string err_msg = "";
            d_img = DB.Prod_Cate_Img_List(ref err_msg, " ", "B");
            ViewData["action_sty"] = "add";
            ViewData["d_img"] = d_img;
            return View("Prod_CateB_Data");
        }
        #endregion

        #region 產品類別(大)_修改 Prod_CateB_Edit
        public ActionResult Prod_CateB_Edit(string cate_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.Prod_CateB_List(ref err_msg, cate_id);
            DataTable d_img = DB.Prod_Cate_Img_List(ref err_msg, cate_id, "B");
            ViewData["dt"] = dt;
            ViewData["d_img"] = d_img;

            ViewData["action_sty"] = "edit";

            return View("Prod_CateB_Data");
        }
        #endregion

        #region 產品類別(大)_刪除 Prod_CateB_Del
        public ActionResult Prod_CateB_Del(string cate_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Prod_CateB_Del(cate_id);
            return RedirectToAction("Prod_CateB_List");
        }
        #endregion

        #region 產品類別(大)_儲存 Prod_CateB_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Prod_CateB_Save(string action_sty, string cate_id, string cate_name, string cate_desc, string show, string sort,string img_no)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Prod_CateB_Insert(cate_name, cate_desc, show, sort,img_no);
                    break;
                case "edit":
                    DB.Prod_CateB_Update(cate_id, cate_name, cate_desc, show, sort);
                    break;
            }

            return RedirectToAction("Prod_CateB_List");
        }

        #endregion
        #endregion

        #region 產品類別(小)
        #region 產品類別(小)_陳列 Prod_CateS_List
        public ActionResult Prod_CateS_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string txt_show = "",string cateb_id="")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;
            DataTable dt_b;


            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取產品類別(小)資料
            dt = DB.Prod_CateS_List(ref err_msg, "", c_sort, txt_show, txt_title_query,cateb_id);
            //抓取產品類別(大)
            dt_b = DB.Prod_CateB_List(ref err_msg, "", "sort");


            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;
            ViewData["cateb_id"] = cateb_id;

            return View();
        }
        #endregion

        #region 產品類別(小)_新增 Prod_CateS_Add
        public ActionResult Prod_CateS_Add()
        {
            DataTable d_img;
            DataTable dt_b;
            string err_msg = "";
            d_img = DB.Prod_Cate_Img_List(ref err_msg, " ", "S");
            dt_b = DB.Prod_CateB_List(ref err_msg, "", "sort");
            ViewData["action_sty"] = "add";
            ViewData["d_img"] = d_img;
            ViewData["dt_b"] = dt_b;
            return View("Prod_CateS_Data");
        }
        #endregion

        #region 產品類別(小)_修改 Prod_CateS_Edit
        public ActionResult Prod_CateS_Edit(string cate_id = "")
        {
            //string cateb_id = "";
            string err_msg = "";
            DataTable dt = DB.Prod_CateS_List(ref err_msg, cate_id);
            DataTable d_img = DB.Prod_Cate_Img_List(ref err_msg, cate_id, "S");
            DataTable dt_b = DB.Prod_CateB_List(ref err_msg, "", "sort");

            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["d_img"] = d_img;

            ViewData["action_sty"] = "edit";

            return View("Prod_CateS_Data");
        }
        #endregion

        #region 產品類別(小)_刪除 Prod_CateS_Del
        public ActionResult Prod_CateS_Del(string cate_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Prod_CateS_Del(cate_id);
            return RedirectToAction("Prod_CateS_List");
        }
        #endregion

        #region 產品類別(小)_儲存 Prod_CateS_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Prod_CateS_Save(string action_sty, string cate_id, string cate_name, string cate_desc, string show, string sort, string img_no,string cateb_id)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Prod_CateS_Insert(cate_name, cate_desc, show, sort, img_no,cateb_id);
                    break;
                case "edit":
                    DB.Prod_CateS_Update(cate_id, cate_name, cate_desc, show, sort,cateb_id);
                    break;
            }

            return RedirectToAction("Prod_CateS_List");
        }

        #endregion

        #endregion

        #region 產品

        #region 產品陳列 Prod_List        
        public ActionResult Prod_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string cateb_id = "", string cates_id = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;
            DataTable dt_b;
            DataTable dt_s;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取產品資料
            dt = DB.Prod_List(ref err_msg, "", c_sort, "", txt_title_query, cateb_id, cates_id);
            dt_b = DB.Prod_CateB_List(ref err_msg, "", "sort","Y","");
            dt_s = DB.Prod_CateS_List(ref err_msg, "", "sort","Y","",cateb_id);

            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["dt_s"] = dt_s;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_cateb_id"] = cateb_id;
            ViewData["txt_cates_id"] = cates_id;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 產品資料新增 Prod_Add
        // 產品資料 新增
        public ActionResult Prod_Add()
        {
            DataTable dt_b;
            DataTable dt_s;
            DataTable d_img;
            string err_msg = "";
            string cateb_id = "";
            dt_b = DB.Prod_CateB_List(ref err_msg, "", "sort", "Y", "");
            if(dt_b.Rows.Count > 0)
            {
                cateb_id = dt_b.Rows[0]["cate_b_id"].ToString();
            }
            dt_s = DB.Prod_CateS_List(ref err_msg, "", "sort", "Y", "", cateb_id);
            d_img = DB.Prod_Img_List(ref err_msg, "");
            ViewData["dt_b"] = dt_b;
            ViewData["dt_s"] = dt_s;
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "add";

            return View("Prod_Data");
        }
        #endregion

        #region 產品資料修改 Prod_Edit
        public ActionResult Prod_Edit(string prod_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.Prod_List(ref err_msg, prod_id);
            DataTable dt_b = DB.Prod_CateB_List(ref err_msg, "", "sort", "Y", "");
            DataTable dt_s = DB.Prod_CateS_List(ref err_msg, "", "sort", "Y", "",dt.Rows[0]["cate_b_id"].ToString());
            DataTable d_img = DB.Prod_Img_List(ref err_msg, prod_id);

            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["dt_s"] = dt_s;
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "edit";

            return View("Prod_Data");
        }
        #endregion

        #region 產品資料刪除 Prod_Del
        public ActionResult Prod_Del(string prod_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Prod_Del(prod_id);
            return RedirectToAction("Prod_List");
        }
        #endregion

        #region 產品資料儲存 Prod_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Prod_Save(string action_sty, string prod_id, string cateb_id, string cates_id, string prod_name, string prod_desc, string show, string sort, string img_no)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Prod_Insert(cateb_id, cates_id, prod_name, prod_desc, show, sort, img_no);
                    break;
                case "edit":
                    DB.Prod_Update(prod_id, cateb_id, cates_id, prod_name, prod_desc, show, sort);
                    break;
            }

            return RedirectToAction("Prod_List");
        }
        #endregion

        #region 產品類別取得 Prod_Cate_Get
        public ActionResult Prod_Cate_Get(string cateb_id)
        {
            string str_return = "";
            string err_msg = "";
            DataTable Prod_Cate;
            Prod_Cate = DB.Prod_CateS_List(ref err_msg, "", "sort","Y","",cateb_id);
            str_return = JsonConvert.SerializeObject(Prod_Cate, Newtonsoft.Json.Formatting.Indented);

            return Content(str_return);
        }
        #endregion

        #endregion

        #region 里程碑

        #region 里程碑陳列 His_List        
        public ActionResult His_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取里程碑資料
            dt = DB.His_List(ref err_msg, "", c_sort, "", txt_title_query);

            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 里程碑資料新增 His_Add
        // 產品資料 新增
        public ActionResult His_Add()
        {
            string err_msg = "";
            DataTable d_img = DB.His_Img_List(ref err_msg, "");
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "add";

            return View("His_Data");
        }
        #endregion

        #region 里程碑資料修改 His_Edit
        public ActionResult His_Edit(string his_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.His_List(ref err_msg, his_id);
            DataTable d_img = DB.His_Img_List(ref err_msg, his_id);

            ViewData["dt"] = dt;
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "edit";

            return View("His_Data");
        }
        #endregion

        #region 里程碑資料刪除 His_Del
        public ActionResult His_Del(string his_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.His_Del(his_id);
            return RedirectToAction("His_List");
        }
        #endregion

        #region 里程碑資料儲存 His_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult His_Save(string action_sty, string his_id, string his_ym, string his_title, string his_desc, string show, string sort, string img_no)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.His_Insert(his_ym, his_title, his_desc, show, sort, img_no);
                    break;
                case "edit":
                    DB.His_Update(his_id, his_ym, his_title, his_desc, show, sort);
                    break;
            }

            return RedirectToAction("His_List");
        }
        #endregion

        #endregion

        #region 小圖廣告類別
        #region 小圖廣告類別_陳列 ADS_Cate_List()
        public ActionResult ADS_Cate_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string txt_show = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取小圖廣告類別資料
            dt = DB.ADS_Cate_List(ref err_msg, "", c_sort, txt_show, txt_title_query);
            
            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 小圖廣告類別_新增 ADS_Cate_Add
        public ActionResult ADS_Cate_Add()
        {
            DataTable d_img;
            string err_msg = "";
            d_img = DB.ADS_Cate_Img_List(ref err_msg, " ");
            ViewData["action_sty"] = "add";
            ViewData["d_img"] = d_img;
            return View("ADS_Cate_Data");
        }
        #endregion

        #region 小圖廣告類別_修改 ADS_Cate_Edit
        public ActionResult ADS_Cate_Edit(string cate_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.ADS_Cate_List(ref err_msg, cate_id);
            DataTable d_img = DB.ADS_Cate_Img_List(ref err_msg, cate_id);
            ViewData["dt"] = dt;
            ViewData["d_img"] = d_img;

            ViewData["action_sty"] = "edit";

            return View("ADS_Cate_Data");
        }
        #endregion

        #region 小圖廣告類別_刪除 ADS_Cate_Del
        public ActionResult ADS_Cate_Del(string cate_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.ADS_Cate_Del(cate_id);
            return RedirectToAction("ADS_Cate_List");
        }
        #endregion

        #region 小圖廣告類別_儲存 ADS_Cate_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ADS_Cate_Save(string action_sty, string cate_id, string cate_name, string cate_desc, string show, string sort, string img_no)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.ADS_Cate_Insert(cate_name, cate_desc, show, sort, img_no);
                    break;
                case "edit":
                    DB.ADS_Cate_Update(cate_id, cate_name, cate_desc, show, sort);
                    break;
            }

            return RedirectToAction("ADS_Cate_List");
        }

        #endregion
        #endregion

        #region 小圖廣告

        #region 小圖廣告 ADS_List        
        public ActionResult ADS_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string cate_id = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;
            DataTable dt_b;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取小圖廣告
            dt = DB.ADS_List(ref err_msg, "", c_sort, "", txt_title_query, cate_id);
            //抓取小圖廣告類別
            dt_b = DB.ADS_Cate_List(ref err_msg, "", "sort", "Y", "");
            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_cate_id"] = cate_id;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 小圖廣告新增 ADS_Add
        // 產品資料 新增
        public ActionResult ADS_Add()
        {
            string err_msg = "";
            DataTable dt_b = DB.ADS_Cate_List(ref err_msg, "", "sort", "Y", "");
            DataTable d_img = DB.ADS_Img_List(ref err_msg, "");
            ViewData["dt_b"] = dt_b;
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "add";

            return View("ADS_Data");
        }
        #endregion

        #region 小圖廣告修改 ADS_Edit
        public ActionResult ADS_Edit(string ad_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.ADS_List(ref err_msg, ad_id);
            DataTable dt_b = DB.ADS_Cate_List(ref err_msg, "", "sort", "Y", "");
            DataTable d_img = DB.ADS_Img_List(ref err_msg, ad_id);

            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "edit";

            return View("ADS_Data");
        }
        #endregion

        #region 小圖廣告刪除 ADS_Del
        public ActionResult ADS_Del(string ad_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.ADS_Del(ad_id);
            return RedirectToAction("ADS_List");
        }
        #endregion

        #region 小圖廣告儲存 ADS_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ADS_Save(string action_sty, string ad_id, string cate_id, string ad_title, string ad_url, string show, string sort, string img_no)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.ADS_Insert(cate_id, ad_title, ad_url, show, sort, img_no);
                    break;
                case "edit":
                    DB.ADS_Update(ad_id, cate_id, ad_title, ad_url, show, sort);
                    break;
            }

            return RedirectToAction("ADS_List");
        }
        #endregion
        #endregion

        #region Faq

        #region Faq陳列 Faq_List        
        public ActionResult Faq_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取Faq資料
            dt = DB.Faq_List(ref err_msg, "", c_sort, "", txt_title_query);

            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region Faq資料新增 Faq_Add
        public ActionResult Faq_Add()
        {
            ViewData["action_sty"] = "add";

            return View("Faq_Data");
        }
        #endregion

        #region Faq資料修改 Faq_Edit
        public ActionResult Faq_Edit(string faq_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.Faq_List(ref err_msg, faq_id);

            ViewData["dt"] = dt;
            ViewData["action_sty"] = "edit";

            return View("Faq_Data");
        }
        #endregion

        #region Faq資料刪除 Faq_Del
        public ActionResult Faq_Del(string faq_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Faq_Del(faq_id);
            return RedirectToAction("Faq_List");
        }
        #endregion

        #region Faq資料儲存 Faq_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Faq_Save(string action_sty, string faq_id, string faq_title, string faq_desc, string show, string sort)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Faq_Insert(faq_title, faq_desc, show, sort);
                    break;
                case "edit":
                    DB.Faq_Update(faq_id, faq_title, faq_desc, show, sort);
                    break;
            }

            return RedirectToAction("Faq_List");
        }
        #endregion

        #endregion

        #region 職工福利

        #region 職工福利_List Staff_List        
        public ActionResult Staff_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取Staff資料
            dt = DB.Staff_List(ref err_msg, "", c_sort, "", txt_title_query);

            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 職工福利資料新增 Staff_Add
        public ActionResult Staff_Add()
        {
            ViewData["action_sty"] = "add";

            return View("Staff_Data");
        }
        #endregion

        #region 職工福利資料修改 Staff_Edit
        public ActionResult Staff_Edit(string s_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.Staff_List(ref err_msg, s_id);

            ViewData["dt"] = dt;
            ViewData["action_sty"] = "edit";

            return View("Staff_Data");
        }
        #endregion

        #region 職工福利資料刪除 Staff_Del
        public ActionResult Staff_Del(string s_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Staff_Del(s_id);
            return RedirectToAction("Staff_List");
        }
        #endregion

        #region 職工福利資料儲存 Staff_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Staff_Save(string action_sty, string s_id, string s_title, string s_desc, string show, string sort)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Staff_Insert(s_title, s_desc, show, sort);
                    break;
                case "edit":
                    DB.Staff_Update(s_id, s_title, s_desc, show, sort);
                    break;
            }

            return RedirectToAction("Staff_List");
        }
        #endregion

        #endregion

        #region 合作夥伴類別
        #region 合作夥伴類別_陳列 Partner_Cate_List()
        public ActionResult Partner_Cate_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string txt_show = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取合作夥伴類別資料
            dt = DB.Partner_Cate_List(ref err_msg, "", c_sort, txt_show, txt_title_query);

            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 合作夥伴類別_新增 Partner_Cate_Add
        public ActionResult Partner_Cate_Add()
        {
            ViewData["action_sty"] = "add";
            return View("Partner_Cate_Data");
        }
        #endregion

        #region 合作夥伴類別_修改 Partner_Cate_Edit
        public ActionResult Partner_Cate_Edit(string cate_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.Partner_Cate_List(ref err_msg, cate_id);
            ViewData["dt"] = dt;
            
            ViewData["action_sty"] = "edit";

            return View("Partner_Cate_Data");
        }
        #endregion

        #region 合作夥伴類別_刪除 Partner_Cate_Del
        public ActionResult Partner_Cate_Del(string cate_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Partner_Cate_Del(cate_id);
            return RedirectToAction("Partner_Cate_List");
        }
        #endregion

        #region 合作夥伴類別_儲存 Partner_Cate_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Partner_Cate_Save(string action_sty, string cate_id, string cate_name, string cate_desc, string show, string sort)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Partner_Cate_Insert(cate_name, cate_desc, show, sort);
                    break;
                case "edit":
                    DB.Partner_Cate_Update(cate_id, cate_name, cate_desc, show, sort);
                    break;
            }

            return RedirectToAction("Partner_Cate_List");
        }

        #endregion
        #endregion

        #region 合作夥伴

        #region 合作夥伴 Partner_List        
        public ActionResult Partner_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string cate_id = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;
            DataTable dt_b;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取合作夥伴
            dt = DB.Partner_List(ref err_msg, "", c_sort, "", txt_title_query, cate_id);
            //抓取合作夥伴類別
            dt_b = DB.Partner_Cate_List(ref err_msg, "", "sort", "Y", "");
            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_cate_id"] = cate_id;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 合作夥伴新增 Partner_Add
        // 合作夥伴 新增
        public ActionResult Partner_Add()
        {
            string err_msg = "";
            DataTable dt_b = DB.Partner_Cate_List(ref err_msg, "", "sort", "Y", "");
            DataTable d_img = DB.Partner_Img_List(ref err_msg, "");
            ViewData["dt_b"] = dt_b;
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "add";

            return View("Partner_Data");
        }
        #endregion

        #region 合作夥伴修改 Partner_Edit
        public ActionResult Partner_Edit(string part_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.Partner_List(ref err_msg, part_id);
            DataTable dt_b = DB.Partner_Cate_List(ref err_msg, "", "sort", "Y", "");
            DataTable d_img = DB.Partner_Img_List(ref err_msg, part_id);

            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "edit";

            return View("Partner_Data");
        }
        #endregion

        #region 合作夥伴刪除 Partner_Del
        public ActionResult Partner_Del(string part_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Partner_Del(part_id);
            return RedirectToAction("Partner_List");
        }
        #endregion

        #region 合作夥伴儲存 Partner_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Partner_Save(string action_sty, string part_id, string cate_id, string part_title, string part_url, string show, string sort, string img_no)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Partner_Insert(cate_id, part_title, part_url, show, sort, img_no);
                    break;
                case "edit":
                    DB.Partner_Update(part_id, cate_id, part_title, part_url, show, sort);
                    break;
            }

            return RedirectToAction("Partner_List");
        }
        #endregion
        #endregion

        #region Foot

        #region Foot資料陳列 Foot_List        
        public ActionResult Foot_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;
            
            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取Foot資料
            dt = DB.Foot_List(ref err_msg, "", c_sort, "", txt_title_query);
            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region Foot新增 Foot_Add
        // Foot 新增
        public ActionResult Foot_Add()
        {
            ViewData["action_sty"] = "add";

            return View("Foot_Data");
        }
        #endregion

        #region Foot修改 Foot_Edit
        public ActionResult Foot_Edit(string foot_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.Foot_List(ref err_msg, foot_id);
            
            ViewData["dt"] = dt;
            ViewData["action_sty"] = "edit";

            return View("Foot_Data");
        }
        #endregion

        #region Foot刪除 Foot_Del
        public ActionResult Foot_Del(string foot_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Foot_Del(foot_id);
            return RedirectToAction("Foot_List");
        }
        #endregion

        #region Foot儲存 Foot_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Foot_Save(string action_sty, string foot_id, string foot_title, string foot_url, string show, string sort, string is_blank)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Foot_Insert(foot_title, foot_url, show, sort,is_blank);
                    break;
                case "edit":
                    DB.Foot_Update(foot_id, foot_title, foot_url, show, sort, is_blank);
                    break;
            }

            return RedirectToAction("Foot_List");
        }
        #endregion
        #endregion

        #region 微軟專區
        public ActionResult Microsoft()
        {
            string err_msg = "";
            DataTable d_video = DB.Microsoft_Img_List(ref err_msg, "", "", "", "sort");
            ViewData["d_video"] = d_video;
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Microsoft_Update(string[] img_id, string[] img_desc, string[] img_url, string[] img_sort, string img_status, string img_count)
        {
            int i_count = 0;
            string str_img_desc = "";
            string str_img_id = "";
            string str_status = "";
            string str_img_url = "";
            string str_img_sort = "";

            string[] arr_status;

            arr_status = img_status.Split(',');

            i_count = Convert.ToInt32(img_count);

            if (arr_status.Count() < i_count) // 因為刪除選擇取消，還是會跑，但是沒有狀態資料，會造成陣列出錯
            {
                return RedirectToAction("Microsoft");
            }

            //Activity_Img
            str_img_id = "";
            str_img_desc = "";
            str_status = "";

            for (int i = 0; i < i_count; i++)
            {
                str_img_id = img_id[i];
                str_img_desc = img_desc[i];
                str_status = arr_status[i];
                str_img_url = img_url[i];
                str_img_sort = img_sort[i];

                DB.Microsoft_Update(str_img_id, str_img_desc, str_status, str_img_url, str_img_sort);
            }
            return RedirectToAction("Microsoft");
        }
        #endregion

        #region 經典案例類別
        #region 經典案例類別_陳列 Classic_Cate_List()
        public ActionResult Classic_Cate_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string txt_show = "")
        {
            //定義變數
            string c_sort = "";
            string err_msg = "";
            DataTable dt;

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取經典案例類別資料
            dt = DB.Classic_Cate_List(ref err_msg, "", c_sort, txt_show, txt_title_query);

            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region 經典案例類別_新增 Classic_Cate_Add
        public ActionResult Classic_Cate_Add()
        {
            ViewData["action_sty"] = "add";
            return View("Classic_Cate_Data");
        }
        #endregion

        #region 經典案例類別_修改 Classic_Cate_Edit
        public ActionResult Classic_Cate_Edit(string cate_id = "")
        {
            string err_msg = "";
            DataTable dt = DB.Classic_Cate_List(ref err_msg, cate_id);
            ViewData["dt"] = dt;

            ViewData["action_sty"] = "edit";

            return View("Classic_Cate_Data");
        }
        #endregion

        #region 經典案例類別_刪除 Classic_Cate_Del
        public ActionResult Classic_Cate_Del(string cate_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Classic_Cate_Del(cate_id);
            return RedirectToAction("Classic_Cate_List");
        }
        #endregion

        #region 經典案例類別_儲存 Classic_Cate_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Classic_Cate_Save(string action_sty, string cate_id, string cate_name, string cate_desc, string show, string sort)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Classic_Cate_Insert(cate_name, cate_desc, show, sort);
                    break;
                case "edit":
                    DB.Classic_Cate_Update(cate_id, cate_name, cate_desc, show, sort);
                    break;
            }

            return RedirectToAction("Classic_Cate_List");
        }

        #endregion
        #endregion

        #region 經典案例

        #region 經典案例 Classic_List
        public ActionResult Classic_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string txt_start_date = "", string txt_end_date = "", string txt_show = "", string txt_index = "", string cate_id = "")
        {
            //定義變數
            string c_sort = "";
            DataTable dt;
            DataTable dt_b;
            string err_msg = "";

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取經典案例資料
            dt = DB.Classic_List(ref err_msg, "", c_sort, txt_show, txt_title_query, txt_start_date, txt_end_date, txt_index,cate_id);
            //抓取經典案例類別
            dt_b = DB.Classic_Cate_List(ref err_msg, "", "sort", "Y", "");
            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_start_date"] = txt_start_date;
            ViewData["txt_end_date"] = txt_end_date;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;
            ViewData["txt_cate_id"] = cate_id;

            return View();
        }
        #endregion

        #region 經典案例新增 Classic_Add
        public ActionResult Classic_Add()
        {
            string err_msg = "";

            DataTable d_img = DB.Classic_Img_List(ref err_msg, "");
            DataTable dt_b = DB.Classic_Cate_List(ref err_msg, "", "sort", "Y", "");
            ViewData["dt_b"] = dt_b;
            ViewData["d_img"] = d_img;
            ViewData["action_sty"] = "add";
            return View("Classic_Data");
        }
        #endregion

        #region 經典案例修改 Classic_Edit
        public ActionResult Classic_Edit(string n_id = "")
        {
            string err_msg = "";
            DataTable d_news = DB.Classic_List(ref err_msg, n_id);
            DataTable dt_b = DB.Classic_Cate_List(ref err_msg, "", "sort", "Y", "");
            DataTable d_img = DB.Classic_Img_List(ref err_msg, n_id);

            ViewData["d_img"] = d_img;
            ViewData["dt_b"] = dt_b;
            ViewData["d_news"] = d_news;
            ViewData["action_sty"] = "edit";

            return View("Classic_Data");
        }
        #endregion

        #region 經典案例刪除 Classic_Del
        public ActionResult Classic_Del(string n_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Classic_Del(n_id);
            return RedirectToAction("Classic_List");
        }
        #endregion

        #region 經典案例儲存 Classic_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Classic_Save(string action_sty, string n_id, string n_title, string n_date, string n_desc, string show, string hot, string sort, string n_memo, string img_no, string n_url,string cate_id)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Classic_Insert(n_title, n_date, n_desc, show, hot, sort, n_memo, n_url, img_no,cate_id);
                    break;
                case "edit":
                    DB.Classic_Update(n_id, n_title, n_date, n_desc, show, hot, sort, n_memo, n_url,cate_id);
                    break;
            }

            return RedirectToAction("Classic_List");
        }

        #endregion

        #endregion

        #region EDM

        #region EDM陳列 Edm_List
        public ActionResult Edm_List(string txt_title_query = "", int page = 1, string txt_sort = "", string txt_a_d = "", string txt_start_date = "", string txt_end_date = "", string txt_show = "", string txt_index = "")
        {
            //定義變數
            string c_sort = "";
            DataTable dt;
            string err_msg = "";

            //排序設定
            if (txt_sort.Trim().Length > 0)
            {
                c_sort = c_sort + "a1." + txt_sort;
            }
            if (txt_a_d.Trim().Length > 0)
            {
                c_sort = c_sort + " " + txt_a_d;
            }

            //抓取Edm資料
            dt = DB.Edm_List(ref err_msg, "", c_sort, txt_show, txt_title_query, txt_start_date, txt_end_date, txt_index);

            //設定傳值
            ViewData["page"] = page;
            ViewData["dt"] = dt;
            ViewData["txt_title_query"] = txt_title_query;
            ViewData["txt_start_date"] = txt_start_date;
            ViewData["txt_end_date"] = txt_end_date;
            ViewData["txt_sort"] = txt_sort;
            ViewData["txt_a_d"] = txt_a_d;

            return View();
        }
        #endregion

        #region EDM新增 Edm_Add
        public ActionResult Edm_Add()
        {
            ViewData["action_sty"] = "add";

            return View("Edm_Data");
        }
        #endregion

        #region EDM修改 Edm_Edit
        public ActionResult Edm_Edit(string edm_id = "")
        {
            string err_msg = "";
            DataTable d_edm = DB.Edm_List(ref err_msg, edm_id);
            ViewData["d_edm"] = d_edm;
            ViewData["action_sty"] = "edit";

            return View("Edm_Data");
        }
        #endregion

        #region EDM刪除 Edm_Del
        public ActionResult Edm_Del(string edm_id = "")
        {
            //OverlookDBService OverlookDB = new OverlookDBService();
            DB.Edm_Del(edm_id);
            return RedirectToAction("Edm_List");
        }
        #endregion

        #region EDM儲存 Edm_Save
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edm_Save(string action_sty, string edm_id, string edm_title, string edm_date, string edm_desc, string show, string hot, string sort, string edm_memo)
        {
            //OverlookDBService OverlookDB = new OverlookDBService();

            switch (action_sty)
            {
                case "add":
                    DB.Edm_Insert(edm_title, edm_date, edm_desc, show, hot, sort, edm_memo);
                    break;
                case "edit":
                    DB.Edm_Update(edm_id, edm_title, edm_date, edm_desc, show, hot, sort, edm_memo);
                    break;
            }

            return RedirectToAction("Edm_List");
        }

        #endregion

        #endregion
    }
}