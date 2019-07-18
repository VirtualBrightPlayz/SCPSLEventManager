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
    public class EMEventHandler : IEventHandlerCallCommand, IEventHandlerRoundRestart, IEventHandlerWaitingForPlayers, IEventHandlerRoundStart, IEventHandlerPlayerJoin, IEventHandlerPlayerDropItem
    {
        private EventManager eMan;
        //steamid | event id
        private Dictionary<string, string> events;
        //item type | event id
        private Dictionary<ItemType, string> items;
        private const bool bypass = false;

        public EMEventHandler(EventManager eventManager)
        {
            this.eMan = eventManager;
            items = new Dictionary<ItemType, string>();
            events = new Dictionary<string, string>();
        }

        void IEventHandlerCallCommand.OnCallCommand(PlayerCallCommandEvent ev)
        {
            if (ev.Command.ToLower().Equals("debug"))
            {
                string str = "";
                foreach (string plid in eMan.events)
                {
                    str += plid + "\n";
                }
                ev.ReturnMessage = str;
            }
        }

        void IEventHandlerRoundRestart.OnRoundRestart(RoundRestartEvent ev)
        {
            eMan.forceEvent = false;
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

        void IEventHandlerWaitingForPlayers.OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
            eMan.forceEvent = false;
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
            if (ev.Player.TeamRole.Role != Role.UNASSIGNED)
                return;
            if (eMan.Server.NumPlayers >= eMan.minPlayers || true)
            {
                GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, "The items in your inventory is for voting what event should happen.\nDrop what you want to vote for.", 4, false);

                foreach (var item in items)
                {
                    GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, item.Key.ToString() + " = " + PluginManager.Manager.GetPlugin(item.Value).Details.name, 2, false);
                    ev.Player.GiveItem(item.Key);
                }
            }
        }

        void IEventHandlerPlayerDropItem.OnPlayerDropItem(PlayerDropItemEvent ev)
        {
            if (ev.Player.TeamRole.Role != Role.UNASSIGNED || events.ContainsKey(ev.Player.SteamId) || eMan.forceEvent)
                return;
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
                CalcEvent();
            }
            else
            {
                ev.Allow = false;
                CalcEvent();
                ((GameObject)ev.Player.GetGameObject()).GetComponent<Inventory>().ServerDropAll();
                foreach (var item in items)
                {
                    ev.Player.GiveItem(item.Key);
                }
                GameObject.FindObjectOfType<Broadcast>().CallTargetAddElement(((GameObject)ev.Player.GetGameObject()).GetComponent<NetworkIdentity>().connectionToClient, "Error registering vote: Probably not enough players.", 4, false);
            }
        }

        public void

        private void CalcEvent()
        {
            if (eMan.Server.NumPlayers < eMan.minPlayers) return;
            List<string> evlist = new List<string>();
            foreach (KeyValuePair<string, string> item in events)
            {
                evlist.Add(item.Value);
                eMan.Info(item.Value);
            }
            //eMan.Info(eMan.forceEvent.ToString());
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
            eMan.Info(eMan.curEvent);
            if (eMan.curEvent != string.Empty)
            {
                //eMan.programData = new string[] { };
                PluginManager.Manager.EnablePlugin(PluginManager.Manager.GetPlugin(eMan.curEvent));
            }
        }
    }
}