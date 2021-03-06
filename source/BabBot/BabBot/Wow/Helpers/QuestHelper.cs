﻿using System;
using System.Collections.Generic;
using System.Text;
using BabBot.Manager;
using BabBot.Common;
using System.Threading;
using System.Text.RegularExpressions;

namespace BabBot.Wow.Helpers
{
    #region Quest Reguest

    public struct QuestReq
    {
        public readonly int NpcId;
        public readonly string ActionName;
        public readonly string QuestStatus;
        public readonly string NpcDestText;
        public readonly string ProcName;
        public readonly QuestStates FinalState;

        public int Choice;

        public QuestReq(int npc_id, string action_name, string npc_dest_text, 
                    string quest_status, string proc_name, QuestStates final_state) :
            this(npc_id, action_name, npc_dest_text, quest_status, proc_name, 0, final_state) {}

        public QuestReq(int npc_id, string action_name, string npc_dest_text, 
                string quest_status, string proc_name, int choice, QuestStates final_state)
        {
            NpcId = npc_id;
            ActionName = action_name;
            NpcDestText = npc_dest_text;
            QuestStatus = quest_status;
            ProcName = proc_name;
            Choice = choice;
            FinalState = final_state;
        }
    }


    #endregion Quest Request

    #region Quest Processing Exceptions

    /// <summary>
    /// Define exception happened during quest processing
    /// </summary>
    public class QuestProcessingException : Exception
    {
        public QuestProcessingException(string msg) :
            base(msg) { }
    }

    /// <summary>
    /// Generate exception that causes bot abandom quest
    /// </summary>
    public class QuestSkipException : Exception
    {
        public QuestSkipException(string msg) :
            base(msg + ". Skipping the quest") { }
    }

    public class QuestObjCoordNotFound : Exception
    {
        public QuestObjCoordNotFound(int q_id, int obj_id)
            : base("HotSpot coordinates not found for quest objective " +
                q_id + "#" + obj_id) { }
    }

    #endregion

    public static class QuestHelper
    {
        /// <summary>
        /// Get quest list from opened Gossip dialog
        /// </summary>
        /// <returns></returns>
        public static string[] GetGossipQuests(string type)
        {
            // Get list of quests
            string[] res = ProcessManager.
                Injector.Lua_ExecByName("GetGossipQuests", 
                                    new string[] { type });
            return res;
        }

        /// <summary>
        /// Find quest ID on NPC opened Gossip dialog
        /// </summary>
        /// <param name="title">Quest title</param>
        /// <param name="title">Quest type i.e Active or Available</param>
        /// <returns>Quest Id or -1 if quest not found</returns>
        public static int FindGossipQuestIdByTitle(string title, string type)
        {
            string[] qinfo = GetGossipQuests(type);

            int max_num = (int)(qinfo.Length / 3);
            for (int i = 0; i < max_num; i++)
            {
                string t = qinfo[i * 3];
                if ((t != null) && t.Equals(title))
                    return i + 1;
            }
            return -1;
        }

        public static GameObject FindQuestGameObj(QuestReq req, Quest q, string lfs)
        {
            // Set player current zone
            WowPlayer player = ProcessManager.Player;

            player.SetCurrentMapInfo();

            string qt = q.Title;

            // Get quest npc
            GameObject obj = q.GameObjList[req.NpcId];
            if (obj == null)
                throw new QuestSkipException(
                    "Quest " + req.NpcDestText + " NPC not found for quest '" + qt);

            Output.Instance.Log(lfs, "Located NPC '" + obj.Name +
                "' as quest " + req.NpcDestText + " for quest '" + qt + "'");

            q.State = QuestStates.OBJ_FOUND;
            return obj;
        }

        public static void MoveTargetQuestGameObj(QuestReq req, GameObject obj, Quest q, string lfs)
        {
            try
            {
                Output.Instance.Log(lfs, "Moving to quest " + req.NpcDestText + " ...");
                NpcHelper.MoveToGameObj(obj, lfs);
            }
            catch (CantReachNpcException e1)
            {
                throw new QuestSkipException(e1.Message);
            }
            catch (Exception e)
            {
                throw new QuestSkipException("Unable reach NPC. " + e.Message);
            }

            Output.Instance.Debug(lfs, "Reached the quest " + req.NpcDestText + ".");
            NpcHelper.TargetGameObj(obj, lfs);
        }

       

