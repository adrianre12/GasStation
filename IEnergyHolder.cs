namespace Catopia.GasStation.Energy
{
    internal interface IEnergyHolder
    {

        /// <summary>
        /// Total capacity of the container
        /// </summary>
        double Capacity { get; }

        /// <summary>
        /// Available gas in container
        /// </summary>
        double Available { get; }

        /// <summary>
        /// Free capacity in the tank
        /// </summary>
        double Free { get; }

        //
        // Summary:
        //     Gets the current fill level of this tank as a value between 0 (empty) and 1 (full).
        double FilledRatio { get; }

        /// <summary>
        /// Add to container, rate limited
        /// </summary>
        /// <param name="Amount"></param>
        /// <returns>the amount gas added</returns>
        double Fill(long Amount);


        /// <summary>
        /// Remove from container, rate unlimited
        /// </summary>
        /// <param name="Amount"></param>
        /// <returns>the amount of gas removed</returns>
        double Drain(long Amount);

        /// <summary>
        /// Has the block been marked for close
        /// </summary>
        bool IsMarkedForClose { get; }
    }

}
