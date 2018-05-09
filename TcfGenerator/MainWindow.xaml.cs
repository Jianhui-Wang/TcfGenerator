using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;
using Keysight.S8901A.Common;
using System.ComponentModel;
using System.Configuration;

namespace TcfGenerator
{
    public partial class MainWindow : Window
    {
        #region Object bind with UI
        public ObservableCollection<TestMapping> TestMappings { get; set; }
        public ObservableCollection<SettingMapping> mrsettingMappings { get; set; }
        public ObservableCollection<Tuple<string /*Name*/, string /*DisplayName*/>> TestStepList { get; set; }
        public ObservableCollection<Tuple<string /*Name*/, string /*DisplayName*/, string /*Type*/>> PropList { get; set; }
        public ObservableCollection<Tuple<string /*EnumValueName*/, int /*EnumValue*/>> TestItemList { get; set; }
        #endregion

        private ObservableCollection<ValueMapping> ValueMappings { get; set; }
        private XmlNode Node_TestSteps;
        private XmlNode Node_TestItems;
        private int teststep_idx; /* Selected item index of the teststep ComboBox */
        private int testmapping_rule_idx; /* the top cursor of the TestMappings array */
        private int settingmapping_rule_idx; /* the top cursor of the SettingMappings array */
        public MappingRules mr;

        public MainWindow()
        {
            InitializeComponent();

            mr = new MappingRules();

            TestStepList = new ObservableCollection<Tuple<string, string>>();
            PropList = new ObservableCollection<Tuple<string, string, string>>();
            TestItemList = new ObservableCollection<Tuple<string, int>>();
            settingmapping_rule_idx = 0;
            testmapping_rule_idx = 0;

            var XmlFile = ConfigurationManager.AppSettings["TapStepFieldXml"];
            InitializeTestList(XmlFile);

            settingMapping.DataContext = mr.settingMappings;
            testMapping.DataContext = mr.testMappings;
            testItem.DataContext = this;
            testStep1.DataContext = this;
            testStep2.DataContext = this;
            Property.DataContext = this;
            excelTestColumn.DataContext = mr;
            excelStartRow.DataContext = mr;
            excelEndRow.DataContext = mr;
            excelNote.DataContext = mr;
            excelNote1.DataContext = mr;
            excelFilename.DataContext = mr;
            highLimitCol.DataContext = mr;
            lowLimitCol.DataContext = mr;
        }


        private void InitializeTestList(string xmlFile)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlFile);

            Node_TestSteps = xml.SelectSingleNode("Root").ChildNodes[0];
            Node_TestItems = xml.SelectSingleNode("Root").ChildNodes[1];

            foreach (var teststep in Node_TestSteps.ChildNodes)
            {
                string n = (teststep as XmlNode).Attributes["Name"].Value;
                string d = (teststep as XmlNode).Attributes["DisplayName"].Value;
                TestStepList.Add(new Tuple<string, string>(n, d));
            }

