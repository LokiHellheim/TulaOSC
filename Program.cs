using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpOSC;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using WebSocketSharp;

namespace TulaOSC
{
    class Program
    {
        private static DateTime time;
        private static float Minutos;
        private static float Horas;
        private static string localhost;
        private static int port;
        private static string wsUrl;
        static void Main(string[] args)
        {
            StreamReader r = new StreamReader("./config.json");
            string jsonString = r.ReadToEnd();
            jsonConfig m = JsonConvert.DeserializeObject<jsonConfig>(jsonString);
            localhost = m.localhost;
            port = m.port;
            wsUrl = m.wsUrl;

            getHR();
            getTime();

        }

        private static void getTime()
        {

            while (true)
            {
                Thread.Sleep(10000);
                time = DateTime.Now;
                Minutos = time.Minute;
                Horas = time.Hour;
                var message = new OscMessage("/avatar/parameters/timeH", Horas / 25);
                var sender = new UDPSender(localhost, port);
                sender.Send(message);
                message = new OscMessage("/avatar/parameters/timeM", Minutos / 200);
                sender.Send(message);
                Console.WriteLine("Enviado: " + Horas + ":" + Minutos);
            }
        }
        private static void getHR()
        {
            var ws = new WebSocket(wsUrl);


                ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            ws.OnMessage += onMessageWS;

            ws.Connect();

          

        }

        private static void onMessageWS(object sender, MessageEventArgs e)
        {
            jsonStromno m = JsonConvert.DeserializeObject<jsonStromno>(e.Data.ToString());
            Console.WriteLine("Heart Rate : " + m.data.heartRate);

            var message = new SharpOSC.OscMessage("/avatar/parameters/hr_connected", true);
            var s = new SharpOSC.UDPSender(localhost, port);
            s.Send(message);
            message = new SharpOSC.OscMessage("/avatar/parameters/hr_percent", m.data.heartRate / 200);
            s.Send(message);

        }
    }

    public class jsonConfig
    {
        public string localhost { get; set; }
        public string widgetId { get; set; }
        public int port { get; set; }
        public string wsUrl { get; set; }
    }
    public class jsonStromno
    {
        public string timestamp { get; set; }
        public jsonHR data { get; set; }
    }
    public class jsonHR
    {
        public float heartRate { get; set; }
    }

}