        public static void SelectGameObjQuest(QuestReq req, GameObject obj, Quest q, string lfs)
        {
            string[] dinfo;
            try
            {
                dinfo = NpcHelper.InteractNpc(obj.Name, false, lfs);
            }
            catch (NpcInteractException ne)
            {
                throw new QuestProcessingException(ne.Message);
            }
            catch (Exception e)
            {
                throw new Exception("Unable Interact with NPC." +
                        e.Message);
            }

            // If NPC has a single quest it can be opened already
            // null dinfo[0] cause NpcInteractException
            if (!dinfo[0].Equals(req.QuestStatus))
            {
                // Check if quest available
                bool avail;
                try
                {
                    avail = CheckQuest(q, dinfo, req, lfs);
                }
                catch (Exception e)
                {
                    throw new QuestSkipException(e.Message);
                }

                if (!avail)
                    throw new QuestSkipException("NPC doesn't have a quest");
            }

            q.State = QuestStates.SELECTED;
        }

        private static void ProcessQuestRequest(QuestReq req, Quest q, string lfs)
        {
            if (!DoBeforeStart(req, q, lfs))
                return;

            GameObject obj = FindQuestGameObj(req, q, lfs);

            MoveTargetQuestGameObj(req, obj, q, lfs);

            SelectGameObjQuest(req, obj, q, lfs);

            DoActionEx(req, q, lfs);
        }

        /// <summary>
        /// Accept currently opened quest
        /// </summary>
        /// <param name="q">Quest</param>
        public static void AcceptQuest(Quest q, string lfs)
        {
            ProcessQuestRequest(MakeAcceptQuestReq(), q, lfs);
        }

        /// <summary>
        /// Check if current quest avail
        /// If it on NPC gossip frame than select it
        /// If it already open than we fine
        /// </summary>
        /// <param name="q">Quest</param>
        /// <returns>true if NPC has quest</returns>
        private static bool CheckQuest(Quest q, 
                        string[] dinfo, QuestReq req, string lfs)
        {
            string cur_service = null;

            if (dinfo == null)
            {
                dinfo = NpcHelper.GetTargetNpcDialogInfo(q.Src.Name, false, lfs);
                cur_service = dinfo[0];
            }
            else
                cur_service = dinfo[0];

            if (cur_service.Equals("gossip"))
            {
                Output.Instance.Debug(lfs, "GossipFrame opened.");

                Output.Instance.Debug(lfs, "Looking for quest ...");

                int idx = QuestHelper.FindGossipQuestIdByTitle(q.Title, req.ProcName);
                if (idx < 0)
                    return false;

                // Selecting quest
                Output.Instance.Debug(lfs, "Selecting quest by Id: " + idx);
                LuaHelper.Exec("SelectGossip" + req.ProcName + "Quest", idx);

                // Wait for quest frame pop up
                try
                {
                    NpcHelper.WaitDialogOpen("Quest", lfs);
                }
                catch (NpcInteractException ne)
                {
                    throw new QuestProcessingException(ne.Message);
                }
                catch (Exception e)
                {
                    throw new QuestProcessingException(
                        "NPC doesn't show QuestFrame. " + e.Message);
                }

                // Call itself again to parse the quest
                return CheckQuest(q, null, req, lfs);

            }
            else if (cur_service.Equals("quest_start"))
            {
                Output.Instance.Debug("Parsing quest info line '" + dinfo[1] + "'");
                string[] headers = dinfo[1].Split(new string[] { 
                                        "::" }, StringSplitOptions.None);

                string title = headers[0];
                return (!string.IsNullOrEmpty(title) && title.Equals(q.Title));
            }
            else if (cur_service.Equals("quest_progress"))
            {
                // Quest progress
                // Click continue and check next screen
                LuaHelper.Exec("CompleteQuest");
                // Wait to change quest frame
                Thread.Sleep(2000);
                return CheckQuest(q, null, req, lfs);
            }
            else if (cur_service.Equals("quest_end"))
                return true;
            else
                // Quest not found nor on gossip frame nor on active frame
                throw new QuestProcessingException(
                    "Quest not found nor on GossipFrame nor on ActiveFrame");
        }

