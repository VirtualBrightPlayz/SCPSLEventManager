using Smod2;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.EventSystem.Events;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace VirtualBrightPlayz.SCPSL.EventManager
{
    public class EMEventHandler : IEventHandlerCallCommand, IEventHandlerRoundRestart, IEventHandlerWaitingForPlayers, IEventHandlerRoundStart, IEventHandlerPlayerJoin, IEventHandlerPlayerDropItem, IEventHandlerDecideTeamRespawnQueue
    {
        private EventManager eMan;
        //steamid | event id
        private Dictionary<string, string> events;
        //item type | event id
        private Dictionary<ItemType, string> items;
        private Dictionary<string, EventInfo> cache;
        private List<EventInfo> cache2;
        private const bool bypass = false;
        private bool waiting = true;

        public EMEventHandler(EventManager eventManager)
        {
            this.eMan = eventManager;
            items = new Dictionary<ItemType, string>();
            events = new Dictionary<string, string>();
            cache = new Dictionary<string, EventInfo>();
            cache2 = new List<EventInfo>();
        }

        void IEventHandlerCallCommand.OnCallCommand(PlayerCallCommandEvent ev)
        {
            if (ev.Command.ToLower().Equals("debug"))
            {
                string str = "";
                if (eMan.use_config_files)
                {
                    foreach (string plid in eMan.eventFiles)
                    {
                        var t = GetEventName(plid);
                        str += t.ToString();
                    }
                }
                else
                {
                    foreach (string plid in eMan.events)
                    {
                        str += plid + "\n";
                    }
                }
                ev.ReturnMessage = str;
            }
        }

        void IEventHandlerRoundRestart.OnRoundRestart(RoundRestartEvent ev)
        {
            ResetEvents();
        }

        private void ResetEvents()
        {
            eMan.forceEvent = false;
            if (eMan.use_config_files)
            {
                foreach (EventInfo ei in cache2)
                {
                    foreach (string id in ei.enabled)
                    {
                        Plugin pl = PluginManager.Manager.GetPlugin(id);
                        if (pl != null)
                        {
                            PluginManager.Manager.DisablePlugin(pl);
                        }
                    }
                    foreach (string id in ei.disabled)
                    {
                        Plugin pl = PluginManager.Manager.GetPlugin(id);
                        if (pl != null)
                        {
                            PluginManager.Manager.EnablePlugin(pl);
                        }
                    }
                }
                cache.Clear();
                cache2.Clear();
                foreach (string plid in eMan.eventFiles)
                {
                    var t = GetEventName(plid);
                    cache.Add(t.file, t);
                    cache2.Add(t);
                }
                eMan.curEvent = string.Empty;
                items.Clear();
                events.Clear();
                for (int i = 0; i < cache.Count; i++)
                {
                    //string setevent = cache2[i].file;
                    items.Add((ItemType)eMan.items[i], cache2[i].file);
                }
            }
            else
            {
                foreach (string plid in eMan.events)
                {
                    try
                    {
                        PluginManager.Manager.DisablePlugin(PluginManager.Manager.GetPlugin(plid));
                    }
                    catch (NullReferenceException e)
                    { }
                }
                eMan.curEvent = string.Empty;
                events.Clear();
                items.Clear();
                for (int i = 0; i < eMan.events.Length; i++)
                {
                    string setevent = eMan.events[i];
                    items.Add((ItemType)eMan.items[i], PluginManager.Manager.GetPlugin(setevent).Details.id);
                    //events.Add(PluginManager.Manager.GetPlugin(setevent).Details.id, PluginManager.Manager.GetPlugin(setevent).Details.name);
                }
            }
        }

        void IEventHandlerWaitingForPlayers.OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
            ResetEvents();
            waiting = true;
        }

        int TotalVotes(List<string> list, string ev)
        {
            int votes = 0;
            foreach (string evnt in list)
            {
                if (evnt.Equals(ev))
                    votes++;
            }
            return votes;
        }

        int NotTotalVotes(List<string> list, string ev)
        {
            int votes = 0;
            foreach (string evnt in list)
            {
                if (!evnt.Equals(ev))
                    votes++;
            }
            return votes;
        }

        void IEventHandlerRoundStart.OnRoundStart(RoundStartEvent ev)
        {
            /*foreach (Player plr in ev.Server.GetPlayers())
            {
                ((GameObject)plr.GetGameObject()).GetComponent<Inventory>().ServerDropAll();
            }*/
            //eMan.program.RoundStartParseData(ev);
        }

        void IEventHandlerPlayerJoin.OnPlayerJoin(PlayerJoinEvent ev)
        {
            if (!waiting)
                return;
            //CalcEvent();
            if (eMan.Server.NumPlayers >= eMan.minPlayers || true)
            {
                GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, "The items in your inventory is for voting what event should happen.\nDrop what you want to vote for.", 4, false);

                if (eMan.use_config_files)
                {
                    foreach (var item in items)
                    {
                        GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, item.Key.ToString() + " = " + cache[item.Value].name, 2, false);
                        ev.Player.GiveItem(item.Key);
                    }
                }
                else
                {
                    foreach (var item in items)
                    {
                        GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, item.Key.ToString() + " = " + PluginManager.Manager.GetPlugin(item.Value).Details.name, 2, false);
                        ev.Player.GiveItem(item.Key);
                    }
                }
            }
        }

        void IEventHandlerPlayerDropItem.OnPlayerDropItem(PlayerDropItemEvent ev)
        {
            if (!waiting || events.ContainsKey(ev.Player.SteamId) || eMan.forceEvent)
                return;
            if (eMan.use_config_files)
            {
                if (items.ContainsKey(ev.Item.ItemType) && eMan.Server.NumPlayers >= eMan.minPlayers)
                {
                    ev.Allow = false;
                    GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, "Set vote as: " + cache[items[ev.Item.ItemType]].name + "!", 4, false);
                    ((GameObject)ev.Player.GetGameObject()).GetComponent<Inventory>().ServerDropAll();
                    events.Add(ev.Player.SteamId, items[ev.Item.ItemType]);
                    //CalcEvent();
                }
                else
                {
                    ev.Allow = false;
                    //CalcEvent();
                    ((GameObject)ev.Player.GetGameObject()).GetComponent<Inventory>().ServerDropAll();
                    foreach (var item in items)
                    {
                        ev.Player.GiveItem(item.Key);
                    }
                    GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, "Error registering vote: Probably not enough players.", 4, false);
                }
            }
            else
            {
                if (items.ContainsKey(ev.Item.ItemType) && eMan.Server.NumPlayers >= eMan.minPlayers)
                {
                    ev.Allow = false;
                    GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, "Set vote as: " + PluginManager.Manager.GetPlugin(items[ev.Item.ItemType]).Details.name + "!", 4, false);
                    ((GameObject)ev.Player.GetGameObject()).GetComponent<Inventory>().ServerDropAll();
                    try
                    {
                        events.Add(ev.Player.SteamId, items[ev.Item.ItemType]);
                    }
                    catch (ArgumentException e)
                    {

                    }
                    //CalcEvent();
                }
                else
                {
                    ev.Allow = false;
                    //CalcEvent();
                    ((GameObject)ev.Player.GetGameObject()).GetComponent<Inventory>().ServerDropAll();
                    foreach (var item in items)
                    {
                        ev.Player.GiveItem(item.Key);
                    }
                    GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, "Error registering vote: Probably not enough players.", 4, false);
                }
            }
        }

        public EventInfo GetEventName(string filepath)
        {
            string[] lines = File.ReadAllLines(FileManager.GetAppFolder() + filepath);
            int mode = 0;

            EventInfo ei = new EventInfo();
            ei.file = filepath;
            foreach (string line in lines)
            {
                if (line.Trim().ToLower().Equals("--enabled"))
                {
                    mode = 1;
                    continue;
                }
                else if (line.Trim().ToLower().Equals("--disabled"))
                {
                    mode = 2;
                    continue;
                }
                else if (line.Trim().ToLower().Equals("--end"))
                {
                    mode = 0;
                    continue;
                }
                else if (line.Trim().StartsWith("--desc: "))
                {
                    ei.desc = line.Trim().Substring(7);
                    continue;
                }
                else if (line.Trim().StartsWith("--name: "))
                {
                    ei.name = line.Trim().Substring(7);
                    continue;
                }
                else if (mode == 1)
                {
                    Plugin pl = PluginManager.Manager.GetPlugin(line.Trim());
                    if (pl != null)
                    {
                        ei.enabled.Add(line.Trim());
                    }
                }
                else if (mode == 2)
                {
                    Plugin pl = PluginManager.Manager.GetPlugin(line.Trim());
                    if (pl != null)
                    {
                        ei.disabled.Add(line.Trim());
                    }
                }
            }

            return ei;
        }

        public void ReadAndSetupPlugins(string filepath)
        {
            string[] lines = File.ReadAllLines(FileManager.GetAppFolder() + filepath);
            int mode = 0;

            foreach (string line in lines)
            {
                if (line.Trim().ToLower().Equals("--enabled"))
                {
                    mode = 1;
                    continue;
                }
                else if (line.Trim().ToLower().Equals("--disabled"))
                {
                    mode = 2;
                    continue;
                }
                else if (line.Trim().ToLower().Equals("--end"))
                {
                    mode = 0;
                    continue;
                }
                else if (line.Trim().ToLower().StartsWith("--desc: ") || line.Trim().ToLower().StartsWith("--name: "))
                {
                    //mode = 0;
                    continue;
                }
                else if (mode == 1)
                {
                    Plugin pl = PluginManager.Manager.GetDisabledPlugin(line.Trim());
                    if (pl != null)
                    {
                        PluginManager.Manager.EnablePlugin(pl);
                    }
                }
                else if (mode == 2)
                {
                    Plugin pl = PluginManager.Manager.GetEnabledPlugin(line.Trim());
                    if (pl != null)
                    {
                        PluginManager.Manager.DisablePlugin(pl);
                    }
                }
            }
        }

        private void CalcEvent()
        {
            if (eMan.Server.NumPlayers < eMan.minPlayers) return;
            List<string> evlist = new List<string>();
            foreach (KeyValuePair<string, string> item in events)
            {
                evlist.Add(item.Value);
                //eMan.Info(item.Value);
            }
            //eMan.Info(eMan.forceEvent.ToString());
            if (eMan.use_config_files)
            {
                if (!eMan.forceEvent)
                {
                    string newevent = string.Empty;
                    for (int i = 0; i < cache2.Count; i++)
                    {
                        newevent = TotalVotes(evlist, cache2[i].file) > NotTotalVotes(evlist, cache2[i].file) ? cache2[i].file : newevent;
                    }
                    eMan.curEvent = TotalVotes(evlist, newevent) >= eMan.minPlayers ? newevent : eMan.default_event;
                }
                //eMan.Info(eMan.curEvent);
                if (eMan.curEvent != string.Empty)
                {
                    foreach (EventInfo ei in cache2)
                    {
                        if (ei.file.Equals(eMan.curEvent))
                        {
                            foreach (string id in ei.enabled)
                            {
                                Plugin pl = PluginManager.Manager.GetPlugin(id);
                                if (pl != null)
                                {
                                    PluginManager.Manager.EnablePlugin(pl);
                                }
                            }
                            foreach (string id in ei.disabled)
                            {
                                Plugin pl = PluginManager.Manager.GetPlugin(id);
                                if (pl != null)
                                {
                                    PluginManager.Manager.DisablePlugin(pl);
                                }
                            }
                            break;
                        }
                    }
                }
                else
                {
                    eMan.curEvent = eMan.default_event;
                    foreach (EventInfo ei in cache2)
                    {
                        if (ei.file.Equals(eMan.curEvent))
                        {
                            foreach (string id in ei.enabled)
                            {
                                Plugin pl = PluginManager.Manager.GetPlugin(id);
                                if (pl != null)
                                {
                                    PluginManager.Manager.EnablePlugin(pl);
                                }
                            }
                            foreach (string id in ei.disabled)
                            {
                                Plugin pl = PluginManager.Manager.GetPlugin(id);
                                if (pl != null)
                                {
                                    PluginManager.Manager.DisablePlugin(pl);
                                }
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                if (!eMan.forceEvent)
                {
                    string newevent = string.Empty;
                    for (int i = 0; i < eMan.events.Length; i++)
                    {
                        newevent = TotalVotes(evlist, eMan.events[i]) > NotTotalVotes(evlist, eMan.events[i]) ? eMan.events[i] : newevent;
                    }
                    //eMan.Info(newevent);
                    eMan.curEvent = TotalVotes(evlist, newevent) >= eMan.minPlayers ? newevent : eMan.default_event;
                }
                foreach (string plid in eMan.events)
                {
                    try
                    {
                        PluginManager.Manager.DisablePlugin(PluginManager.Manager.GetPlugin(plid));
                    }
                    catch (NullReferenceException e)
                    { }
                }
                //eMan.Info(eMan.curEvent);
                if (eMan.curEvent != string.Empty)
                {
                    //eMan.programData = new string[] { };
                    PluginManager.Manager.EnablePlugin(PluginManager.Manager.GetPlugin(eMan.curEvent));
                }
            }
        }

        void IEventHandlerDecideTeamRespawnQueue.OnDecideTeamRespawnQueue(DecideRespawnQueueEvent ev)
        {
            if (!waiting)
                return;
            CalcEvent();
            waiting = false;
            eMan.Server.Map.ClearBroadcasts();
            eMan.Info(eMan.curEvent);
            if (eMan.use_config_files)
            {
                eMan.Server.Map.Broadcast(3, "Event: " + cache[eMan.curEvent].name, false);
            }
            else
            {
                eMan.Server.Map.Broadcast(3, "Event: " + PluginManager.Manager.GetPlugin(eMan.curEvent).Details.name, false);
            }
            //CalcEvent();
            Smod2.Events.EventManager.Manager.HandleEvent<IEventHandlerDecideTeamRespawnQueue>(ev);
        }
    }

    public class EventInfo
    {
        public string name;
        public string desc;
        public string file;

        public List<string> enabled = new List<string>();
        public List<string> disabled = new List<string>();

        public override string ToString()
        {
            string str = name + "\n" + desc + "\nEnabled:";
            foreach (string str2 in enabled)
            {
                str += "\n- " + str2;
            }
            str += "\nDisabled:";
            foreach (string str2 in disabled)
            {
                str += "\n- " + str2;
            }
            return str;
        }
    }
}