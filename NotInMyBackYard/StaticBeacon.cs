using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotInMyBackYard
{
    public class StaticBeacon : IBeacon
    {

        public string Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Range { get; set; }

        public bool Active { get { return Range > 0; } set { } }

        public StaticBeacon(string beaconName, double lat, double lon, double beaconRange)
        {
            Name = beaconName;
            Latitude = lat;
            Longitude = lon;
            Range = beaconRange;
        }

        public StaticBeacon(ConfigNode node)
        {
            Name = node.GetValue("name");
            double latitude, longitude, range;

            double.TryParse(node.GetValue("latitude"), out latitude);
            double.TryParse(node.GetValue("longitude"), out longitude);
            double.TryParse(node.GetValue("range"), out range);

            Latitude = latitude;
            Longitude = longitude;
            Range = range;
        }

        public ConfigNode AsNode()
        {
            ConfigNode retNode = new ConfigNode("Beacon");
            retNode.AddValue("name", Name);
            retNode.AddValue("latitude", Latitude);
            retNode.AddValue("longitude", Longitude);
            retNode.AddValue("range", Range);

            return retNode;
        }

        public double GreatCircleDistance(Vessel vessel)
        {
            if (vessel == null)
            {
                return 0;
            }

            return GreatCircleDistance(vessel.mainBody.Radius, vessel.latitude, vessel.longitude);
        }

        public double GreatCircleDistance(double radius, double latitude, double longitude)
        {
            return NotInMyBackYard.DefaultGreatCircleDistance(radius, Latitude, Longitude, latitude, longitude);
        }

        public bool CanRecoverVessel(Vessel vessel)
        {
            return Active && GreatCircleDistance(vessel) < Range;
        }
    }
}
