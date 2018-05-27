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
    public class ProjectController : Controller
    {
        DBService DB = new DBService();
        //Log 記錄
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ProjectController()
        {
            ViewBag.IsFirstPage = false;
        }



        // GET: Project

        public ActionResult Index(int page = 1)
        {
            return RedirectToAction("List", new { page = page });
        }

        // 套程式-最新消息
        // 列表
        public ActionResult List(int page = 1)
        {
            //定義變數
            DataTable d_proj;
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable d_microsoft;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();

            //抓取消息資料
            d_proj = DB.Proj_List(ref err_msg, "", "proj_date desc", "Y", "", "", "", "");
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");
            //抓取微軟專區
            d_microsoft = DB.Microsoft_Img_List(ref err_msg, "", "Y", "", "sort");

            //設定傳值
            ViewData["d_proj"] = d_proj;
            ViewData["page"] = page;
            //ViewData["d_menub"] = d_menub;
            //ViewData["d_menus"] = d_menus;
            ViewData["d_foot"] = d_foot;
            ViewData["d_microsoft"] = d_microsoft;

            return View();
        }

        // 內容
        public ActionResult ProjData(string proj_id = "")
        {
            //定義變數
            DataTable d_proj;
            //變數設定 
            //DataTable d_menub;
            //DataTable d_menus;
            DataTable d_foot;
            DataTable d_microsoft;
            string err_msg = "";
            //Menu
            //d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            //d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            chk_menu();

            d_proj = DB.Proj_List(ref err_msg, proj_id, "", "Y", "", "", "", "");
            //Foot
            d_foot = DB.Foot_List(ref err_msg, "", "sort", "Y", "");
            //抓取微軟專區
            d_microsoft = DB.Microsoft_Img_List(ref err_msg, "", "Y", "", "sort");

            if (d_proj.Rows.Count > 0)
            {
                ViewData["d_proj"] = d_proj;
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
            if(Session["d_menub"] != null && Session["d_menub"].ToString() != "")
            {

            }
            else
            {
                d_menub = DB.Prod_CateB_List(ref err_msg, "", "sort", "Y", "");
                Session["d_menub"] = d_menub;
            }

            if(Session["d_menus"] != null && Session["d_menus"].ToString() != "")
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