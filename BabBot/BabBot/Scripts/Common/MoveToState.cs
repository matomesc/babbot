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
using System.Threading;
using System.Windows.Forms;
using BabBot.Common;
using BabBot.Manager;
using BabBot.Wow;
using Pather.Graph;
using BabBot.States;

namespace BabBot.Scripts.Common
{
    public class MoveToState : State<WowPlayer>
    {
        protected float _LastDistance;

        public MoveToState(Vector3D Destination)
        {
            SetDefaults(Destination);
        }

        public MoveToState(Vector3D Destination, float iTolerance)
        {
            SetDefaults(Destination, iTolerance);
        }

        public MoveToState(Path TravelPath)
        {
            this.TravelPath = TravelPath;

            SetDefaults(new Vector3D());
        }

        public Vector3D Destination { get; protected set; }
        public Path TravelPath { get; protected set; }
        public Location CurrentWaypoint { get; protected set; }
        public float Tolerance { get; protected set; }

        protected void SetDefaults(Vector3D iDestination, float iTolerance)
        {
            Destination = iDestination;
            Tolerance = iTolerance;
        }

        protected void SetDefaults(Vector3D iDestination)
        {
            SetDefaults(iDestination, 1.0f);
        }

        protected override void DoEnter(WowPlayer Entity)
        {
            /*
            //if travel path is not defined then generate from location points
            if (TravelPath == null)
            {
                //get current and destination as ppather locations
                var currentLocation = new Location(Entity.Location.X, Entity.Location.Y, Entity.Location.Z);
                var destinationLocation = new Location(Destination.X, Destination.Y, Destination.Z);
                //calculate and store travel path
                Output.Instance.Script("Calculating path started.", this);
                TravelPath = ProcessManager.Caronte.CalculatePath(currentLocation, destinationLocation);
                //TravelPath.locations = new List<Location>(TravelPath.locations.Distinct<Location>());
                Output.Instance.Script("Calculating path finished.", this);
            }

            //if there are locations then set first waypoint
            if (TravelPath.locations.Count > 0)
            {
                CurrentWaypoint = TravelPath.RemoveFirst();

                //Entity.Face(new Vector3D(CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z));
                _LastDistance = WaypointVector3DHelper.Vector3DToLocation(Entity.Location).GetDistanceTo(CurrentWaypoint);

                //if the distance to the next waypoint is less then 1f, use the get next waypoint method
                if (_LastDistance < 3f)
                {
                    CurrentWaypoint = GetNextWayPoint();
                    _LastDistance =
                        WaypointVector3DHelper.Vector3DToLocation(Entity.Location).GetDistanceTo(CurrentWaypoint);
                }
            }
             * */
        }

        protected override void DoExecute(WowPlayer Entity)
        {
            //if travel path is not defined then generate from location points
            if (TravelPath == null)
            {
                //get current and destination as ppather locations
                var currentLocation = new Location(Entity.Location.X, Entity.Location.Y, Entity.Location.Z);
                var destinationLocation = new Location(Destination.X, Destination.Y, Destination.Z);
                //calculate and store travel path
                Output.Instance.Script("Calculating path started.", this);
                TravelPath = ProcessManager.Caronte.CalculatePath(currentLocation, destinationLocation);
                //TravelPath.locations = new List<Location>(TravelPath.locations.Distinct<Location>());
                Output.Instance.Script("Calculating path finished.", this);
            }

            //if there are locations then set first waypoint
            if (TravelPath.locations.Count > 0)
            {
                CurrentWaypoint = TravelPath.RemoveFirst();

                //Entity.Face(new Vector3D(CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z));
                _LastDistance = WaypointVector3DHelper.Vector3DToLocation(Entity.Location).GetDistanceTo(CurrentWaypoint);

                //if the distance to the next waypoint is less then 1f, use the get next waypoint method
                while ((_LastDistance < 5f) && (CurrentWaypoint != null))
                {
                    CurrentWaypoint = GetNextWayPoint();
                    _LastDistance =
                        WaypointVector3DHelper.Vector3DToLocation(Entity.Location).GetDistanceTo(CurrentWaypoint);
                }
            }


            //on execute, first verify we have a waypoit to follow, else exit
            if (CurrentWaypoint == null)
            {
                Exit(Entity);
                return;
            }

            //const CommandManager.ArrowKey key = CommandManager.ArrowKey.Up;


            // Move on...
            float distance = MathFuncs.GetDistance(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint),
                                                   Entity.Location, false);
            //Entity.PlayerCM.ArrowKeyDown(key);

            /// We face our destination waypoint while we are already moving, so that it looks 
            /// more human-like
            float angle = MathFuncs.GetFaceRadian(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint),
                                                  Entity.Location);

