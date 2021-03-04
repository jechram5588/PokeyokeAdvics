using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Profinet.Keyence;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace PokayokeTracking
{
    public class Pokayoke
    {
        private KeyenceMcNet keyence_net = (KeyenceMcNet)null;
        private Thread ClassThread = (Thread)null;
        public int Errors = 0;
        public const string PLC_IP = "192.168.0.160";
        public const string Port = "5000";

        public bool Running { get; set; }

        public void Start()
        {
            this.PrimaryLog("Inicio", "Arranque del proceso", EventLogEntryType.Warning, true);
            this.ClassThread = new Thread(new ThreadStart(this.ProcesoMain));
            this.Running = true;
            this.ClassThread.Start();
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

        public void ProcesoMain()
        {
            this.PrimaryLog("Inicio", "Proceso Principal", EventLogEntryType.Information, true);
            int num = 0;
            string QRCode = "";
            while (this.Running)
            {
                Thread.Sleep(500);
                switch (num)
                {
                    case 20:
                        this.PrimaryLog("QR", "Lectura de QR", EventLogEntryType.Information, false);
                        QRCode = Pokayoke.LeerBulkPLC((IReadWriteNet)this.keyence_net, "D299", (ushort)11);
                        if (QRCode != "" && QRCode.Length == 20)
                        {
                            num = 30;
                            this.Errors = 0;
                            break;
                        }
                        else
                        {
                            QRCode = "";
                            this.Errors = this.Errors + 1;
                            if (this.Errors > 5)
                            {
                                this.DesconectaPLC();
                                this.Errors = 0;
                                num = 0;
                                this.PrimaryLog("Lectura QR", "Desconexión", EventLogEntryType.Error, true);
                            }
                            break;
                        }
                    case 30:
                        this.PrimaryLog("Database", "Consultamos QR en DB", EventLogEntryType.Information, false);
                        DataTable dataTable = this.spSearchQROnStations(QRCode);
                        if (dataTable != null)
                        {
                            if (dataTable.Rows.Count > 0)
                            {
                                this.PrimaryLog("Database", "Codigo " + QRCode + " Resultado " + dataTable.Rows[0][0].ToString(), EventLogEntryType.Information, false);
                                if (dataTable.Rows[0][0].ToString() == "0")
                                {
                                    this.EscribePLC(Pokayoke.TipoDato.Entero, "D20026", "1");
                                }
                                else
                                {
                                    this.PrimaryLog("Database", "Codigo " + QRCode + " Resultado " + dataTable.Rows[0][0].ToString(), EventLogEntryType.Warning, true);
                                    this.EscribePLC(Pokayoke.TipoDato.Entero, "D20026", "2");
                                    Thread.Sleep(500);
                                    this.EscribePLC(Pokayoke.TipoDato.Entero, "D20028", dataTable.Rows[0][0].ToString());
                                }
                            }
                            else
                                this.EscribePLC(Pokayoke.TipoDato.Entero, "D20026", "3");
                        }
                        else
                            this.EscribePLC(Pokayoke.TipoDato.Entero, "D20026", "4");
                        num = 10;
                        break;
                    case 0:
                        this.PrimaryLog("Inicio", "Conexion a PLC", EventLogEntryType.Warning, true);
                        if (this.ConectaPLC())
                        {
                            this.PrimaryLog("Trigger", "Escaneo de trigger", EventLogEntryType.Information, false);
                            num = 10;
                            break;
                        }
                        else
                            break;
                    case 10:
                        string str = this.LeePLC(Pokayoke.TipoDato.Entero, "D20024");
                        if (str == "1")
                        {
                            this.EscribePLC(Pokayoke.TipoDato.Entero, "D20024", "0");
                            num = 20;
                        }
                        if (str == "")
                            this.Errors = this.Errors + 1;
                        if (this.Errors > 5)
                        {
                            this.DesconectaPLC();
                            this.Errors = 0;
                            num = 0;
                            this.PrimaryLog("Escaneo", "Desconexión", EventLogEntryType.Error, true);
                            break;
                        }
                        else
                            break;
                }
            }
        }

        public DataTable spSearchQROnStations(string QRCode)
        {
            string connectionString = "Server=.;Database=ProductionDatas;Trusted_Connection=True;";
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    SqlCommand selectCommand = new SqlCommand("spSearchQROnStations", connection);
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
                    selectCommand.CommandType = CommandType.StoredProcedure;
                    selectCommand.Parameters.AddWithValue("@QRCode", (object)QRCode);
                    sqlDataAdapter.Fill(dataTable);
                }
                catch (Exception ex)
                {
                    this.PrimaryLog("Database", "Error " + ex.Message, EventLogEntryType.Error, true);
                    dataTable = (DataTable)null;
                }
            }
            return dataTable;
        }

        public bool ConectaPLC()
        {
            try
            {
                this.keyence_net = (KeyenceMcNet)null;
                this.keyence_net = new KeyenceMcNet();
                IPAddress address;
                if (!IPAddress.TryParse("192.168.0.160", out address))
                {
                    this.PrimaryLog("Conección a PLC", "IP no valida", EventLogEntryType.Error, true);
                    return false;
                }
                else
                {
                    this.keyence_net.IpAddress = "192.168.0.160";
                    int result;
                    if (!int.TryParse("5000", out result))
                    {
                        this.PrimaryLog("Conección a PLC", "Puerto Erroneo", EventLogEntryType.Error, true);
                        return false;
                    }
                    else
                    {
                        this.keyence_net.Port = Convert.ToInt32(result);
                        this.keyence_net.ConnectClose();
                    }
                }
            }
            catch (Exception ex)
            {
                this.PrimaryLog("Conexion PLC", "Error al conectar", EventLogEntryType.Error, true);
            }
            try
            {
                if (this.keyence_net.ConnectServer().IsSuccess)
                {
                    this.PrimaryLog("Conección a PLC", "Conexion OK", EventLogEntryType.Error, true);
                    return true;
                }
                else
                {
                    this.PrimaryLog("Conección a PLC", "No se logro conectar", EventLogEntryType.Error, true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                this.PrimaryLog("Conección a PLC", ((object)ex.Message).ToString(), EventLogEntryType.Error, true);
                return false;
            }
        }

        public void DesconectaPLC()
        {
            if (this.keyence_net == null)
                return;
            this.keyence_net.ConnectClose();
        }

        public bool EscribePLC(Pokayoke.TipoDato Tipo, string Variable, string Valor)
        {
            if (Valor != null)
            {
                OperateResult operateResult = new OperateResult();
                try
                {
                    switch (Tipo)
                    {
                        case Pokayoke.TipoDato.Entero:
                            operateResult = ((NetworkDeviceBase<MelsecQnA3EBinaryMessage, RegularByteTransform>)this.keyence_net).Write(Variable, int.Parse(Valor));
                            break;
                        case Pokayoke.TipoDato.Texto:
                            operateResult = ((NetworkDeviceBase<MelsecQnA3EBinaryMessage, RegularByteTransform>)this.keyence_net).Write(Variable, Valor);
                            break;
                        case Pokayoke.TipoDato.Flotante:
                            operateResult = ((NetworkDeviceBase<MelsecQnA3EBinaryMessage, RegularByteTransform>)this.keyence_net).Write(Variable, float.Parse(Valor, (IFormatProvider)CultureInfo.InvariantCulture));
                            break;
                    }
                    return operateResult.IsSuccess;
                }
                catch (Exception ex)
                {
                    this.PrimaryLog("EscribePLC", string.Format("{0}", (object)ex.Message), EventLogEntryType.Error, true);
                    return false;
                }
            }
            else
            {
                this.PrimaryLog("EscribePLC", "Error de null", EventLogEntryType.Error, true);
                return false;
            }
        }

        public string LeePLC(Pokayoke.TipoDato dato, string Variable)
        {
            string str = "";
            switch (dato)
            {
                case Pokayoke.TipoDato.Entero:
                    str = Pokayoke.ReadResultRender<int>(this.keyence_net.ReadInt32(Variable));
                    break;
                case Pokayoke.TipoDato.Texto:
                    str = Pokayoke.ReadResultRender<string>(this.keyence_net.ReadString(Variable, (ushort)2));
                    break;
                case Pokayoke.TipoDato.Flotante:
                    str = Pokayoke.ReadResultRender<float>(this.keyence_net.ReadFloat(Variable));
                    break;
            }
            return str;
        }

        public static string LeerBulkPLC(IReadWriteNet readWrite, string Variable, ushort Cantidad)
        {
            try
            {
                OperateResult<byte[]> operateResult = readWrite.Read(Variable, Cantidad);
                if (!operateResult.IsSuccess)
                    return "";
                char[] chArray = Encoding.ASCII.GetString(operateResult.Content).ToCharArray();
                string str = "";
                int index = 2;
                while (index < chArray.Length)
                {
                    str = index != 0 ? str + chArray[index + 1].ToString() + chArray[index].ToString() : chArray[index].ToString();
                    index += 2;
                }
                return str;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static string ReadResultRender<T>(OperateResult<T> result)
        {
            if (result.IsSuccess)
                return result.Content.ToString();
            else
                return "";
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
