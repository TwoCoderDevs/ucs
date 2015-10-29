﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using UCS.Helpers;
using UCS.Logic;
using UCS.Network;
using UCS.Core;

namespace UCS.PacketProcessing
{
    //14715
    class SendGlobalChatLineMessage : Message
    {
        public SendGlobalChatLineMessage(Client client, BinaryReader br) : base(client, br)
        {
        }

        public override void Decode()
        {
            using (var br = new BinaryReader(new MemoryStream(GetData())))
            {
                Message = br.ReadScString();
            }
        }

        public String Message { get; set; }

        public override void Process(Level level)
        {
            if(Message.Length > 0)
            {
                if(Message[0] == '/')
                {
                    object obj = GameOpCommandFactory.Parse(Message);
                    if (obj != null)
                    {
                        string player = "";
                        if (level != null)
                            player += " (" + level.GetPlayerAvatar().GetId() + ", " + level.GetPlayerAvatar().GetAvatarName() + ")";
                        Debugger.WriteLine("\t" + obj.GetType().Name + player);
                        ((GameOpCommand)obj).Execute(level);
                    }
                }
                else
                {
                    long senderId = level.GetPlayerAvatar().GetId();
                    string senderName = level.GetPlayerAvatar().GetAvatarName();
                    foreach(var onlinePlayer in ResourcesManager.GetOnlinePlayers())
                    {
                        var p = new GlobalChatLineMessage(onlinePlayer.GetClient());
                        if(onlinePlayer.GetAccountPrivileges() > 0)
                            p.SetPlayerName(senderName + " #" + senderId);
                        else
                            p.SetPlayerName(senderName);
                        {
                            string fileName = @"filter\filter.txt";
                            var lines = File.ReadAllLines(fileName)
                                ;
                            foreach (var line in lines)
                            {
                                if (Message.Contains(line))
                                {
                                    p.SetChatMessage("******");
                                    p.SetLeagueId(level.GetPlayerAvatar().GetLeagueId());
                                    p.SetPlayerId(senderId);
                                    PacketManager.ProcessOutgoingPacket(p);

                                    var warnMessage = new GlobalChatLineMessage(Client);
                                    warnMessage.SetPlayerId(0L);
                                    warnMessage.SetChatMessage("Our system has detect that you using bad words.Please consider it before typing");
                                    warnMessage.SetPlayerName("System Admin");
                                    warnMessage.SetLeagueId(22);
                                    PacketManager.ProcessOutgoingPacket(warnMessage); // process message
                                }
                                else
                                {
                                    p.SetChatMessage(this.Message);
                                    p.SetPlayerId(senderId);
                                    p.SetLeagueId(level.GetPlayerAvatar().GetLeagueId());
                                    PacketManager.ProcessOutgoingPacket(p);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

