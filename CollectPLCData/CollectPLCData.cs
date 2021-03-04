using HslCommunication.Profinet.Keyence;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;

namespace CollectPLCData
{
    public class ReadPLC
    {
        private KeyenceMcNet keyence_net = (KeyenceMcNet)null;
        private Thread ClassThread = (Thread)null;
        private List<PLC> lPLCs;
        public string RutaApp;
        public string Ruta_Archivo_PLCs;

        public bool Running { get; set; }

        public void Start()
        {
            this.Running = true;
            this.lPLCs = new List<PLC>();
            this.CargaParametros();
            this.Proceso();
        }

        public void Stop()
        {
            try
            {
                this.Running = false;
                if (this.ClassThread != null)
                    this.ClassThread.Abort();
            }
            catch (Exception ex)
            {
                this.PrimaryLog("Detención de servicio", "Error al detener servicio" + ((object)ex.Message).ToString(), EventLogEntryType.Error, true);
            }
            this.PrimaryLog("Detención de servicio", "el servicio se mando detener", EventLogEntryType.Warning, true);
        }

        public void Proceso()
        {
            foreach (PLC plc in this.lPLCs)
            {
                plc.Running = this.Running;
                plc.Start();
            }
            while (this.Running)
                Thread.Sleep(1000);
            foreach (PLC plc in this.lPLCs)
                plc.Running = this.Running;
        }

        public void CargaParametros()
        {
            this.PrimaryLog("CargaParametros", "Carga de Parametros XML", EventLogEntryType.Warning, true);
            this.RutaApp = ((object)AppDomain.CurrentDomain.BaseDirectory).ToString();
            this.Ruta_Archivo_PLCs = this.RutaApp + "PLCs.xml";
            if (File.Exists(this.Ruta_Archivo_PLCs))
            {
                try
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(File.ReadAllText(this.Ruta_Archivo_PLCs ?? ""));
                    foreach (XmlNode xmlNode1 in xmlDocument.SelectNodes("PLCs/PLC"))
                    {
                        PLC plc = new PLC();
                        XmlNode xmlNode2 = xmlNode1.SelectSingleNode("IP");
                        plc.IP = ((object)xmlNode2.InnerText).ToString();
                        XmlNode xmlNode3 = xmlNode1.SelectSingleNode("DM_Trigger");
                        plc.DM_Trigger = ((object)xmlNode3.InnerText).ToString();
                        XmlNode xmlNode4 = xmlNode1.SelectSingleNode("DM_QR");
                        plc.DM_QR = ((object)xmlNode4.InnerText).ToString();
                        XmlNode xmlNode5 = xmlNode1.SelectSingleNode("DM_Result");
                        plc.DM_Result = ((object)xmlNode5.InnerText).ToString();
                        XmlNode xmlNode6 = xmlNode1.SelectSingleNode("Station");
                        plc.Estacion = ((object)xmlNode6.InnerText).ToString();
                        plc.Running = true;
                        this.lPLCs.Add(plc);
                    }
                }
                catch (Exception ex)
                {
                    this.PrimaryLog("CargaParametros", "Error al cargar parametros " + ex.Message, EventLogEntryType.Error, true);
                }
            }
            else
                this.PrimaryLog("CargaParametros", "No existe archivo con parametros", EventLogEntryType.Error, true);
        }

        public void PrimaryLog(string origen, string evento, EventLogEntryType tipo, bool Forzarlog)
        {
            Debug.Print(string.Format("{0}: {1}-> {2}", (object)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), (object)origen, (object)evento));
            Console.WriteLine(string.Format("{0}:{1}-> {2}", (object)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), (object)origen, (object)evento));
            if (!Forzarlog && tipo == EventLogEntryType.Information)
                return;
            try
            {
                StreamWriter streamWriter = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogDeErrores_" + DateTime.Now.ToString("yyyyMMdd") + ".txt", true);
                streamWriter.WriteLine(string.Format("{0}: {1}-> {2}", (object)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), (object)origen, (object)evento));
                ((TextWriter)streamWriter).Flush();
                streamWriter.Close();
            }
            catch
            {
            }
        }

        public enum TipoDato
        {
            Entero,
            Texto,
            Flotante,
        }
    }
}
