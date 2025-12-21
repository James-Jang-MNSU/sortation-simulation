using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortationEngine
{
    public static class StatUtils
    {
        // Random instance
        private static readonly Random _rand = new Random();

        // Generate a number following a normal distribution
        public static double NextGaussian(double mean, double stdDev)
        {
            // The Box-Muller Transform: Transforms uniformly random numbers into standard normally random numbers
            double u1 = 1.0 - _rand.NextDouble();
            double u2 = 1.0 - _rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2);

            // Shift and strech to the desired normal distribution
            return mean + stdDev * randStdNormal;
        }
    }
}