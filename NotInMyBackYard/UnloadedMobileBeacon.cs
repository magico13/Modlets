using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotInMyBackYard
{
    public class UnloadedMobileBeacon : IBeacon
    {
        private Vessel _vessel = null;
        public string Name
        {
            get
            {
                return _vessel.GetDisplayName();
            }
            set { }
        }

        public double Latitude
        {
            get
            {
                return _vessel.latitude;
            }
            set { }
        }

        public double Longitude
        {
            get
            {
                return _vessel.longitude;
            }
            set { }
        }

        public double Range
        {
            get
            {
                return NotInMyBackYard.MobileBeaconRange;
            }
            set { }
        }

        public bool Active
        {
            get
            {
                return NotInMyBackYard.MobileBeaconRequirementsMet(_vessel);
            }

            set { }
        }

        public UnloadedMobileBeacon(Vessel vessel)
        {
            _vessel = vessel;
        }


        public bool CanRecoverVessel(Vessel vessel)
        {
            return vessel.id != _vessel.id && GreatCircleDistance(vessel.mainBody.Radius, vessel.latitude, vessel.longitude) < Range;
        }

        public double GreatCircleDistance(double radius, double latitude, double longitude)
        {
            return NotInMyBackYard.DefaultGreatCircleDistance(radius, Latitude, Longitude, latitude, longitude);
        }

        public double GreatCircleDistance(Vessel vessel)
        {
            return GreatCircleDistance(vessel.mainBody.Radius, vessel.latitude, vessel.longitude);
        }
    }
}
