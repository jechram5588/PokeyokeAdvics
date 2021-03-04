using CollectPLCData;
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
            ReadPLC readPlc = new ReadPLC();
            readPlc.Start();
            while (Console.ReadLine() == "X")
                Thread.Sleep(1000);
            readPlc.Running = false;
        }
    }
}