            foreach (var testitem in Node_TestItems.ChildNodes)
            {
                string n = (testitem as XmlNode).Attributes["Name"].Value;
                int v = Convert.ToInt16((testitem as XmlNode).Attributes["Value"].Value);
                TestItemList.Add(new Tuple<string, int>(n, v));
            }
        }

        private void TestStepChanged(object sender, SelectionChangedEventArgs e)
        {
            teststep_idx = (sender as ComboBox).SelectedIndex;
            if (teststep_idx == -1)
                teststep_idx = 0;

            var teststep = Node_TestSteps.ChildNodes[teststep_idx];

            PropList.Clear();

            foreach (var property in teststep.ChildNodes)
            {
                string n = (property as XmlNode).Attributes["Name"].Value;
                string d = (property as XmlNode).Attributes["DisplayName"].Value;
                string t = (property as XmlNode).Attributes["Type"].Value;
                PropList.Add(new Tuple<string, string, string>(n, d, t));
            }
        }

        private void Add_SettingMapping(object sender, RoutedEventArgs e)
        {
            SettingMapping m = new SettingMapping();
            int teststep_idx = testStep2.SelectedIndex;
            int property_idx = Property.SelectedIndex;

            if (teststep_idx != -1 && property_idx != -1)
            {
                m.Serial = ++settingmapping_rule_idx;
                m.ExcelColumn = excelColumn.Text;
                m.TestStep = TestStepList[teststep_idx].Item1;
                m.TestStep_DispName = TestStepList[teststep_idx].Item2;
                m.Property = PropList[property_idx].Item1;
                m.Property_DispName = PropList[property_idx].Item2;
                m.PropertyType = PropList[property_idx].Item3;
                m.ValMapping = ValueMappings;
                mr.settingMappings.Add(m);

                settingMapping.Items.Refresh();
            }
        }

        private void Delete_SettingMapping(object sender, RoutedEventArgs e)
        {
            int index = settingMapping.SelectedIndex;
            if (index == -1) return;
            mr.settingMappings.RemoveAt(index);
            settingmapping_rule_idx--;
            for (int i = index; i < mr.settingMappings.Count; i++)
            {
                mr.settingMappings[i].Serial = i + 1;
            }
            settingMapping.Items.Refresh();
        }

        private void PropertyChanged(object sender, SelectionChangedEventArgs e)
        {
            if (teststep_idx == -1) return;
            int property_idx = (sender as ComboBox).SelectedIndex;
            if (property_idx == -1) return;

            var teststep = Node_TestSteps.ChildNodes[teststep_idx];

            ValueMappings = new ObservableCollection<ValueMapping>();

            foreach (XmlNode p in teststep.ChildNodes)
            {
                if (PropList[property_idx].Item1 == p.Attributes["Name"].Value)
                {
                    var typeAttr = p.Attributes["Type"];
                    var availValuesAttr = p.Attributes["AvailValues"];

                    if ((typeAttr != null && typeAttr.Value == "System.String" && availValuesAttr != null && availValuesAttr.Value == "True")
                        || typeAttr != null && typeAttr.Value.StartsWith("Enumeration"))
                    {
                        var values = p.ChildNodes;

                        foreach (XmlNode v in values)
                        {
                            ValueMappings.Add(new TcfGenerator.ValueMapping() { TapValue = v.InnerText, ExcelValue = "" });
                        }
                    }
                }
            }

        }

        private void Save_MappingRules(object sender, RoutedEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MappingRules));
            SaveFileDialog dialog = new SaveFileDialog();
            string filename = "";

            if (dialog.ShowDialog() == true)
            {
                filename = dialog.FileName;
                FileStream xmlStream = new FileStream(filename, FileMode.Create);
                serializer.Serialize(xmlStream, mr);
                xmlStream.Close();
            }
        }

        private void Load_MappingRules(object sender, RoutedEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MappingRules));
            OpenFileDialog dialog = new OpenFileDialog();
            string filename = "";

            if (dialog.ShowDialog() == true)
            {
                filename = dialog.FileName;

                FileStream xmlStream = new FileStream(filename, FileMode.Open);
                object o = serializer.Deserialize(xmlStream);
                mr = o as MappingRules;
                xmlStream.Close();

                settingMapping.DataContext = mr.settingMappings;
                settingmapping_rule_idx = mr.settingMappings.Count;
                testMapping.DataContext = mr.testMappings;
                testmapping_rule_idx = mr.testMappings.Count;
                testItem.DataContext = this;
                testStep1.DataContext = this;
                testStep2.DataContext = this;
                Property.DataContext = this;
                excelTestColumn.DataContext = mr;
                excelStartRow.DataContext = mr;
                excelEndRow.DataContext = mr;
                excelNote.DataContext = mr;
                excelNote1.DataContext = mr;
                excelFilename.DataContext = mr;
                highLimitCol.DataContext = mr;
                lowLimitCol.DataContext = mr;
            }
        }

        private void Add_TestMapping(object sender, RoutedEventArgs e)
        {
            TestMapping m = new TestMapping();
            int teststep_idx = testStep1.SelectedIndex;

            if (teststep_idx != -1)
            {
                m.Serial = ++testmapping_rule_idx;
                m.TestStep = TestStepList[teststep_idx].Item1;
                m.TestStep_DispName = TestStepList[teststep_idx].Item2;
                m.MatchRule = testMappingRule.SelectedValue.ToString().Split(':')[1];
                m.Keyword = keyword.Text;
                m.TestItem = (TestItem_Enum)(testItem.SelectedValue);
                mr.testMappings.Add(m);

                testMapping.Items.Refresh();
            }

        }

        private void Delete_TestMapping(object sender, RoutedEventArgs e)
        {
            int index = testMapping.SelectedIndex;
            if (index == -1) return;
            mr.testMappings.RemoveAt(index);
            testmapping_rule_idx--;
            for (int i = index; i < mr.testMappings.Count; i++)
            {
                mr.testMappings[i].Serial = i + 1;
            }
            testMapping.Items.Refresh();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ExcelParser ep = new ExcelParser(mr);
            ep.ParseExcel();
            MessageBox.Show("TAP Plan generated!");
        }

        private void ChooseExcelFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                mr.excelFile = dialog.FileName;
            }
        }
    }

    [Serializable]
    public class SettingMapping
    {
        public int Serial { get; set; }
        public string ExcelColumn { get; set; }
        public string TestStep { get; set; }
        public string TestStep_DispName { get; set; }
        public string Property { get; set; }
        public string Property_DispName { get; set; }
        public string PropertyType { get; set; }
        public ObservableCollection<ValueMapping> ValMapping { get; set; }
    }

    [Serializable]
    public class TestMapping
    {
        public int Serial { get; set; }
        public string MatchRule { get; set; }
        public string Keyword { get; set; }
        public string TestStep { get; set; }
        public string TestStep_DispName { get; set; }
        public TestItem_Enum TestItem { get; set; }
    }

    [Serializable]
    public class MappingRules : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public ObservableCollection<TestMapping> testMappings { get; set; }
        public ObservableCollection<SettingMapping> settingMappings { get; set; }

        private string _excelFile;
        public string excelFile
        {
            get { return _excelFile; }
            set
            {
                _excelFile = value;
                OnPropertyChanged("excelFile");
            }
        }

        private string _excelSheet;
        public string excelSheet
        {
            get { return _excelSheet; }
            set
            {
                _excelSheet = value;
                OnPropertyChanged("excelSheet");
            }
        }


        private string _rowStart;
        public string rowStart
        {
            get { return _rowStart; }
            set
            {
                _rowStart = value;
                OnPropertyChanged("rowStart");
            }
        }

        private string _rowEnd;
        public string rowEnd
        {
            get { return _rowEnd; }
            set
            {
                _rowEnd = value;
                OnPropertyChanged(rowEnd);
            }
        }

        private string _testNameColumn;
        public string testNameColumn
        {
            get { return _testNameColumn; }
            set
            {
                _testNameColumn = value;
                OnPropertyChanged("testNameColumn");
            }
        }

        private string _lowLimitColumn;
        public string lowLimitColumn
        {
            get { return _lowLimitColumn; }
            set
            {
                _lowLimitColumn = value;
                OnPropertyChanged("lowLimitColumn");
            }
        }

        private string _highLimitColumn;
        public string highLimitColumn
        {
            get { return _highLimitColumn; }
            set
            {
                _highLimitColumn = value;
                OnPropertyChanged("highLimitColumn");
            }
        }

        public MappingRules()
        {
            testMappings = new ObservableCollection<TestMapping>();
            settingMappings = new ObservableCollection<SettingMapping>();
        }
        public MappingRules(ObservableCollection<TestMapping> tm, ObservableCollection<SettingMapping> sm)
        {
            testMappings = tm;
            settingMappings = sm;
        }
    }

    public class ValueMapping : IEquatable<ValueMapping>
    {
        public string ExcelValue { get; set; }
        public string TapValue { get; set; }
        public bool Equals(ValueMapping vm)
        {
            if (ExcelValue != vm.ExcelValue) return false;
            if (TapValue != vm.TapValue) return false;
            return true;
        }
    }

}
