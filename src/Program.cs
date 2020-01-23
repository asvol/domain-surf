using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainSurf.Commands;
using ManyConsole;

namespace DomainSurf
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            var commands = new ConsoleCommand[]
            {
                new GenerateCmd(), 
                new VerifyCmd(), 
            };

            try
            {
                return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Unhandled exception: {0}", ex.Message);
                return -1;
            }
        }
    }
}
