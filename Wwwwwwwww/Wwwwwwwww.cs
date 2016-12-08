using System.Runtime.InteropServices;
using UnityEngine;

namespace Wwwwwwwww
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Wwwwwwwww : MonoBehaviour
    {
        bool simulatingWalk = false;
        bool simulatingRun = false;

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        public void Update()
        {
            if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.isEVA)
            {
                if (simulatingWalk || simulatingRun)
                {
                    if (Input.GetKeyDown(KeyCode.W))
                    {
                        simulatingWalk = false;
                        simulatingRun = false;
                    }
                    else
                    {
                        if (simulatingRun)
                            keybd_event(0xA0, 0, 0, 0); //left shift //0x0001
                        keybd_event(0x57, 0, 0, 0); //w
                    }
                }
                else
                {
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.W))
                    {
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            simulatingRun = true;
                        }
                        else
                        {
                            simulatingWalk = true;
                        }
                    }
                }
            }
            else
            {
                simulatingRun = false;
                simulatingWalk = false;
            }
        }
    }
}
