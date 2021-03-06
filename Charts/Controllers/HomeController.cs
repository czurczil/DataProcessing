﻿using System;
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
                catch (NullReferenceException ex)
                {
                    ViewBag.Error = ex.Message + " Nie zapisano, uzupełnij brakujące dane:";
                }
            }
            db.Dispose();

            //delete file from server
            if (System.IO.File.Exists(fullpath))
            {
                System.IO.File.Delete(fullpath);
            }

            return View(dt);
        }

        public List<Data> charts_values(List<Chart> data)
        {
            List<Data> list = new List<Data>();
            List<JSON_Values> values = new List<JSON_Values>();
            JavaScriptSerializer js = new JavaScriptSerializer();
            foreach (var c in data)
            {
                list.Add(new Controllers.HomeController.Data() { OutledID = c.OutletID, KPI = c.KPI, values = js.Deserialize<JSON_Values>(c.values) });
            }
            return list;
        }

        public ActionResult Charts()
        {
            var dat = db.Charts.ToList();

            PieChart(dat);

            BarChart(dat);

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


            ViewBag.PieTitle = data[0].KPI;


            ArrayList d = new ArrayList { new ArrayList { "OutledID", "current value" } };
            foreach (var x in data)
            {
                d.Add(new ArrayList { "OutledID " + x.OutledID, x.values.current_value });
            }


            var sdata = JsonConvert.SerializeObject(d, Formatting.None);
            ViewBag.PieData = new HtmlString(sdata);
        }

        public void BarChart(List<Chart> dat)
        {
            var data = charts_values(dat);

            ArrayList titles = new ArrayList();
            ArrayList ranges = new ArrayList();
            ArrayList min_max = new ArrayList();
            ViewBag.Count = dat.Count;

            ArrayList d = new ArrayList();
            for(int i = 0; i < dat.Count; i++)
            {
                //adding thresholds with min and max value
                var rang = data[i].values.thresholds;
                if(!rang.Contains(data[i].values.min_value)) rang.Insert(0, data[i].values.min_value);
                if(!rang.Contains(data[i].values.max_value)) rang.Insert(0, data[i].values.max_value);
                rang.Sort();
                ranges.AddRange(new ArrayList() { rang });

                //adding chart titles                                                                           // every index of all of arrays contains data for one chart
                titles.Add(data[i].KPI);

                //adding min and max values
                min_max.Add(new ArrayList() { data[i].values.min_value, data[i].values.max_value });

                //adding ids 
                d.Add(new ArrayList { "OutledID " + data[i].OutledID, data[i].values.current_value });
            }
  
            //converting datas to json 
            var sdata = JsonConvert.SerializeObject(d, Formatting.None);
            ViewBag.BarData = new HtmlString(sdata);

            var stitles = JsonConvert.SerializeObject(titles, Formatting.None);
            ViewBag.BarTitles = new HtmlString(stitles);

            var sranges= JsonConvert.SerializeObject(ranges, Formatting.None);
            ViewBag.Ranges = sranges;

            var smin_max = JsonConvert.SerializeObject(min_max, Formatting.None);
            ViewBag.MinMax = smin_max;
        }
    }
}