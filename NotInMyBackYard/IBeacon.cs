using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotInMyBackYard
{
    public interface IBeacon
    {
        string Name { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }

        double Range { get; set; }

        bool Active { get; set; }

        bool CanRecoverVessel(Vessel vessel);

        double GreatCircleDistance(Vessel vessel);

        double GreatCircleDistance(double radius, double latitude, double longitude);
    }
}
