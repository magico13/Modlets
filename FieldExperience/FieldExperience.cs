using UnityEngine;

namespace FieldExperience
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FieldExperience : MonoBehaviour
    {
        public static void CheckExperience()
        {
            if (HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return;

            foreach (ProtoCrewMember crew in HighLogic.CurrentGame.CrewRoster.Crew)
            {
                if (crew.rosterStatus == ProtoCrewMember.RosterStatus.Assigned)
                {
                    FlightLog careerCopy = crew.careerLog.CreateCopy();
                    FlightLog flightCopy = crew.flightLog.CreateCopy();

                    flightCopy.MergeWith(careerCopy);

                    float careerXP = KerbalRoster.CalculateExperience(careerCopy);
                    float flightXP = KerbalRoster.CalculateExperience(flightCopy);
                    crew.experience = flightXP;
                    crew.experienceLevel = KerbalRoster.CalculateExperienceLevel(flightXP);

                    Debug.Log(crew.name + " - EXP (career): " + careerXP + " EXP (flight): " + flightXP + " LVL: " + crew.experienceLevel);
                }
            }
        }
    }



    [KSPAddon(KSPAddon.Startup.EveryScene, true)]
    class Launcher : MonoBehaviour
    {
        public void Start()
        {
            GameEvents.onGameStateLoad.Add(onGameStateLoad);
        }

        public void onGameStateLoad(ConfigNode root)
        {
            FieldExperience.CheckExperience();
        }

    }
}
/*
Copyright (C) 2016  Michael Marvin

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/