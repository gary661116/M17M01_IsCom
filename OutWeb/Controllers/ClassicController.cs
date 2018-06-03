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
    public class ClassicController : Controller
    {
        DBService DB = new DBService();

        public ClassicController()
        {
            ViewBag.IsFirstPage = false;
        }

        // GET: Classic
        public ActionResult Index(int page = 1, string cate_id = "")
        {
            return RedirectToAction("List", new { page = page, cate_id = cate_id });
        }

        // 套程式-經典案例
        // 列表
        public ActionResult List(int page = 1,string cate_id = "")
        {
            //定義變數
            DataTable d_news;
            DataTable d_cate;
            string err_msg = "";
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable d_microsoft;
            

            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");
            //抓取微軟專區
            d_microsoft = DB.Microsoft_Img_List(ref err_msg, "", "Y", "", "sort");

            //抓取經典案例種類
            d_cate = DB.Classic_Cate_List(ref err_msg, "", "sort", "Y", ""); 
            //抓取經典案例資料
            d_news = DB.Classic_List(ref err_msg, "", "n_date desc", "Y", "", "", "", "");

            //設定傳值
            ViewData["d_news"] = d_news;
            ViewData["d_cate"] = d_cate;
            ViewData["cate_id"] = cate_id;
            ViewData["page"] = page;
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;
            ViewData["d_microsoft"] = d_microsoft;
            return View();
        }

        // 內容
        public ActionResult ClassicData(string n_id = "")
        {
            //定義變數
            DataTable d_news;
            DataTable d_cate;
            DataTable dt;
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable d_microsoft;

            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");
            //抓取經典案例種類
            d_cate = DB.Classic_Cate_List(ref err_msg, "", "sort", "Y", "");
            dt = DB.Classic_List(ref err_msg, "", "", "Y", "", "", "", "");
            d_news = DB.DB_Select(dt, "n_id=" + n_id);
            //d_news = DB.Classic_List(ref err_msg, n_id, "", "Y", "", "", "", "");
            //抓取微軟專區
            d_microsoft = DB.Microsoft_Img_List(ref err_msg, "", "Y", "", "sort");

            if (d_news.Rows.Count > 0)
            {
                ViewData["d_news"] = d_news;
                ViewData["d_cate"] = d_cate;
                ViewData["dt"] = dt;
                //ViewData["d_menub"] = d_menub;
                //ViewData["d_menus"] = d_menus;
                ViewData["d_foot"] = d_foot;
                ViewData["d_microsoft"] = d_microsoft;
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