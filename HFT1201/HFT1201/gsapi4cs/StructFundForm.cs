using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TDFAPI;
using FIXApi;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Data.SqlClient;


namespace HFT
{
    public partial class StructFundForm : Form
    {

        public Dictionary<string, SingleStockData> dataDict;

        public DataClass dc;
        public FIXTrade tc;
        public EngineClass ec;
        public String constr;


        public DataTable orderDT = new DataTable();
        public String SeqNo;


        public StructFundForm()
        {
            InitializeComponent();
            constr = "server=.;database=HedgeHogDB;integrated security=SSPI";
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SeqNo = Convert.ToString(comboBox1.SelectedIndex + 1);
            double openNum = 100;
            if (String.IsNullOrEmpty(textBox2.Text))
            { return; }
            double weight = Convert.ToDouble(textBox2.Text) / 100000;
            DataTable stockDT = new DataTable();
            stockDT.Columns.Add("代码");
            stockDT.Columns.Add("数量");
            stockDT.Columns.Add("剩余可融数");

            SqlConnection conn = new SqlConnection(constr);

            conn.Open();
            string sql = "select StructFundInfo.StockTicker,StructFundInfo.Quantity,LockList.Quantity" +
                         " from StructFundInfo left join LockList on StructFundInfo.StockTicker=LockList.Ticker" +
                          " where StructFundInfo.SeqNo=" + SeqNo;

            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader sqldr = cmd.ExecuteReader();
            while (sqldr.Read())
            {
                DataRow dr = stockDT.NewRow();
                dr["代码"] = sqldr.GetString(0);
                dr["数量"] = Convert.ToString(sqldr.GetInt32(1) * weight);
                dr["剩余可融数"] = Convert.ToString(sqldr.GetInt32(2));
                stockDT.Rows.Add(dr);
                openNum = Math.Min(openNum, Math.Floor(Convert.ToDouble(sqldr.GetInt32(2)) / Convert.ToDouble(sqldr.GetInt32(1))));

            }

            sqldr.Close();
            conn.Close();
            textBox3.Text = Convert.ToString(openNum * 100000);


            dataGridView1.DataSource = stockDT;
            dataGridView1.Refresh();
            orderDT = stockDT.Copy();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            String BTicker = "";
            int BQuantity;
            Dictionary<string, int> structDict = new Dictionary<string, int>();
            List<string> BTickerList = new List<string>();

            SqlConnection conn = new SqlConnection(constr);

            conn.Open();
            string sql = "select SeqNo,BTicker from StructFundInfo group by SeqNo,BTicker";

            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader sqldr = cmd.ExecuteReader();
            while (sqldr.Read())
            {
                string tmpstring = sqldr.GetString(1);
                BTickerList.Add(tmpstring);
            }

            sqldr.Close();
            conn.Close();

            BTicker = BTickerList[Convert.ToInt16(comboBox1.SelectedIndex)];
            BQuantity = Convert.ToInt32(textBox2.Text);

            foreach (DataRow dr in orderDT.Rows)
            { structDict.Add(Convert.ToString(dr["代码"]), Convert.ToInt32(dr["数量"])); }

            ec.OrderList = structDict;
            
            string tickerListStr = dc.LoadDataDict();
            dc.SetSubscription(tickerListStr, SubscriptionType.SUBSCRIPTION_SET);

            if (checkBoxBuyB.Checked)
            {
                string bufferNotUsed = "";
                if (tc != null)
                    tc.SendOrder(OrderType.CreditBuy, BTicker, BQuantity.ToString(), "1.5", true, out bufferNotUsed);
            }
            

            Thread th = new Thread(new ThreadStart(ec.ShortSellTest));
            th.Start();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            String BTicker = "";
            int BQuantity;
            Dictionary<string, int> structDict = new Dictionary<string, int>();
            List<string> BTickerList = new List<string>();

            SqlConnection conn = new SqlConnection(constr);

            conn.Open();
            string sql = "select SeqNo,BTicker from StructFundInfo group by SeqNo,BTicker";

            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader sqldr = cmd.ExecuteReader();
            while (sqldr.Read())
            {
                string tmpstring = sqldr.GetString(1);
                BTickerList.Add(tmpstring);
            }

            sqldr.Close();
            conn.Close();

            BTicker = BTickerList[Convert.ToInt16(comboBox1.SelectedIndex)];
            BQuantity = Convert.ToInt32(textBox2.Text);

            foreach (DataRow dr in orderDT.Rows)
            { structDict.Add(Convert.ToString(dr["代码"]), Convert.ToInt32(dr["数量"])); }

            ec.OrderList = structDict;

            string tickerListStr = dc.LoadDataDict();
            dc.SetSubscription(tickerListStr, SubscriptionType.SUBSCRIPTION_SET);

            string bufferNotUsed = "";
            if (tc != null)
            {
                if (checkBoxBuyB.Checked)
                    tc.SendOrder(OrderType.CreditSell, BTicker, BQuantity.ToString(), "1.5", true, out bufferNotUsed);

                foreach (DataRow dr in orderDT.Rows)
                {
                    tc.SendOrder(OrderType.CreditMarginBuy, Convert.ToString(dr["代码"]), Convert.ToString(dr["数量"]), "10", true, out bufferNotUsed);
                }
            }
        }

        private void buttonBatchShortsell_Click(object sender, EventArgs e)
        {

            Dictionary<string, int> structDict = new Dictionary<string, int>();

            SqlConnection conn = new SqlConnection(constr);

            conn.Open();
            string sql = "select Ticker, Quantity from LockList order by ticker";
            //string sql = "select Ticker, Quantity from LockList where ticker='000002'";

            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader sqldr = cmd.ExecuteReader();
            while (sqldr.Read())
            {
                string tickerString = sqldr.GetString(0);
                int quantitiy = sqldr.GetInt32(1);
                //int quantitiy = 100;
                structDict.Add(tickerString, quantitiy);
            }

            sqldr.Close();
            conn.Close();

            ec.OrderList = structDict;

            string tickerListStr = dc.LoadDataDict();
            dc.SetSubscription(tickerListStr, SubscriptionType.SUBSCRIPTION_SET);


            Thread th = new Thread(new ThreadStart(ec.ShortSellTest));
            th.Start();
        }
    }
}
