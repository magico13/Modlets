using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotInMyBackYard
{
    public class Beacon
    {
        public string name = "";
        public double latitude = 0, longitude = 0, range = 0;

        public Beacon(string beaconName, double lat, double lon, double beaconRange)
        {
            name = beaconName;
            latitude = lat;
            longitude = lon;
            range = beaconRange;
        }

        public Beacon(ConfigNode node)
        {
            name = node.GetValue("name");
            double.TryParse(node.GetValue("latitude"), out latitude);
            double.TryParse(node.GetValue("longitude"), out longitude);
            double.TryParse(node.GetValue("range"), out range);
        }

        public ConfigNode AsNode()
        {
            ConfigNode retNode = new ConfigNode("Beacon");
            retNode.AddValue("name", name);
            retNode.AddValue("latitude", latitude);
            retNode.AddValue("longitude", longitude);
            retNode.AddValue("range", range);

            return retNode;
        }

        public double GreatCircleDistance(Vessel vessel)
        {
            if (vessel == null)
                return 0;

            //http://www.movable-type.co.uk/scripts/latlong.html
            double radius = vessel.mainBody.Radius;
            double delLat = (latitude - vessel.latitude) * Math.PI / 180;
            double delLon = (longitude - vessel.longitude) * Math.PI / 180;
            double lat1 = latitude * Math.PI / 180;
            double lat2 = vessel.longitude * Math.PI / 180;

            double a = Math.Pow(Math.Sin(delLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(delLon / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = radius * c;

            return Math.Sqrt(d * d);
        }
    }
}
