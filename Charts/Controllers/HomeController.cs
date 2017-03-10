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
using FastMember;
using System.Collections;
using Newtonsoft.Json;

namespace Charts.Controllers
{
    public class HomeController : Controller
    {
        private ChartDBEntities db = new ChartDBEntities();

        public class Data
        {
            public int OutledID;
            public string KPI;
            public JSON_Values values;
        }

        [Serializable]
        public class JSON_Values
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

        public List<List<Data>> charts_values(List<Chart> data)
        {
            List<List<Data>> list = new List<List<Data>>();
            List<JSON_Values> values = new List<JSON_Values>();
            JavaScriptSerializer js = new JavaScriptSerializer();
            foreach(var c in data)
            {
                list.Add(new List<Data>() { new Controllers.HomeController.Data() { OutledID = c.OutletID, KPI = c.KPI, values = js.Deserialize<JSON_Values>(c.values) } });
            }
            return list;
        }

        public ActionResult Charts()
        {

            //ArrayList header = new ArrayList { "Current value", "value" };
            //ArrayList data1 = new ArrayList { "OutletID 1", 30 };
            //ArrayList data2 = new ArrayList { "OutletID 2", 146 };
            //ArrayList data3 = new ArrayList { "OutletID 3", 13 };
            //ArrayList data = new ArrayList { header, data1, data2, data3 };

            //var sdata = JsonConvert.SerializeObject(data, Formatting.None);
            //ViewBag.Data1 = new HtmlString(sdata);
            var dat = db.Charts.ToList();
            PieChart(dat);
            //BarChart(dat);

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public void PieChart(List<Chart> dat)
        {
            var data = charts_values(dat);
            ArrayList d = new ArrayList { new ArrayList { "OutledID", "current value" } };
            foreach (var x in data)
            {
                foreach (var y in x)
                {
                    ViewBag.PieTitle = y.KPI;
                    d.Add(new ArrayList { "OutledID " + y.OutledID, y.values.current_value });
                }
            }
            var sdata = JsonConvert.SerializeObject(d, Formatting.None);
            ViewBag.PieData = new HtmlString(sdata);
        }

        public void BarChart(List<Chart> dat)
        {
            var data = charts_values(dat);
            ArrayList d = new ArrayList { new ArrayList { "OutledID", "current value" } };

        }
    }
}