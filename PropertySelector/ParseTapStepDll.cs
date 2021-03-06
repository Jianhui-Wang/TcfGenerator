﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Collections.ObjectModel;
using System.ComponentModel;

using Keysight.Tap;
using Keysight.S8901A.Common;
using System.Xml.Serialization;

namespace PropertySelector
{
    [Serializable]
    public class TapSetting : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }

        private bool selected;
        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                OnPropertyChanged("Selected");
            }
        }

        private Type type;
        [XmlIgnore]
        public Type @Type
        {
            get { return type; }
            set
            {
                type = value;
                TypeName = value.AssemblyQualifiedName;
            }
        }

        public string TypeName
        {
            get { return type.AssemblyQualifiedName; }
            set
            {
                type = Type.GetType(value);
            }
        }

        public bool IsKeysight { get; set; }
        public List<string> AvailableValues { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

    }

    [Serializable]
    public class TapTestStep : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }

        private bool selected;
        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                if (Settings != null)
                {
                    foreach (var a in Settings)
                    {
                        a.Selected = selected;
                    }
                }
                OnPropertyChanged("Selected");
            }
        }

        public ObservableCollection<TapSetting> Settings { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }


    public static class TapStepDllParser
    {
        static string[] TapStepsDlls = ConfigurationManager.AppSettings["TapStepsPlugin"].Split(',');
        static string BasePlugin = ConfigurationManager.AppSettings["BasePlugin"];
        static string IniFile = ConfigurationManager.AppSettings["IniConfig"];
        static string SwitchConfigFile = ConfigurationManager.AppSettings["SwitchConfig"];

        static ObservableCollection<TapTestStep> testSteps = new ObservableCollection<TapTestStep>();
        static HashSet<Type> keysightTypes = new HashSet<Type>();
        static List<Tuple<string, TestItem_Enum>> testItems = new List<Tuple<string, TestItem_Enum>>();
        static Dictionary<string, List<string>> enumDefinitions = new Dictionary<string, List<string>>();

        static IEnumerable<Type> GetAllTypes(string dllName, Type baseType)
        {
            Assembly plugin = null;
            try
            {
                plugin = Assembly.LoadFrom(dllName);
            }
            catch (FileLoadException ex)
            {
                Console.WriteLine("File Load Exception:" + ex.FileName);
                throw;
            }
            catch (TypeLoadException ex)
            {
                Console.WriteLine("Type Load Exception:" + ex.TypeName);
                throw;
            }

            if (plugin != null)
            {
                Type[] types = plugin.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(baseType))
                    {
                        yield return type;
                    }
                }
            }
            else
                throw new InvalidDataException("Load Dll '" + dllName + "' failed is null!");
        }

        static List<string> ReadTechnologies(string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException();
            return INIAccess.IniReadAllSection(filename);
        }
        static List<string> ReadSwitchPaths(string filename)
        {
            List<string> returnValue = new List<string>();
            SwitchMatrix sw = new SwitchMatrix(SwitchConfigFile);
            foreach (var path in sw.paths)
            {
                returnValue.Add(path.desc);
            }
            return returnValue;
        }

        static void CreateNodeTree(XmlDocument xmldoc, XmlElement parent, List<TapTestStep> testSteps,
            string child_name, string child_value)
        {
            foreach (var ts in testSteps.Where(x => x.Selected))
            {
                XmlElement e = xmldoc.CreateElement(child_name);
                e.SetAttribute("Name", ts.Name);
                e.SetAttribute("DisplayName", ts.DisplayName);

                foreach (var p in ts.Settings.Where(x => x.Selected))
                {
                    var t = p.Type.ToString();
                    XmlElement e2 = xmldoc.CreateElement(child_value);

                    e2.SetAttribute("Name", p.Name);
                    e2.SetAttribute("DisplayName", p.DisplayName);

                    if (p.Type.IsEnum)
                    {
                        if (enumDefinitions.ContainsKey(p.Type.ToString()))
                        {
                            string[] temp = t.Split('.');
                            e2.SetAttribute("Type", "Enumeration." + temp[temp.Length - 1]);
                            foreach (var v in enumDefinitions[t])
                            {
                                XmlElement e3 = xmldoc.CreateElement("Enum-Value");
                                e3.InnerText = v as string;
                                e2.AppendChild(e3);
                            }
                        }
                    }
                    else if (p.Type.Equals(typeof(string)) && p.AvailableValues.Count > 0)
                    {
                        e2.SetAttribute("Type", t);
                        e2.SetAttribute("AvailValues", "True");
                        foreach (var v in p.AvailableValues)
                        {
                            XmlElement e3 = xmldoc.CreateElement("Value");
                            e3.InnerText = v as string;
                            e2.AppendChild(e3);
                        }
                    }
                    else
                    {
                        e2.SetAttribute("Type", t);
                    }

                    e.AppendChild(e2);
                }
                parent.AppendChild(e);
            }
        }

        public static ObservableCollection<TapTestStep> GetAllDefinitions()
        {
            var curDir = Directory.GetCurrentDirectory();
            for (int i = 0; i < TapStepsDlls.Length; i++)
            {
                var dllName = curDir + "\\" + TapStepsDlls[i];
                foreach (var a in GetAllTypes(dllName, typeof(TestStep)).
                    Where(x => (!x.IsDefined(typeof(BrowsableAttribute))) ||
                               (x.IsDefined(typeof(BrowsableAttribute)) &&
                               (x.GetCustomAttribute(typeof(BrowsableAttribute)) as BrowsableAttribute).Browsable == true)
                         ))
                {
                    string DisplayName = a.Name;
                    ObservableCollection<TapSetting> settings = new ObservableCollection<TapSetting>();
                    if (a.IsDefined(typeof(DisplayAttribute)))
                    {
                        var att = a.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute;
                        DisplayName = att.Name;
                    }

                    TapTestStep ts = new TapTestStep();
                    ts.Name = a.Name;
                    ts.DisplayName = DisplayName;
                    ts.Selected = false;
                    ts.Settings = settings;

                    foreach (var p in (a.GetProperties().
                        Where(x => x.Module.ToString() == TapStepsDlls[i] &&
                                   x.Name != "pa_instrument" &&
                                   (
                                    (!x.IsDefined(typeof(BrowsableAttribute)))
                                    ||
                                    (x.IsDefined(typeof(BrowsableAttribute)) &&
                                    (x.GetCustomAttribute(typeof(BrowsableAttribute)) as BrowsableAttribute).Browsable == true)
                                    )
                                   )))
                    {
                        string PropertyDisplayName = p.Name;
                        if (p.IsDefined(typeof(DisplayAttribute)))
                        {
                            var propAtt = p.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute;
                            PropertyDisplayName = propAtt.Name;
                        }
                        TapSetting setting = new TapSetting();
                        setting.Name = p.Name;
                        setting.DisplayName = PropertyDisplayName;
                        setting.Selected = false;
                        setting.Type = p.PropertyType;
                        setting.IsKeysight = (p.PropertyType.ToString().Contains("Keysight")) ? true : false;
                        setting.AvailableValues = new List<string>();

                        if (setting.Type.IsEnum && setting.IsKeysight)
                        {
                            keysightTypes.Add(p.PropertyType);
                        }

                        // For string type with Available values, handle specially
                        if (p.PropertyType.Equals(typeof(string))
                            && p.IsDefined(typeof(AvailableValuesAttribute)))
                        {
                            // SelectTechnology.technology
                            if (ts.Name == "SelectTechnology" && p.Name == "technology")
                            {
                                setting.AvailableValues.AddRange(ReadTechnologies(IniFile));
                            }

                            // SelectTechnology.awg_waveform
                            else if (ts.Name == "SelectTechnology" && p.Name == "awg_waveform")
                            {
                                //TODO: add this
                            }

                            // Switch_Setup.switch_path
                            else if (ts.Name == "Switch_Setup" && p.Name == "switch_path")
                            {
                                setting.AvailableValues.AddRange(ReadSwitchPaths(SwitchConfigFile));
                            }

                            // PPMU_Measurement.ppmu_chan_name
                            else if (ts.Name == "PPMU_Measurement" && p.Name == "ppmu_chan_name")
                            {
                                //TODO: add this
                            }

                            else if (ts.Name == "PA_NFColdSourceMeasurement" && p.Name == "freqmode")
                            {
                                //TODO: add this
                            }

                            else if (ts.Name == "PA_NFYFactorMeasurement" && p.Name == "freqmode")
                            {
                                //TODO: add this
                            }

                            else
                            {
                                throw new Exception("AvailableValues: Need special handling for Test Step:[" + ts.Name + "], " + "Property:[" + p.Name + "]");
                            }

                        }

                        settings.Add(setting);
                    }

                    testSteps.Add(ts);
                }

                GetEnums();
            }

            GetTestItems();

            return testSteps;
        }

        static void GetEnums()
        {
            // For the moment, only support Enumeration and 
            // all the Enum definitions should be in Base plugin
            var curDir = Directory.GetCurrentDirectory();
            var baseDll = curDir + "\\" + BasePlugin;
            List<string> dlls = new List<string>();
            dlls.Add(baseDll);
            foreach (var a in TapStepsDlls)
            {
                var dll = curDir + "\\" + a;
                dlls.Add(dll);
            }


            foreach (var dll in dlls)
            {
                var plugin = Assembly.LoadFrom(dll);
                if (plugin != null)
                {
                    var types = plugin.GetTypes();
                    foreach (Type c in keysightTypes)
                    {
                        var t = types.ToList().Find(x => (x == c) && x.IsEnum);
                        if (t != null)
                        {
                            List<string> values = new List<string>();

                            foreach (var ev in c.GetEnumValues())
                            {
                                values.Add(ev.ToString());
                            }

                            if (!enumDefinitions.ContainsKey(c.ToString()))
                            {
                                enumDefinitions.Add(c.ToString(), values);
                            }
                        }
                    }
                }
            }
        }

        static void GetTestItems()
        {
            var curDir = Directory.GetCurrentDirectory();
            var baseDll = curDir + "\\" + BasePlugin;

            var plugin = Assembly.LoadFrom(baseDll);
            if (plugin != null)
            {
                for (int i = 0; i < Enum.GetNames(typeof(TestItem_Enum)).Count(); i++)
                {
                    testItems.Add(new Tuple<string, TestItem_Enum>(Enum.GetNames(typeof(TestItem_Enum))[i],
                                    ((TestItem_Enum[])(Enum.GetValues(typeof(TestItem_Enum))))[i]));
                }
            }
        }

        public static void GenerateXml(string filename, ObservableCollection<TapTestStep> testSteps)
        {
            XmlDocument myXml = new XmlDocument();
            XmlNode docNode = myXml.CreateXmlDeclaration("1.0", "UTF-8", null);

            myXml.AppendChild(docNode);
            XmlElement rootElem = myXml.CreateElement("Root");
            myXml.AppendChild(rootElem);

            XmlElement teststepsElem = myXml.CreateElement("TestSteps");
            rootElem.AppendChild(teststepsElem);
            CreateNodeTree(myXml, teststepsElem, testSteps.ToList(), "TestStep", "Property");

            XmlElement testItemsElem = myXml.CreateElement("TestItems");
            rootElem.AppendChild(testItemsElem);
            for (int i = 0; i < testItems.Count(); i++)
            {
                XmlElement item = myXml.CreateElement("Item");
                item.SetAttribute("Name", testItems[i].Item1);
                item.SetAttribute("Value", Convert.ToInt16(testItems[i].Item2).ToString());
                testItemsElem.AppendChild(item);
            }

            myXml.Save(filename);
        }
    }
}
