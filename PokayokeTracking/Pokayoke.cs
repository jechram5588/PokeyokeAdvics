using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication.Profinet.Keyence;
using HslCommunication;
using HslCommunication.Profinet;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace PokayokeTracking
{
    public class Pokayoke
    {
        Thread ClassThread = null;
        public bool Running { get; set; }
        List<PLC> lPLCs;

        public string RutaApp, Ruta_Archivo_PLCs; 
        public void Start()
        {
            //PrimaryLog("Inicio", "Arranque del proceso", EventLogEntryType.Warning, true);
            //ClassThread = new Thread(ProcesoMain);
            Running = true;
            //ClassThread.Start();
            lPLCs = new List<PLC>();
            CargaParametros();
            Proceso();
        }
        public void Stop()
        {
            try
            {
                Running = false;
                if (ClassThread != null)
                    ClassThread.Abort();
            }
            catch (Exception ex)
            {
                PrimaryLog("Detención de servicio", "Error al detener servicio" + ex.Message.ToString(), EventLogEntryType.Error, true);
            }
            PrimaryLog("Detención de servicio", "el servicio se mando detener", EventLogEntryType.Warning, true);
        }

        public void Proceso()
        {
            /*Start Queue threads*/
            foreach (PLC p in lPLCs)
            {
                p.Running = Running;
                p.Start();
            }

            while (Running)
            {                
                Thread.Sleep(1000);
            }

            foreach (PLC p in lPLCs)
            {
                p.Running = Running;
            }
        }

        public void CargaParametros() {
            Console.WriteLine("Parametros");
            RutaApp = AppDomain.CurrentDomain.BaseDirectory.ToString();

            Ruta_Archivo_PLCs = RutaApp + "PLCs.xml";

            if (File.Exists(Ruta_Archivo_PLCs))
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(File.ReadAllText(@"" + Ruta_Archivo_PLCs));
                XmlNodeList xnList = xml.SelectNodes("PLCs/PLC");
                int a = 0;
                foreach (XmlNode xn in xnList)
                {
                    PLC pl = new PLC();
                    XmlNode xmlNode = xn.SelectSingleNode("IP");
                    pl.IP = xmlNode.InnerText.ToString();

                    xmlNode = xn.SelectSingleNode("DM_QR");
                    pl.DM_QR = xmlNode.InnerText.ToString();

                    xmlNode = xn.SelectSingleNode("DM_Result");
                    pl.DM_Result = xmlNode.InnerText.ToString();

                    pl.HiloNo = a++;
                    pl.Running = true;
                    lPLCs.Add(pl);
                }
            }
        }

       


        #endregion
    }
}