        private static object[] CheckLogQuest(string title, string lfs)
        {
            Output.Instance.Debug(lfs, "Looking for quest index in toon quest log ...");
            string[] res = LuaHelper.Exec("FindLogQuest", title);

            // Trying convert result
            int idx = 0;
            try
            {
                idx = Convert.ToInt32(res[0]);
            }
            catch { }

            object[] ret = new object[res.Length];
            ret[0] = idx;
            for (int i = 1; i < res.Length; i++ )
                ret[i] = res[i];
            return ret;
        }

        public static bool CheckQuest(Quest q, string lfs)
        {
            object[] res = CheckLogQuest(q.Title, lfs);
            q.Idx = (int)res[0];
            if (q.Idx > 0)
            {
                q.State = QuestStates.ACCEPTED;

                string s = (string)res[2];
                q.Completed = (!string.IsNullOrEmpty(s) &&
                                                s.Equals("1"));
            }

            return (q.Idx > 0);
        }

        /// <summary>
        /// Check if quest in toon log
        /// </summary>
        /// <param name="q">Quest Title</param>
        /// <returns>
        /// Quest index in toon log (starting from 1) 
        /// or 0 if quest not found
        /// </returns>
        public static int FindLogQuest(string title, string lfs)
        {
            object[] res = CheckLogQuest(title, lfs);
            int idx = (int)res[0];

            if (idx > 0)
                Output.Instance.Debug(lfs, "Quest located with index: " + idx);
            else
                Output.Instance.Debug(lfs, "Quest not found.");
            return idx;
        }

        public static string[] GetQuestObjectives(Quest q)
        {
            string[] ret = LuaHelper.Exec("GetQuestObjectives", q.Title);

            return ret;
        }

        public static void AbandonQuest(string qtitle, string lfs)
        {
            // Find quest id in toon log
            Output.Instance.Log(lfs, "Abandoning Quest '" + qtitle + "' ... ");
            int idx = FindLogQuest(qtitle, lfs);
            if (idx == 0)
            {
                Output.Instance.Log(lfs, "Not found in toon's quest log");
                return;
            }

            // Open QuestLogFrame
            LuaHelper.Exec("ToggleFrame", "QuestLog");
            NpcHelper.WaitDialogOpen("QuestLog", lfs);

            string[] ret = LuaHelper.Exec("SelectAbandonQuest", idx);
            string aq_name = ret[0];

            if (string.IsNullOrEmpty(aq_name))
                throw new QuestProcessingException(
                    "Abandoning preparation procedure didn't return quest name");
            else if (!aq_name.Equals(qtitle))
                throw new QuestProcessingException("Abandoned quest '" +
                    aq_name + "' is different from expected '" + qtitle + "'.");
            else
            {
                // Wait to get selection activated
                Thread.Sleep(2000);
                LuaHelper.Exec("AbandonQuest");
                // Wait to get abandon action activated
                Thread.Sleep(2000);

                if (FindLogQuest(qtitle, lfs) == 0)
                    Output.Instance.Log(lfs, "Quest '" +
                    qtitle + "' removed from toon logs");
                else
                    throw new QuestProcessingException("Abandoned quest '" +
                        qtitle + "' is still in toon quest log after abandon action.");
            }
        }

        public static void DeliverQuest(Quest q, string lfs, int choice, object item)
        {
            ProcessQuestRequest(MakeDeliveryQuestReq(choice), q, lfs);

            if (item != null)
                LuaHelper.Exec("EquipItemByName", item.ToString());
        }

