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
    public class NewsController : Controller
    {
        DBService DB = new DBService();

        public NewsController()
        {
            ViewBag.IsFirstPage = false;
        }

        public ActionResult Index(int page = 1)
        {
            return RedirectToAction("List", new { page = page });
        }

        // 套程式-最新消息
        // 列表
        public ActionResult List(int page=1)
        {
            //定義變數
            DataTable d_news;
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

            //抓取消息資料
            d_news = DB.News_List(ref err_msg ,"", "n_date desc", "Y", "", "", "", "");

            //設定傳值
            ViewData["d_news"] = d_news;
            ViewData["page"] = page;
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;
            return View();
        }

        // 內容
        public ActionResult NewsData(string n_id = "")
        {
            //定義變數
            DataTable d_news;
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

            d_news = DB.News_List(ref err_msg,n_id, "", "Y", "", "", "", "");

            if (d_news.Rows.Count > 0)
            {
                ViewData["d_news"] = d_news;
                //ViewData["d_menub"] = d_menub;
                //ViewData["d_menus"] = d_menus;
                ViewData["d_foot"] = d_foot;
                return View("Content");
            }
            else
            {
                return RedirectToAction("index");
            }
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