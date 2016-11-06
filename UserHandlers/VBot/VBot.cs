using System;
using System.Timers;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace SteamBotLite
{

    class VBot : UserHandler
    {
        public string Username = "V2Bot";
        
        bool Autojoin = true; 

        // Class members
        MotdModule motdModule;
        MapModule mapModule;
        ServerModule serverModule;
        RepliesModule replyModule;
        AdminModule adminmodule;
        SearchModule searchModule;
        ImpNaoModule impnaomodule;
       
        MapWebServer WebServer;

        public UsersModule usersModule;

        public List<BaseModule> ModuleList;

        public List<BaseCommand> chatCommands = new List<BaseCommand>();
        public List<BaseCommand> chatAdminCommands = new List<BaseCommand>();

        // Loading Config
        Dictionary<string, object> jsconfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(System.IO.File.ReadAllText(@"config.json"));

        /// <summary>
        /// Do not try using steamfriends, steamuser and all that since it'll be uninitialised at this point 
        /// </summary>
        /// <param name="SteamConnectionHandler"></param>
        public VBot() 
        {
            Console.WriteLine("Vbot Initialised");
            Console.WriteLine("Loading modules and stuff");
            
            // loading modules

            motdModule = new MotdModule(this, jsconfig);
            mapModule = new MapModule(this, jsconfig);
            serverModule = new ServerModule(this, jsconfig);
            usersModule = new UsersModule(this, jsconfig);
            replyModule = new RepliesModule(this, jsconfig);
            adminmodule = new AdminModule(this, jsconfig);
            searchModule = new SearchModule(this, jsconfig);

            WebServer = new MapWebServer(this, jsconfig);

            ModuleList = new List<BaseModule> { motdModule,mapModule,serverModule,usersModule,replyModule,adminmodule,searchModule, WebServer };
            Console.WriteLine("Modules loaded and ModuleList intitialised");

            OnMaplistchange(mapModule.mapList);

        }

        public override void OnLoginCompleted(object sender, EventArgs e)
        {
            if (Autojoin)
                {
                    base.FireMainChatRoomEvent(ChatroomEventEnum.EnterChat);
                }
            Console.WriteLine("UserHandler: {0} Has Loaded", this.GetType());
        }

        public override void ProcessPrivateMessage(object sender, MessageProcessEventData e) 
        {
            ApplicationInterface AppInterface = (ApplicationInterface)sender;
            e.InterfaceHandlerDestination = AppInterface;
            e.ReplyMessage = ChatMessageHandler(e, e.ReceivedMessage);
            if (e.ReplyMessage != null)
            {
                e.InterfaceHandlerDestination.SendPrivateMessage(this, e);
            }
        }

        public override void ProcessChatRoomMessage(object sender, MessageProcessEventData e)
        {
            e.ReplyMessage = ChatMessageHandler(e, e.ReceivedMessage);
            if (e.ReplyMessage != null)
            {
                e.InterfaceHandlerDestination.SendChatRoomMessage(this, e);
                //AppInterface.SendChatRoomMessage(this, e);
                //base.SendChatRoomMessageProcessEvent(e);
            }
        }

        public void Disablemodule(string ModuleToRemove)
        {
            int x = 0;
            int EntryToRemove = 0;
            bool RemoveModule = false;
            foreach (BaseModule Module in ModuleList)
            {
                Console.WriteLine(Module.GetType().ToString());
                if (Module.GetType().Name.ToString().Equals(ModuleToRemove))
                {
                    EntryToRemove = x;
                    RemoveModule = true;
                }
                x++;
            }
            if (RemoveModule && ModuleList[EntryToRemove].DeletableModule)
            {
                ModuleList[EntryToRemove] = null;
                ModuleList.RemoveAt(EntryToRemove);
            }
        }
        public void Enablemodule(string ModuleToAdd)
        {
            Type T = Type.GetType("SteamBotLite." + ModuleToAdd); //We attempt to translate the string to an existing type
            if ((T.GetType() != null) & (T.BaseType.ToString().Equals("SteamBotLite.BaseModule"))) //Then we check its valid AND if its a base of userhandler
            {                
                BaseModule module =  (BaseModule)Activator.CreateInstance(T, new object[] { this, jsconfig });
                bool AlreadyExists = false;

                foreach (BaseModule ExistingModule in ModuleList)
                {
                    if (ExistingModule.GetType() == module.GetType())
                    {
                        AlreadyExists = true;
                    }
                }
                if (!AlreadyExists)
                {
                    ModuleList.Add(module);
                }
            }
        }


        public string ChatMessageHandler(MessageProcessEventData Msg , string Message)
        {
            string response = null;
            foreach (BaseModule module in ModuleList)
            {
                if (module != null)
                {
                    foreach (BaseCommand c in module.commands)
                    {
                        if (Message.StartsWith(c.command, StringComparison.OrdinalIgnoreCase))
                        {
                            response = c.run(Msg, Message);
                            return response;
                        }
                    }
                }
            }

            if (usersModule.admincheck(Msg.Sender)) //Verifies that it is a moderator, Can you please check if the "ISAdmin" is being used correctly? 
            {
                foreach (BaseModule module in ModuleList)
                {
                    if (module != null)
                    {
                        foreach (BaseCommand c in module.adminCommands)
                            if (Message.StartsWith(c.command, StringComparison.OrdinalIgnoreCase))
                            {
                                response = c.run(Msg, Message);
                                return response;
                            }
                    }
                }
            }
            return response;
        }
        public void OnMaplistchange(ObservableCollection<MapModule.Map> maplist, object sender = null, NotifyCollectionChangedEventArgs args = null)
        {
            base.SetUsernameEventProcess("[" + maplist.Count + "]" + Username);
            if (WebServer != null)
            {
                WebServer.MapListUpdate(maplist);
            }
        }
        public void ServerUpdated(object sender, ServerModule.ServerInfo args)
        {
            Console.WriteLine("Entered VBot");
            if (mapModule != null)
            {
                mapModule.HandleEvent(sender, args);
            }
        }

        public override void ChatMemberInfo(UserIdentifier useridentifier, bool AdminStatus)
        {
            usersModule.updateUserInfo(useridentifier, AdminStatus);
        }
    }
}