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
    public class HomeController : Controller
    {
        DBService DB = new DBService();
        //Log 記錄
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public HomeController()
        {
            ViewBag.IsFirstPage = false;
        }

        // all 靜態
        public ActionResult Index()
        {
            ViewBag.IsFirstPage = true;
            //記錄
            //logger.Info("Index-Test");
            //定義變數
            DataTable d_news;
            DataTable d_ad;
            DataTable d_proj;
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_ads;
            DataTable d_ads_cate;
            DataTable d_foot;
            string err_msg = "";
            //抓取大圖廣告
            d_ad = DB.Ad_Img_List(ref err_msg,"","Y");

            //抓取消息資料
            d_news = DB.News_List(ref err_msg, "", "n_date desc", "Y", "", "", "", "Y");
            //抓取成交案例
            d_proj = DB.Proj_List(ref err_msg, "", "proj_date desc", "Y", "", "", "", "Y");

            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "");
            chk_menu();
            
            //抓取小圖廣告
            d_ads = DB.ADS_List(ref err_msg, "", "sort", "Y", "", "");
            d_ads_cate = DB.ADS_Cate_List(ref err_msg, "", "sort", "Y", "");
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            //設定傳值
            ViewData["d_news"] = d_news;
            ViewData["d_ad"] = d_ad;
            ViewData["d_proj"] = d_proj;
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_ads"] = d_ads;
            ViewData["d_ads_cate"] = d_ads_cate;
            ViewData["d_foot"] = d_foot;
            return View();            
        }

        // 公司簡介
        public ActionResult AboutUs()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");
            //-------------------------------------------------//
            string cTitle = "";
            if (d_foot.Rows.Count > 0)
            {
                DataTable dr = d_foot.Select("foot_url='~/Home/AboutUs'").CopyToDataTable();
                if (dr.Rows.Count > 0)
                {
                    cTitle = dr.Rows[0]["foot_title"].ToString();
                }
            }

            ViewData["cTitle"] = cTitle;
            //--------------------------------------------------//
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;

            return View();
        }

        //里程碑
        public ActionResult AboutHistory()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable dt;
            string err_msg = "";
            
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            dt = DB.His_List(ref err_msg, "", "sort desc", "Y", "");

            //-------------------------------------------------//
            string cTitle = "";
            if (d_foot.Rows.Count > 0)
            {
                DataTable dr = d_foot.Select("foot_url='~/Home/AboutHistory'").CopyToDataTable();
                if (dr.Rows.Count > 0)
                {
                    cTitle = dr.Rows[0]["foot_title"].ToString();
                }
            }

            ViewData["cTitle"] = cTitle;
            //--------------------------------------------------//

            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;

            ViewData["dt"] = dt;

            return View();
        }

        // 客服中心
        public ActionResult ContactUs()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;

            return View();
        }

        //合作夥伴
        public ActionResult Partner()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable dt;
            DataTable dt_b;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            //抓取小圖廣告
            dt = DB.Partner_List(ref err_msg, "", "sort", "Y", "", "");
            dt_b = DB.Partner_Cate_List(ref err_msg, "", "sort", "Y", "");

            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;

            //-------------------------------------------------//
            string cTitle = "";
            if (d_foot.Rows.Count > 0)
            {
                DataTable dr = d_foot.Select("foot_url='~/Home/Partner'").CopyToDataTable();
                if (dr.Rows.Count > 0)
                {
                    cTitle = dr.Rows[0]["foot_title"].ToString();
                }
            }

            ViewData["cTitle"] = cTitle;
            //--------------------------------------------------//

            ViewData["d_foot"] = d_foot;
            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;

            return View();
        }

        //員工福利
        public ActionResult Staff()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable dt;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            dt = DB.Staff_List(ref err_msg, "", "sort", "Y", "");

            //-------------------------------------------------//
            string cTitle = "";
            if (d_foot.Rows.Count > 0)
            {
                DataTable dr = d_foot.Select("foot_url='~/Home/Staff'").CopyToDataTable();
                if (dr.Rows.Count > 0)
                {
                    cTitle = dr.Rows[0]["foot_title"].ToString();
                }
            }

            ViewData["cTitle"] = cTitle;
            //--------------------------------------------------//
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;
            ViewData["dt"] = dt;
            return View();
        }

        //FAQ
        public ActionResult Faq()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable dt;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            //Faq
            dt = DB.Faq_List(ref err_msg, "", "sort", "Y", "");

            //-------------------------------------------------//
            string cTitle = "";
            if (d_foot.Rows.Count > 0)
            {
                DataTable dr = d_foot.Select("foot_url='~/Home/Faq'").CopyToDataTable();
                if (dr.Rows.Count > 0)
                {
                    cTitle = dr.Rows[0]["foot_title"].ToString();
                }
            }

            ViewData["cTitle"] = cTitle;
            //--------------------------------------------------//
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;
            ViewData["dt"] = dt;

            return View();
        }


        public ActionResult Privacy()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            //-------------------------------------------------//
            string cTitle = "";
            if (d_foot.Rows.Count > 0)
            {
                DataTable dr = d_foot.Select("foot_url='~/Home/Privacy'").CopyToDataTable();
                if (dr.Rows.Count > 0)
                {
                    cTitle = dr.Rows[0]["foot_title"].ToString();
                }
            }

            ViewData["cTitle"] = cTitle;
            //--------------------------------------------------//
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;

            return View();
        }

        public ActionResult SiteMap()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            //-------------------------------------------------//
            string cTitle = "";
            if (d_foot.Rows.Count > 0)
            {
                DataTable dr = d_foot.Select("foot_url='~/Home/SiteMap'").CopyToDataTable();
                if (dr.Rows.Count > 0)
                {
                    cTitle = dr.Rows[0]["foot_title"].ToString();
                }
            }

            ViewData["cTitle"] = cTitle;
            //--------------------------------------------------//
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;

            return View();
        }


        //發送MAIL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ContactUs(string txt_name, string txt_email, string txt_subject, string txt_message, string ValidCode)
        {
            string cmsg = "";
            string n_date = DateTime.Now.ToString("yyyy-MM-dd");
            string n_time = DateTime.Now.ToString("yyyy-MM-dd HH:mi:ss");
            //string txt_body = "";
            string To_Mail = "service@iscom.com.tw";
            string To_CC = "";
            //string To_Mail_Name = "";
            string cHtmlBody = "";
            string cSubject = "";
            DataTable dt = new DataTable();
            string err_msg = "";
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;

            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;

            try
            {
                Service sc = new Service();
                //if (string.IsNullOrWhiteSpace(txt_name))
                //{
                //    if (cmsg.Trim().Length > 0)
                //    {
                //        cmsg = cmsg + "<br />";
                //    }
                //    cmsg = cmsg + "請輸入姓名";
                //}

                if (string.IsNullOrWhiteSpace(txt_email))
                {
                    if (cmsg.Trim().Length > 0)
                    {
                        cmsg = cmsg + "<br />";
                    }
                    cmsg = cmsg + "請輸入e-mail";
                }

                if (string.IsNullOrWhiteSpace(txt_subject))
                {
                    txt_subject = "";
                }
                //if (string.IsNullOrWhiteSpace(txt_message))
                //{
                //    if (cmsg.Trim().Length > 0)
                //    {
                //        cmsg = cmsg + "<br />";
                //    }
                //    cmsg = cmsg + "請輸入內容";
                //}
                if (cmsg.Trim().Length == 0)
                {
                    //比對驗證碼
                    if (ValidCode != Session["ValidateCode"].ToString())
                    {
                        if (cmsg.Trim().Length > 0)
                        {
                            cmsg = cmsg + "<br />";
                        }
                        cmsg = cmsg + "驗證碼不正確";
                    }
                    else
                    {
                        //呼叫mail
                        cSubject = "采威網站系統 - 線上諮詢 [" + n_date + "]";
                        cHtmlBody = "";
                        cHtmlBody = cHtmlBody + "您好，" + "<br>";
                        cHtmlBody = cHtmlBody + "這是由采威網站系統所寄發的一封 - 線上諮詢！" + "<br>";
                        cHtmlBody = cHtmlBody + "以下為諮詢內容：" + "<br>";
                        cHtmlBody = cHtmlBody + "==========================================================" + "<br>";
                        cHtmlBody = cHtmlBody + "姓　　名：" + txt_name + "<br><br>";
                        cHtmlBody = cHtmlBody + "E-Mail  ：" + txt_email + "<br><br>";
                        cHtmlBody = cHtmlBody + "連絡電話：" + txt_subject + "<br><br>";
                        cHtmlBody = cHtmlBody + "諮詢內容：" + "<br>";
                        cHtmlBody = cHtmlBody + txt_message + "<br><br>";
                        //cHtmlBody = cHtmlBody + "請立即處理此諮詢資料！" + "<br>";
                        cHtmlBody = cHtmlBody + "==========================================================" + "<br>";
                        //biztest@azuretestsandbox.onmicrosoft.com
                        //dt = sc.Mail("WebAdm@iscom.com.tw", "Web_Adm", To_Mail, cSubject, cHtmlBody);
                        dt = sc.Mail("EIP@iscom.com.tw", "Web_Adm", To_Mail, txt_email, cSubject, cHtmlBody);

                        if (dt.Rows[0]["status"].ToString() == "Y")
                        {
                            TempData["msg"] = "Your message has been sent to us.\n已通知相關人員，請耐心等待回覆~~謝謝";
                            TempData["msg_status"] = "Y";
                        }
                        else
                        {
                            TempData["msg"] = "There was an error sending your message.\n" + dt.Rows[0]["err_msg"].ToString();
                            TempData["msg_status"] = "N";
                        }
                    }
                }


                //if (cmsg.Trim().Length > 0)
                //{
                //    TempData["msg"] = cmsg;
                //    TempData["msg_status"] = "N";
                //}
                //else
                //{
                //    if (dt.Rows.Count > 0)
                //    {
                //        if(dt.Rows[0]["status"].ToString() == "Y")
                //        {
                //            //TempData["message"] = "已通知相關人員，請耐心等待回覆~~謝謝";
                //            TempData["msg"] = "<strong>Success!</strong> Your message has been sent to us.";
                //            TempData["msg_status"] = "Y";
                //        }
                //        else
                //        {
                //            //TempData["message"] = "通知失敗!!";
                //            TempData["msg"] = "<strong>Error!</strong> There was an error sending your message.";
                //            TempData["msg_status"] = "N";
                //        }

                //        //TempData["message"] = "status:" + dt.Rows[0]["status"].ToString() + ";c_msg:" + dt.Rows[0]["status"].ToString() + ";err_msg:" + dt.Rows[0]["err_msg"].ToString();
                //    }
                //}

                //ViewData["Url"] = "~/Home/ContactUs";
                //return View("To_Location");
                return View("ContactUs");
                //return RedirectToAction("ContactUs");

            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                logger.Error(err_msg);
                return View("Error");
                //return RedirectToAction("Index");
            }
        }

        public ActionResult Error()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            string err_msg = "";

            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;

            return View();
        }

        // 菁英招募
        public ActionResult JoinUs()
        {
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            string err_msg = "";

            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");

            //-------------------------------------------------//
            string cTitle = "";
            if (d_foot.Rows.Count > 0)
            {
                DataTable dr = d_foot.Select("foot_url='~/Home/JoinUs'").CopyToDataTable();
                if (dr.Rows.Count > 0)
                {
                    cTitle = dr.Rows[0]["foot_title"].ToString();
                }
            }

            ViewData["cTitle"] = cTitle;
            //--------------------------------------------------//

            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;

            return View();
        }

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

        public ActionResult ChkValidateCode(string Vali_Code)
        {
            string str_return = "";
            string CVali_Code = "";

            CVali_Code = (string)Session["ValidateCode"];
            if (Vali_Code == CVali_Code)
            {
                str_return = "Y";
            }
            else
            {
                str_return = "N";
            }
            return Content(str_return);
        }

        public void chk_menu()
        {
            DataTable d_menub;
            DataTable d_menus;
            string err_msg = "";
            //Menu
            if (Session["d_menub"] != null && Session["d_menub"].ToString() != "")
            {

            }
            else
            {
                d_menub = DB.Prod_CateB_List(ref err_msg, "", "sort", "Y", "");
                Session["d_menub"] = d_menub;
            }

            if (Session["d_menus"] != null && Session["d_menus"].ToString() != "")
            {

            }
            else
            {
                d_menus = DB.Prod_CateS_List(ref err_msg, "", "sort", "Y", "");
                Session["d_menus"] = d_menus;
            }

        }

    }
}