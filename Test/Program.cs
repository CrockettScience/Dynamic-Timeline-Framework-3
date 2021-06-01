using System;
using DTF3.Core;
using DTF3.Utilities;

namespace Test
{
    public class Program
    {
        public static Multiverse Multiverse;

        public static void Main(string[] args)
        {
            Multiverse =
                new Multiverse(
                    "C:\\Users\\cohib\\Documents\\Projects\\DynamicTimelineFramework3\\Test\\DTFObjectsTestCosmos.json");

            var gal = new Galaxy(Multiverse);

            var galaxyContinuity = Multiverse.RootUniverse.GetContinuity(gal);
            var birthDate = 10UL.Billion().Years() + 10;

            galaxyContinuity.Assert(birthDate, new Position("Initial", gal), out _);
            
            var star = new Star(Multiverse, gal);

            var starContinuity = Multiverse.RootUniverse.GetContinuity(star);

            for (var i = 1UL; i < 150UL; i += 1UL)
            {
                Console.WriteLine($"{i} Billion Years: {starContinuity.Measure(i.Billion().Years() + i)}");
            }
        }
    }
}