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

namespace CollectPLCData
{
    public class PLC
    {
        public int Errors = 0;
        private KeyenceMcNet keyence_net = (KeyenceMcNet)null;
        private string ConStr = "Server=192.168.0.180,1433;Database=ProductionDatas;User Id=sa;Password=Advics001;";
        public const string Port = "5000";

        public string XML_Parametros { get; set; }

        public string IP { get; set; }

        public string DM_QR { get; set; }

        public string DM_Result { get; set; }

        public bool Running { get; set; }

        public string DM_Trigger { get; set; }

        public string Estacion { get; set; }

        public void Start()
        {
            new Thread(new ThreadStart(this.Proceso)).Start();
        }

        public void Proceso()
        {
            this.PrimaryLog("Inicio", "Proceso Principal " + this.IP, EventLogEntryType.Information, true);
            int num1 = 0;
            string QRCode = "";
            while (this.Running)
            {
                Thread.Sleep(500);
                switch (num1)
                {
                    case 20:
                        QRCode = PLC.LeerBulkPLC((IReadWriteNet)this.keyence_net, this.DM_QR, (ushort)11);
                        if (QRCode != "" && QRCode.Length == 20)
                        {
                            num1 = 30;
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
                                num1 = 0;
                                this.PrimaryLog("Lectura QR", "Desconexión", EventLogEntryType.Error, true);
                            }
                            break;
                        }
                    case 30:
                        string str1 = this.LeePLC(PLC.TipoDato.Texto, this.DM_Result);
                        if (str1.Length >= 2)
                        {
                            if (!(this.Estacion == "10"))
                            {
                                PLC plc = this;
                                string origen = "Update";
                                string[] strArray = new string[6];
                                int index1 = 0;
                                string str2 = "Estacion ";
                                strArray[index1] = str2;
                                int index2 = 1;
                                string estacion = this.Estacion;
                                strArray[index2] = estacion;
                                int index3 = 2;
                                string str3 = " Update QR '";
                                strArray[index3] = str3;
                                int index4 = 3;
                                string str4 = QRCode;
                                strArray[index4] = str4;
                                int index5 = 4;
                                string str5 = "' Estatus: ";
                                strArray[index5] = str5;
                                int index6 = 5;
                                string str6 = str1;
                                strArray[index6] = str6;
                                string evento = string.Concat(strArray);
                                int num2 = 4;
                                int num3 = 0;
                                plc.PrimaryLog(origen, evento, (EventLogEntryType)num2, num3 != 0);
                                this.spUpdateQR(QRCode, str1.Substring(1, 1) + str1.Substring(0, 1));
                                num1 = 10;
                            }
                            else
                            {
                                num1 = 10;
                                PLC plc = this;
                                string origen = "Insert";
                                string[] strArray = new string[6];
                                int index1 = 0;
                                string str2 = "Estacion ";
                                strArray[index1] = str2;
                                int index2 = 1;
                                string estacion = this.Estacion;
                                strArray[index2] = estacion;
                                int index3 = 2;
                                string str3 = " Insertamos QR '";
                                strArray[index3] = str3;
                                int index4 = 3;
                                string str4 = QRCode;
                                strArray[index4] = str4;
                                int index5 = 4;
                                string str5 = "' Estatus: ";
                                strArray[index5] = str5;
                                int index6 = 5;
                                string str6 = str1;
                                strArray[index6] = str6;
                                string evento = string.Concat(strArray);
                                int num2 = 4;
                                int num3 = 0;
                                plc.PrimaryLog(origen, evento, (EventLogEntryType)num2, num3 != 0);
                                this.spInsertQR(QRCode, str1.Substring(1, 1) + str1.Substring(0, 1));
                            }
                            this.Errors = 0;
                        }
                        if (str1 == "")
                            this.Errors = this.Errors + 1;
                        if (this.Errors > 5)
                        {
                            this.DesconectaPLC();
                            this.Errors = 0;
                            num1 = 0;
                            this.PrimaryLog("Resultado", "Desconexión", EventLogEntryType.Error, true);
                            break;
                        }
                        else
                            break;
                    case 0:
                        this.PrimaryLog("Inicio", "Conexion a PLC", EventLogEntryType.Warning, true);
                        if (this.ConectaPLC())
                        {
                            this.PrimaryLog("Trigger", "Escaneo de trigger", EventLogEntryType.Information, false);
                            num1 = 10;
                            break;
                        }
                        else
                            break;
                    case 10:
                        string str7 = this.LeePLC(PLC.TipoDato.Entero, this.DM_Trigger);
                        if (str7 == "1")
                        {
                            Thread.Sleep(100);
                            this.EscribePLC(PLC.TipoDato.Entero, this.DM_Trigger, "0");
                            num1 = 20;
                        }
                        if (str7 == "")
                            this.Errors = this.Errors + 1;
                        if (this.Errors > 5)
                        {
                            this.DesconectaPLC();
                            this.Errors = 0;
                            num1 = 0;
                            this.PrimaryLog("Escaneo", "Desconexión", EventLogEntryType.Error, true);
                            break;
                        }
                        else
                            break;
                }
            }
        }

