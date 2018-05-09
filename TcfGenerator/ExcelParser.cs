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
using System.Configuration;

namespace TcfGenerator
{
    internal class InternalTestPoint
    {
        public TestItem_Enum testitem;
        public string limitHigh;
        public string limitLow;
    }

    internal class InternalProperty : IEquatable<InternalProperty>
    {
        public string name { get; set; }
        public string type { get; set; }
        public string value { get; set; }
        public List<ValueMapping> valueMappings { get; set; }
        public bool Equals(InternalProperty ip)
        {
            if (name != ip.name) return false;
            if (type != ip.type) return false;
            if (value != ip.value) return false;
            if (!valueMappings.SequenceEqual(ip.valueMappings)) return false;
            return true;
        }
    }

    internal class InternalTestStep : IEquatable<InternalTestStep>
    {
        public Type t { get; set; }
        public ITestStep ts { get; set; }
        public List<InternalProperty> props { get; set; }

        public InternalTestStep()
        {
            t = null;
            ts = null;
            props = new List<InternalProperty>();
        }

        public bool Equals(InternalTestStep its)
        {
            if (t.FullName != its.t.FullName) return false;
            if (ts.Name != its.ts.Name) return false;
            if (!props.SequenceEqual(its.props)) return false;
            return true;
        }
    }

    internal class InternalMeasurement : InternalTestStep
    {
        public List<InternalTestPoint> testpoints;

        public InternalMeasurement() : base()
        {
            testpoints = new List<InternalTestPoint>();
        }
    }

    internal class InternalTestEntry
    {
        public List<InternalTestStep> test_conditions;
        public List<InternalMeasurement> measurements;
        public InternalTestEntry()
        {
            test_conditions = new List<InternalTestStep>();
            measurements = new List<InternalMeasurement>();
        }
    }

    class ExcelParser
    {
        static Dictionary<TestItem_Enum, Tuple<string, string, string, string>> supportedTestPoints =
            new Dictionary<TestItem_Enum, Tuple<string, string, string, string>>
            {
                #region PIN
                { TestItem_Enum.PIN,  new Tuple<string, string, string, string>(
                    "System.Double",
                    "meas_pin",
                    "pin_low_limit",
                    "pin_high_limit")
                },
                #endregion

                #region POUT
                { TestItem_Enum.POUT, new Tuple<string, string, string, string>(
                    "System.Double",
                    null,
                    "pout_low_limit",
                    "pout_high_limit")
                },
                #endregion

                #region PA_GAIN
                { TestItem_Enum.PA_GAIN, new Tuple<string, string, string, string>(
                    "System.Double",
                    "meas_gain",
                    "gain_low_limit",
                    "gain_high_limit")
                },
                #endregion

                #region ICC
                { TestItem_Enum.ICC, new Tuple<string, string, string, string>(
                    "System.Double",
                    "meas_icc",
                    "icc_low_limit",
                    "icc_high_limit")
                },
                #endregion

                #region ACPR_H1
                { TestItem_Enum.ACPR_H1, new Tuple<string, string, string, string>(
                    "System.Double",
                    "meas_acpr_h1",
                    "acpr_h1_low_limit",
                    "acpr_h1_high_limit")
                },
                #endregion
            };

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

        public ExcelParser(MappingRules mr)
        {
            this.mr = mr;
            if (mr == null)
                throw new InvalidDataException("Mapping Rules is null!");
            if (!File.Exists(mr.excelFile))
                throw new FileNotFoundException("Excel File " + mr.excelFile + " Not Exist!");
            excelFile = mr.excelFile;
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
            Assembly.LoadFrom(ConfigurationManager.AppSettings["ReferenceDir"] + @"\Keysight.S8901A.Measurement.TapSteps.dll"),
            Assembly.LoadFrom(ConfigurationManager.AppSettings["ReferenceDir"] + @"\Keysight.S8901A.Measurement.CommonTapSteps.dll"),
            Assembly.LoadFrom(ConfigurationManager.AppSettings["ReferenceDir"] + @"\Keysight.S8901A.Common.BasePlugins.dll")
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
                // ??? Why this happen ???
                if (propertyName == "technology")
                {
                    ((SelectTechnology)ts).technology = propertyValue;
                }
                else
                {
                    pInfo.SetValue(ts, propertyValue, null);
                }
            }

            else if (propertyType == "System.Boolean")
            {
                bool result;
                if (bool.TryParse(propertyValue, out result))
                {
                    pInfo.SetValue(ts, result, null);
                }
            }

