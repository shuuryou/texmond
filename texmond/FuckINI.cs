using System;
using System.Collections.Generic;
using System.IO;

namespace texmond
{
    // There is not a single INI parser in C# out there that is not
    // totally over-engineered. I just want to load a few key-value
    // pairs that are organized in sections.
    // So here is my own light-weight C# INI parser. (Version 1.2)
    internal sealed class FuckINI
    {
        private Dictionary<string, Dictionary<string, string>> m_Sections;

        public FuckINI(string file)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            if (!File.Exists(file))
                throw new FileNotFoundException("The specified INI file does not exist.", file);

            m_Sections = new Dictionary<string, Dictionary<string, string>>();

            string curSection = null;

            using (StreamReader sr = File.OpenText(file))
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine().Trim();

                    if (line.Length == 0 || line.IndexOf(';') == 0)
                        continue;

                    if (line.IndexOf('[') == 0 && line.IndexOf(']') == line.Length - 1)
                    {
                        curSection = line.Substring(1, line.Length - 2).ToUpperInvariant();
                        if (!m_Sections.ContainsKey(curSection))
                            m_Sections.Add(curSection, new Dictionary<string, string>());

                        continue;
                    }

                    int idx = line.IndexOf('=');

                    if (idx > 0 && curSection != null)
                    {
                        string key = line.Substring(0, idx).ToUpperInvariant();
                        string value = line.Substring(idx + 1);

                        // Inline comments
                        idx = value.IndexOf(';');
                        if (idx != -1)
                            value = value.Substring(0, idx);

                        key = key.Trim();
                        value = value.Trim();

                        if (m_Sections[curSection].ContainsKey(key))
                            m_Sections[curSection][key] = value;
                        else
                            m_Sections[curSection].Add(key, value);

                        continue;
                    }

                    throw new InvalidDataException("Could not parse: \"" + line + "\"");
                }
        }

        public string Get(string section, string key)
        {
            if (section == null)
                throw new ArgumentNullException("section");

            if (key == null)
                throw new ArgumentNullException("key");

            if (section.Length == 0 || key.Length == 0)
                return null;

            section = section.ToUpperInvariant();
            key = key.ToUpperInvariant();

            if (!m_Sections.ContainsKey(section))
                return null;

            if (!m_Sections[section].ContainsKey(key))
                return null;

            return m_Sections[section][key];
        }
    }
}