using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSkassa
{
	class Check
    {
		public IDictionary<string, Section> Sections;

		public Check(string file)
		{
			Sections = new Dictionary<string, Section>();
			Parse(file);
		}

		private void Parse(string path)
		{
            System.IO.StreamReader file = new System.IO.StreamReader(path, Program.TSSettings.MainEncoding);
			string line;
            //int strId = 0;
            //bool printLast = true;
            //string curSection = "";
            Section curSection = null;
            string nameOfSection = "";
			while ((line = file.ReadLine()) != null)
			{
                //удалить пробелы
                line = line.Trim();
                if (line == "") continue;

                if (line.IndexOf("[") != -1) //This is section
                {
                    int index = line.IndexOf("]");
                    int length = index == -1 ? line.Length : index;
                    nameOfSection = line.Substring(1, length - 1);
                    curSection = new Section(nameOfSection);
                    Sections.Add(nameOfSection, curSection);
                    Sections.TryGetValue(nameOfSection, out curSection);
                    continue;
                }
                else
                {
                    string key = "";
                    string value = "";
                    int index = 0;

                    index = line.IndexOf("=");
                    if (index == -1)
                    {
                        continue;
                    }
                    key = line.Substring(0, index);
                    value = line.Substring(index + 1, line.Length - index - 1);

                    if (curSection != null)
                    {
                        curSection.AddKeyValue(key, value);
                    }
                }
            }
        }

        public Section[] GetAllStrings()
        {
            IList<Section> strings = new List<Section>();
            foreach (var section in Sections)
            {
                if (section.Key.Contains("STRING"))
                {
                    strings.Add(section.Value);
                }
            }
            return strings.ToArray();
        }
    }

		class Section
		{
			public string Name { get; set; }
			public IDictionary<string, string> ListKeyValue;

			public Section(string name)
			{
				ListKeyValue = new Dictionary<string, string>();
                Name = name;
			}

			public void AddKeyValue(string key, string value)
			{
            try
            {
                ListKeyValue.Add(key, value);
            }
            catch (Exception)
            {
                Program.MainLog.WriteLog(EVENTS.KEYVALUE_REPEAT);
            }
				
			}
			public void RemoveKeyValue(string key)
			{
				ListKeyValue.Remove(key);
			}

		}

	}

//System.IO.StreamReader temp = new System.IO.StreamReader(@"Kasbi02MF.queries", Program.TSSettings.MainEncoding);
//IList<string> properties = new List<string>();
//            while ((line = temp.ReadLine()) != null)
//            {
//                string tempLine = line;
//tempLine = tempLine.ToLower();
//                int index = 0;
//index = tempLine.IndexOf("parametername");
//                if (index == -1)
//                {
//                    continue;
//                }
//                index = line.IndexOf(":");
//                string tempRight = line.Substring(index + 1, line.Length - index - 1).Trim().Substring(1);
//index = tempRight.IndexOf("\"");
//                tempRight = tempRight.Substring(0, index);
//                if (properties.Contains(tempRight))
//                {
//                    continue;
//                }
//                properties.Add(tempRight);
//            }
//            int a = 0;