        private static string[] GetQuestObjectives(int quest_idx)
        {
            Output.Instance.Debug("xxx", "Checking Quest Objectives ... ");

            string[] ret = LuaHelper.Exec("GetQuestObjectives", quest_idx);

            string[] obj = new string[0];

            if (!string.IsNullOrEmpty(ret[0]))
                obj = ret[0].Split(new string[] { "::" },
                                        StringSplitOptions.None);

            if (obj.Length > 0)
                Output.Instance.Debug("xxx", "Quest has " + obj.Length + " objectives");
            else
                Output.Instance.Debug("xxx", "Quest has no objectives");

            return obj;
        }

        public static bool IsQuestLogCompleted(int idx)
        {

            // Check if quest completed
            string[] ret = LuaHelper.Exec("IsLogQuestCompleted", idx);
            return (!string.IsNullOrEmpty(ret[0]) && ret[0].Equals("1"));

            /*
            // Check if quest has objectives. With 0 objectives it's auto-completed by finding quest dest
            string[] obj = QuestHelper.GetQuestObjectives(idx);

            for (int i = 0; i < obj.Length; i++)
            {
                string[] items = obj[i].Split(',');
                if (string.IsNullOrEmpty(items[2]) || !items[2].Equals("1"))
                    return false;
            }
            
            return true;
             */
        }

        private static bool DoBeforeStart(QuestReq req, Quest q, string lfs)
        {
            if (req.QuestStatus.Equals("quest_start"))
            {
                // Check if quest already in quest log
                q.Idx = QuestHelper.FindLogQuest(q.Title, lfs);
                if (q.Idx > 0)
                {
                    Output.Instance.Log(lfs, "Quest already accepted");
                    return false;
                }

                q.State = QuestStates.ACCEPTED;
                return true;
            }
            else if (req.QuestStatus.Equals("quest_end"))
            {
                // Check if quest already in quest log
                int idx = QuestHelper.FindLogQuest(q.Title, lfs);
                if (idx == 0)
                {
                    Output.Instance.Log(lfs, "Quest not in a log list");
                    return false;
                }

                // Check if quest completed
                if (!QuestHelper.IsQuestLogCompleted(idx))
                {
                    Output.Instance.Log(lfs, "Quest not is not completed");
                    return false;
                }

                return true;
            }
            else
                throw new QuestSkipException(
                    "Unknown quest status '" + req.QuestStatus);
        }

        public static void DoAction(QuestReq req, string lfs)
        {
            Output.Instance.Debug(lfs, req.ActionName + "ing quest ...");

            if (req.ActionName.Equals("Accept"))
                LuaHelper.Exec("AcceptQuest");
            else if (req.ActionName.Equals("Deliver"))
                LuaHelper.Exec("DeliverQuest", req.Choice);

            // Wait a bit to update log
            Thread.Sleep(2000);
        }

        public static void DoActionEx(QuestReq req, Quest q, string lfs)
        {
            DoAction(req, lfs);

            string qtitle = q.Title;

            // After action
            q.Idx = QuestHelper.FindLogQuest(qtitle, lfs);

            if (req.QuestStatus.Equals("quest_start"))
            {
                // Check that quest is in toon log
                if (q.Idx == 0)
                    throw new QuestSkipException(
                        "Unable find quest in toon log after it been accepted '");
            }
            else if (req.QuestStatus.Equals("quest_end"))
            {
                // Check that quest is in toon log
                if (q.Idx > 0)
                    throw new QuestSkipException(
                        "Quest still in toon's quest log after it been delivered");
            } 
            else
                throw new QuestSkipException(
                    "Unknown quest status '" + req.QuestStatus);

            Output.Instance.Log(lfs, "Quest '" + qtitle +
                "' successfully " + req.ActionName.ToLower() + "ed'");

            q.State = req.FinalState;
        }

        public static QuestReq MakeAcceptQuestReq()
        {
            return new QuestReq(0, "Accept", "giver", 
                "quest_start", "Available", QuestStates.ACCEPTED);
        }

        public static QuestReq MakeDeliveryQuestReq(int choice)
        {
            return new QuestReq(1, "Deliver", "receiver", 
                "quest_end", "Active", choice, QuestStates.DELIVERED);
        }
    }
}