            Output.Instance.Script(string.Format("Entity Location: X:{0} Y:{1} Z:{2}", Entity.Location.X, Entity.Location.Y, Entity.Location.Z), this);
            Output.Instance.Script(string.Format("Current Waypoint: X:{0} Y:{1} Z:{2}", CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z), this);
            //Entity.FaceUsingMemoryWrite(angle, true);

            // Start profiler for WayPointTimeOut
            DateTime start = DateTime.Now;

            do
            {
                float currentDistance = distance;
                while (distance > Tolerance)
                {
                    if (Math.Abs(currentDistance - distance) < 0.1f)
                    {
                        DoExit(Entity);
                        return;
                        //Output.Instance.Script(string.Format("Stuck, ClickToMove(X:{0} Y:{1} Z:{2})", CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z), this);
                        //Entity.ClickToMove(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint));
                    }
                    Thread.Sleep(250);
                    currentDistance = distance;
                    distance = MathFuncs.GetDistance(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint),
                                                     Entity.Location, false);
                }

                    Output.Instance.Script(string.Format("Distance: {0}", distance));
                    Thread.Sleep(50);
                    Application.DoEvents();

                    DateTime end = DateTime.Now;
                    TimeSpan tsTravelTime = end - start;


                    distance = MathFuncs.GetDistance(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint),
                                                     Entity.Location, false);
                    angle = MathFuncs.GetFaceRadian(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint),
                                      Entity.Location);
                    //Entity.FaceUsingMemoryWrite(angle, true);
                    Output.Instance.Script(string.Format("ClickToMove(X:{0} Y:{1} Z:{2})", CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z), this);
                    Entity.ClickToMove(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint));
                    Thread.Sleep(1000);
                    // we take as granted that we should move at least 0.1 yards per cycle (might be a good idea to get this routine synchronized so that 
                    // we can actually know exactly how much we move "per-tick")
                    
                    //if (Math.Abs(currentDistance - distance) < 0.1f && Math.Abs(currentDistance - distance) > 0.0001f)
                    if (Math.Abs(currentDistance - distance) < 0.1f)
                    {
                        Output.Instance.Script(string.Format("Stuck! Distance difference: {0}", Math.Abs(currentDistance - distance)), this);
                        //Entity.Unstuck();
                    }
                
                    //repoint at the waypoint if we are getting off course
                    //angle = MathFuncs.GetFaceRadian(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint), Entity.Location);
                    //if (Math.Abs(Entity.Rotation - angle) > 0.1f)
                    //{
                    //    Entity.FaceUsingMemoryWrite(angle, false);
                    //}

                    // We release every 250 ms
                    /*
                    if (tsTravelTime.TotalMilliseconds > 250)
                    {
                        Output.Instance.Script(string.Format("Releasing after 250ms"), this);
                        Finish(Entity);
                        Exit(Entity);
                        //Entity.PlayerCM.ArrowKeyUp(key);
                        return;
                    }
                */
                //}



                Output.Instance.Script("Getting next waypoint", this);
                CurrentWaypoint = GetNextWayPoint();
                if (CurrentWaypoint == null) break;
                Output.Instance.Script(string.Format("Entity Location: X:{0} Y:{1} Z:{2}", Entity.Location.X, Entity.Location.Y, Entity.Location.Z), this);
                Output.Instance.Script(string.Format("Current Waypoint: X:{0} Y:{1} Z:{2}", CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z), this);
                distance = MathFuncs.GetDistance(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint),
                                 Entity.Location, false);
                angle = MathFuncs.GetFaceRadian(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint),
                                      Entity.Location);
                //Entity.FaceUsingMemoryWrite(angle, true);
                Output.Instance.Script(string.Format("ClickToMove(X:{0} Y:{1} Z:{2})", CurrentWaypoint.X, CurrentWaypoint.Y, CurrentWaypoint.Z), this);
                Entity.ClickToMove(WaypointVector3DHelper.LocationToVector3D(CurrentWaypoint));
                Thread.Sleep(3000);
            } while (CurrentWaypoint != null);
   
            //get next waypoint (may be null)
            //CurrentWaypoint = GetNextWayPoint();

            //if (CurrentWaypoint == null)
            //{
                Finish(Entity);
                Exit(Entity);
                //stop going forward
                //Entity.PlayerCM.ArrowKeyUp(key);
            //}
        }

        protected Location GetNextWayPoint()
        {
            Location Next = null;

            //get the next waypoint
            // The only criteria is that the next waypoint be at least 3 yards away from current
            // if all fail then skip to end
            while (TravelPath.GetFirst() != null)
            {
                //get next waypoint and remove it from the list at the same time
                Next = TravelPath.RemoveFirst();

                //check distance to the waypoint
                float distance = CurrentWaypoint.GetDistanceTo(Next);

                //if distance greater then 3f then return this waypoint
                if (distance > 3f)
                {
                    break;
                }
            }

            return Next;
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
            return;
        }
    }
}