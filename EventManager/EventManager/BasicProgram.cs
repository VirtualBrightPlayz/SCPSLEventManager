using Smod2.API;
using Smod2.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualBrightPlayz.SCPSL.EventManager
{
    public class BasicProgram
    {

        public enum FuncType
        {
            NONE = 0,
            PLAYER_TEAMSPAWN,
            WORLD_RESTARTROUND,
            WORLD_STARTROUND,
        }

        string[] programData;
        public Dictionary<FuncType, string[]> funcProgs;

        string name;
        List<int> roleSpawnQueue;
        List<int> teamSpawnQueue;

        EventManager eMan;


        public BasicProgram(string file, EventManager eman)
        {
            programData = File.ReadAllLines(file);
            eMan = eman;
            funcProgs = new Dictionary<FuncType, string[]>();
            roleSpawnQueue = new List<int>();
            teamSpawnQueue = new List<int>();
            name = string.Empty;
            PreParseData();
        }

        public void ParseData()
        {

        }

        public void RoundStartParseData(RoundStartEvent ev)
        {
            eMan.Info(funcProgs.ContainsKey(FuncType.WORLD_STARTROUND).ToString());
            if (!funcProgs.ContainsKey(FuncType.WORLD_STARTROUND))
                return;
            string[] prog = funcProgs[FuncType.WORLD_STARTROUND];
            for (int i = 0; i < prog.Length; i++)
            {
                var line = prog[i];
                var splitline = line.Split('#')[0].Trim().Split(' ');

                if (splitline[0].ToLower().StartsWith("tell:"))
                {
                    eMan.Info(splitline.Length.ToString());
                    if (splitline.Length >= 2)
                    {
                        string str = string.Empty;
                        for (int j = 1; j < splitline.Length; j++)
                        {
                            str += " " + splitline[j];
                        }
                        str = str.Trim();
                        foreach (Player plr in ev.Server.GetPlayers())
                            plr.PersonalBroadcast((uint)(str.Split(' ').Length + 2), str, false);
                    }
                    else
                    {
                        foreach (Player plr in ev.Server.GetPlayers())
                            plr.PersonalClearBroadcasts();
                    }
                }
            }
        }

        public void RegisterFunction(FuncType func, string[] funcProgram)
        {
            funcProgs.Add(func, funcProgram);
        }

        private void PreParseData()
        {
            bool infunc = false;
            FuncType func = FuncType.NONE;
            List<string> funcProgram = new List<string>();
            for (int i = 0; i < programData.Length; i++)
            {
                var line = programData[i];
                var splitline = line.Split('#')[0].Trim().Split(' ');


                //end function
                if (splitline[0].ToLower().StartsWith("_end_"))
                {
                    if (!infunc)
                        eMan.Info("Line #" + i + " - FunctionEndError: not in a function");
                    infunc = false;
                    if (!funcProgs.ContainsKey(func))
                        RegisterFunction(func, funcProgram.ToArray());
                    else
                        eMan.Info("Line #" + i + " - FunctionEndError: duplicate function");
                    func = FuncType.NONE;
                    funcProgram.Clear();
                    continue;
                }

                if (infunc)
                {
                    funcProgram.Add(line);
                }

                //event name variable
                if (splitline[0].ToLower().StartsWith("event_name:"))
                {
                    if (splitline.Length < 2)
                    {
                        eMan.Info("Line #" + i + " - VariableUnsetError");
                    }
                    else
                    {
                        for (int j = 1; j < splitline.Length; j++)
                        {
                            name += " " + splitline[j];
                        }
                        name = name.Trim();
                    }
                }
                //event id variable
                if (splitline[0].ToLower().StartsWith("event_id:"))
                {
                    if (splitline.Length < 2)
                    {
                        eMan.Info("Line #" + i + " - VariableUnsetError");
                    }
                    if (splitline.Length > 2)
                    {
                        eMan.Info("Line #" + i + " - SpacedStringError");
                    }
                    else
                    {
                        for (int j = 1; j < splitline.Length; j++)
                        {
                            name += " " + splitline[j];
                        }
                        name = name.Trim();
                    }
                }

                /*//role spawn queue variable
                if (splitline[0].ToLower().StartsWith("role_spawn_queue:"))
                {
                    if (splitline.Length < 2)
                    {
                        eMan.Info("Line #" + i + " - VariableUnsetError");
                    }
                    if (splitline.Length > 2)
                    {
                        eMan.Info("Line #" + i + " - TypeMismatchError");
                    }
                    else
                    {
                        for (int j = 0; j < splitline[1].ToCharArray().Length; j++)
                        {
                            int res = 0;
                            if (!int.TryParse(splitline[1].ToCharArray()[j].ToString(), out res))
                            {
                                eMan.Info("Line #" + i + " - TypeMismatchError: Not a positive integer");
                            }
                            else
                            {
                                roleSpawnQueue.Add(res);
                            }
                        }
                    }
                }*/

                //team spawn queue variable
                if (splitline[0].ToLower().StartsWith("team_spawn_queue:"))
                {
                    if (splitline.Length < 2)
                    {
                        eMan.Info("Line #" + i + " - VariableUnsetError");
                    }
                    if (splitline.Length > 2)
                    {
                        eMan.Info("Line #" + i + " - TypeMismatchError");
                    }
                    else
                    {
                        for (int j = 0; j < splitline[1].ToCharArray().Length; j++)
                        {
                            int res = 0;
                            if (!int.TryParse(splitline[1].ToCharArray()[j].ToString(), out res))
                            {
                                eMan.Info("Line #" + i + " - TypeMismatchError: Not a positive integer");
                            }
                            else
                            {
                                teamSpawnQueue.Add(res);
                            }
                        }
                    }
                }

                //function team spawn
                if (splitline[0].ToLower().StartsWith("game_event_spawn_team:"))
                {
                    string[] teamnumber2 = splitline[0].ToLower().Split(':');
                    if (teamnumber2.Length < 2)
                    {
                        eMan.Info("Line #" + i + " - InvalidEventError");
                    }
                    if (teamnumber2.Length > 2)
                    {
                        eMan.Info("Line #" + i + " - InvalidEventError");
                    }
                    int teamnumber = 0;
                    if (!int.TryParse(teamnumber2[1], out teamnumber))
                    {
                        eMan.Info("Line #" + i + " - TypeMismatchError: is not a positive integer");
                    }
                    else
                    {
                        infunc = true;
                        func = FuncType.PLAYER_TEAMSPAWN;
                    }
                }

                //function round start
                if (splitline[0].ToLower().StartsWith("game_event_round_start:"))
                {
                    infunc = true;
                    func = FuncType.WORLD_STARTROUND;
                    string[] teamnumber2 = splitline[0].ToLower().Split(':');
                    if (teamnumber2.Length > 1)
                    {
                        eMan.Info("Line #" + i + " - InvalidEventError");
                    }
                    else
                    {
                    }
                }
            }
        }
    }
}
