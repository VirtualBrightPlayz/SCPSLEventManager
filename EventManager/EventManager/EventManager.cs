using Smod2;
using Smod2.Attributes;
using Smod2.Config;
using Smod2.Piping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualBrightPlayz.SCPSL.EventManager;

namespace VirtualBrightPlayz.SCPSL.EventManager
{
    [PluginDetails(author = "VirtualBrightPlayz",
        description = "Manages events on your server",
        id = "virtualbrightplayz.scpsl.eventmanager",
        name = "Event Manager",
        configPrefix = "event_manager",
        version = "1.0",
        SmodMajor = 3,
        SmodMinor = 0,
        SmodRevision = 0)]
    public class EventManager : Plugin
    {
        [ConfigOption("min_players", "How many players are needed to start an event")]
        public int minPlayers = 2;

        [ConfigOption("events", "What events there are by plugin id")]
        public string[] events = new string[] { };

        [ConfigOption("event_files", "What events based on event configs")]
        public string[] eventFiles = new string[] { };
        //public List<string> events = new List<string>();

        [ConfigOption("use_config_files", "Use event configs or not")]
        public bool use_config_files = false;

        [ConfigOption("default_event", "The default event by plugin id")]
        public string default_event = string.Empty;

        [ConfigOption("items", "What items to show the events as. MUST BE 8 ITEMS!!!")]
        public int[] items = new int[] { 15, 17, 18, 11, 10, 9, 3, 6 };
        //public List<int> items = new List<int>() { 15, 17, 18, 11, 10, 9, 3, 6 };

        public BasicProgram program;

        [PipeField(true)]
        public string curEvent;

        public bool forceEvent = false;

        public override void OnDisable()
        {
        }

        public override void OnEnable()
        {
            this.Info("EventManager enabled.");
            //program = new BasicProgram(FileManager.GetAppFolder() + "basic.txt", this);
        }

        public override void Register()
        {
            curEvent = string.Empty;
            this.AddEventHandlers(new EMEventHandler(this), Smod2.Events.Priority.High);
            this.AddCommand("event", new EMEventCmd(this));
        }
    }
}
