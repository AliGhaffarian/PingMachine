using System.Text.Json.Nodes;
using System.Net.NetworkInformation;
using System.Net;


// issues : name(adress)
namespace Program.cs
{

    public class Program
    {
        class SourceFailures
        {
            static public void ServersListEmpty()
            {
                Console.WriteLine("No servers found in config.json");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            static public void StabilityTest(List<string> servers)
            {
                if (servers.Count == 0)
                    ServersListEmpty();
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

                if (tripTimeList.Count == 0)
                {
                    Console.WriteLine("Failed");
                    return;
                }

                for (int i = 0; i < tripTimeList.Count; i++)
                    Console.WriteLine(tripTimeList[i]);
            }
            public int FailedTripCount(int pingTimes)
            {
                return pingTimes - ((tripTimeList == null) ? pingTimes : tripTimeList.Count);
            }
        }

        static public int CountLines(StreamReader reader)
        {
            int lineCount = 0;
            while (reader.ReadLine() != null) { lineCount++; }
            return lineCount;
        }

        static public int? Pinger(string server)
        {
            Ping ping = new Ping();
            PingReply pingReply;
            pingReply = ping.Send(server);

            if (pingReply.Status != IPStatus.Success)
                return null;

            return Convert.ToInt32((pingReply.RoundtripTime));
        }
        static public List<int> PingSingleServer(string server, int pingTimes)
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
        static public List<PingData> PingMultipleServers(List<string> servers, int pingTimes)
        {
            Ping ping = new Ping();
            List<PingData> pingDataList = new List<PingData>();
            string server;

            List<int> currentTripTimes;

            for (int i = 0; i < servers.Count; i++)
            {
                server = servers[i];
                string dnsServer = server;

                if (SourceAtt.firstCycle == true)
                    Console.WriteLine("Pinging " + dnsServer);

                currentTripTimes = new List<int>(PingSingleServer(dnsServer, pingTimes));

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

            for (int i = 0; i < ints.Count; i++)
                sum += ints[i];

            return (sum / (ints.Count != 0 ? ints.Count : 1));
        }

        public static void PrintResultOfMultiplePing(List<PingData> pingDataList, int pingTimes)
        {
            Console.WriteLine("Average trip time to servers : ");
            for (int i = 0; i < pingDataList.Count; i++)
            {
                int resultOfFailedTripCount;


                Console.Write(pingDataList[i].serverAdress + " : ");
                Console.Write(pingDataList[i].tripTimeList == null ? "failed to ping" : Average(pingDataList[i].tripTimeList) + "ms.");

                resultOfFailedTripCount = pingDataList[i].FailedTripCount(pingTimes);

                if (resultOfFailedTripCount != 0)
                    Console.Write("(" + resultOfFailedTripCount + " failed ping attemps" + ")");

                Console.WriteLine();
            }
        }

        public struct Config
        {
            public Config()
            {
                servers = new List<string>();
            }
            public List<string> servers;
            public int pingTimes = 0;
            public int delay = 0;
        }

        public static Config ParseConfig(string path)
        {
            var text = File.ReadAllText("./config.json");
            var configJSON = JsonNode.Parse(text);

            Config result = new Config();
            if (configJSON == null)
                return result;

            var serversProperty = configJSON["servers"];
            result.servers = new List<string>();
            for (int i = 0; i < serversProperty!.AsArray().Count(); i++)
                result.servers.Add(serversProperty[i]!.ToString());
            result.pingTimes = Int32.Parse(configJSON["pingTimes"]!.ToString());
            result.delay = Int32.Parse(configJSON["delay"]!.ToString());

            return result;
        }

        public static void Main()
        {
            var config = ParseConfig("./config.json");
            SourceFailures.StabilityTest(config.servers);

            Ping ping = new Ping();
            List<PingData> PingDataList = new List<PingData>();

            while (true)
            {
                PingDataList = new List<PingData>(PingMultipleServers(config.servers, config.pingTimes));

                Console.Clear();

                PrintResultOfMultiplePing(PingDataList, config.pingTimes);

                Thread.Sleep(config.delay);
                SourceAtt.firstCycle = false;
            }
        }
    }
}
