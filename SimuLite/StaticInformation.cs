using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimuLite
{
    public static class StaticInformation
    {
        public static bool IsSimulating { get; set; } = false;
        public static double RemainingCoreHours { get; set; } = 0;
        public static double CurrentComplexity { get; set; } = 0;

        public static EditorFacility LastEditor = EditorFacility.None;
        public static ConfigNode LastShip = null;
    }
}
