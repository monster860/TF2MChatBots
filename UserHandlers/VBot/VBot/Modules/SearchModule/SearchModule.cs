﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace SteamBotLite
{
    internal partial class SearchModule : BaseModule
    {
        private List<SearchClassEntry> Searches;

        public SearchModule(VBot bot, Dictionary<string, Dictionary<string, object>> Jsconfig) : base(bot, Jsconfig)
        {
            loadPersistentData();
            foreach (SearchClassEntry Entry in Searches)
            {
                commands.Add(new Search(bot, Entry));
            }
        }

        public override string getPersistentData()
        {
            return JsonConvert.SerializeObject(Searches);
        }

        public override void loadPersistentData()
        {
            try
            {
                Searches = JsonConvert.DeserializeObject<List<SearchClassEntry>>(File.ReadAllText(ModuleSavedDataFilePath()));
            }
            catch
            {
                Console.WriteLine("Error Loading SearchModule");
            }
        }

        public override void OnAllModulesLoaded()
        {
        }
    }
}