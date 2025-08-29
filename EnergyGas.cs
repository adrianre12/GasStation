using Sandbox.ModAPI;
using System;

namespace Catopia.GasStation.Energy
{
    /// <summary>
    /// Base Class for Gas Energy tanks
    /// </summary>
    internal class EnergyGas : IEnergyHolder
    {

        private const double FillRatioDelta = 1.6 / 20; //1.6=100 ticks, 20s time to fill at vanilla stockpile rate

        private IMyGasTank tank;

        public EnergyGas(IMyGasTank tank)
        {
            this.tank = tank;
        }

        /// <summary>
        /// Total capacity of the tank
        /// </summary>
        public double Capacity { get { return (double)tank.Capacity; } }

        /// <summary>
        /// Available gas in tank
        /// </summary>
        public double Available { get { return (tank.Capacity * tank.FilledRatio); } }

        /// <summary>
        /// Free capacity in the tank
        /// </summary>
        public double Free { get { return (tank.Capacity * (1 - tank.FilledRatio)); } }

        public double FilledRatio
        {
            get
            {
                return tank.FilledRatio;
            }
        }

        /// <summary>
        /// Add gas to tank, rate limited
        /// </summary>
        /// <param name="Amount"></param>
        /// <returns>the amount gas added</returns>
        public double Fill(long Amount)
        {
            if (tank.FilledRatio == 1)
                return 0;

            double ratioDelta = Math.Min((double)Amount / (double)tank.Capacity, FillRatioDelta);
            double newRatio = tank.FilledRatio + ratioDelta; ;

            if (newRatio > 1)
            {
                var tmp = Free;
                tank.ChangeFilledRatio(1, true);
                return tmp;
            }

            tank.ChangeFilledRatio(newRatio, true);
            return ratioDelta * tank.Capacity;
        }

        /// <summary>
        /// Remove gas from tank, rate unlimited
        /// </summary>
        /// <param name="Amount"></param>
        /// <returns>the amount of gas removed</returns>
        public double Drain(long Amount)
        {
            if (tank.FilledRatio == 0)
                return 0;

            double newRatio = tank.FilledRatio - (double)Amount / (double)tank.Capacity;
            //Log.Msg($"newRatio={newRatio}, Available={Available} Amount={Amount} Cpacity={tank.Capacity} filledRatio={tank.FilledRatio} delta={(double)Amount / (double)tank.Capacity}");
            if (newRatio < 0)
            {
                var tmp = Available;
                tank.ChangeFilledRatio(0, true);
                return tmp;
            }
            tank.ChangeFilledRatio(newRatio, true);
            return Amount;

        }

        public bool IsMarkedForClose
        {
            get { return tank.MarkedForClose; }
        }

    }
}
