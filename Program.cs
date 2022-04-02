using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpOSC;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace TulaOSC
{
    class Program
    {
        private static DateTime time;
        private static float Minutos;
        private static float Horas;
        
        static void Main(string[] args)
        {
            getTime();
        }

        private static void getTime() {

                System.IO.StreamReader r = new StreamReader("./config.json");
                string jsonString = r.ReadToEnd();
                jsonConfig m = JsonConvert.DeserializeObject<jsonConfig>(jsonString);

                while (true) {
                    Thread.Sleep(10000);
                    time = DateTime.Now;
                    Minutos = time.Minute;
                    Horas = time.Hour;
                    var message = new SharpOSC.OscMessage("/avatar/parameters/timeH",Horas/25);
                    var sender = new SharpOSC.UDPSender(m.localhost, m.port);
                    sender.Send(message);
                    message = new SharpOSC.OscMessage("/avatar/parameters/timeM",Minutos/200);
                    sender.Send(message);
                    Console.WriteLine("Enviado: "+Horas+":"+Minutos);
                }
        }
    }

    public class jsonConfig {
        public string localhost { get; set; }
        public string widgetId { get; set; }
        public int port { get; set; }


    }
}
