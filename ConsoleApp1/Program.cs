namespace ConsoleApp1 {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Malody2Eve;

    class Program {

        //static void Main(string[] args) {
        //    DirectoryInfo d = new DirectoryInfo(Directory.GetCurrentDirectory());
        //    string fileinput = d.GetFiles(@"INPUT\" + "*.mc")[0].Name;
        //    string soundinput = d.GetFiles(@"INPUT\" + "*.ogg")[0].Name;
        //    MalodyChart mc = JsonConvert.DeserializeObject<MalodyChart>(File.ReadAllText(@"INPUT\" + fileinput), new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate });
        //    TagLib.File f = TagLib.File.Create(@"INPUT\" + soundinput, TagLib.ReadStyle.Average);
        //    int length = (int)f.Properties.Duration.TotalSeconds;
        //    EveChart ec = new EveChart(mc, length);
        //    FileStream fs = new FileStream(@"OUTPUT\" + fileinput.Split('.')[0] + ".eve", FileMode.Create, FileAccess.ReadWrite);
        //    ec.Write(fs);
        //    fs.Close();
        //}

        static void Main(string[] args) {
            while (true) {
                try {
                    Console.WriteLine("File name:");
                    var fn = Console.ReadLine();
                    Console.WriteLine("Length (seconds):");
                    var time = int.Parse(Console.ReadLine());
                    var mc = MalodyChart.FromFile(fn);
                    var eve = EveChart.FromMalodySeconds(mc, time);
                    File.WriteAllText(fn + ".eve", eve.ToString());
                }
                catch (Exception e) {
                    Console.WriteLine("Exception: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
