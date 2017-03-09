using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Charts.Models;
using System.IO;
using NPOI.SS.UserModel;
using System.Data;
using NPOI.XSSF.UserModel;
using System.Web.Script.Serialization;
using System.Data.Entity.Migrations;

namespace Charts.Controllers
{
    public class HomeController : Controller
    {
        private ChartDBEntities db = new ChartDBEntities();

        [Serializable]
        private class JSON_Values
        {
            public int current_value;
            public int min_value;
            public int max_value;
            public List<int> thresholds;
        }

        // GET: Home/Index
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        // POST: Home/Index
        [HttpPost]
        public ActionResult Index(HttpPostedFileBase excel)
        {
            //initialize workbook
            XSSFWorkbook workbook;
            //save file in project directory
            string path = HttpContext.Server.MapPath("~/App_Data");
            string fullpath = Path.Combine(path, excel.FileName);
            excel.SaveAs(fullpath);

            using (FileStream fs = new FileStream(fullpath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(fs);
            }

            ISheet sheet = workbook.GetSheetAt(0);

            //show data from excel in DataTable
            DataTable dt = new DataTable();

            for (int i = sheet.FirstRowNum; i <= sheet.LastRowNum; i++)
            {
                DataRow dr = dt.NewRow();
                var row = sheet.GetRow(i);
                int x = 0;

                for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                {
                    if (i == sheet.FirstRowNum)
                    {
                        dt.Columns.Add(row.GetCell(j).ToString());
                    }
                    else
                    {
                        dr[x] = row.GetCell(j).ToString();
                        x++;
                    } 
                }
                if (i == sheet.FirstRowNum) continue;
                dt.Rows.Add(dr);
            }

            JSON_Values jv = new JSON_Values();

            for (int i = sheet.FirstRowNum + 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);

                try
                {
                    //convert data to json
                    jv.current_value = Convert.ToInt32(row.GetCell(row.FirstCellNum + 2).ToString());
                    jv.min_value = Convert.ToInt32(row.GetCell(row.FirstCellNum + 3).ToString());
                    jv.max_value = Convert.ToInt32(row.GetCell(row.FirstCellNum + 4).ToString());

                    jv.thresholds = new List<int>();
                    for (int j = row.FirstCellNum + 5; j < row.LastCellNum; j++)
                    {
                        jv.thresholds.Add(Convert.ToInt32(row.GetCell(row.FirstCellNum + j).ToString()));
                    }
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    string jsonData = js.Serialize(jv);

                    //save to database
                    Chart chart = new Chart()
                    {
                        OutletID = Convert.ToInt32(row.GetCell(row.FirstCellNum).ToString()),
                        KPI = row.GetCell(row.FirstCellNum + 1).ToString(),
                        values = jsonData
                    };

                    db.Charts.AddOrUpdate(chart);
                    db.SaveChanges();
                }
                catch(NullReferenceException ex)
                {
                    ViewBag.Error = ex.Message + " Nie zapisano, uzupełnij brakujące dane:";
                }
            }
            db.Dispose();

            if(System.IO.File.Exists(fullpath))
            {
                System.IO.File.Delete(fullpath);
            }

            return View(dt);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}