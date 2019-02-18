using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EditorTime 
{
    public class TimeKeeper
    {
        private Settings settings;

        public DateTime lastUpdate = DateTime.MaxValue;
        public DateTime lastOutsource = DateTime.MaxValue;

        public double Time {
            get { return HighLogic.CurrentGame.flightState.universalTime; }
            set { HighLogic.CurrentGame.flightState.universalTime = value; }
        }

        public TimeKeeper(Settings settings)
        {
            this.settings = settings;
        }

        public void Start()
        {
            //to avoid multiple calls messing up our time.
            if (lastUpdate == DateTime.MaxValue)
                lastUpdate = DateTime.Now;
        }

        public void Stop()
        {
            lastUpdate = DateTime.MaxValue;
        }

        public TimeSpan? OutsourceTimer()
        {
            return OutsourceTimer(DateTime.Now);
        }

        public TimeSpan? OutsourceTimer(DateTime now)
        {
            if (lastOutsource == DateTime.MaxValue)
                return null;
            return lastOutsource.AddMinutes(settings.outsourceTime) - now;
        }

        public void Update()
        {
            //Get and save the current time 
            //(we don't call this repeatedly, as it might return slightly different values at various points in the execution and we don't want to lose any time)
            DateTime now = DateTime.Now;

            if (lastUpdate == DateTime.MaxValue)
            {
                TimeSpan? timer = OutsourceTimer(now);
                if (timer.HasValue && timer.Value.Ticks <= 0)
                {
                    //outsourcing is over, resume as normal.
                    lastUpdate = now;
                    lastOutsource = DateTime.MaxValue;
                    //we still let it return without adding any time as it would simply add 0 seconds anyway.
                }
                return; //do not add any time as the game is either not ready or being outsourced.
            }
            
            //Get the amount of time that has passed since the last update
            double timeDelta = (now - lastUpdate).TotalMilliseconds / 1000;

            //Multiply the time passed (in seconds) by the timeRatio
            timeDelta *= settings.timeRatio;
                
            //Update the in-game time
            Time += timeDelta;

            //Make sure we update the lastUpdate to now
            lastUpdate = now;
        }

        //"Outsourcing" is when a player pays to freeze time for a number of minutes.
        //This allows the player to make crafts faster when in a hurry, but at an increased cost.
        public void Outsource()
        {
            //return if player cannot afford or is already outsourcing.
            if (!Funding.CanAfford(settings.outsourceCost)
                || OutsourceTimer().HasValue)
                return;

            Funding.Instance.AddFunds(-settings.outsourceCost, TransactionReasons.None);
            lastUpdate = DateTime.MaxValue;
            lastOutsource = DateTime.Now;
        }
    }
}
