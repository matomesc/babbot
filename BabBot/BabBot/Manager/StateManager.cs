﻿using BabBot.Wow;

namespace BabBot.Manager
{
    public class StateManager
    {
        private PlayerState CurrentState;
        private PlayerState LastState;

        public StateManager()
        {
            CurrentState = LastState = PlayerState.Start;
        }

        public PlayerState State
        {
            get { return CurrentState; }
        }

        public void UpdateState()
        {
            LastState = CurrentState;

            if (CurrentState == PlayerState.Start)
            {
                CurrentState = PlayerState.Roaming;
            }

            if (CurrentState == PlayerState.Roaming)
            {
                if (ProcessManager.Player.IsBeingAttacked())
                {
                    /// We should target the mob that is attacking us
                    /// and I have no clue how to do it at the moment.
                    /// That way we can also know the location of the mob
                    /// in case we want to move closer in order to be able to fight
                    /// if it's a caster
                    /// 
                    /// Idea #1:
                    /// get the mob GUID (we have it) 
                    /// get the location of that mob
                    /// turn in order to face it
                    /// send TAB and check the current target GUID and keep
                    /// TABbing until the GUID matches
                    /// 
                    /// Should we implement this in the cscript? (I think so)
                    CurrentState = PlayerState.PreCombat;
                }
            }

            if (CurrentState == PlayerState.PreCombat)
            {
                CurrentState = PlayerState.InCombat;
            }

            if (CurrentState == PlayerState.InCombat)
            {
                /// We should check if our target died and
                /// in that case go to PostCombat
                if (ProcessManager.Player.IsTargetDead())
                {
                    CurrentState = PlayerState.PostCombat;
                }
            }

            if (CurrentState == PlayerState.PostCombat)
            {
                /// We should check if we need to rest
                CurrentState = PlayerState.PreRest;
            }

            if (CurrentState == PlayerState.PreRest)
            {
                /// We should check if we finished resting
                CurrentState = PlayerState.Rest;
            }

            if (CurrentState == PlayerState.Rest)
            {
                /// We should check if we finished resting
                CurrentState = PlayerState.PostRest;
            }

            if (CurrentState == PlayerState.PostRest)
            {
                /// We finished resting, go back to roaming
                CurrentState = PlayerState.Roaming;
            }

            if (ProcessManager.Player.IsDead())
            {
                CurrentState = PlayerState.Dead;
            }

            if (ProcessManager.Player.IsGhost())
            {
                CurrentState = PlayerState.Dead;
            }

            if (ProcessManager.Player.IsAtGraveyard())
            {
                CurrentState = PlayerState.Graveyard;
            }
        }
    }
}