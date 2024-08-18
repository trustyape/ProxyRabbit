```  ___                    ___      _    _    _ _   
 | _ \_ _ _____ ___  _  | _ \__ _| |__| |__(_) |_   (\(\ 
 |  _/ '_/ _ \ \ / || | |   / _` | '_ \ '_ \ |  _|  ( -.-)
 |_| |_| \___/_\_\_, | |_|_\__,_|_.__/_.__/_|\__|  o_(")(")
                  |__/  
Usage: proxyrabbit [protocol] [timeout] [country] [ssl] [anonymity]
Options:
  -h             Display help information
  protocol       Protocol used: http, socks4, socks5, all
  timeout        Timeout in miliseconds, maximum 10000ms, 30-200 optimal
  country        Alpha 2 ISO country code or 'all' (like US for USA, RU for Russian Federation etc)
  ssl            Should the proxies support SSL? (yes, no, all)
  anonymity      Define which anonymity level the proxies should have:
                         elite, anonymous, transparent, all

example: ./proxyrabbit http 100 RU all all

Description: ProxyRabbit will scrape available proxies, test them, and store them in proxychains.conf file for you.
Depending on your choice, program will add proxies to existing file, display them in console, or backup old proxychains.conf and create minimal one for you with new proxies.```
