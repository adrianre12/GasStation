using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catopia.GasStation
{
    internal class GasTanks
    {
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
            /// Add gas to tank
            /// </summary>
            /// <param name="Amount"></param>
            /// <returns>the amount gas added</returns>
            public double Fill(int Amount)
            {
                if (tank.FilledRatio == 1)
                    return 0;

                double newRatio = (double)Amount / (double)tank.Capacity + tank.FilledRatio;

                if (newRatio > 1)
                {
                    var tmp = Free;
                    tank.ChangeFilledRatio(1,true);
                    return tmp;
                }

                tank.ChangeFilledRatio(newRatio,true);
                return Amount;
            }

            /// <summary>
            /// Remove gas from tank
            /// </summary>
            /// <param name="Amount"></param>
            /// <returns>the amount of gas removed</returns>
            public double Drain(int Amount)
            {
                if (tank.FilledRatio == 0)
                    return 0;

                double newRatio = tank.FilledRatio - (double)Amount / (double)tank.Capacity ;
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
            foreach ( var gasTank in  tanks)
            {
                if (gasTank.tank.MarkedForClose)
                    return true;
            }
            return false;
        }

        public double TotalCapacity { get {
                double totalCapacity = 0;
                foreach (var tank in tanks)
                {
                    totalCapacity += tank.Capacity;
                }
                return totalCapacity; } 
        }
        public double TotalAvailable {  get {
                double totalAvailable = 0;
                foreach(var tank in tanks) { 
                    totalAvailable += tank.Available; 
                }
                return totalAvailable; } 
        }
        public double TotalFree { get { return TotalCapacity - TotalAvailable; } }

        public GasTanks() {
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
        public int Fill(int AmountRequest, int tankSpread)
        {
            double amount = AmountRequest;
            int ammountPerTank = AmountRequest/tankSpread;
            int remainder = AmountRequest % tankSpread;
            tanks.Sort(
                delegate (GasTank p1, GasTank p2)
                {
                    return p1.tank.FilledRatio.CompareTo(p2.tank.FilledRatio);
                });
            foreach (var tank in tanks)
            {
                amount -= tank.Fill(ammountPerTank + remainder);
                remainder = 0;
                if (amount <= 0)
                    break;
            }
            return AmountRequest - (int)amount;
        }

        /// <summary>
        /// Remove gas from tank
        /// </summary>
        /// <returns>the amount of gas removed</returns>
        public int Drain(int AmountRequest, int tankSpread)
        {
            double amount = AmountRequest;
            int ammountPerTank = AmountRequest / tankSpread;
            int remainder = AmountRequest % tankSpread;
            tanks.Sort(
                delegate (GasTank p1, GasTank p2)
            {
                return p2.tank.FilledRatio.CompareTo(p1.tank.FilledRatio);
            });
            foreach (var tank in tanks)
            {
                amount -= tank.Drain(ammountPerTank + remainder);
                remainder = 0;
                if (amount <= 0)
                    break;
            }
            return AmountRequest - (int)amount;
        }
    }
}
