using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FIXApi;
using TDFAPI;
using System.Threading;

namespace HFT
{
    public partial class StockTrend : Form
    {
        public Dictionary<string, SingleStockData> dataDict;

        public DataClass dc;
        public FIXTrade tc;
        public EngineClass ec;
        public MainForm MF;
        public TDXTrade tdx;
        
        public StockTrend()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string tickerListStr = dc.LoadDataDict();
            dc.SetSubscription(tickerListStr, SubscriptionType.SUBSCRIPTION_SET);

            while (dataDict.Count < 1)
                Thread.Sleep(100);

            Thread th = new Thread(new ThreadStart(ec.Run));
            th.Start();
        }
    }
}
