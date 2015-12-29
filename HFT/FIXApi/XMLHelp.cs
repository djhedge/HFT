using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace FIXApi
{
    public class XMLHelp
    {
        // 读取sectionGroup节点
        private static XmlNode ReadMapping(string filePath, string key)
        {
            XmlDocument Xdoc;
            string fileName = filePath;
            Xdoc = new XmlDocument();
            Xdoc.Load(fileName);
            XmlNodeList list = Xdoc.SelectNodes(@"configuration/configSections/sectionGroup");
            foreach (XmlNode node in list)
            {
                if (node.Attributes["key"].Value.Equals(key))
                {
                    return node;
                }
            }
            return null;
        }

        public static string GetInnerValueStr(string filePath, string group, string key)
        {
            string innerValue = string.Empty;
            try
            {
                XmlNode xmlNode = ReadMapping(filePath, group);
                XmlNodeList list = xmlNode.SelectNodes("setting");
                foreach (XmlNode node in list)
                {
                    if (node.Attributes["key"].Value.Equals(key))
                    {
                        innerValue = node.InnerText;
                    }
                }
            }
            catch
            {
                innerValue = "unknown";
            }
            return innerValue;
        }
    }
}
