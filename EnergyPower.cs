using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;

namespace Catopia.GasStation.Energy
{
    internal class EnergyPower : IEnergyHolder
    {
        private const double maxChargeDeltaRatio = 1.6 / 20; //1.6=100 ticks, 20s time to fill at vanilla stockpile rate

        private MyBatteryBlock battery;

        public EnergyPower(IMyBatteryBlock battery)
        {
            this.battery = battery as MyBatteryBlock;
        }

        public double Capacity { get { return battery.MaxStoredPower * 1000000; } }

        public double Available { get { return battery.CurrentStoredPower * 1000000; } }

        public double Free { get { return Capacity - Available; } }

        public double FilledRatio { get { return Available / Capacity; } }

        public double Fill(long Amount)
        {
            if (Free == 0)
                return 0;

            var amount = Math.Min(Amount, maxChargeDeltaRatio * Capacity);

            if (amount > Free)
            {
                var free = Free;
                battery.CurrentStoredPower = battery.MaxStoredPower;
                return free;
            }

            battery.CurrentStoredPower += (float)amount / 1000000;
            return amount;
        }

        public double Drain(long Amount)
        {
            if (Free == 0)
                return 0;

            if (Amount > Available)
            {
                var available = Available;
                battery.CurrentStoredPower = 0;
                return available;
            }

            battery.CurrentStoredPower -= Amount / 1000000;
            return Amount;
        }

        public bool IsMarkedForClose { get { return battery.MarkedForClose; } }
    }
}
