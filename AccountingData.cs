using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StonksAccounting
{
    public class AccountingData
    {
        /// <summary>
        /// The current cash balance of the player.
        /// </summary>
        public float CashBalance { get; set; }
        /// <summary>
        /// The current online balance of the player.
        /// </summary>
        public float OnlineBalance { get; set; }
        /// <summary>
        /// Current day's transactions (gains/losses on cash and online).
        /// </summary>
        public AccountingTransactions CurrentDayTransaction { get; set; } = new AccountingTransactions();
        /// <summary>
        /// A dictionary to store the transaction history, where the key is the day number and the value is a Transactions object.
        /// </summary>
        public Dictionary<int, AccountingTransactions> TransactionHistory { get; set; } = new Dictionary<int, AccountingTransactions>();

        public Dictionary<int, float> GetSevenDayTotalProfitPerDayForGraph()
        {
            Dictionary<int, float> result = new Dictionary<int, float>();

            if (TransactionHistory.Count < 6)
            {
                var orderedTransactions = TransactionHistory
                    .OrderByDescending(x => x.Key)
                    .ToList();
                foreach (var transaction in orderedTransactions)
                {
                    float total = transaction.Value.cashGain + transaction.Value.cashLoss + transaction.Value.onlineGain + transaction.Value.onlineLoss;
                    result[transaction.Value.dayNumber] = total;
                }
            }
            else
            {
                var orderedTransactions = TransactionHistory
                    .OrderByDescending(x => x.Key)
                    .ToList(); // Convert to a list to preserve the order

                // Iterate over the last 6 transactions
                for (int i = 0; i < 6; i++)
                {
                    float total = orderedTransactions[i].Value.cashGain + orderedTransactions[i].Value.cashLoss + orderedTransactions[i].Value.onlineGain + orderedTransactions[i].Value.onlineLoss;
                    result[orderedTransactions[i].Value.dayNumber] = total;
                }
            }

            float currentTotal = CurrentDayTransaction.cashGain + CurrentDayTransaction.cashLoss + CurrentDayTransaction.onlineGain + CurrentDayTransaction.onlineLoss;
            result[CurrentDayTransaction.dayNumber] = currentTotal;

            return result;
        }

        public Dictionary<int, float> GetSevenDayTotalMoneyPerDayForGraph()
        {
            Dictionary<int, float> result = new Dictionary<int, float>();

            if (TransactionHistory.Count < 6)
            {
                var orderedTransactions = TransactionHistory
                    .OrderByDescending(x => x.Key)
                    .ToList();
                foreach (var transaction in orderedTransactions)
                {
                    float total = transaction.Value.cashGain + transaction.Value.cashLoss + transaction.Value.onlineGain + transaction.Value.onlineLoss + transaction.Value.startCash + transaction.Value.startOnline;
                    result[transaction.Value.dayNumber] = total;
                }
            }
            else
            {
                var orderedTransactions = TransactionHistory
                    .OrderByDescending(x => x.Key)
                    .ToList(); // Convert to a list to preserve the order

                // Iterate over the last 6 transactions
                for (int i = 0; i < 6; i++)
                {
                    float total = orderedTransactions[i].Value.cashGain + orderedTransactions[i].Value.cashLoss + orderedTransactions[i].Value.onlineGain + orderedTransactions[i].Value.onlineLoss+ orderedTransactions[i].Value.startCash + orderedTransactions[i].Value.startOnline;
                    result[orderedTransactions[i].Value.dayNumber] = total;
                }
            }

            float currentTotal = CurrentDayTransaction.cashGain + CurrentDayTransaction.cashLoss + CurrentDayTransaction.onlineGain + CurrentDayTransaction.onlineLoss+ CurrentDayTransaction.startCash + CurrentDayTransaction.startOnline;
            result[CurrentDayTransaction.dayNumber] = currentTotal;

            return result;
        }

        /// <summary>
        /// Gets the cash gain/loss for the last 7 days, including today.
        /// </summary>
        /// <param name="isGain">Do we get Gain or Loss</param>
        /// <returns></returns>
        public float GetSevenDayCash(bool isGain)
        {
            float amount = 0f;

            //Add the current day's transactions to the amount
            if (isGain)
                amount += CurrentDayTransaction.cashGain;
            else
                amount += CurrentDayTransaction.cashLoss;

            //If we have less than 6 transactions, we can just add them all
            if (TransactionHistory.Count < 6)
            {
                foreach (var transaction in TransactionHistory)
                {
                    if (isGain)
                        amount += transaction.Value.cashGain;
                    else
                        amount += transaction.Value.cashLoss;
                }
            }
            else
            {
                // Order the dictionary by Key in descending order
                var orderedTransactions = TransactionHistory
                    .OrderByDescending(x => x.Key)
                    .ToList(); // Convert to a list to preserve the order

                // Iterate over the last 6 transactions
                for (int i = 0; i < 6; i++)
                {
                    if (isGain)
                        amount += orderedTransactions[i].Value.cashGain;
                    else
                        amount += orderedTransactions[i].Value.cashLoss;
                }
            }

            return amount;
        }

        /// <summary>
        /// Gets the online gain/loss for the last 7 days, including today.
        /// </summary>
        /// <param name="isGain">Do we get Gain or Loss</param>
        /// <returns></returns>
        public float GetSevenDayOnline(bool isGain)
        {
            float amount = 0f;

            //Add the current day's transactions to the amount
            if (isGain)
                amount += CurrentDayTransaction.onlineGain;
            else
                amount += CurrentDayTransaction.onlineLoss;

            //If we have less than 6 transactions, we can just add them all
            if (TransactionHistory.Count < 6)
            {
                foreach (var transaction in TransactionHistory)
                {
                    if (isGain)
                        amount += transaction.Value.onlineGain;
                    else
                        amount += transaction.Value.onlineLoss;
                }
            }
            else
            {
                // Order the dictionary by Key in descending order
                var orderedTransactions = TransactionHistory
                    .OrderByDescending(x => x.Key)
                    .ToList(); // Convert to a list to preserve the order

                // Iterate over the last 6 transactions
                for (int i = 0; i < 6; i++)
                {
                    if (isGain)
                        amount += orderedTransactions[i].Value.onlineGain;
                    else
                        amount += orderedTransactions[i].Value.onlineLoss;
                }
            }

            return amount;
        }
    }

    public class AccountingTransactions
    {
        public int dayNumber;
        public float cashGain;
        public float cashLoss;

        public float onlineGain;
        public float onlineLoss;

        public float startCash;
        public float startOnline;
        public float endCash;
        public float endOnline;
    }
}
