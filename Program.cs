using System;
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

            static public void StabilityTest()
            {
                StreamReader? reader;
                try
                {
                    reader = new StreamReader(SourceConfigs.fileName);
                }

                catch(System.IO.FileNotFoundException)
                {
                    SourceFailures.ServerFileNotFound();
                }
            }
        }
        public struct SourceConfigs
        {
            static public string fileName = "servers.txt";
            static public  int PINGTIMES = 10;
            static public  int DELAY = 1000;
        }
        public struct SourceAtt
        {
           static public bool firstCycle = true;
        }

        public struct PingData
        {
            public List<int>? tripTimeList;
            public string serverAdress = "NoAdress";
            public string serverName = "NoName";
            public void Print()
            {
                Console.WriteLine(serverAdress);

                if(tripTimeList == null)
                {
                    Console.WriteLine("Failed");
                    return;
                }

                for(int i = 0; i < tripTimeList.Count; i++)
                    Console.WriteLine(tripTimeList[i]);
            }
            public int FailedTripCount()
            {
                return SourceConfigs.PINGTIMES - ((tripTimeList == null) ? SourceConfigs.PINGTIMES : tripTimeList.Count) ;
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
        static public List<int> PingSingleServer (string server)
        {
            List<int> tripTimeList = new List<int>();
            Ping ping = new Ping();

            int? currentPingTripTime;
            
            for (int i = 0; i < SourceConfigs.PINGTIMES; i++)
            {
                currentPingTripTime = Pinger(server);

                if (currentPingTripTime == null)
                    continue;

                tripTimeList.Add(currentPingTripTime.Value);

            }

            return tripTimeList;
        }
        static public List<PingData> PingMultipleServers ()
        {
            Ping ping = new Ping();
            List<PingData> pingDataList = new List<PingData>();
            string line;
            StreamReader reader;

            List<int>? currentTripTimes;

            reader = new StreamReader(SourceConfigs.fileName);


            while ((line = reader.ReadLine()) != null)
            {
                string dnsServer = line;

                if (SourceAtt.firstCycle == true)
                    Console.WriteLine("Pingings " + dnsServer);

                currentTripTimes = new List<int>(PingSingleServer(dnsServer));

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

        public static void PrintResultOfMultiplePing(List<PingData> pingDataList)
        {
            Console.WriteLine("Average trip time to servers : ");
            for(int i = 0;i < pingDataList.Count;i++)
            {
                int resultOfFailedTripCount;
                
               
                Console.Write(pingDataList[i].serverAdress + " : ");
                Console.Write(pingDataList[i].tripTimeList == null? "failed to ping" : Average(pingDataList[i].tripTimeList) + "ms.");

                resultOfFailedTripCount = pingDataList[i].FailedTripCount();

                if(resultOfFailedTripCount != 0)
                    Console.Write("(" + resultOfFailedTripCount + " failed ping attemps" + ")");

                Console.WriteLine();    
            }
        }

        public static void Main()
        {
            SourceFailures.StabilityTest();

            Ping ping = new Ping();
            
            List <PingData> PingDataList = new List<PingData>();


           

            while (true)
            {

                PingDataList = new List<PingData>(PingMultipleServers());

                Console.Clear();

                PrintResultOfMultiplePing(PingDataList);
                
                Thread.Sleep(SourceConfigs.DELAY);
                SourceAtt.firstCycle = false;
            }
        }
    }
}