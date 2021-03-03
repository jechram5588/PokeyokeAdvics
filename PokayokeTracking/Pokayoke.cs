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

namespace PokayokeTracking
{
    public class Pokayoke
    {
        private KeyenceMcNet keyence_net = null;
        Thread ClassThread = null;
        public bool Running { get; set; }
        public const string PLC_IP = "192.168.0.160";
        public const string Port = "5000";
        public int Errors = 0;
        public enum TipoDato{ Entero, Texto, Flotante }

        public void Start()
        {
            PrimaryLog("Inicio", "Arranque del proceso", EventLogEntryType.Warning, true);
            ClassThread = new Thread(ProcesoMain);
            Running = true;
            ClassThread.Start();
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

        public void ProcesoMain()
        {
            PrimaryLog("Inicio", "Proceso Principal", EventLogEntryType.Information, true);
            int Step = 0;
            string QR = "";
            string Trigger = "";
            while (Running)
            {
                Thread.Sleep(500);
                switch (Step)
                {
                    case 0:
                        PrimaryLog("Inicio", "Conexion a PLC", EventLogEntryType.Warning, true);
                        if (ConectaPLC())
                        {
                            PrimaryLog("Trigger", "Escaneo de trigger", EventLogEntryType.Information, false);
                            Step = 10;
                        }
                        break;
                    case 10:
                        Trigger = LeePLC(TipoDato.Entero, "D20024");
                        if (Trigger == "1")
                        {
                            EscribePLC(TipoDato.Entero, "D20024", "0");
                            Step = 20;
                        }
                        if(Trigger == "")
                            Errors++;
                        
                        if (Errors > 5)
                        {
                            DesconectaPLC();
                            Errors = 0;
                            Step = 0;
                            PrimaryLog("Escaneo", "Desconexión", EventLogEntryType.Error, true);
                        }
                        break;
                    case 20:
                        PrimaryLog("QR", "Lectura de QR", EventLogEntryType.Information, false);
                        QR = LeerBulkPLC(keyence_net, "D299", 11);
                        if (QR != "" && QR.Length == 20)
                        {
                            Step = 30;
                            Errors = 0;
                        }
                        else
                        {
                            QR = "";
                            Errors++;
                            if (Errors > 5)
                            {
                                DesconectaPLC();
                                Errors = 0;
                                Step = 0;
                                PrimaryLog("Lectura QR", "Desconexión", EventLogEntryType.Error, true);
                            }
                        }
                        break;
                    case 30:
                        PrimaryLog("Database", "Consultamos QR en DB", EventLogEntryType.Information, false);
                        DataTable dt = spSearchQROnStations(QR);
                        if (dt != null)
                            if (dt.Rows.Count > 0)
                            {
                                PrimaryLog("Database", "Codigo "+QR+" Resultado "+ dt.Rows[0][0].ToString(), EventLogEntryType.Information, false);
                                if (dt.Rows[0][0].ToString() == "0")
                                {
                                    EscribePLC(TipoDato.Entero, "D20026", "1");
                                }
                                else
                                {
                                    PrimaryLog("Database", "Codigo " + QR + " Resultado " + dt.Rows[0][0].ToString(), EventLogEntryType.Warning, true);
                                    EscribePLC(TipoDato.Entero, "D20026", "2");
                                    Thread.Sleep(500);
                                    EscribePLC(TipoDato.Entero, "D20028", dt.Rows[0][0].ToString());
                                }
                            }
                            else
                                EscribePLC(TipoDato.Entero, "D20026", "3");
                        else
                            EscribePLC(TipoDato.Entero, "D20026", "4");
                        Step = 10;
                        break;
                    default:
                        break;
                }
            }
        }

        #region SQLMethods
        public DataTable spSearchQROnStations(string QRCode)
        {
            string ConStr = "Server=.;Database=ProductionDatas;Trusted_Connection=True;";

            DataTable dtData = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConStr))
            {
                try
                {
                    SqlCommand command = new SqlCommand("spSearchQROnStations", conn);
                    SqlDataAdapter sqlAdapter = new SqlDataAdapter(command);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@QRCode", QRCode);
                    sqlAdapter.Fill(dtData);
                }
                catch (Exception ex)
                {
                    PrimaryLog("Database", "Error "+ex.Message, EventLogEntryType.Error, true);
                    dtData = null;
                }
            }
            return dtData;
        }
        #endregion
        #region PLCMethods
        public bool ConectaPLC()
        {
            try
            {
                keyence_net = null;
                keyence_net = new KeyenceMcNet();

                if (!System.Net.IPAddress.TryParse(PLC_IP, out System.Net.IPAddress address))
                {
                    PrimaryLog("Conección a PLC", "IP no valida", EventLogEntryType.Error, true);
                    return false;
                }

                keyence_net.IpAddress = PLC_IP;

                if (!int.TryParse(Port, out int port))
                {
                    PrimaryLog("Conección a PLC", "Puerto Erroneo", EventLogEntryType.Error, true);
                    return false;
                }

                keyence_net.Port = Convert.ToInt32(port);
                keyence_net.ConnectClose();
            }
            catch (Exception ex)
            {
                PrimaryLog("Conexion PLC", "Error al conectar", EventLogEntryType.Error, true);
            }

            try
            {
                OperateResult connect = keyence_net.ConnectServer();
                if (connect.IsSuccess)
                {
                    PrimaryLog("Conección a PLC", "Conexion OK", EventLogEntryType.Error, true);
                    return true;
                }
                else
                {
                    PrimaryLog("Conección a PLC", "No se logro conectar", EventLogEntryType.Error, true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                PrimaryLog("Conección a PLC", ex.Message.ToString(), EventLogEntryType.Error, true);
                return false;
            }
        }
        public void DesconectaPLC()
        {
            if (keyence_net != null)
                keyence_net.ConnectClose();
        }
        public bool EscribePLC(TipoDato Tipo, string Variable, string Valor)
        {
            if (Valor != null)
            {
                OperateResult result = new OperateResult();
                try
                {
                    switch (Tipo)
                    {
                        case TipoDato.Entero:
                            result = keyence_net.Write(Variable, int.Parse(Valor));
                            break;
                        case TipoDato.Flotante:
                            result = keyence_net.Write(Variable, float.Parse(Valor, CultureInfo.InvariantCulture));
                            break;
                        case TipoDato.Texto:
                            result = keyence_net.Write(Variable, Valor);
                            break;
                    }
                    if (result.IsSuccess)
                        return true;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    PrimaryLog("EscribePLC", string.Format("{0}", ex.Message), EventLogEntryType.Error, true);
                    return false;
                }
            }
            else
            {
                PrimaryLog("EscribePLC", "Error de null", EventLogEntryType.Error, true);
                return false;
            }
        }
        public string LeePLC(TipoDato dato, string Variable)
        {
            string res = "";
            switch (dato)
            {
                case TipoDato.Entero:
                    res = ReadResultRender(keyence_net.ReadInt32(Variable));
                    break;
                case TipoDato.Flotante:
                    res = ReadResultRender(keyence_net.ReadFloat(Variable));
                    break;
                case TipoDato.Texto:
                    res = ReadResultRender(keyence_net.ReadString(Variable, 2));
                    break;
            }
            return res;
        }
        public static string LeerBulkPLC(HslCommunication.Core.IReadWriteNet readWrite, string Variable, ushort Cantidad)
        {
            try
            {
                OperateResult<byte[]> read = readWrite.Read(Variable, Cantidad);
                if (read.IsSuccess)
                {
                    char[] data = System.Text.Encoding.ASCII.GetString(read.Content).ToCharArray();
                    string QRCode = "";
                    for (int i = 2; i < data.Length; i += 2)
                    {
                        if (i == 0)
                            QRCode = data[i].ToString();
                        else
                            QRCode += data[i + 1].ToString() + data[i].ToString();
                    }
                    return QRCode;
                }
                else
                    return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public static string ReadResultRender<T>(OperateResult<T> result)
        {
            if (result.IsSuccess)
            {
                return result.Content.ToString();
            }
            else
            {
                return "";
            }
        }

        public void PrimaryLog(string origen, string evento, EventLogEntryType tipo, bool Forzarlog)
        {
            Debug.Print(string.Format("{0}: {1}-> {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), origen, evento));
            Console.WriteLine(string.Format("{0}:{1}-> {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), origen, evento));

            if (Forzarlog || tipo != EventLogEntryType.Information)
            {
                StreamWriter sw = null;
                try
                {
                    sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogDeErrores_" + DateTime.Now.ToString("yyyyMMdd") + ".txt", true);
                    sw.WriteLine(string.Format("{0}: {1}-> {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), origen, evento));
                    sw.Flush();
                    sw.Close();
                }
                catch { }
            }

        }


        #endregion
    }
}
