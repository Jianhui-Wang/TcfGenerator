﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using Keysight.S8901A.Common;
using Keysight.S8901A.Measurement.TapSteps;
using Keysight.Tap;
using System.Reflection;

namespace TcfGenerator
{
    class ExcelParser
    {
        private MappingRules mr { get; set; }
        private string excelFile { get; set; }
        private string sheetName { get; set; }
        private int rowStart { get; set; }
        private int rowEnd { get; set; }

        private string ReadData(Excel.Worksheet xlWorkSheet, string column, int row)
        {
            Excel.Range r = xlWorkSheet.Cells[row, column];
            if (r.Value2 == null)
                return string.Empty;
            else
                return r.Value2.ToString();
        }

        public ExcelParser() { }
        public ExcelParser(string filename, MappingRules mr)
        {
            this.mr = mr;
            if (mr == null)
                throw new InvalidDataException("Mapping Rules is null!");
            if (!File.Exists(filename))
                throw new FileNotFoundException("Excel File "+ filename + " Not Exist!");
            excelFile = filename;
        }

        public bool FindMatchingRule(string testname, out string tapStep, out TestItem_Enum tapTestItem)
        {
            tapStep = "None";
            tapTestItem = TestItem_Enum.POUT;
            bool found = false;
            foreach (var r in mr.testMappings)
            {
                switch (r.MatchRule)
                {
                    default:
                    case "Contains":
                        if (testname.Contains(r.Keyword))
                        {
                            tapStep = r.TestStep;
                            tapTestItem = r.TestItem;
                            found = true;
                            break;
                        }
                        break;
                }
            }

            return found;
        }


        public void ParseExcel()
        {
            var plugin = Assembly.LoadFrom(@"C:\Drive_E\PA_Learning\PilotProject\TcfGenerator\ParseTapStepDll\Keysight.S8901A.Measurement.TapSteps.dll");

            TestPlan tp = new TestPlan();

            string v = "SelectTechnology";
            Type t = plugin.GetType("Keysight.S8901A.Measurement.TapSteps.SelectTechnology");
            TestStep ts = (TestStep)Activator.CreateInstance(t);
            tp.Steps.Add(ts);
        }

        public void ParseExcel2()
        {
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;

            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Open(excelFile);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.Item[sheetName];
            string testName_Column = "A"; // TODO: testName Column need to be inclued in MappingRules

            for (int i = rowStart; i <= rowEnd; i++)
            {
                int row = i;
                string tn = ReadData(xlWorkSheet, testName_Column, row);

                string tapStep;
                TestItem_Enum tapTestItem;
                bool find = FindMatchingRule(tn, out tapStep, out tapTestItem);

                if (!find) continue;

                Console.WriteLine("Row[{3}] Test[{0}] => TapStep [{1}], TestItem [{2}]", tn, tapStep, tapTestItem, row);

                TestPlan tp = new TestPlan();

                foreach (var r in mr.settingMappings)
                {
                    string v = ReadData(xlWorkSheet, r.ExcelColumn, row);
                    Type t = Type.GetType("Keysight.S8901A.Measurement.TapSteps." + r.TestStep);
                    TestStep ts = (TestStep)Activator.CreateInstance(t);
                    tp.Steps.Add(ts);
                }
            }

        }
    }
}