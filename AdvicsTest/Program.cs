using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvicsTest
{
    class Program
    {

        static void Main(string[] args)
        {
            ExportarExcel();
        }

        public static void ExportarExcel()
        {
            DataTable dt = SQLMethods.spSearchProductioByActualDayAndOperation("OP10");
            var GetDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (dt != null)
            {
                SLDocument sSL = new SLDocument();
                sSL.ImportDataTable(1, 1, dt, true);
                sSL.SaveAs(GetDirectory+@"\demo.xls");
                Console.WriteLine("Terminado");
            }
            Console.ReadKey();
        }

    }
}
