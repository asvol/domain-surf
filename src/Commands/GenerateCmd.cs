using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ManyConsole;

namespace DomainSurf.Commands
{
    public class GenerateCmd:ConsoleCommand
    {
        static readonly string[] Chars = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
        static readonly string[] Digits = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        static readonly string[] Symbols = { "_", "-"};

        private string _domain = "me";
        private string _regex = @"^[a-zA-Z0-9][a-zA-Z0-9-]{1,61}[a-zA-Z0-9]";
        private int _length = 3;

        public GenerateCmd()
        {
            IsCommand("gen", "Generate domain names");
            HasOption("d|domain=","Domain zone 'ru','com', etc", _ => _domain = _);
            HasOption("e|regex=", "Regex filter", _ => _regex = _);
            HasOption("l|length=", "Length", (int _) => _length = _);
            AllowsAnyAdditionalArguments("file name");
        }

        public override int Run(string[] remainingArguments)
        {
            var regex = new Regex(_regex, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var items = Generate(Chars.Concat(Digits).Concat(Symbols).ToArray(), _length).Where(_ => regex.IsMatch(_)).Select(_ => string.Concat(_, ".", _domain));
            var fileName = remainingArguments.FirstOrDefault();
            if (fileName == null)
            {
                foreach (var item in items)
                {
                    Console.WriteLine(item);
                }
            }
            else
            {
                var arr = items.ToArray();
                File.WriteAllLines(fileName, arr);
                Console.WriteLine($"Saved {arr.Length} items to {fileName}");
            }
            return 0;
        }

        public static IEnumerable<string> Generate(IEnumerable<string> chars,int count)
        {
            foreach (var c in chars)
            {
                yield return c;
                if (count > 1)
                {
                    foreach (var subitem in Generate(chars,count - 1))
                    {
                        yield return c + subitem;
                    }
                }
            }
        }
    }
}