        public bool ConectaPLC()
        {
            try
            {
                this.keyence_net = (KeyenceMcNet)null;
                this.keyence_net = new KeyenceMcNet();
                IPAddress address;
                if (!IPAddress.TryParse(this.IP, out address))
                {
                    this.PrimaryLog("Conexión a PLC", "IP no valida", EventLogEntryType.Error, true);
                    return false;
                }
                else
                {
                    this.keyence_net.IpAddress = this.IP;
                    int result;
                    if (!int.TryParse("5000", out result))
                    {
                        this.PrimaryLog("Conexión a PLC", "Puerto Erroneo", EventLogEntryType.Error, true);
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
                    this.PrimaryLog("Conexión a PLC", "Conexion OK", EventLogEntryType.Error, true);
                    return true;
                }
                else
                {
                    this.PrimaryLog("Conexión a PLC", "No se logro conectar", EventLogEntryType.Error, true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                this.PrimaryLog("Conexión a PLC", ((object)ex.Message).ToString(), EventLogEntryType.Error, true);
                return false;
            }
        }

        public void DesconectaPLC()
        {
            if (this.keyence_net == null)
                return;
            this.keyence_net.ConnectClose();
        }

        public bool EscribePLC(PLC.TipoDato Tipo, string Variable, string Valor)
        {
            if (Valor != null)
            {
                OperateResult operateResult = new OperateResult();
                try
                {
                    switch (Tipo)
                    {
                        case PLC.TipoDato.Entero:
                            operateResult = ((NetworkDeviceBase<MelsecQnA3EBinaryMessage, RegularByteTransform>)this.keyence_net).Write(Variable, int.Parse(Valor));
                            break;
                        case PLC.TipoDato.Texto:
                            operateResult = ((NetworkDeviceBase<MelsecQnA3EBinaryMessage, RegularByteTransform>)this.keyence_net).Write(Variable, Valor);
                            break;
                        case PLC.TipoDato.Flotante:
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

        public string LeePLC(PLC.TipoDato dato, string Variable)
        {
            string str = "";
            switch (dato)
            {
                case PLC.TipoDato.Entero:
                    str = PLC.ReadResultRender<int>(this.keyence_net.ReadInt32(Variable));
                    break;
                case PLC.TipoDato.Texto:
                    str = PLC.ReadResultRender<string>(this.keyence_net.ReadString(Variable, (ushort)2));
                    break;
                case PLC.TipoDato.Flotante:
                    str = PLC.ReadResultRender<float>(this.keyence_net.ReadFloat(Variable));
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
            string format1 = "{0}:\t[{1}]\t {2}\t{3}";
            object[] objArray1 = new object[4];
            int index1 = 0;
            string str1 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            objArray1[index1] = (object)str1;
            int index2 = 1;
            string estacion1 = this.Estacion;
            objArray1[index2] = (object)estacion1;
            int index3 = 2;
            string str2 = origen;
            objArray1[index3] = (object)str2;
            int index4 = 3;
            string str3 = evento;
            objArray1[index4] = (object)str3;
            Debug.Print(string.Format(format1, objArray1));
            string format2 = "{0}:\t[{1}]\t{2}\t{3}";
            object[] objArray2 = new object[4];
            int index5 = 0;
            string str4 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            objArray2[index5] = (object)str4;
            int index6 = 1;
            string estacion2 = this.Estacion;
            objArray2[index6] = (object)estacion2;
            int index7 = 2;
            string str5 = origen;
            objArray2[index7] = (object)str5;
            int index8 = 3;
            string str6 = evento;
            objArray2[index8] = (object)str6;
            Console.WriteLine(string.Format(format2, objArray2));
            if (!Forzarlog && tipo == EventLogEntryType.Information)
                return;
            try
            {
                StreamWriter streamWriter1 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogDeErrores_" + DateTime.Now.ToString("yyyyMMdd") + ".txt", true);
                StreamWriter streamWriter2 = streamWriter1;
                string format3 = "{0}:\t[{1}]\t{2}\t{3}";
                object[] objArray3 = new object[4];
                int index9 = 0;
                string str7 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                objArray3[index9] = (object)str7;
                int index10 = 1;
                string estacion3 = this.Estacion;
                objArray3[index10] = (object)estacion3;
                int index11 = 2;
                string str8 = origen;
                objArray3[index11] = (object)str8;
                int index12 = 3;
                string str9 = evento;
                objArray3[index12] = (object)str9;
                string str10 = string.Format(format3, objArray3);
                streamWriter2.WriteLine(str10);
                ((TextWriter)streamWriter1).Flush();
                streamWriter1.Close();
            }
            catch
            {
            }
        }

        public bool spInsertQR(string QRCode, string Result)
        {
            bool flag = false;
            using (SqlConnection connection = new SqlConnection(this.ConStr))
            {
                try
                {
                    SqlCommand selectCommand = new SqlCommand("spInsertQR", connection);
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
                    selectCommand.CommandType = CommandType.StoredProcedure;
                    selectCommand.Parameters.AddWithValue("@QR", (object)QRCode);
                    selectCommand.Parameters.AddWithValue("@Result", (object)Result);
                    connection.Open();
                    selectCommand.ExecuteNonQuery();
                    connection.Close();
                    flag = true;
                }
                catch (Exception ex)
                {
                    this.PrimaryLog("Database", "Error " + ex.Message, EventLogEntryType.Error, true);
                }
            }
            return flag;
        }

        public bool spUpdateQR(string QRCode, string Result)
        {
            bool flag = false;
            using (SqlConnection connection = new SqlConnection(this.ConStr))
            {
                try
                {
                    SqlCommand selectCommand = new SqlCommand("spUpdateQR", connection);
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
                    selectCommand.CommandType = CommandType.StoredProcedure;
                    selectCommand.Parameters.AddWithValue("@QR", (object)QRCode);
                    selectCommand.Parameters.AddWithValue("@Result", (object)Result);
                    selectCommand.Parameters.AddWithValue("@Station", (object)this.Estacion);
                    connection.Open();
                    selectCommand.ExecuteNonQuery();
                    connection.Close();
                    flag = true;
                }
                catch (Exception ex)
                {
                    this.PrimaryLog("Database", "Error " + ex.Message, EventLogEntryType.Error, true);
                }
            }
            return flag;
        }

        public enum TipoDato
        {
            Entero,
            Texto,
            Flotante,
        }
    }
}
