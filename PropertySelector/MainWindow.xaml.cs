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

namespace PropertySelector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<MyTest> treeData = new ObservableCollection<MyTest>();

        public MainWindow()
        {
            InitializeComponent();

            MyTest m = new PropertySelector.MyTest("Outer1", "Inner1", 1);
            treeData.Add(m);
            m = new PropertySelector.MyTest("Outer2", "Inner2", 2);
            treeData.Add(m);
            treeView.DataContext = this.treeData;
        }
    }

    public class MyTest
    {
        public class MyInnerType
        {
            public string name { get; set; }
            public int value { get; set; }
        }

        public MyTest() { name = "Outer"; data = new MyInnerType() { name = "Inner", value = 1 }; }
        public MyTest(string s1, string s2, int v) { name = s1; data = new MyInnerType() { name = s2, value = v }; }
        public string name { get; set; }
        public MyInnerType data { get; set; }
    }
}