            else if (propertyType.StartsWith("Enumeration."))
            {
                Type t = PluginGetType(pInfo.PropertyType.FullName);
                object value = Enum.Parse(t, propertyValue);
                pInfo.SetValue(ts, value, null);
            }

            else
            {
                throw new InvalidDataException
                    ("Property[" + propertyName + "] type " + propertyType + "is not supported!");
            }
        }

        private List<InternalTestStep> CombineTestConditions(List<Tuple<Type, ITestStep, string, string, string, List<ValueMapping>>> tss)
        {
            List<InternalTestStep> its = new List<InternalTestStep>();

            // Process the property mapping list
            foreach (var i in tss)
            {
                InternalTestStep it;

                // If the test step has not been added into the list, add it
                if ((it = its.Find(e => e.t.Name == i.Item1.Name)) == null)
                {
                    it = new InternalTestStep();
                    it.t = i.Item1;
                    it.ts = i.Item2;
                    InternalProperty p =
                        new InternalProperty() { name = i.Item3, type = i.Item4, value = i.Item5, valueMappings = i.Item6 };
                    it.props.Add(p);
                    its.Add(it);
                }

                // If the test step exist in the list, add the property into the test step
                else
                {
                    InternalProperty p =
                        new InternalProperty() { name = i.Item3, type = i.Item4, value = i.Item5, valueMappings = i.Item6 };
                    it.props.Add(p);
                }
            }

            return its;
        }

        private InternalTestEntry CreateTestItem(List<InternalTestStep> tcList, Tuple<Type, ITestStep, TestItem_Enum, string, string> measTestStep)
        {
            InternalMeasurement measTS = new InternalMeasurement();
            measTS.t = measTestStep.Item1;
            measTS.ts = measTestStep.Item2;
            measTS.testpoints.Add(new InternalTestPoint() { testitem = measTestStep.Item3, limitHigh = measTestStep.Item4, limitLow = measTestStep.Item5 });

            InternalTestEntry it = new InternalTestEntry();
            it.test_conditions = tcList;
            it.measurements.Add(measTS);
            return it;
        }

        private void Optimize(List<InternalTestEntry> testItemList)
        {
            foreach (var item in testItemList)
            {
                foreach (var meas in item.measurements)
                {
                    var tcs = item.test_conditions.FindAll(tc => tc.t.FullName == meas.t.FullName);
                    tcs.ForEach(tc => meas.props.AddRange(tc.props));
                }

                foreach (var meas in item.measurements)
                {
                    item.test_conditions.RemoveAll(i => i.t.FullName == meas.t.FullName);
                }
            }
        }

        private List<ITestStep> GenTestSteps(List<InternalTestEntry> testItemList)
        {
            List<ITestStep> testSteps = new List<ITestStep>();

            foreach (var its in testItemList)
            {

                // Generate Tap Step for Test Conditions
                foreach (var tc in its.test_conditions)
                {
                    foreach (var prop in tc.props)
                    {
                        PropertyInfo pInfo = tc.t.GetProperty(prop.name);
                        if (prop.valueMappings.Count > 0)
                        {
                            string realValue = (from v in prop.valueMappings
                                                where v.ExcelValue == prop.value
                                                select v.TapValue).Single();
                            SetProperty(tc.ts, pInfo, prop.name, prop.type, realValue);
                        }
                        else
                        {
                            SetProperty(tc.ts, pInfo, prop.name, prop.type, prop.value);
                        }
                    }
                    testSteps.Add(tc.ts);
                }

                // Generate Tap Step for measurements
                foreach (var meas in its.measurements)
                {
                    foreach (var prop in meas.props)
                    {
                        PropertyInfo pInfo = meas.t.GetProperty(prop.name);
                        if (prop.valueMappings.Count > 0)
                        {
                            string realValue = (from v in prop.valueMappings
                                                where v.ExcelValue == prop.value
                                                select v.TapValue).Single();
                            SetProperty(meas.ts, pInfo, prop.name, prop.type, realValue);
                        }
                        else
                        {
                            SetProperty(meas.ts, pInfo, prop.name, prop.type, prop.value);
                        }
                    }

                    foreach (var ti in meas.testpoints)
                    {
                        SetTestPoint(meas.ts, ti.testitem, ti.limitHigh, ti.limitLow);
                    }

                    testSteps.Add(meas.ts);
                }

            }
            return testSteps;
        }

