using Smod2;
using Smod2.Commands;

namespace VirtualBrightPlayz.SCPSL.EventManager
{
    internal class EMEventCmd : ICommandHandler
    {
        private EventManager eventManager;

        public EMEventCmd(EventManager eventManager)
        {
            this.eventManager = eventManager;
        }

        string ICommandHandler.GetCommandDescription()
        {
            return "Forces an event";
        }

        string ICommandHandler.GetUsage()
        {
            return "event <event index>";
        }

        string[] ICommandHandler.OnCall(ICommandSender sender, string[] args)
        {
            if (args.Length != 1)
                return new string[] { "Need more args.", eventManager.curEvent };
            else
            {
                int outint = 0;
                if (int.TryParse(args[0], out outint))
                {
                    eventManager.forceEvent = true;
                    eventManager.curEvent = eventManager.events[outint];
                    PluginManager.Manager.EnablePlugin(PluginManager.Manager.GetDisabledPlugin(eventManager.curEvent));
                    return new string[] { "Set the event to: " + PluginManager.Manager.GetEnabledPlugin(eventManager.curEvent).Details.name };
                }
                else
                    return new string[] { "Args must be an integer." };
            }
        }
    }
}