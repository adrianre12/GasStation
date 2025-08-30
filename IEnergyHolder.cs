namespace Catopia.GasStation.Energy
{
    internal interface IEnergyHolder
    {

        /// <summary>
        /// Total capacity of the container
        /// </summary>
        double Capacity { get; }

        /// <summary>
        /// Available energy in holder
        /// </summary>
        double Available { get; }

        /// <summary>
        /// Free capacity
        /// </summary>
        double Free { get; }

        //
        // Summary:
        //     Gets the current fill level as a value between 0 (empty) and 1 (full).
        double FilledRatio { get; }

        /// <summary>
        /// Add to holder, rate limited
        /// </summary>
        /// <param name="Amount"></param>
        /// <returns>the amount added</returns>
        double Fill(long Amount);


        /// <summary>
        /// Remove from holder, rate unlimited
        /// </summary>
        /// <param name="Amount"></param>
        /// <returns>the amount of removed</returns>
        double Drain(long Amount);

        /// <summary>
        /// Has the block been marked for close
        /// </summary>
        bool IsMarkedForClose { get; }
    }

}
