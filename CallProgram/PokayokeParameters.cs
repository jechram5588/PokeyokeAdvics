using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CallProgram
{
    public class PokayokeParameters
    {
        public static decimal OP10TightenTorqueMin { get { return GetVal("OP10TightenTorqueMin"); }  }
        public static decimal OP10TightenTorqueMax { get { return GetVal("OP10TightenTorqueMax"); } }
        public static decimal OP20MTMin { get { return GetVal("OP20MTMin"); } }
        public static decimal OP20MTMax { get { return GetVal("OP20MTMax"); } }
        public static decimal OP20LowPressureValueMin { get { return GetVal("OP20LowPressureValueMin"); } }
        public static decimal OP20LowPressureValueMax { get { return GetVal("OP20LowPressureValueMax"); } }
        public static decimal OP20HighPressureValueMax { get { return GetVal("OP20HighPressureValueMax"); } }
        public static decimal OP20HighPressureValueMin { get { return GetVal("OP20HighPressureValueMin"); } }

        public static decimal GetVal(string node)
        {
            string Ruta = ((object)AppDomain.CurrentDomain.BaseDirectory).ToString()+ "OPRanges.xml";
            decimal ret = -1;
            if (File.Exists(Ruta))
            {
                try
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(File.ReadAllText(Ruta ?? ""));
                    ret = Convert.ToDecimal(xmlDocument.SelectSingleNode("OPERATIONS/"+node).InnerText);
                }
                catch (Exception ex)
                {
                    ret = 0;
                }
            }
            return ret;
        }
    }
}
