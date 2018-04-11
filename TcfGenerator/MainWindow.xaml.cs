using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace TcfGenerator
{
    public partial class MainWindow : Window
    {

        public ObservableCollection<ExcelMapping> MappingRules { get; set; }
        public ObservableCollection<Tuple<string,string>> TestStepList { get; set; }
        public ObservableCollection<Tuple<string,string,string>> PropList { get; set; }

        private XmlNode Node_TestSteps;
        private int teststep_idx;

        public MainWindow()
        {
            InitializeComponent();

            TestStepList = new ObservableCollection<Tuple<string,string>>();
            PropList = new ObservableCollection<Tuple<string,string,string>>();
            MappingRules = new ObservableCollection<ExcelMapping>();

            InitializeTestStepList(@"c:\temp\test.xml");
            tapMapping.DataContext = MappingRules;
            testStep.DataContext = this;
            Property.DataContext = this;
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
            ExcelMapping m = new ExcelMapping();
            int teststep_idx = testStep.SelectedIndex;
            int property_idx = Property.SelectedIndex;

            if (teststep_idx != -1 && property_idx != -1)
            {
                m.ExcelColumn = excelColumn.Text;
                m.TestStep = TestStepList[teststep_idx].Item1;
                m.TestStep_DispName = TestStepList[teststep_idx].Item2 ;
                m.Property = PropList[property_idx].Item1;
                m.Property_DispName = PropList[property_idx].Item2;
                m.PropertyType = PropList[property_idx].Item3;
                MappingRules.Add(m);
            }
        }

        private void Delete_One(object sender, RoutedEventArgs e)
        {
            int index = tapMapping.SelectedIndex;
            if (index == -1) return;
            MappingRules.RemoveAt(index);
        }
    }

    public class ExcelMapping
    {
        public string ExcelColumn { get; set; }
        public string TestStep { get; set; }
        public string TestStep_DispName { get; set; }
        public string Property { get; set; }
        public string Property_DispName{ get; set; }
        public string PropertyType { get; set; }
    }
}
