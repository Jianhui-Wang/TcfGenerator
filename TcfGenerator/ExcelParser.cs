using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using Keysight.S8901A.Common;
using Keysight.S8901A.Measurement.TapSteps;
using Keysight.S8901A.Measurement.TapInstruments;
using Keysight.Tap;
using System.Reflection;

namespace TcfGenerator
{
    internal class InternalTestStep
    {
        internal class InternalProperty
        {
            public string name { get; set; }
            public string type { get; set; }
            public string value { get; set; }
            public List<ValueMapping> valueMappings { get; set; }
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

        private void SetProperty(ITestStep ts, PropertyInfo pInfo, string propertyName, string propertyType, string propertyValue)
        {
            if (propertyType == "System.Double")
            {
                double result;
                if (Double.TryParse(propertyValue, out result))
                {
                    pInfo.SetValue(ts, result, null);
                }
            }

            else if (propertyType == "System.Int32")
            {
                int result;
                if (Int32.TryParse(propertyValue, out result))
                {
                    pInfo.SetValue(ts, result, null);
                }
            }

            else if (propertyType == "System.String")
            {
                pInfo.SetValue(ts, propertyValue, null);
            }

            else if (propertyType == "System.Boolean")
            {
                bool result;
                if (bool.TryParse(propertyValue, out result))
                {
                    pInfo.SetValue(ts, result, null);
                }
            }

            else
            {
                throw new InvalidDataException
                    ("Property[" + propertyName + "] type " + propertyType + "is not supported!");
            }
        }

        private InternalTestStep FindTestStep(List<InternalTestStep> its, string testStepName)
        {
            foreach (var ts in its)
            {
                if (ts.t.Name == testStepName) return ts;
            }
            return null;
        }

        private List<InternalTestStep> CombineTestSteps(List<Tuple<Type, ITestStep, string, string, string, List<ValueMapping>>> tss, Tuple<Type, ITestStep> measTestStep)
        {
            List<InternalTestStep> its = new List<InternalTestStep>();

            // Init the measurement test step
            InternalTestStep measTS = new InternalTestStep();
            measTS.t = measTestStep.Item1;
            measTS.ts = measTestStep.Item2;

            // Process the property mapping list
            foreach (var i in tss)
            {
                // If the test step is the same with the measurement test step
                // Add the property into measurement test step
                if (measTS.t.Name == i.Item1.Name)
                {
                    InternalTestStep.InternalProperty p =
                        new InternalTestStep.InternalProperty() { name = i.Item3, type = i.Item4, value = i.Item5, valueMappings = i.Item6 };
                    measTS.props.Add(p);
                }

                // If the test step has not been added into the list, add it
                else if (FindTestStep(its, i.Item1.Name) == null)
                {
                    InternalTestStep it = new InternalTestStep();
                    it.t = i.Item1;
                    it.ts = i.Item2;
                    InternalTestStep.InternalProperty p =
                        new InternalTestStep.InternalProperty() { name = i.Item3, type = i.Item4, value = i.Item5, valueMappings = i.Item6 };
                    it.props.Add(p);
                    its.Add(it);
                }

                // If the test step exist in the list, add the property into the test step
                else
                {
                    InternalTestStep it = FindTestStep(its, i.Item1.Name);
                    InternalTestStep.InternalProperty p =
                        new InternalTestStep.InternalProperty() { name = i.Item3, type = i.Item4, value = i.Item5, valueMappings = i.Item6 };
                    it.props.Add(p);
                }
            }

            // Add the measurement test step at the end of the list
            its.Add(measTS);

            return its;
        }

        private List<ITestStep> GenTestSteps(List<InternalTestStep> testStepList)
        {
            List<ITestStep> testSteps = new List<ITestStep>();
            foreach (var its in testStepList)
            {
                foreach (var prop in its.props)
                {
                    PropertyInfo pInfo = its.t.GetProperty(prop.name);
                    if (prop.valueMappings.Count > 0)
                    {
                        string realValue = (from v in prop.valueMappings
                                            where v.ExcelValue == prop.value
                                            select v.TapValue).Single();
                        SetProperty(its.ts, pInfo, prop.name, prop.type, realValue);
                    }
                    else
                    {
                        SetProperty(its.ts, pInfo, prop.name, prop.type, prop.value);
                    }
                }
                testSteps.Add(its.ts);
            }
            return testSteps;
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

                List<Tuple<Type, ITestStep, string, string, string, List<ValueMapping>>> tss =
                    new List<Tuple<Type, ITestStep, string, string, string, List<ValueMapping>>>();

                // TODO: Load Instrument Setting from file
                PA_Instrument pa_inst = new PA_Instrument();
                pa_inst.LoadHwConfigFile(@"C:\Program Files\Keysight\Power Amplifier Solution\Site2\SiteInstrument.xml");
                pa_inst.ParseHwConfigFile();

                foreach (var r in mr.settingMappings)
                {
                    string v = ReadData(xlWorkSheet, r.ExcelColumn, row);
                    Type t = PluginGetType("Keysight.S8901A.Measurement.TapSteps." + r.TestStep);
                    ITestStep ts;
                    if (t.Name == "SelectTechnology")
                    {
                        ts = (ITestStep)Activator.CreateInstance(t, args: technologies);
                    }
                    else
                    {
                        ts = (ITestStep)Activator.CreateInstance(t);
                    }

                    tss.Add(new Tuple<Type, ITestStep, string, string, string, List<ValueMapping>>
                        (t, ts, r.Property, r.PropertyType, v, r.ValMapping.ToList()));
                }

                Type tMeas = PluginGetType("Keysight.S8901A.Measurement.TapSteps." + tapStep);
                var tsMeas = (ITestStep)Activator.CreateInstance(tMeas);

                var internalTestStepList = CombineTestSteps(tss, new Tuple<Type, ITestStep>(tMeas, tsMeas));
                var testStepList = GenTestSteps(internalTestStepList);

                // Generate TestSteps
                foreach (var ts in testStepList)
                {
                    tp.ChildTestSteps.Add(ts);
                }
            }

            tp.Save(@"c:\temp\1.tapplan");
        }
    }
}
