using System;
using AssettoCorsaSharedMemory;

namespace ac_mem_app
{
    class Program
    {
        static void Main(string[] args)
        {
            AssettoCorsa ac = new AssettoCorsa();
            ac.PhysicsUpdated += AC_PhysicsUpdated; // Add event listener for StaticInfo
            ac.Start(); // Connect to shared memory and start interval timers 

            Console.ReadKey();
        }

        static void AC_PhysicsUpdated(object sender, PhysicsEventArgs e)
        {
            Console.WriteLine("Pitch = " + e.Physics.Pitch + ", Roll = " + e.Physics.Roll);
        }
    }
}
