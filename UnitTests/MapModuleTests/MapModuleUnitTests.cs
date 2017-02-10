﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamBotLite;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MapModuleTests
{

    abstract class MapModuleTest
    {
        public MapModuleTest()
        {
            
        }
    }
    [TestClass]
    public class SyntaxUnitTests 
    {
        string AddCommand = "!add";
        string DeleteCommand = "!delete";
        string UpdateCommand = "!update";
        string WipeCommand = "!wipe";
        string Mapname = "mapname";
        string url = "http://URL";
        string notes = "these are notes";
        MapModule module;

        string identifier = "0";
        ChatroomEntity TestUser;
        

        public SyntaxUnitTests() {
            TestUser = new User(identifier, null);
            module = new MapModule(new TestUserHandler(), MakeConfig());
            
        }

        Dictionary<string, object> MakeConfig()
        {
            Dictionary<string, object> MapModuleConfig = new Dictionary<string, object>();

            MapModuleConfig.Add("ServerMapListUrl", new ObservableCollection<Map>());
            MapModuleConfig.Add("MaxMapList", 5);
            
            return MapModuleConfig;
        }

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void Initialize()
        {
            module = new MapModule(new TestUserHandler(), MakeConfig());
            TestUser = new User(identifier, null);
            module = new MapModule(new TestUserHandler(), MakeConfig());
        }

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void Cleanup()
        {
            module.ClearMapListWithMessage("Test Wipe");
            module = new MapModule(new TestUserHandler(), MakeConfig());
            Assert.IsTrue(module.mapList.GetSize() == 0);
        }



        [TestMethod]
        public void RegularSyntax()
        {
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand  +" "+  Mapname  +" "+  url +" "+  notes;
            Message.Sender = TestUser;

            Console.WriteLine(FireCommand(Message , module));

            Map TestMap = module.mapList.GetMap(0);
            
            Assert.AreEqual(TestMap.Filename, Mapname);
            Assert.AreEqual(TestMap.DownloadURL, url);
            Assert.AreEqual(TestMap.Notes, notes);
            Assert.AreEqual(TestMap.Submitter, identifier);

            Assert.AreNotEqual(TestMap.Filename, Mapname + 1); //Ensure that its a string check
        }

        [TestMethod]
        public void CheckPersistance()
        {
            RegularSyntax();
            module = module = new MapModule(new TestUserHandler(), MakeConfig());

            Map TestMap = module.mapList.GetMap(0);

            Assert.AreEqual(TestMap.Filename, Mapname);
            Assert.AreEqual(TestMap.DownloadURL, url);
            Assert.AreEqual(TestMap.Notes, notes);
            Assert.AreEqual(TestMap.Submitter, identifier);

            Assert.AreNotEqual(TestMap.Filename, Mapname + 1); //Ensure that its a string check
        }

        [TestMethod]
        public void CheckPersistanceOfExtraData()
        {
            string command = "!forceuploaded" + " " + "True" + " " + "Map Not Uploaded";
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = command;
            Message.Sender = TestUser;
            
            FireAdminCommand(Message,module);

            Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname + " " + url + " " + notes;
            Message.Sender = TestUser;

            FireCommand(Message, module);

            AssertMaplistSize(0);

            module = module = new MapModule(new TestUserHandler(), MakeConfig());
            
            
            command = "!forceuploaded" + " " + "false" + " " + "Map Not Uploaded";
            Message.ReceivedMessage = command;
            FireAdminCommand(Message, module);
            Assert.IsFalse(module.mapList.AllowOnlyUploadedMaps);
            
            module = module = new MapModule(new TestUserHandler(), MakeConfig());

            RegularSyntax();

            AssertMaplistSize(1);


        }

        [TestMethod]
        public void DeleteMap()
        {
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = DeleteCommand + " " + Mapname;
            Message.Sender = TestUser;

            RegularSyntax();
            
            FireCommand(Message, module);
            AssertMaplistSize(0);
        }

        [TestMethod]
        public void AdminDeleteMap()
        {
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = DeleteCommand + " " + Mapname;

            Message.Sender = new User(TestUser.identifier.ToString() + 1, null);
            Message.Sender.Rank = ChatroomEntity.AdminStatus.True;

            RegularSyntax();

            Console.WriteLine(FireCommand(Message, module));
            AssertMaplistSize(0);
        }

        [TestMethod]
        public void NonAdminDeleteMapFail()
        {
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = DeleteCommand + " " + Mapname;

            Message.Sender = new User(TestUser.identifier.ToString() + 1, null);
            Message.Sender.Rank = ChatroomEntity.AdminStatus.False;

            RegularSyntax();

            FireCommand(Message, module);
            AssertMaplistSize(1);
        }

        [TestMethod]
        public void DeleteSpecificMapFIFO()
        {
            MessageEventArgs Message = new MessageEventArgs(null);
            
            Message.Sender = TestUser;
            RegularSyntax();
            string SecondMapName = Mapname + 2;
            Message.ReceivedMessage = AddCommand + " " + SecondMapName + " " + url + " " + notes;
            FireCommand(Message, module);

            Map FirstTestMap = module.mapList.GetMap(0);
            Assert.AreEqual(FirstTestMap.Filename, Mapname);

            Map SecondTestMap = module.mapList.GetMap(1);
            Assert.AreEqual(SecondTestMap.Filename, SecondMapName);

            Message.ReceivedMessage = DeleteCommand + " " + SecondMapName;

            FireCommand(Message, module);

            FirstTestMap = module.mapList.GetMap(0);
            Assert.AreEqual(FirstTestMap.Filename, Mapname);
        }

        [TestMethod]
        public void DeleteSpecificMapTestLIFO()
        {
            MessageEventArgs Message = new MessageEventArgs(null);

            Message.Sender = TestUser;
            RegularSyntax();
            string SecondMapName = Mapname + 2;
            Message.ReceivedMessage = AddCommand + " " + SecondMapName + " " + url + " " + notes;
            FireCommand(Message, module);

            Map FirstTestMap = module.mapList.GetMap(0);
            Assert.AreEqual(FirstTestMap.Filename, Mapname);

            Map SecondTestMap = module.mapList.GetMap(1);
            Assert.AreEqual(SecondTestMap.Filename, SecondMapName);

            Message.ReceivedMessage = DeleteCommand + " " + Mapname;

            FireCommand(Message, module);

            SecondTestMap = module.mapList.GetMap(0);
            Assert.AreEqual(SecondTestMap.Filename, SecondMapName);
        }

        void AddNumberOfMaps(int numberofmaps)
        {
            for (int i = 0; i < numberofmaps; i++)
            {
                MessageEventArgs Message = new MessageEventArgs(null);
                Message.ReceivedMessage = AddCommand + " " + Mapname + i + " " + url + i + " " + notes + i;
                Message.Sender = TestUser;
                Message.Sender.identifier += i.ToString();
                FireAdminCommand(Message, module);
            }
           
        }

        [TestMethod]
        public void WipeMapList()
        {
            AddNumberOfMaps(50);

            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = WipeCommand;
            Message.Sender = new User(1, null);

            Message.Sender.Rank = ChatroomEntity.AdminStatus.True;

            FireCommand(Message, module);

            AssertMaplistSize(0);
        }


        string FireCommand (MessageEventArgs Message , BaseModule module)
        {
            string param = " ";

            foreach (BaseCommand c in module.commands)
            {
                if (c.CheckCommandExists(Message, Message.ReceivedMessage))
                {
                    param = c.run(Message, Message.ReceivedMessage);
                }
            }

            
            return param;
        }

        string FireAdminCommand(MessageEventArgs Message, BaseModule module)
        {
            string param = " ";

            foreach (BaseCommand c in module.adminCommands)
            {
                if (c.CheckCommandExists(Message, Message.ReceivedMessage))
                {
                    param = c.run(Message, Message.ReceivedMessage);
                }
            }


            return param;
        }
        [TestMethod]
        public void NoURL()
        {

            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname;
            Message.Sender = TestUser;

            FireCommand(Message, module);

            AssertMaplistSize(0);
        }

        [TestMethod]
        public void NoData()
        {

            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand;
            Message.Sender = TestUser;

            FireCommand(Message, module);

            AssertMaplistSize(0);
        }

        [TestMethod]
        public void ExtraSpacing()
        {

            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + "   " + Mapname + "   " + url + "          " + notes;
            Message.Sender = TestUser;

            Console.WriteLine(FireCommand(Message, module));

            Map TestMap = module.mapList.GetMap(0);

            Assert.AreEqual(TestMap.Filename, Mapname);
            Assert.AreEqual(TestMap.DownloadURL, url);
            Assert.AreEqual(TestMap.Notes, notes);
            Assert.AreEqual(TestMap.Submitter, identifier);

            Assert.AreNotEqual(TestMap.Filename, Mapname + 1); //Ensure that its a string check
        }

        [TestMethod]
        public void UploadedMap()
        {
            module.SubstituteWebPageWithString(Mapname);

            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname;
            Message.Sender = TestUser;

            Console.WriteLine(FireCommand(Message, module));

            Map TestMap = module.mapList.GetMap(0);

            Assert.AreEqual(TestMap.Filename, Mapname);

            Assert.AreNotEqual(TestMap.Filename, Mapname + 1); //Ensure that its a string check
        }

        [TestMethod]
        public void UploadedMapAndNote()
        {
            module.SubstituteWebPageWithString(Mapname);

            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname + " " + notes;
            Message.Sender = TestUser;

            Console.WriteLine(FireCommand(Message, module));

            Map TestMap = module.mapList.GetMap(0);

            Assert.AreEqual(TestMap.Filename, Mapname);
            Assert.AreEqual(TestMap.Notes, notes);
            Assert.AreEqual(TestMap.Submitter, identifier);

            Assert.AreNotEqual(TestMap.Filename, Mapname + 1); //Ensure that its a string check
        }

        void AssertMaplistSize(int i)
        {
            Assert.IsTrue(module.mapList.GetSize() == i);
        }
        

        [TestMethod]
        public void MapNotUploaded()
        {
            module.SubstituteWebPageWithString("NULL");

            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname;
            Message.Sender = TestUser;

            Console.WriteLine(FireCommand(Message, module));

            AssertMaplistSize(0);
        }

        [TestMethod]
        public void UpdateMapAllProperties()
        {
            string NewMapName = "foo";


            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname + " " + url + " " + notes;
            Message.Sender = TestUser;

            FireCommand(Message, module);

            string NewURL = url + 2;
            string NewNote = notes + 2;

            Message.ReceivedMessage = UpdateCommand + " " + Mapname + " " + NewMapName + " " + NewURL + " " + NewNote;

            Console.WriteLine(FireCommand(Message, module));

            Map TestMap = module.mapList.GetMap(0);

            Assert.AreEqual(NewMapName, TestMap.Filename );
            Assert.AreEqual(NewURL , TestMap.DownloadURL);
            Assert.AreEqual(NewNote , TestMap.Notes );
            Assert.AreEqual(identifier , TestMap.Submitter );
        }
        [TestMethod]
        public void Update_RejectNoURL()
        {
            string NewMapName = "foo";

            module.SubstituteWebPageWithString(Mapname);
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname + " " + url + " " + notes;
            Message.Sender = TestUser;

            FireCommand(Message, module);

            string NewURL = url + 2;

            Message.ReceivedMessage = UpdateCommand + " " + Mapname + " " + NewMapName;

            Console.WriteLine(FireCommand(Message, module));

            Map TestMap = module.mapList.GetMap(0);

            Assert.AreEqual(TestMap.Filename, Mapname);
        }

        [TestMethod]
        public void UpdateMapNameOnlyOldOneUploaded()
        {
            string NewMapName = "foo";

            module.SubstituteWebPageWithString(Mapname);
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname + " " + notes;
            Message.Sender = TestUser;

            FireCommand(Message, module);

            string NewURL = url + 2;

            Message.ReceivedMessage = UpdateCommand + " " + Mapname + " " + NewMapName + " " + NewURL;

            Console.WriteLine(FireCommand(Message, module));

            Map TestMap = module.mapList.GetMap(0);

            Assert.AreEqual(TestMap.Filename, NewMapName);
            Assert.AreEqual(TestMap.DownloadURL, NewURL);

            Assert.IsFalse(TestMap.Uploaded);
            Assert.AreEqual(TestMap.Submitter, identifier);
        }
        //First map isn't uploaded, so it needs a url, but the second map is uploaded and needs a url
        [TestMethod]
        public void UpdateMapOnlyNewOneUploaded()
        {
            string NewMapName = "foo";

            module.SubstituteWebPageWithString(NewMapName);
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname + " " + url + " " + notes;
            Message.Sender = TestUser;

            FireCommand(Message, module);

            string NewURL = url + 2;

            Message.ReceivedMessage = UpdateCommand + " " + Mapname + " " + NewMapName;

            Console.WriteLine(FireCommand(Message, module));

            Map TestMap = module.mapList.GetMap(0);

            Assert.AreEqual(TestMap.Filename, NewMapName);
            Assert.IsNull(TestMap.DownloadURL);
            Assert.IsTrue(TestMap.Uploaded);
            
            Assert.AreEqual(TestMap.Submitter, identifier);
        }

        //First map isn't uploaded, so it needs a url, but the second map is uploaded and needs a url
        [TestMethod]
        public void RejectNewUpdateAsMapIsntUploaded()
        {
            string NewMapName = "foo";

            module.SubstituteWebPageWithString(Mapname);
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + Mapname + " " + url + " " + notes;
            Message.Sender = TestUser;

            FireCommand(Message, module);

            string NewURL = url + 2;

            Message.ReceivedMessage = UpdateCommand + " " + Mapname + " " + NewMapName;

            Console.WriteLine(FireCommand(Message, module));

            Map TestMap = module.mapList.GetMap(0);

            Assert.AreEqual(TestMap.Filename, Mapname);
        }
        [TestMethod]
        public void UpdateSingleMap()
        {
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.Sender = TestUser;
            for (int i = 0; i < 3; i++)
            {
                Message.ReceivedMessage = AddCommand + " " + Mapname + i + " " + url + " " + notes;
                Console.WriteLine(FireCommand(Message, module));
            }


            string NewMapName = "newmap";
            string NewURL = "HTTP://NEWURL";
            string NewNote = "NEW NOTES";

            Message.ReceivedMessage = UpdateCommand + " " + Mapname + 1 + " " + NewMapName + " " + NewURL + " " + NewNote;

            FireCommand(Message, module);

            Map TestMap = module.mapList.GetMap(1);

            Assert.AreEqual(TestMap.Filename, NewMapName);
            Assert.AreEqual(TestMap.DownloadURL, NewURL);
            Assert.AreEqual(TestMap.Notes, NewNote);
            Assert.AreEqual(TestMap.Submitter, identifier);
        }
        [TestMethod]
        public void NoDoubling()
        {
            RegularSyntax();
            RegularSyntax();

            AssertMaplistSize(1);

        }

        [TestMethod]
        public void NoCapitals()
        {
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + "MAPNAME" + " " + url + " " + notes;
            Message.Sender = TestUser;

            AssertMaplistSize(0);

        }
        [TestMethod]
        public void SmallerThan27Chars()
        {
            MessageEventArgs Message = new MessageEventArgs(null);
            Message.ReceivedMessage = AddCommand + " " + "mapnamemapnamemapnamemapnamemapnamemapnamemapnamemapnamemapnamemapnamemapnamemapnamemapname" + " " + url + " " + notes;
            Message.Sender = TestUser;

            AssertMaplistSize(0);
        }


    }
}
