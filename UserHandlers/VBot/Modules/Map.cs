using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using SteamKit2;
using Newtonsoft.Json;

namespace SteamBotLite
{
    class MapModule : BaseModule
    {
        // public List<Map> mapList = new List<Map>();  //OLD MAP SYSTEM
        public ObservableCollection<Map> mapList = new ObservableCollection<Map>();


        int MaxMapNumber = 10;
        string ServerMapListUrl;

        public MapModule(VBot bot, Dictionary<string, object> Jsconfig) : base(bot, Jsconfig)
        {
            loadPersistentData();

            ServerMapListUrl = config["ServerMapListUrl"].ToString();
            MaxMapNumber = int.Parse(config["MaxMapList"].ToString());
            Console.WriteLine("URL list is now {0} and maximum map number {1}", ServerMapListUrl, MaxMapNumber);

            userhandler = bot;

            mapList.CollectionChanged += MapChange;

            commands.Add(new Add(bot, this));
            commands.Add(new Maps(bot, this));
            commands.Add(new Update(bot, this));
            commands.Add(new UpdateName(bot, this));
            commands.Add(new Delete(bot, this));
            commands.Add(new UploadCheck(bot, ServerMapListUrl));
            adminCommands.Add(new Wipe(bot, this));
        }

        void MapChange (object sender, NotifyCollectionChangedEventArgs args)
            {
            userhandler.OnMaplistchange(mapList.Count, sender, args);
            }

        public class Map
        {
            public string Submitter { get; set; }
            public string SubmitterName { get; set; }
            public string Filename { get; set; }
            public string DownloadURL { get; set; }
            public string Notes { get; set; }
        }

        public override string getPersistentData()
        {
            return JsonConvert.SerializeObject(mapList);
        }

        public override void loadPersistentData()
        {
            try
            {
                Console.WriteLine("Loading Map List");
                mapList = JsonConvert.DeserializeObject<ObservableCollection<Map>>(System.IO.File.ReadAllText(ModuleSavedDataFilePath()));
                Console.WriteLine("Loaded Map List");
            }
            catch
            {
                Console.WriteLine("Error Loading Map List");
            }
        }

        public void HandleEvent(object sender, ServerModule.ServerInfo args)
        {
            Console.WriteLine("Going to possibly remove {0} Map...", args.currentMap);
            Map map = mapList.FirstOrDefault(x => x.Filename == args.currentMap);


            if (map != null)
            {
                UserIdentifier Submitter = new UserIdentifier(map.Submitter);
                Console.WriteLine("Found map, sending message to {0}", Submitter);
                userhandler.SendPrivateMessageProcessEvent(new MessageProcessEventData(null) { Sender = Submitter, ReplyMessage = string.Format("Map {0} is being tested on the {1} server and has been DELETED.", map.Filename, args.tag)});               
                mapList.Remove(map);
                Console.WriteLine("Map {0} is being tested on the {1} server and has been DELETED.", map.Filename, args.tag);
                savePersistentData();
            }
            Console.Write("...Not Found");
            return;
        }

        // The abstract command for motd

        abstract public class MapCommand : BaseCommand
        {
            protected MapModule MapModule;

            public MapCommand(VBot bot, string command, MapModule mapMod)
                : base(bot, command)
            {
                this.MapModule = mapMod;
            }
        }

        private sealed class UploadCheck : BaseCommand
        {
            string ServerMapListURL;
            public UploadCheck(VBot bot, string Website) : base(bot, "!uploadcheck")
            {
                ServerMapListURL = Website;
            }
            protected override string exec(UserIdentifier sender, string param)
            {
                return SearchClass.CheckDataExistsOnWebPage(ServerMapListURL, param).ToString(); 
            }
        }

        private sealed class UpdateName : BaseCommand
        {
            MapModule mapmodule;
            public UpdateName(VBot bot, MapModule module) : base(bot, "!nameupdate")
            {
                mapmodule = module;
            }
            protected override string exec(UserIdentifier sender, string param)
            {
                NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                userhandler.OnMaplistchange(mapmodule.mapList.Count, sender, args);
                return "Name has been updated";
            }
        }

        // The commands

        private class Add : MapCommand
        {
            
            public bool uploadcheck(string MapName, string Website)
            {
                return SearchClass.CheckDataExistsOnWebPage(Website, MapName); //TODO develop method to check website
            }

            public Add(VBot bot, MapModule mapModule) : base(bot, "!add", mapModule) { }

