using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Aditi.Common.Azure;

namespace Eventstore.Utils
{
    public class MemSettingsProvider : ISettingsProvider
    {
        private readonly IDictionary<string, string> data;
        public MemSettingsProvider()
        {
            data = new Dictionary<string, string>();
        }

        public MemSettingsProvider(IDictionary<string, string> dict )
        {
            data = dict;
        }

        public static MemSettingsProvider FromServiceConfig(string pathUri, string role)
        {
            XNamespace ns = "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";
            XElement root = XElement.Load(pathUri);
            //root.Dump();
            var settings = from el in root.Elements(ns + "Role")
                           where el.Attribute("name").Value == role
                           select el.Descendants(ns + "Setting");
            var roleList = settings as IList<IEnumerable<XElement>> ?? settings.ToList();
            if (!roleList.Any())
            {
                throw new ArgumentException("No settings found for the role");
            }
            var dict = roleList.First()
                                   .ToDictionary(kv => kv.Attribute("name").Value, kv => kv.Attribute("value").Value);
            return new MemSettingsProvider(dict);
            
        }
        public void Add(string key, string value)
        {
            data[key] = value;
        }
        public bool TryGetString(string key, out string result)
        {
            if (data.ContainsKey(key))
            {
                result = data[key];
                return true;
            }
            result = string.Empty;
            return false;

        }

        public string TryGetString(string key)
        {
            string output = String.Empty;
            var result = TryGetString(key, out output);
            return output;
        }

        public string GetStringOrThrow(string key)
        {
            return data[key];
        }
    }
}