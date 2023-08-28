using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace VolvoWrench.Demo_stuff.GoldSource.Verify
{
    public class Config
    {
        public string bxt_version = "";

        public List<Tuple<String, Commandtype>> BaseRules;
        public List<Cvar> BaseCvarRules;

        public List<Category> categories;

        public Config(string file)
        {
            BaseRules = new List<Tuple<string, Commandtype>>();
            categories = new List<Category>();
            BaseCvarRules = new List<Cvar>();
            JObject jsonfile = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(file));
            bxt_version = (string)jsonfile["bxt_version"];
            var cats = (JArray)jsonfile["categories"];
            foreach(var rule in (JArray)jsonfile["base_command_rules"])
            {
                if(rule["rule"] != null)
                {
                    BaseRules.Add(new Tuple<string, Commandtype>(rule["command"].ToString(),
                    (Commandtype)Enum.Parse(typeof(Commandtype), rule["rule"].ToString())));
                }
                // create a separate json object "base_cvar_rules" like i did before?
                // to avoid confusion ofc
                else
                {
                    BaseCvarRules.Add(new Cvar(rule as JObject));
                }
            }
            foreach(var category in cats)
            {
                Category c = new Category();
                if(category["command_rules"] != null)
                {
                    foreach (var rule in (JArray)category["command_rules"])
                    {
                        c.CommandRules.Add(new Tuple<string, Commandtype>(rule["command"].ToString(),
                            (Commandtype)Enum.Parse(typeof(Commandtype), rule["rule"].ToString())));
                    }
                }
                if(category["cvar_rules"] != null)
                {
                    foreach (var rule in (JArray)category["cvar_rules"])
                    {
                        c.CvarRules.Add(new Cvar(rule as JObject));
                    }
                }
                c.name = category["name"].ToString();
                categories.Add(c);
            }
        }
    }
}
