using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq.Expressions;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;


internal class ProxyRabbit{

    static async Task Main(string[] args){

        if (args.Length > 0 && args[0] == "-h"){
            Console.WriteLine("  ___                    ___      _    _    _ _   ");
            Console.WriteLine(" | _ \\_ _ _____ ___  _  | _ \\__ _| |__| |__(_) |_   (\\(\\ ");
            Console.WriteLine(" |  _/ '_/ _ \\ \\ / || | |   / _` | '_ \\ '_ \\ |  _|  ( -.-)");
            Console.WriteLine(" |_| |_| \\___/_\\_\\_, | |_|_\\__,_|_.__/_.__/_|\\__|  o_(\")(\")");
            Console.WriteLine("                  |__/  ");
            Console.WriteLine("Usage: proxyrabbit [protocol] [timeout] [country] [ssl] [anonymity]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -h             Display help information");
            Console.WriteLine("  protocol       Protocol used: http, socks4, socks5, all");
            Console.WriteLine("  timeout        Timeout in miliseconds, maximum 10000ms, 30-200 optimal");
            Console.WriteLine("  country        Alpha 2 ISO country code or \'all\' (like US for USA, RU for Russian Federation etc)");
            Console.WriteLine("  ssl            Should the proxies support SSL? (yes, no, all)");
            Console.WriteLine("  anonymity      Define which anonymity level the proxies should have:");
            Console.WriteLine("                         elite, anonymous, transparent, all");
            Console.WriteLine("\nexample: ./proxyrabbit http 100 RU all all");
            Console.WriteLine("\nDescription: ProxyRabbit will scrape available proxies, test them, and store them in proxychains.conf file for you.");
            Console.WriteLine("Depending on your choice, program will add proxies to existing file, display them in console, or backup old proxychains.conf and create minimal one for you with new proxies.");
        } else {
            string protocol = args[0];
            string timeout = args[1];
            string country = args[2];
            string ssl = args[3];
            string anonymity = args[4];

            string response = string.Empty;

            using (HttpClient client = new HttpClient()){
                string url = "https://api.proxyscrape.com/v2/?request=displayproxies&protocol="+protocol+"&timeout="+timeout+"&country="+country+"&ssl="+ssl+"&anonymity="+anonymity;
                response = client.GetStringAsync(url).Result;
            }
            
            List<string> proxyList = new List<string>(response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));
            List<string> responded = new List<string>();

            var tests = new List<Task<List<string>>>();
            foreach (string proxy in proxyList){
                var test = TimeoutTest(proxy, timeout);
                tests.Add(test);
            }
            List<string>[] results = await Task.WhenAll(tests);

            foreach (var result in results){
                responded.AddRange(result);
            }
            Loop:
            Console.WriteLine("[D]isplay proxies, [A]dd to proxychains.conf file, [O]verwrite proxychain.config file, backup old one, E[x]it?");

            ConsoleKeyInfo choice = Console.ReadKey(true);
            if (choice.Key == ConsoleKey.D && !choice.Modifiers.HasFlag(ConsoleModifiers.Shift) || choice.Key == ConsoleKey.D){
                foreach (var pinged in responded){
                    Console.WriteLine(protocol + " " + pinged);
                }         
            } else if (choice.Key == ConsoleKey.A && !choice.Modifiers.HasFlag(ConsoleModifiers.Shift) || choice.Key == ConsoleKey.A){
            Console.WriteLine("Checking if proxychains.conf exists");
                if ((File.Exists("/etc/proxychains.conf")) || (File.Exists("/etc/proxychains4.conf"))){
                    Console.WriteLine("Writing proxies to file: ");
                    using (StreamWriter proxychains = File.AppendText("/etc/proxychains.conf")){
                        foreach (var pinged in responded){
                            string[] fpinged = pinged.Split(":");
                            string presult = fpinged[0] + " " + fpinged[1];
                            
                            Console.WriteLine(protocol + " " + presult);
                            proxychains.WriteLine(protocol + " " + presult);
                        }
                    }
                } else {
                    Console.WriteLine("No file found");
                }
            } else if (choice.Key == ConsoleKey.O && !choice.Modifiers.HasFlag(ConsoleModifiers.Shift) || choice.Key == ConsoleKey.O){
            Console.WriteLine("Checking if proxychains.conf exists.");
                if ((File.Exists("/etc/proxychains.conf")) || (File.Exists("/etc/proxychains4.conf"))){
                    Console.WriteLine("Backingup proxychains.conf. ");
                    // Check if folder exists 
                    if (!Directory.Exists("/etc/proxychains.backup/"))
                    {
                        Directory.CreateDirectory("/etc/proxychains.backup/");
                    } 
                    // Copy + rename config file
                    string randomFileName = Path.GetRandomFileName();
                    if (File.Exists("/etc/proxychains.conf")){
                        File.Move("/etc/proxychains.conf", "/etc/proxychains.backup/" + randomFileName + ".conf");
                    } else if (File.Exists("/etc/proxychains4.conf")){
                        File.Move("/etc/proxychains4.conf", "/etc/proxychains.backup/" + randomFileName + "4.conf");
                    }

                    using (StreamWriter newproxychains = File.AppendText("/etc/proxychains.conf")){
                        // Create new proxychains
                        newproxychains.WriteLine("#dynamic_chain\nstrict_chain\n#random_chain\n#chain_len = 2\n#quiet_mode\nproxy_dns\ntcp_read_time_out 15000\ntcp_connect_time_out 8000\n\n[ProxyList]\n");
                        foreach (var pinged in responded){
                            string[] fpinged = pinged.Split(":");
                            string presult = fpinged[0] + " " + fpinged[1];
                            
                            Console.WriteLine(protocol + " " + presult);
                            newproxychains.WriteLine(protocol + " " + presult);
                        }
                    }
                } else {
                    Console.WriteLine("No file found. Porxychains may not be installed on your system, or it is not in /etc/ directory.");
                }

            } else if (choice.Key == ConsoleKey.X && !choice.Modifiers.HasFlag(ConsoleModifiers.Shift) || choice.Key == ConsoleKey.X){
                Console.WriteLine("Exited");
            } else {
                goto Loop;
            }

        }
    }
    static async Task<List<string>> TimeoutTest(string proxy, string timeout){
        var proxyParts = proxy.Split(':');
        if (proxyParts.Length != 2){
            Console.WriteLine($"Invalid proxy format: {proxy}");
        }

        var ip = proxyParts[0];
        List<string> responded = new List<string>();
        try{
            using (var ping = new Ping()){
                int timeoutint = int.Parse(timeout);

                var reply = await ping.SendPingAsync(ip, timeoutint);
                if (reply.Status == IPStatus.Success){
                    // Use when asked for verbose output?
                    // Console.WriteLine($"{proxy} - Ping Success - Response Time: {reply.RoundtripTime} ms");
                    responded.Add(proxy);
                } else {
                    // Console.WriteLine($"{proxy} - Ping Failed - Status: {reply.Status}");
                }
            }
        }
        catch (Exception ex){
            Console.WriteLine($"{proxy} - Ping Error - {ex.Message}");
        }
    return responded;
    }
}

// By Trustyape https://github.com/trustyape
