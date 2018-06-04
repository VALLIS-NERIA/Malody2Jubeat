using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.IO;
using System.ComponentModel;
using mc2eve;

namespace mc2eve {


    class Program {

        static void Main(string[] args) {
            DirectoryInfo d = new DirectoryInfo(Directory.GetCurrentDirectory());
            string fileinput = d.GetFiles(@"INPUT\" + "*.mc")[0].Name;
            string soundinput = d.GetFiles(@"INPUT\" + "*.ogg")[0].Name;
            MalodyChart mc = JsonConvert.DeserializeObject<MalodyChart>(File.ReadAllText(@"INPUT\" + fileinput), new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate });
            TagLib.File f = TagLib.File.Create(@"INPUT\" + soundinput, TagLib.ReadStyle.Average);
            int length = (int)f.Properties.Duration.TotalSeconds;
            eveChart ec = new eveChart(mc, length);
            FileStream fs = new FileStream(@"OUTPUT\" + fileinput.Split('.')[0] + ".eve", FileMode.Create, FileAccess.ReadWrite);
            ec.Write(fs);
            fs.Close();
        }
    }
}
