using Sandbox.ModAPI;
using System;
using System.Collections.Generic;

namespace Catopia.GasStation
{
    internal class GasTanks
    {
        private const double FillRatioDelta = 1.6 / 20; //1.6=100 ticks, 20s time to fill at vanilla stockpile rate

        public struct GasTank
        {
            public IMyGasTank tank;
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
        }

        private List<GasTank> tanks;

        public int Count { get { return tanks.Count; } }

        public bool TanksMarkedForClose()
        {
            foreach (var gasTank in tanks)
            {
                if (gasTank.tank.MarkedForClose)
                    return true;
            }
            return false;
        }

        public double TotalCapacity
        {
            get
            {
                double totalCapacity = 0;
                foreach (var tank in tanks)
                {
                    totalCapacity += tank.Capacity;
                }
                return totalCapacity;
            }
        }
        public double TotalAvailable
        {
            get
            {
                double totalAvailable = 0;
                foreach (var tank in tanks)
                {
                    totalAvailable += tank.Available;
                }
                return totalAvailable;
            }
        }
        public double TotalFree { get { return TotalCapacity - TotalAvailable; } }

        public GasTanks()
        {
            tanks = new List<GasTank>();
        }

        public void Clear()
        {
            tanks.Clear();
        }

        public void Add(IMyGasTank myGasTank)
        {
            GasTank tank = new GasTank();
            tank.tank = myGasTank;

            tanks.Add(tank);
        }

        /// <summary>
        /// Add gas to tanks
        /// </summary>
        /// <returns>the amount gas added</returns>
        public long Fill(long AmountRequest)
        {
            double amount = 0;
            long ammountPerTank = AmountRequest / tanks.Count;
            long remainder = AmountRequest % tanks.Count;
            tanks.Sort(
                delegate (GasTank p1, GasTank p2)
                {
                    return p1.tank.FilledRatio.CompareTo(p2.tank.FilledRatio);
                });
            foreach (var tank in tanks)
            {
                amount += tank.Fill(ammountPerTank + remainder);
                remainder = 0;
                if (amount >= AmountRequest)
                    break;
            }
            return (long)amount;
        }

        /// <summary>
        /// Remove gas from tank
        /// </summary>
        /// <returns>the amount of gas removed</returns>
        public long Drain(long AmountRequest)
        {
            double amount = 0;// AmountRequest;
            long ammountPerTank = AmountRequest / tanks.Count; ;
            long remainder = AmountRequest % tanks.Count; ;
            tanks.Sort(
                delegate (GasTank p1, GasTank p2)
            {
                return p2.tank.FilledRatio.CompareTo(p1.tank.FilledRatio);
            });
            foreach (var tank in tanks)
            {
                amount += tank.Drain(ammountPerTank + remainder);
                remainder = 0;
                //Log.Msg($"Amount drained = {amount}");
                if (amount >= AmountRequest)
                    break;
            }
            //Log.Msg($"Total Amount drained = {amount}");
            return (long)amount;
        }
    }
}
