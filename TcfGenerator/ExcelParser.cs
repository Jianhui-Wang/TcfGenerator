using System;
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

        private string ReadData(Excel.Worksheet xlWorkSheet, string column, int row)
        {
            Excel.Range r = xlWorkSheet.Cells[row, column];
            if (r.Value2 == null)
                return string.Empty;
            else
                return r.Value2.ToString();
        }

        public ExcelParser(string filename, MappingRules mr)
        {
            this.mr = mr;
            if (mr == null)
                throw new InvalidDataException("Mapping Rules is null!");
            if (!File.Exists(filename))
                throw new FileNotFoundException("Excel File " + filename + " Not Exist!");
            excelFile = filename;
            sheetName = @"Sheet1";
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

        private Assembly[] plugins =
        {
            Assembly.LoadFrom(Directory.GetCurrentDirectory() + @"\..\..\..\Reference\Keysight.S8901A.Measurement.TapSteps.dll"),
            Assembly.LoadFrom(Directory.GetCurrentDirectory() + @"\..\..\..\Reference\Keysight.S8901A.Measurement.CommonTapSteps.dll")
        };

        private Type PluginGetType(string typeName)
        {
            Type t = null;
            foreach (var plugin in plugins)
            {
                t = plugin.GetType(typeName);
                if (t != null)
                {
                    break;
                }
            }
            return t;
        }

        //public void ParseExcel(List<string> s)
        //{
        //    SessionLogs.Load(Directory.GetCurrentDirectory() + "\\Session_log.txt");

        //    var plugin = Assembly.LoadFrom(@"C:\Drive_E\PA_Learning\PilotProject\TcfGenerator\ParseTapStepDll\Keysight.S8901A.Measurement.TapSteps.dll");

        //    TestPlan tp = new TestPlan();

        //    Type t = plugin.GetType("Keysight.S8901A.Measurement.TapSteps.SelectTechnology");
        //    ITestStep ts = (ITestStep)Activator.CreateInstance(t, args:s);
        //    tp.Steps.Add(ts);

        //    t = plugin.GetType("Keysight.S8901A.Measurement.TapSteps.Source_Analyzer_Setup");
        //    ts = (ITestStep)Activator.CreateInstance(t);
        //    tp.Steps.Add(ts);

        //    tp.Save(Directory.GetCurrentDirectory() + "\\1.tapplan");
        //}

        private void SetProperty(Type t, ITestStep ts, string propertyName, string propertyType, string propertyValue)
        {
            PropertyInfo pInfo = t.GetProperty(propertyName);

            if (propertyType == "System.Double")
            {
                double result;
                if (Double.TryParse(propertyValue, out result))
                {
                    pInfo.SetValue(ts, result, null);
                }
            }

        }

        internal class InternalTestStep
        {
            internal class InternalProperty
            {
                public string name { get; set; }
                public string type { get; set; }
                public string value { get; set; }
            }

            public Type t { get; set; }
            public ITestStep ts { get; set; }
            public List<InternalProperty> props { get; set; }

            public InternalTestStep()
            {
                t = null;
                ts = null;
                props = new List<InternalProperty>();
            }
        }


        private bool TestStepExist(List<InternalTestStep> its, string testStepName)
        {
            bool ret = false;
            foreach (var ts in its)
            {
                if (ts.t.Name == testStepName) return true;
            }
            return ret;
        }

        private void CombineTestSteps(List<Tuple<Type, ITestStep, string, string, string>> tss, Tuple<Type, ITestStep> measTestStep)
        {
            Tuple<Type, ITestStep> mt = measTestStep;
            List<InternalTestStep> its = new List<InternalTestStep>();

            foreach (var i in tss)
            {
                if (!TestStepExist(its, i.Item1.Name))
                {
                    InternalTestStep it = new InternalTestStep();
                    it.t = i.Item1;
                    it.ts = i.Item2;
                    InternalTestStep.InternalProperty p =
                        new InternalTestStep.InternalProperty() { name = i.Item3, type = i.Item4, value = i.Item5 };
                    it.props.Add(p);
                }
                else
                {

                }
            }
        }


        public void ParseExcel(List<string> technologies)
        {
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;

            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Open(excelFile);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.Item[sheetName];
            var testName_Column = mr.testNameColumn; // TODO: testName Column need to be inclued in MappingRules
            int rowStart, rowEnd;
            bool result;

            if (!(result = Int32.TryParse(mr.rowStart, out rowStart)))
            {
                throw new InvalidCastException("rowStart is not an integer!");
            }

            if (!(result = Int32.TryParse(mr.rowEnd, out rowEnd)))
            {
                throw new InvalidCastException("rowEnd is not an integer!");
            }

            TestPlan tp = new TestPlan();

            for (int i = rowStart; i <= rowEnd; i++)
            {
                int row = i;
                string tn = ReadData(xlWorkSheet, testName_Column, row);

                string tapStep;
                TestItem_Enum tapTestItem;
                bool find = FindMatchingRule(tn, out tapStep, out tapTestItem);

                if (!find) continue;

                List<Tuple<Type, ITestStep, string, string, string>> tss = new List<Tuple<Type, ITestStep, string, string, string>>();

                foreach (var r in mr.settingMappings)
                {
                    string v = ReadData(xlWorkSheet, r.ExcelColumn, row);
                    Type t = PluginGetType("Keysight.S8901A.Measurement.TapSteps." + r.TestStep);
                    ITestStep ts;
                    if (t == typeof(SelectTechnology))
                    {
                        ts = (ITestStep)Activator.CreateInstance(t, args: technologies);
                    }
                    else
                    {
                        ts = (ITestStep)Activator.CreateInstance(t);
                    }

                    tss.Add(new Tuple<Type, ITestStep, string, string, string>(t, ts, r.Property, r.PropertyType, v));
                }

                Type tMeas = PluginGetType("Keysight.S8901A.Measurement.TapSteps." + tapStep);
                var tsMeas = (ITestStep)Activator.CreateInstance(tMeas);

                CombineTestSteps(tss, new Tuple<Type, ITestStep>(tMeas, tsMeas));

                foreach (var ts in tss)
                {
                }
            }

            tp.Save(@"c:\temp\1.tapplan");
        }
    }
}
