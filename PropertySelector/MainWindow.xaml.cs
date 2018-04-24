using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using System.Windows.Threading;
using System.IO;
using System.Xml.Serialization;

namespace PropertySelector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<TapTestStep> testSteps = new ObservableCollection<TapTestStep>();

        public MainWindow()
        {
            InitializeComponent();

            testSteps = TapStepDllParser.GetAllDefinitions();

            treeView.DataContext = testSteps;
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            dlg.Title = "Generate to XML File";
            dlg.CheckFileExists = false;
            dlg.CheckPathExists = true;
            dlg.DefaultExt = "xml";
            dlg.Filter = "Xml files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.FilterIndex = 2;
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {
                TapStepDllParser.GenerateXml(dlg.FileName, testSteps);
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            dlg.Title = "Open Configuration File";
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.DefaultExt = "conf";
            dlg.Filter = "Config files (*.conf)|*.conf|All files (*.*)|*.*";
            dlg.FilterIndex = 2;
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {
                var configFile = dlg.FileName;
                XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<TapTestStep>));
                TextReader reader = new StreamReader(configFile);
                testSteps = ser.Deserialize(reader) as ObservableCollection<TapTestStep>;
                reader.Close();
                treeView.DataContext = testSteps;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            dlg.Title = "Save Configuration File";
            dlg.CheckFileExists = false;
            dlg.CheckPathExists = true;
            dlg.DefaultExt = "conf";
            dlg.Filter = "Config files (*.conf)|*.conf|All files (*.*)|*.*";
            dlg.FilterIndex = 2;
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {
                var configFile = dlg.FileName;
                XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<TapTestStep>));
                TextWriter writer = new StreamWriter(configFile);
                ser.Serialize(writer, testSteps);
                writer.Close();
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var ts in testSteps)
            {
                ts.Selected = true;
                foreach (var s in ts.Settings)
                {
                    s.Selected = true;
                }
            }
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var ts in testSteps)
            {
                ts.Selected = false;
                foreach (var s in ts.Settings)
                {
                    s.Selected = false;
                }
            }
        }
    }

    public class checkBoxBGConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return Colors.Red;
            else
                return Colors.Blue;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