        private void SetTestPoint(ITestStep measStep, TestItem_Enum testItem, string limitHigh, string limitLow)
        {
            PropertyInfo pInfo;
            double lowD, highD;
            string tpType, tpField, tpLimitLowField, tpLimitHighField;

            Tuple<string, string, string, string> tpInfo;
            bool result = supportedTestPoints.TryGetValue(testItem, out tpInfo);

            if (result)
            {
                tpType = tpInfo.Item1;
                tpField = tpInfo.Item2;
                tpLimitLowField = tpInfo.Item3;
                tpLimitHighField = tpInfo.Item4;
            }
            else
            {
                throw new Exception("Test Item not exist:" + testItem.ToString());
            }

            if (tpType == "System.Double")
            {
                if (!Double.TryParse(limitLow, out lowD))
                {
                    throw new InvalidDataException("Low Limit invalid!");
                }
                if (!Double.TryParse(limitHigh, out highD))
                {
                    throw new InvalidDataException("High Limit invalid!");
                }
            }
            else
            {
                throw new Exception("Test Point type is not supported:" + tpType);
            }

            if (tpField != null)
            {
                pInfo = measStep.GetType().GetProperty(tpField);
                SetProperty(measStep, pInfo, pInfo.Name, pInfo.PropertyType.FullName, "true");
            }
            pInfo = measStep.GetType().GetProperty(tpLimitLowField);
            SetProperty(measStep, pInfo, pInfo.Name, pInfo.PropertyType.FullName, limitLow);
            pInfo = measStep.GetType().GetProperty(tpLimitHighField);
            SetProperty(measStep, pInfo, pInfo.Name, pInfo.PropertyType.FullName, limitHigh);
        }

        public void ParseExcel()
        {
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;

            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Open(excelFile);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.Item[sheetName];
            var testName_Column = mr.testNameColumn;
            var limitLow_Column = mr.lowLimitColumn;
            var limitHigh_Column = mr.highLimitColumn;
            int rowStart, rowEnd;
            bool result;

            List<InternalTestEntry> testitem_list = new List<InternalTestEntry>();

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
                string limitLow = ReadData(xlWorkSheet, limitLow_Column, row);
                string limitHigh = ReadData(xlWorkSheet, limitHigh_Column, row);

                string tapStep;
                TestItem_Enum tapTestItem;
                bool find = FindMatchingRule(tn, out tapStep, out tapTestItem);

                if (!find) continue;

                List<Tuple<Type, ITestStep, string, string, string, List<ValueMapping>>> tss =
                    new List<Tuple<Type, ITestStep, string, string, string, List<ValueMapping>>>();

                // TODO: Load Instrument Setting from file
                PA_Instrument pa_inst = new PA_Instrument();

                pa_inst.LoadHwConfigFile(ConfigurationManager.AppSettings["SiteInstrument"]);
                pa_inst.ParseHwConfigFile();
                pa_inst.config_file = ConfigurationManager.AppSettings["IniConfig"];
                pa_inst.waveform_path = ConfigurationManager.AppSettings["WaveformPath"];

                InstrumentSettings.Current.Add(pa_inst);

                foreach (var r in mr.settingMappings)
                {
                    string v = ReadData(xlWorkSheet, r.ExcelColumn, row);
                    Type t = PluginGetType("Keysight.S8901A.Measurement.TapSteps." + r.TestStep);
                    ITestStep ts;
                    if (t.Name == "SelectTechnology")
                    {
                        ts = new SelectTechnology();
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

                var testconditions = CombineTestConditions(tss);
                var testitem = CreateTestItem(testconditions,
                    new Tuple<Type, ITestStep, TestItem_Enum, string, string>
                    (tMeas, tsMeas, tapTestItem, limitHigh, limitLow));

                var element = testitem_list.Find(e => testconditions.SequenceEqual(e.test_conditions));

                if (element == null)
                {
                    testitem_list.Add(testitem);
                }
                else
                {
                    // This should be only 1 measurement
                    var meas = testitem.measurements.Single();

                    var index = element.measurements.FindIndex(e => e.t.FullName == meas.t.FullName);
                    if (index == -1)
                    {
                        element.measurements.AddRange(testitem.measurements);
                    }
                    else
                    {
                        element.measurements[index].testpoints.AddRange(meas.testpoints);
                    }
                }
            }

            // Optimize
            Optimize(testitem_list);

            // Generate TestSteps
            var testStepList = GenTestSteps(testitem_list);
            foreach (var ts in testStepList)
            {
                tp.ChildTestSteps.Add(ts);
            }

            tp.Save(ConfigurationManager.AppSettings["TapPlanFile"]);

            xlWorkBook.Close();
        }
    }
}
