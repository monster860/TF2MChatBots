﻿using SteamBotLite.ApplicationInterfaces.HTTP_Discord;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SteamBotLite
{
    internal class Program
    {
        public static void AssignConnection(UserHandler userhandler, ApplicationInterface applicationinterface)
        {
            userhandler.AssignAppInterface(applicationinterface);
            applicationinterface.AssignUserHandler(userhandler);
        }

        public void DoWork(ApplicationInterface Bot)
        {
            bool Running = true;
            while (Running)
            {
                Bot.tick();
            }
        }

        private static void Main(string[] args)
        {
            

            //Create userHandlers//
            List<UserHandler> UserHandlers = new List<UserHandler>();
            Console.WriteLine("RUNNING");
            ConsoleUserHandler consolehandler = new ConsoleUserHandler();
            MediaBot MediaHandler = new MediaBot();
            VBot VbotHandler = new VBot();
            GhostChecker ghostchecker = new GhostChecker();

            // Create Interfaces//
            List<ApplicationInterface> Bots = new List<ApplicationInterface>();

            ConsoleInterface DebugInterface = new ConsoleInterface();
            
            Console.WriteLine("Would you like to run the console? Y/N");

            /*
            if (Console.ReadLine().Equals("Y"))
            {
                bool RunConsole = true;

                while (RunConsole)
                {
                    MessageEventArgs Msg = new MessageEventArgs(DebugInterface);
                    Msg.Sender = new ChatroomEntity("Console", DebugInterface);
                    Msg.ReceivedMessage = Console.ReadLine();
                    Msg.Sender.Rank = ChatroomEntity.AdminStatus.True;
                    VbotHandler.ProcessPrivateMessage(DebugInterface, Msg);
                    Msg.InterfaceHandlerDestination = DebugInterface;

                    if (Msg.ReceivedMessage.Equals("Exit"))
                    {
                        RunConsole = false;
                    }
                }
            }
            */

            HttpInterface Test_Bot = new HttpInterface();
            Bots.Add(Test_Bot);
            AssignConnection(VbotHandler, Test_Bot);


            SteamAccountVBot SteamPlatformInterface = new SteamAccountVBot();
            
            Bots.Add(SteamPlatformInterface);
            AssignConnection(consolehandler, SteamPlatformInterface);
            //DiscordAccountVBot DiscordPlatformInterfaceRelay = new DiscordAccountVBot();
            //Bots.Add(DiscordPlatformInterfaceRelay);
            //AssignConnection(VbotHandler, DiscordPlatformInterfaceRelay);
            
            
            //Link userhandlers and classes that are two way//
            //AssignConnection(MediaHandler, DiscordPlatformInterfaceRelay);
            AssignConnection(MediaHandler, SteamPlatformInterface);

            
            AssignConnection(VbotHandler, SteamPlatformInterface);
            //AssignConnection(consolehandler, DiscordPlatformInterfaceRelay);
            
            AssignConnection(ghostchecker, SteamPlatformInterface);

            Thread[] BotThreads = new Thread[Bots.Count];

            //Start looping and iterating//
            for (int x = 0; x < Bots.Count; x++)
            {
                BotThreads[x] = new Thread(new ThreadStart(Bots[x].StartTickThreadLoop));
                BotThreads[x].Start();
            }

            bool Running = true;

            while (Running)
            {
            }
        }
    }
}