using System.Collections.Generic;

namespace Catopia.GasStation.Energy
{
    /// <summary>
    /// Class represents a collection of generic Energy holders (tanks/batteries)
    /// </summary>
    internal class EnergyHolders
    {

        private List<IEnergyHolder> holders;

        public int Count { get { return holders.Count; } }

        public bool HoldersMarkedForClose()
        {
            foreach (var holder in holders)
            {
                if (holder.IsMarkedForClose)
                    return true;
            }
            return false;
        }

        public double TotalCapacity
        {
            get
            {
                double totalCapacity = 0;
                foreach (var holder in holders)
                {
                    totalCapacity += holder.Capacity;
                }
                return totalCapacity;
            }
        }
        public double TotalAvailable
        {
            get
            {
                double totalAvailable = 0;
                foreach (var holder in holders)
                {
                    totalAvailable += holder.Available;
                }
                return totalAvailable;
            }
        }
        public double TotalFree { get { return TotalCapacity - TotalAvailable; } }

        public EnergyHolders()
        {
            holders = new List<IEnergyHolder>();
        }

        public void Clear()
        {
            holders.Clear();
        }

        public void Add(IEnergyHolder energyHolder)
        {
            holders.Add(energyHolder);
        }

        /// <summary>
        /// Add Energy to holders
        /// </summary>
        /// <returns>the amount gas added</returns>
        public long Fill(long AmountRequest)
        {
            double amount = 0;
            long ammountPerTank = AmountRequest / holders.Count;
            long remainder = AmountRequest % holders.Count;
            holders.Sort(
                delegate (IEnergyHolder p1, IEnergyHolder p2)
                {
                    return p1.FilledRatio.CompareTo(p2.FilledRatio);
                });
            foreach (var holder in holders)
            {
                amount += holder.Fill(ammountPerTank + remainder);
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
            long ammountPerTank = AmountRequest / holders.Count; ;
            long remainder = AmountRequest % holders.Count; ;
            holders.Sort(
                delegate (IEnergyHolder p1, IEnergyHolder p2)
            {
                return p2.FilledRatio.CompareTo(p1.FilledRatio);
            });
            foreach (var holder in holders)
            {
                amount += holder.Drain(ammountPerTank + remainder);
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
