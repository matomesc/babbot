﻿/*
    This file is part of BabBot.

    BabBot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BabBot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BabBot.  If not, see <http://www.gnu.org/licenses/>.
  
    Copyright 2009 BabBot Team
*/
using System;
using BabBot.Common;
using BabBot.Wow;
using BabBot.States;
using BabBot.Scripts;

namespace BabBot.Scripts.Common
{
    public class InCombatState : State<WowPlayer>
    {
        protected override void DoEnter(WowPlayer Entity)
        {
        }

        /// <summary>
        /// This routine gets called every time we end up fighting (because we pulled or 
        /// because a mob aggroed)
        /// This should be implemented in the spcific class script
        /// </summary>
        protected override void DoExecute(WowPlayer Entity)
        {
            throw new NotImplementedException("OnInCombat() not implemented.");
        }

        protected override void DoExit(WowPlayer Entity)
        {
            //on exit, if there is a previous state, go back to it
            if (PreviousState != null)
            {
                CallChangeStateEvent(Entity, PreviousState, false, false);
            }
        }

        protected override void DoFinish(WowPlayer Entity)
        {
        }
    }
}