using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;


// issues : name(adress)
namespace Program.cs
{

    public class Program
    {
        class SourceFailures
        {
            static public void ServerFileNotFound()
            {
                Console.WriteLine("Failed to find servers.txt file \nplease make sure to read the README.txt");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            static public void StabilityTest(string serversPath)
            {
                StreamReader reader;
                try
                {
                    reader = new StreamReader(serversPath);
                }

                catch(System.IO.FileNotFoundException)
                {
                    SourceFailures.ServerFileNotFound();
                }
            }
        }
        public class SourceAtt
        {
           static public bool firstCycle = true;
        }

        public class PingData
        {
            public List<int> tripTimeList = new List<int>();
            public string serverAdress = "NoAdress";
            public string serverName = "NoName";
            public void Print()
            {
                Console.WriteLine(serverAdress);

                if(tripTimeList.Count == 0)
                {
                    Console.WriteLine("Failed");
                    return;
                }

                for(int i = 0; i < tripTimeList.Count; i++)
                    Console.WriteLine(tripTimeList[i]);
            }
            public int FailedTripCount(int pingTimes)
            {
                return pingTimes - ((tripTimeList == null) ? pingTimes : tripTimeList.Count) ;
            }
        }

        static public int CountLines(StreamReader reader)
        {
            int lineCount = 0;
            while (reader.ReadLine() != null) { lineCount++; }
            return lineCount;
        }

        static public int? Pinger (string server)
        {
            Ping ping = new Ping();
            PingReply pingReply;
            pingReply = ping.Send(server);

            if (pingReply.Status != IPStatus.Success)
                return null;

            return Convert.ToInt32((pingReply.RoundtripTime));
        }
        static public List<int> PingSingleServer (string server, int pingTimes)
        {
            List<int> tripTimeList = new List<int>();
            Ping ping = new Ping();

            int? currentPingTripTime;
            
            for (int i = 0; i < pingTimes; i++)
            {
                currentPingTripTime = Pinger(server);

                if (currentPingTripTime == null)
                    continue;

                tripTimeList.Add(currentPingTripTime.Value);

            }

            return tripTimeList;
        }
        static public List<PingData> PingMultipleServers(string serversPath, int pingTimes)
        {
            Ping ping = new Ping();
            List<PingData> pingDataList = new List<PingData>();
            string? line;
            StreamReader reader;

            List<int> currentTripTimes;

            reader = new StreamReader(serversPath);


            while ((line = reader.ReadLine()) != null)
            {
                string dnsServer = line;

                if (SourceAtt.firstCycle == true)
                    Console.WriteLine("Pinging " + dnsServer);

                currentTripTimes = new List<int>(PingSingleServer(dnsServer, pingTimes));

                Console.WriteLine(dnsServer);
                pingDataList.Add(new PingData
                {
                    tripTimeList = currentTripTimes,
                    serverAdress = Dns.GetHostByName(dnsServer).HostName.ToString(),
                    serverName = Dns.GetHostEntry(dnsServer).HostName.ToString()
                });

                if (SourceAtt.firstCycle == true)
                    Console.Clear();
            }

            return pingDataList;
        }

        static int Average(List<int> ints)
        {
            int sum = 0;

            for(int i = 0; i<ints.Count; i++)
                sum+= ints[i];
            
            return sum/ints.Count != 0 ? ints.Count : 1;
        }

        public static void PrintResultOfMultiplePing(List<PingData> pingDataList, int pingTimes)
        {
            Console.WriteLine("Average trip time to servers : ");
            for(int i = 0;i < pingDataList.Count;i++)
            {
                int resultOfFailedTripCount;
                
               
                Console.Write(pingDataList[i].serverAdress + " : ");
                Console.Write(pingDataList[i].tripTimeList == null? "failed to ping" : Average(pingDataList[i].tripTimeList) + "ms.");

                resultOfFailedTripCount = pingDataList[i].FailedTripCount(pingTimes);

                if(resultOfFailedTripCount != 0)
                    Console.Write("(" + resultOfFailedTripCount + " failed ping attemps" + ")");

                Console.WriteLine();    
            }
        }

        public struct Config {
            public string serversPath;
            public int pingTimes;
            public int delay; 
        }

        public static Config ParseConfig(string path) {
            var text = File.ReadAllText("./config.json");
            var configJSON = JsonDocument.Parse(text).RootElement;
            
            Config result = new Config();

            result.serversPath = configJSON.GetProperty("serverList").ToString();
            result.pingTimes = Int32.Parse(configJSON.GetProperty("pingTimes").ToString());
            result.delay = Int32.Parse(configJSON.GetProperty("delay").ToString());

            return result;
        }

        public static void Main()
        {
            var config = ParseConfig("./config.json");
            SourceFailures.StabilityTest(config.serversPath);

            Ping ping = new Ping();
            List <PingData> PingDataList = new List<PingData>();

            while (true)
            {
                PingDataList = new List<PingData>(PingMultipleServers(config.serversPath, config.pingTimes));

                Console.Clear();

                PrintResultOfMultiplePing(PingDataList, config.pingTimes);
                
                Thread.Sleep(config.delay);
                SourceAtt.firstCycle = false;
            }
        }
    }
}
