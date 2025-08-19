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
            public float Capacity { get { return tank.Capacity; } }

            /// <summary>
            /// Available gas in tank
            /// </summary>
            public float Available { get { return (float)(tank.Capacity * tank.FilledRatio); } }

            /// <summary>
            /// Free capacity in the tank
            /// </summary>
            public float Free { get { return (float)(tank.Capacity * (1 - tank.FilledRatio)); } }

            /// <summary>
            /// Add gas to tank
            /// </summary>
            /// <param name="Amount"></param>
            /// <returns>the amount gas not added</returns>
            public double Fill(double Amount)
            {
                if (tank.FilledRatio == 1)
                    return Amount;

                double newRatio = Amount / tank.Capacity + tank.FilledRatio;

                if (newRatio > 1)
                {
                    var remainder = Amount - Free;
                    tank.ChangeFilledRatio(1,true);
                    return remainder;
                }

                tank.ChangeFilledRatio(newRatio,true);
                return 0;
            }

            /// <summary>
            /// Remove gas from tank
            /// </summary>
            /// <param name="Amount"></param>
            /// <returns>the amount of gas not removed</returns>
            public double Drain(double Amount)
            {
                if (tank.FilledRatio == 0)
                    return Amount;

                double newRatio = tank.FilledRatio - Amount / tank.Capacity ;
                if (newRatio < 0)
                {
                    var remainder = Amount - Available;
                    tank.ChangeFilledRatio(0, true);
                    return remainder;
                }
                tank.ChangeFilledRatio(newRatio, true);
                return 0;
                
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
        /// <param name="AmountRequest"></param>
        /// <returns>the amount gas added</returns>
        public double Fill(double AmountRequest)
        {
            double amount = AmountRequest;
            foreach (var tank in tanks)
            {
                amount = tank.Fill(amount);
                if (amount <= 0)
                    break;
            }
            return AmountRequest - amount;
        }

        /// <summary>
        /// Remove gas from tank
        /// </summary>
        /// <param name="AmountRequest"></param>
        /// <returns>the amount of gas removed</returns>
        public double Drain(double AmountRequest)
        {
            double amount = AmountRequest;
            foreach (var tank in tanks)
            {
                amount = tank.Drain(amount);
                if (amount <= 0)
                    break;
            }
            return AmountRequest - amount;
        }
    }
}
