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

namespace TcfGenerator
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<SettingMapping> SettingMappings { get; set; }
        public ObservableCollection<Tuple<string,string>> TestStepList { get; set; }
        public ObservableCollection<Tuple<string,string,string>> PropList { get; set; }

        private ObservableCollection<ValueMapping> ValueMappings { get; set; }
        private XmlNode Node_TestSteps;
        private int teststep_idx;
        private int rule_idx;

        public MainWindow()
        {
            InitializeComponent();

            TestStepList = new ObservableCollection<Tuple<string,string>>();
            PropList = new ObservableCollection<Tuple<string,string,string>>();
            SettingMappings = new ObservableCollection<SettingMapping>();
            rule_idx = 0;

            InitializeTestStepList(@"c:\temp\test.xml");
            tapMapping.DataContext = SettingMappings;
            testStep.DataContext = this;
            Property.DataContext = this;
            tapTestName.DataContext = this;
        }

        private void InitializeTestStepList(string xmlFile)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlFile);

            Node_TestSteps = xml.SelectSingleNode("TestSteps");
            foreach (var teststep in Node_TestSteps.ChildNodes)
            {
                string n = (teststep as XmlNode).Attributes["Name"].Value;
                string d = (teststep as XmlNode).Attributes["DisplayName"].Value;
                TestStepList.Add(new Tuple<string, string>(n,d));
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
                PropList.Add(new Tuple<string,string,string>(n,d,t));
            }
        }

        private void Add_One(object sender, RoutedEventArgs e)
        {
            SettingMapping m = new SettingMapping();
            int teststep_idx = testStep.SelectedIndex;
            int property_idx = Property.SelectedIndex;

            if (teststep_idx != -1 && property_idx != -1)
            {
                m.Serial = ++rule_idx;
                m.ExcelColumn = excelColumn.Text;
                m.TestStep = TestStepList[teststep_idx].Item1;
                m.TestStep_DispName = TestStepList[teststep_idx].Item2 ;
                m.Property = PropList[property_idx].Item1;
                m.Property_DispName = PropList[property_idx].Item2;
                m.PropertyType = PropList[property_idx].Item3;
                m.ValMapping = ValueMappings;
                SettingMappings.Add(m);

                tapMapping.Items.Refresh();
            }
        }

        private void Delete_One(object sender, RoutedEventArgs e)
        {
            int index = tapMapping.SelectedIndex;
            if (index == -1) return;
            SettingMappings.RemoveAt(index);
            rule_idx--;
            for (int i = index; i<SettingMappings.Count; i++)
            {
                SettingMappings[i].Serial = i + 1;
            }
            tapMapping.Items.Refresh();
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

        private void Save_SettingMapping(object sender, RoutedEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SettingMapping>));
            SaveFileDialog dialog = new SaveFileDialog();
            string filename = "";

            if (dialog.ShowDialog() == true)
            {
                filename = dialog.FileName;
                FileStream xmlStream = new FileStream(filename, FileMode.Create);
                serializer.Serialize(xmlStream, SettingMappings);
                xmlStream.Close();
            }
        }

        private void Load_SettingMapping(object sender, RoutedEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SettingMapping>));
            OpenFileDialog dialog = new OpenFileDialog();
            string filename = "";

            if (dialog.ShowDialog() == true)
            {
                filename = dialog.FileName;

                FileStream xmlStream = new FileStream(filename, FileMode.Open);
                object o = serializer.Deserialize(xmlStream);
                SettingMappings = o as ObservableCollection<SettingMapping>;
                xmlStream.Close();
                tapMapping.DataContext = SettingMappings;
                rule_idx = SettingMappings.Count;
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
        public string Property_DispName{ get; set; }
        public string PropertyType { get; set; }
        public ObservableCollection<ValueMapping> ValMapping { get; set; }
    }

    public class ValueMapping
    {
        public string ExcelValue { get; set; }
        public string TapValue { get; set; }
    }

}
