using PokayokeTracking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CallProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            Pokayoke p = new Pokayoke();
            p.Start();
            while (Console.ReadLine() == "X")
            {
                Thread.Sleep(1000);
            };
            
            p.Running = false;

            //var GetDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            //string filename = Path.Combine(GetDirectory, "AdvicsTest.exe");
            //var proc = System.Diagnostics.Process.Start(filename, "OP10");
            //proc.CloseMainWindow();
            //proc.Close();
        }
    }
}
