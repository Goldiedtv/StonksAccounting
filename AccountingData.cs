using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StonksAccounting
{
    public class AccountingData
    {
        public float CashBalance { get; set; }
        public float OnlineBalance { get; set; }
        public Transactions? CurrentDayTransaction { get; set; } = new Transactions();
        public Dictionary<int, Transactions> TransactionHistory { get; set; } = new Dictionary<int, Transactions>();
    }

    public class Transactions
    {
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
