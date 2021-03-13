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
            Console.WriteLine(PokayokeParameters.OP10TightenTorqueMin);
            Console.WriteLine(PokayokeParameters.OP10TightenTorqueMax);
            Console.WriteLine(PokayokeParameters.OP20MTMin);
            Console.WriteLine(PokayokeParameters.OP20MTMax);
            Console.WriteLine(PokayokeParameters.OP20LowPressureValueMin);
            Console.WriteLine(PokayokeParameters.OP20LowPressureValueMax);
            Console.WriteLine(PokayokeParameters.OP20HighPressureValueMin);
            Console.WriteLine(PokayokeParameters.OP20HighPressureValueMax);
            Console.ReadKey();
        }
    }
}