            protected override string exec(UserIdentifier sender, string param)
            {
                string[] parameters = param.Split(new char[] { ' ' }, 2);

                Map map = new Map();
                map.Submitter = sender.ToString();

                map.SubmitterName = sender.DisplayName;
                map.Filename = parameters[0];
                map.Notes = "No Notes";

                if (parameters[0].Length == 0)
                {
                    return "Invalid parameters for !add. Syntax: !add <mapname> <url> <notes>";
                }

                if (parameters[0].Any(c => char.IsUpper(c)) )
                {
                    return "Your Map is rejected as it includes an uppercase letter";
                }
                if (parameters[0].Length > 27) //TODO make this the actually needed number
                {
                    return "Your Map is rejected for having a filename too long";
                }
                
                if (uploadcheck(map.Filename, MapModule.ServerMapListUrl)) //Check if the map is uploaded
                {
                    map.DownloadURL = "Uploaded";
                    if (parameters.Length > 1)
                    {
                        map.Notes = parameters.Last();
                    }
                }
                else if (parameters.Length > 1) //If its not uploaded check if a URL was there
                {
                    parameters = param.Split(new char[] { ' ' }, 3);

                    map.DownloadURL = parameters[1];
                    if (parameters.Length > 2)
                    {
                        map.Notes = parameters.Last();
                    }
                }
                else //If a url isn't there lets return an error
                {
                    return "Your map isn't uploaded! Please use include the url with the syntax: !add <mapname> <url> (notes)";
                }

                MapModule.mapList.Add(map);
                MapModule.savePersistentData();
                return string.Format("Map '{0}' added.", map.Filename);

            }
        }

        private class Maps : MapCommand
        {
            public Maps(VBot bot, MapModule mapMod) : base(bot, "!maps", mapMod) { }
            protected override string exec(UserIdentifier sender, string param)
            {
                var maps = MapModule.mapList;
                int maxMaps = MapModule.MaxMapNumber;
                string chatResponse = "";
                string pmResponse = "";

                // Take the max number of maps.
                var mapList = maps
                    .Take(maxMaps)
                    .ToList();

                if (maps.Count == 0)
                {
                    chatResponse = "The map list is empty.";
                }
                else
                {
                    // Build the chat response.
                    chatResponse = string.Join(" , ", mapList.Select(x => x.Filename));
                    if (maps.Count > maxMaps)
                        chatResponse += string.Format(" (and {0} more...)", maps.Count - maxMaps);

                    // Build the private response.
                    pmResponse = "";
                    for (int i = 0; i < maps.Count; i++)
                    {
                        string mapLine = string.Format("{0} // {1} // {2} ({3})", maps[i].Filename, maps[i].DownloadURL , maps[i].SubmitterName, maps[i].Submitter);

                        if (!string.IsNullOrEmpty(maps[i].Notes))
                            mapLine += "\nNotes: " + maps[i].Notes;

                        if (i < maps.Count - 1)
                            mapLine += "\n";

                        pmResponse += mapLine;
                    }
                }

                // PM map list to the caller.
                if (maps.Count != 0)
                {
                    userhandler.SendPrivateMessageProcessEvent(new MessageProcessEventData(null) { Sender = sender, ReplyMessage = pmResponse });
                }

                return chatResponse;
            }
        }

        private class Update : MapCommand
        {
            public Update(VBot bot, MapModule mapMod) : base(bot, "!update", mapMod) { }
            protected override string exec(UserIdentifier sender, string param)
            {
                string[] parameters = param.Split(' ');

                if (parameters.Length < 1)
                {
                    return string.Format("Invalid parameters for !update. Syntax: !update <mapname> (url)");
                }
                else
                {
                    Map editedMap = MapModule.mapList.Where(x => x.Filename.Equals(parameters[0])).FirstOrDefault(); //Needs to be tested
                    // Map editedMap = MapModule.mapList.Find(map => map.filename.Equals(parameters[0])); //OLD Map CODE
                    if (editedMap.Submitter.Equals(sender.ToString()))
                    {
                        MapModule.mapList.Remove(editedMap);

                        editedMap.Filename = parameters[1];
                        if (parameters.Length > 2)
                            editedMap.DownloadURL = parameters[2];
                        MapModule.mapList.Add(editedMap);
                        MapModule.savePersistentData();
                        return string.Format("Map '{0}' has been edited.", editedMap.Filename);
                    }
                    else
                    {
                        return string.Format("You cannot edit map '{0}' as you did not submit it.", editedMap.Filename);
                    }
                }
            }
        }

        private class Delete : MapCommand
        {
            public Delete(VBot bot, MapModule mapMod) : base(bot, "!delete", mapMod) { }
            protected override string exec(UserIdentifier sender, string param)
            {
                string[] parameters = param.Split(' ');

                if (parameters.Length > 0)
                {
                    Map deletedMap = MapModule.mapList.FirstOrDefault(x => x.Filename == parameters[0]);

                    if (deletedMap == null)
                    {
                        return string.Format("Map '{0}' was not found.", parameters[0]);
                    }
                    else
                    {
                        if ((deletedMap.Submitter.Equals(sender.ToString())) || (userhandler.usersModule.admincheck(sender)))
                        {
                            MapModule.mapList.Remove(deletedMap);
                            MapModule.savePersistentData();
                            return string.Format("Map '{0}' DELETED.", deletedMap.Filename);
                        }
                        else
                        {
                            return string.Format("You do not have permission to edit map '{0}'.", deletedMap.Filename);
                        }
                    }
                }
                return "Invalid parameters for !delete. Syntax: !delete <mapname>";
            }

        }

        private class Wipe : MapCommand
        {
            public Wipe(VBot bot, MapModule mapMod) : base(bot, "!wipe", mapMod) { }
            protected override string exec(UserIdentifier sender, string param)
            {
                MapModule.mapList.Clear();
                //MapModule.mapList = new List<Map>(); //OLd Maplist code
                MapModule.savePersistentData();
                return "The map list has been DELETED.";
            }
        }
    }
}
