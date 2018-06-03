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
    public class ProdController : Controller
    {
        DBService DB = new DBService();
        //Log 記錄
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ProdController()
        {
            ViewBag.IsFirstPage = false;
        }

        // GET: Prod
        public ActionResult Index(int page = 1, string cateb_id = "", string cates_id = "")
        {
            return RedirectToAction("List", new { page = page, cateb_id = cateb_id, cates_id = cates_id });
        }

        // 套程式-產品
        // 列表
        public ActionResult List(int page = 1, string cateb_id = "", string cates_id = "")
        {
            //定義變數
            DataTable dt;
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable dt_b;
            DataTable dt_s;
            DataTable d_microsoft;
            string err_msg = "";
            
            if(cateb_id.Length == 0)
            {
                cateb_id = "0";
            }

            if(cates_id.Length == 0)
            {
                cates_id = "0";
            }

            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");
            //抓取微軟專區
            d_microsoft = DB.Microsoft_Img_List(ref err_msg, "", "Y", "", "sort");

            //抓取產品資料
            dt = DB.Prod_List(ref err_msg, "", "sort", "Y", "", cateb_id, cates_id);
            dt_b = DB.Prod_CateB_List(ref err_msg, "", "sort", "Y", "");
            dt_s = DB.Prod_CateS_List(ref err_msg, "", "sort", "Y", "", "");

            //設定傳值
            ViewData["dt"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["dt_s"] = dt_s;
            ViewData["page"] = page;
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["cateb_id"] = cateb_id;
            ViewData["cates_id"] = cates_id;
            ViewData["d_foot"] = d_foot;
            ViewData["d_microsoft"] = d_microsoft;

            return View();
        }

        // 內容
        public ActionResult ProdData(string prod_id = "")
        {
            //定義變數
            DataTable dt;
            DataTable dt1;
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable dt_b;
            DataTable dt_s;
            DataTable d_microsoft;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();
            dt1 = DB.Prod_List(ref err_msg, "", "", "Y", "", "", "");
            dt = DB.DB_Select(dt1, "prod_id=" + prod_id);
            //dt = DB.Prod_List(ref err_msg, prod_id, "", "Y", "", "", "");
            dt_b = DB.Prod_CateB_List(ref err_msg, "", "sort", "Y", "");
            dt_s = DB.Prod_CateS_List(ref err_msg, "", "sort", "Y", "", "");
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");
            //抓取微軟專區
            d_microsoft = DB.Microsoft_Img_List(ref err_msg, "", "Y", "", "sort");

            if (dt.Rows.Count > 0)
            {
                ViewData["dt"] = dt;
                //ViewData["d_menub"] = d_menub;
                //ViewData["d_menus"] = d_menus;
                ViewData["dt1"] = dt1;
                ViewData["dt_b"] = dt_b;
                ViewData["dt_s"] = dt_s;
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