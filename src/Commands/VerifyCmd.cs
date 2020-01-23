using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ManyConsole;
using Whois;

namespace DomainSurf.Commands
{
    public class VerifyCmd:ConsoleCommand
    {
        private string _src = "out.txt";
        private string _free = "free.txt";
        private string _busy = "busy.txt";
        private int _count;
        private int _complete;
        private int _foundFree;
        private int _httpBusy;
        private int _whoisBusy;
        private string _regex;

        public VerifyCmd()
        {
            IsCommand("verify", "Verify domain names by whois service");
            HasRequiredOption("s|src=", "File with domains", _ => _src = _);
            HasOption("e|regex=", "Regex filter", _ => _regex = _);
            HasOption("f|free=", "File with free domain", _ => _free = _);
            HasOption("b|busy=", "File with busy domains", _ => _busy = _);
            HasOption("c|count=", "Verify domain count", (int _) => _count = _);
        }

        public override int Run(string[] remainingArguments)
        {
            var domains = new HashSet<string>(File.ReadAllLines(_src));
            var free = File.Exists(_free) ? new HashSet<string>(File.ReadAllLines(_free), StringComparer.InvariantCultureIgnoreCase) : new HashSet<string>();
            var busy = File.Exists(_busy) ? new HashSet<string>(File.ReadAllLines(_busy), StringComparer.InvariantCultureIgnoreCase) : new HashSet<string>();
            domains.ExceptWith(free);
            domains.ExceptWith(busy);
            if (_count <=0) _count = domains.Count;

            var req = string.IsNullOrWhiteSpace(_regex) ? 
                domains.AsParallel().Select(Verify).Take(_count) : 
                domains.Where(_ => Regex.IsMatch(_, _regex, RegexOptions.IgnoreCase | RegexOptions.Compiled)).AsParallel().Select(Verify).Take(_count);
            
            Console.WriteLine($"Src:{domains.Count}");
            Console.WriteLine($"Free:{free.Count}");
            Console.WriteLine($"Busy:{busy.Count}");
            Observable.Timer(TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000))
                .Subscribe(_ => PrintState());

            var result = req.ToArray();
            return 0;
        }

        private void PrintState()
        {
            var percent = (double) _complete / (double) _count;
            Console.WriteLine(value: $"Porgress             {_complete}/{_count} ({percent:P2})");
            Console.WriteLine(value: $"Free founded         {_foundFree}");
            Console.WriteLine(value: $"Http busy founded    {_httpBusy}");
            Console.WriteLine(value: $"Whois busy founded   {_whoisBusy}");
        }


        public bool Verify(string domain)
        {
            if (VerifyIsBusyByHttp(domain))
            {
                Interlocked.Increment(ref _httpBusy);
                AddToBusy(domain);
            }
            else if (VerifyIsBusyByWhoIs(domain))
            {
                Interlocked.Increment(ref _whoisBusy);
                AddToBusy(domain);
                return false;
            }
            else
            {
                Interlocked.Increment(ref _foundFree);
                AddToFree(domain);
            }
            Interlocked.Increment(ref _complete);
            return true;
        }

        private static bool VerifyIsBusyByWhoIs(string domain)
        {
            try
            {
                var whois = new WhoisLookup().Lookup(domain);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static bool VerifyIsBusyByHttp(string domain)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var a = client.GetAsync("http://" + domain).Result;
                    return true;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private void AddToFree(string domain)
        {
            lock (this)
            {
                File.AppendAllText(_free, domain + "\n");
            }
        }

        private void AddToBusy(string domain)
        {
            lock (this)
            {
                File.AppendAllText(_busy, domain + "\n");
            }
        }
    }
}