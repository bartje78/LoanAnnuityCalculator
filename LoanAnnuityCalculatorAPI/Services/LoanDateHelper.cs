using System;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class LoanDateHelper
    {
        public int CalculateMonthsDifference(DateTime startDate)
        {
            DateTime currentDate = DateTime.Now;

            // Calculate the total difference in months
            int monthsDifference = (currentDate.Year - startDate.Year) * 12 + currentDate.Month - startDate.Month;

            // If the current day is earlier than the start day, subtract one month
            if (currentDate.Day < startDate.Day)
            {
                monthsDifference--;
            }

            return monthsDifference;
        }
    }
}