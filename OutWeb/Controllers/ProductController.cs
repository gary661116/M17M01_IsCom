using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OutWeb.Service;
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
    public class ProductController : Controller
    {
        DBService DB = new DBService();

        public ProductController()
        {
            ViewBag.IsFirstPage = false;
        }



        // GET: Project

        public ActionResult Index(int page = 1, string cateb_id="", string cates_id="")
        {
            return RedirectToAction("List", new { page = page, cateb_id = cateb_id, cates_id = cates_id });
        }

        // 套程式-最新消息
        // 列表
        public ActionResult List(int page = 1, string cateb_id = "", string cates_id = "")
        {
            //定義變數
            DataTable dt;
            DataTable d_menub;
            DataTable d_menus;
            DataTable dt_b;
            DataTable dt_s;
            //Menu
            d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");


            //抓取產品資料
            dt = DB.Prod_List("", "sort", "Y", "", cateb_id, cates_id);
            dt_b = DB.Prod_CateB_List("", "sort", "Y", "");
            dt_s = DB.Prod_CateS_List("", "sort", "Y", "", "");

            //設定傳值
            ViewData["d_prod"] = dt;
            ViewData["dt_b"] = dt_b;
            ViewData["dt_s"] = dt_s;
            ViewData["page"] = page;
            ViewData["d_menub"] = d_menub;
            ViewData["d_menus"] = d_menus;

            return View();
        }

        // 內容
        public ActionResult ProdData(string prod_id = "")
        {
            //定義變數
            DataTable dt;
            //變數設定 
            DataTable d_menub;
            DataTable d_menus;
            DataTable dt_b;
            DataTable dt_s;
            //Menu
            d_menub = DB.Prod_CateB_List("", "sort", "Y", "");
            d_menus = DB.Prod_CateS_List("", "sort", "Y", "", "");
            dt = DB.Prod_List(prod_id, "", "Y", "", "", "");
            dt_b = DB.Prod_CateB_List("", "sort", "Y", "");
            dt_s = DB.Prod_CateS_List("", "sort", "Y", "", "");

            if (dt.Rows.Count > 0)
            {
                ViewData["dt"] = dt;
                ViewData["d_menub"] = d_menub;
                ViewData["d_menus"] = d_menus;
                ViewData["dt_b"] = dt_b;
                ViewData["dt_s"] = dt_s;

                return View("Content");
            }
            else
            {
                return RedirectToAction("index");
            }
        }
    }
}