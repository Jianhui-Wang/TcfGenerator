using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Keysight.Tap;
using Keysight.S8901A.Common;
using Keysight.S8901A.Measurement;
using System.Xml;
using System.Configuration;

namespace ParseTapStepDll
{
    class Program
    {
        static string[] TapStepsDlls = ConfigurationManager.AppSettings["TapStepsPlugin"].Split(',');
        static string BasePlugin = ConfigurationManager.AppSettings["BasePlugin"];
        static string IniFile = ConfigurationManager.AppSettings["IniConfig"];
        static string xmlFile = ConfigurationManager.AppSettings["XmlFile"];

        static List<Type> customTypes = new List<Type>();
        static Dictionary<string, List<string>> stringAvailValues = new Dictionary<string, List<string>>();
        static Dictionary<string, List<string>> enumDefinitions = new Dictionary<string, List<string>>();
        static Dictionary<string, Tuple<string,List<Tuple<string, string, string>>>> stepDefinitions = 
            new Dictionary<string, Tuple<string, List<Tuple<string, string, string>>>>();

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
                    if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(baseType) 
                        && type.IsDefined(typeof(TcfVisibleAttribute)))
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

        static void CreateNodeTree<T>(XmlDocument xmldoc, XmlElement parent, Dictionary<string, Tuple<string,List<T>>> dic, 
            string child_name, string child_value)
        {
            foreach (var avail in dic)
            {
                XmlElement e = xmldoc.CreateElement(child_name);
                e.SetAttribute("Name", avail.Key);
                e.SetAttribute("DisplayName", avail.Value.Item1);

                foreach (var value in avail.Value.Item2)
                {
                    XmlElement e2 = xmldoc.CreateElement(child_value);
                    if (typeof(T) == typeof(string))
                    {
                        e2.InnerText = value as string;
                    }
                    else if (typeof(T) == typeof(Tuple<string,string,string>))
                    {
                        var n = (value as Tuple<string, string, string>).Item1;
                        var d = (value as Tuple<string, string, string>).Item2;
                        var t = (value as Tuple<string, string, string>).Item3;

                        e2.SetAttribute("Name", n);
                        e2.SetAttribute("DisplayName", d);
                        if (enumDefinitions.ContainsKey(t))
                        {
                            e2.SetAttribute("Type", t.Replace(@"Keysight.S8901A.Common","Enumeration"));
                            foreach (var v in enumDefinitions[t])
                            {
                                XmlElement e3 = xmldoc.CreateElement("Enum-Value");
                                e3.InnerText = v as string;
                                e2.AppendChild(e3);
                            }
                        }
                        else if (stringAvailValues.ContainsKey(n) && t.GetType().Equals(typeof(string)))
                        {
                            e2.SetAttribute("Type", t);
                            e2.SetAttribute("AvailValues", "True");
                            foreach (var v in stringAvailValues[n])
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

                    }
                    e.AppendChild(e2);
                }
                parent.AppendChild(e);
            }
        }

        static void Main(string[] args)
        {
            var curDir = Directory.GetCurrentDirectory();
            for (int i = 0; i < TapStepsDlls.Length; i++)
            {
                var dllName = curDir + "\\" + TapStepsDlls[i];
                foreach (var a in GetAllTypes(dllName, typeof(TestStep)))
                {
                    string DisplayName = a.Name;
                    List<Tuple<string,string,string>> props = new List<Tuple<string,string,string>>();
                    foreach (var p in a.GetProperties())
                    {
                        if (p.IsDefined(typeof(TcfVisibleAttribute)))
                        {
                            string PropertyDisplayName = p.Name;
                            if (p.IsDefined(typeof(DisplayAttribute)))
                            {
                                var propAtt = p.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute;
                                PropertyDisplayName = propAtt.Name;
                            }
                            var tuple = new Tuple<string, string, string>(p.Name, PropertyDisplayName, p.PropertyType.ToString());
                            props.Add(tuple);

                            // for Keysight Custom type
                            if (p.PropertyType.ToString().Contains("Keysight"))
                            {
                                customTypes.Add(p.PropertyType);
                            }

                            // For string type with Available values, handle specially
                            if (p.PropertyType.Equals(typeof(string))
                                && p.IsDefined(typeof(AvailableValuesAttribute)))
                            {
                                stringAvailValues.Add(p.Name, ReadTechnologies(IniFile));
                            }
                        }
                    }
                    if (a.IsDefined(typeof(DisplayAttribute)))
                    {
                        var att = a.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute;
                        DisplayName = att.Name;
                    }
                    Tuple<string, List<Tuple<string, string, string>>> t = 
                        new Tuple<string, List<Tuple<string, string, string>>>(DisplayName, props);
                    stepDefinitions.Add(a.Name, t);
                }
            }

            // For the moment, only support Enumeration and 
            // all the Enum definitions should be in Base plugin
            var baseDll = curDir + "\\" + BasePlugin;
            var plugin = Assembly.LoadFrom(baseDll);
            if (plugin != null)
            {
                var types = plugin.GetTypes();
                foreach (Type c in customTypes)
                {
                    var t = types.ToList().Find(x => (x == c) && x.IsEnum);
                    List<string> values = new List<string>();

                    foreach (var ev in c.GetEnumValues())
                    {
                        values.Add(ev.ToString());
                    }
                    enumDefinitions.Add(c.ToString(), values);
                }
            }

            #region Write XML
            XmlDocument myXml = new XmlDocument();
            XmlNode docNode = myXml.CreateXmlDeclaration("1.0", "UTF-8", null);

            myXml.AppendChild(docNode);
            XmlElement rootElem = myXml.CreateElement("TestSteps");
            myXml.AppendChild(rootElem);

            CreateNodeTree(myXml, rootElem, stepDefinitions, "TestStep", "Property");

            myXml.Save(xmlFile);
            #endregion

            Console.ReadKey();
        }
    }
}
