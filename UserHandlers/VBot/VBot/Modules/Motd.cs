﻿using Newtonsoft.Json;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamBotLite
{
    class MotdModule : BaseModule
    {
        public int postCount {get; private set;}
        public int postCountLimit { get; private set; }
        public string message {get; private set;}
        public UserIdentifier setter {get; private set;}

        private BaseTask motdPost;

        public MotdModule(VBot bot, Dictionary<string, object> Jsconfig) : base(bot, Jsconfig)
        {
            // loading config 
            int updateInterval = int.Parse(config["updateInterval"].ToString());

            // loading saved data
            loadPersistentData();

            // loading commands
            commands.Add(new Get(bot, this));
            commands.Add(new Tick(bot, this));
            commands.Add(new Setter(bot, this));
            adminCommands.Add(new Set(bot, this));
            adminCommands.Add(new Remove(bot, this));
            adminCommands.Add(new SetExtended(bot, this));
            adminCommands.Add(new Emulate(bot, this));

            motdPost = new BaseTask(updateInterval, new System.Timers.ElapsedEventHandler(MotdPost));
        }

        public override string getPersistentData()
        {

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("motd", message);
            data.Add("count", postCount.ToString());

            return JsonConvert.SerializeObject(data);
        }

        public override void loadPersistentData()
        {
            try
            {
                Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(this.GetType().Name + ".json"));
                message = data["motd"];
                postCount = int.Parse(data["count"]);
            }
            catch
            {
                postCount = 0;
                postCountLimit = 24;
                motdPost = null;
            }
        }

        /// <summary>
        /// Posts the MOTD to the group chat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MotdPost(object sender, EventArgs e)
        {
            if (message != null && !message.Equals(string.Empty))
            {
                //bot.SteamFriends.SetPersonaName("MOTD");
                userhandler.BroadcastMessageProcessEvent(message);
                
                //bot.SteamFriends.SetPersonaName(bot.DisplayName);
                postCount++;
                if (postCount > postCountLimit)
                    message = null;
                savePersistentData();
            }
        }

        // The abstract command for motd

        abstract public class MotdCommand : BaseCommand
        {
            protected MotdModule motd;

            public MotdCommand(VBot bot, string command, MotdModule motd) : base(bot, command)
            {
                this.motd = motd;
            }
        }

        // The commands

        private class Get : MotdCommand
        {
            public Get(VBot bot, MotdModule motd) : base(bot, "!Motd", motd) { }
            protected override string exec(MessageProcessEventData Msg, string param)
            {
                if (motd.postCount >= motd.postCountLimit){
                    motd.message = null;
                    motd.postCount = 0;
                }

                return motd.message;
            }
        }

        private class Emulate : MotdCommand
        {
            public Emulate(VBot bot, MotdModule motd) : base(bot, "!EmulateMotd", motd) { }
            protected override string exec(MessageProcessEventData Msg, string param)
            {
                
                userhandler.BroadcastMessageProcessEvent(motd.message);
                return "BroadCasted";
            }
        }

        private class Set : MotdCommand
        {
            public Set(VBot bot, MotdModule motd) : base(bot, "!SetMotd", motd) { }
            protected override string exec(MessageProcessEventData Msg, string param)
            {
                if (param == String.Empty)
                    return "Make sure to include a MOTD to display!";
                    
                if (motd.message != null)
                    return "There is currently a MOTD, please remove it first";

                motd.message = param;
                motd.setter = Msg.Sender;
                motd.postCount = 0;
                motd.postCountLimit = 24;
                motd.savePersistentData();
                return "MOTD Set to: " + motd.message;
            }
        }

        private class SetExtended : MotdCommand
        {
            public SetExtended(VBot bot, MotdModule motd) : base(bot, "!setextendedmotd", motd) { }
            protected override string exec(MessageProcessEventData Msg, string param)
            {
                string[] parameters = param.Split(new char[] { ' ' }, 2);

                if (parameters.Length < 2)
                {
                    return "Incorrect syntax, use: !setextendedmotd <days> <motd>";
                }

                if (motd.message != null)
                    return "There is currently a MOTD, please remove it first";

                int Days;
                try
                {
                    Days = int.Parse(parameters[0]);
                }
                catch
                {
                    return "Incorrect syntax, use: !setextendedmotd <Number of Days> <motd>";
                }

                motd.message = parameters[1];
                motd.setter = Msg.Sender;
                motd.postCount = 0;
                motd.postCountLimit = Days * 24;
                motd.savePersistentData();
                return "MOTD Set to: " + motd.message + " | with a post limit of: " + motd.postCountLimit;
            }
        }

        private class Remove : MotdCommand
        {
            public Remove(VBot bot, MotdModule motd) : base(bot, "!RemoveMotd", motd) { }
            protected override string exec(MessageProcessEventData Msg, string param)
            {
                motd.message = null;
                //motd.setter = null; //TODO FIX
                motd.postCount = 0;
                motd.savePersistentData();
                return "Removed MOTD";
            }
        }

        private class Tick : MotdCommand
        {
            public Tick(VBot bot, MotdModule motd) : base(bot, "!TickMotd", motd) { }
            protected override string exec(MessageProcessEventData Msg, string param)
            {
                return "MOTD displayed " + motd.postCount + " times and will display a total of " + motd.postCountLimit + " times in total";
            }
        }

        private class Setter : MotdCommand
        {
            public Setter(VBot bot, MotdModule motd) : base(bot, "!SetterMotd", motd) { }
            protected override string exec(MessageProcessEventData Msg, string param)
            {
                return "MOTD set by " + motd.setter;
            }
        }
    }
}