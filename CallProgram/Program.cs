using CollectPLCData;
using PokayokeTracking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace CallProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            PokayokeTracking.CheckSPs.Revisa();
            Console.ReadKey();
        }
    }
}
