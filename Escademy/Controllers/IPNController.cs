using Escademy.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Escademy.Controllers
{
    public class IPNController : Controller
    {
        //Profile>My Selling Tools>Website preferences> Update
        /*
         <form action="https://www.sandbox.paypal.com/cgi-bin/webscr" method="post">
            <fieldset>
                <input class="full-width" type="hidden" name="business" value="facilitator@escademy.com">
                <input type="hidden" name="cmd" value="_xclick">
                <input type="hidden" name="item_name" value="Coaching">
                <input type="hidden" name="amount" value="9">
                <input type="hidden" name="currency_code" value="USD" />
    
                <input type="hidden" name="no_shipping" value="1">

                <!-- coach_id,game_id -->
                <input type="hidden" name="custom" value="5,2"> 
                
                <input type=hidden name="RETURNURL"
                       value="https://www.escademy.com/IPN">
                <input type="hidden" name="return" value="https://www.escademy.com/IPN">
                <input type="hidden" name="notify_url" value="https://www.escademy.com/IPN">

                <button type="submit">Order now!</button>
            </fieldset>
        </form>
        */

        // GET: IPN
        public ActionResult Index()
        {
            var order = new CoachOrder(); 

            // Receive IPN request from PayPal and parse all the variables returned
            var formVals = new Dictionary<string, string>();
            formVals.Add("cmd", "_notify-synch"); //notify-synch_notify-validate
            formVals.Add("at", "nrvfK97rEC_fq8xMdrFG3EjQTi2Pv4hhWzKK0dIKd4fK6-TEmizy9wBjI4G"); 
            formVals.Add("tx", Request["tx"]);

            // if you want to use the PayPal sandbox change this from false to true
            string response = GetPayPalResponse(formVals, useSandbox: true);

            if (response.Contains("SUCCESS"))
            {
                string transactionID = GetPDTValue(response, "txn_id"); // txn_id //d
                string sAmountPaid = GetPDTValue(response, "mc_gross"); // d
                string sCurrency = GetPDTValue(response, "mc_currency");

                var custom_info = GetPDTValue(response, "custom").Split(','); // d
                string payerEmail = GetPDTValue(response, "payer_email"); // d
                string Item = GetPDTValue(response, "item_name");
                string payment_status = GetPDTValue(response, "payment_status");

                string sQuantity = custom_info[2];
                int.TryParse(sQuantity, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out int quantity);
                    
                string sPayerId = custom_info[3];
                int.TryParse(sPayerId, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out int payer_account_id);

                //validate the order
                decimal amountPaid = 0;
                decimal.TryParse(sAmountPaid, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out amountPaid);

                order.txn_id = transactionID;
                order.mc_gross = amountPaid;
                order.mc_currency = sCurrency;
                order.payer_mail = payerEmail;
                order.receiver_id = int.Parse(custom_info[0]);
                order.game_id = int.Parse(custom_info[1]);
                order.date = DateTime.UtcNow;
                order.quantity = quantity;
                order.payer_account_id = payer_account_id;

                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();

                    if (amountPaid >= retrieve_order_price(order.receiver_id, order.game_id, quantity, conn) - 0.1M && 
                        sCurrency == "USD" && payment_status == "Completed")
                    {
                        var new_order = !check_if_order_exists(transactionID, conn);

                        if (new_order) {
                            order.success = true;
                            insert_new_order(order, conn);
                            create_transaction(order, conn);
                        }
                        else
                        {
                            order.success = false;
                        }

                        ViewBag.order = order; 
                    }
                    else
                    {
                        order.success = false;
                        insert_new_order(order, conn);//Incorrect amount.. just log incident.
                    }

                    conn.Close();
                } 
            }
            else
            {
                //error
            }

            return View();
        }
        
        string GetPayPalResponse(Dictionary<string, string> formVals, bool useSandbox)
        {

            string paypalUrl = useSandbox ? "https://www.sandbox.paypal.com/cgi-bin/webscr" : "https://www.paypal.com/cgi-bin/webscr";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(paypalUrl);

            // Set values for the request back
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            byte[] param = Request.BinaryRead(Request.ContentLength);
            string strRequest = Encoding.ASCII.GetString(param);

            StringBuilder sb = new StringBuilder();
            sb.Append(strRequest);

            foreach (string key in formVals.Keys)
            {
                sb.AppendFormat("&{0}={1}", key, formVals[key]);
            }
            strRequest += sb.ToString();
            req.ContentLength = strRequest.Length;

            //for proxy
            //WebProxy proxy = new WebProxy(new Uri("http://urlort#");
            //req.Proxy = proxy;
            //Send the request to PayPal and get the response
            string response = "";
            using (StreamWriter streamOut = new StreamWriter(req.GetRequestStream(), System.Text.Encoding.ASCII))
            {

                streamOut.Write(strRequest);
                streamOut.Close();
                using (StreamReader streamIn = new StreamReader(req.GetResponse().GetResponseStream()))
                {
                    response = streamIn.ReadToEnd();
                }
            }

            return response;
        }
        string GetPDTValue(string pdt, string key)
        {

            string[] keys = pdt.Split('\n');
            string thisVal = "";
            string thisKey = "";
            foreach (string s in keys)
            {
                string[] bits = s.Split('=');
                if (bits.Length > 1)
                {
                    thisVal = bits[1];
                    thisKey = bits[0];
                    if (thisKey.Equals(key, StringComparison.InvariantCultureIgnoreCase))
                        break;
                }
            }
            return Server.UrlDecode(thisVal);

        }

        void insert_new_order(CoachOrder order, MySqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO esc_orders (txn_id, mc_gross, mc_currency, quantity, payer_email, payer_account_id, receiver_id, game_id, success, date) VALUES (@txnId, @mcGross, @mcCurrency, @qty, @payerMail, @payerid, @recieverId, @gameId, @success, @date)";
                cmd.Parameters.AddWithValue("@txnId", order.txn_id);
                cmd.Parameters.AddWithValue("@mcGross", order.mc_gross);
                cmd.Parameters.AddWithValue("@mcCurrency", order.mc_currency);
                cmd.Parameters.AddWithValue("@qty", order.quantity);
                cmd.Parameters.AddWithValue("@payerMail", order.payer_mail);
                cmd.Parameters.AddWithValue("@payerid", order.payer_account_id);

                cmd.Parameters.AddWithValue("@recieverId", order.receiver_id);
                cmd.Parameters.AddWithValue("@gameId", order.game_id);
                cmd.Parameters.AddWithValue("@success", order.success);
                cmd.Parameters.AddWithValue("@date", order.date);


                cmd.ExecuteNonQuery();
            }
        }
        void create_transaction(CoachOrder order, MySqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                decimal fee = 1.0M;

                cmd.CommandText = "INSERT INTO esc_Transactions (Amount, Status, Date, ReceiverId, senderId) VALUES (@amount, @status, @date, @boosterid, @buyerid)";
                cmd.Parameters.AddWithValue("@amount", order.mc_gross - fee);
                cmd.Parameters.AddWithValue("@status", payment_status.Pending);
                cmd.Parameters.AddWithValue("@date", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@boosterid", order.receiver_id);
                cmd.Parameters.AddWithValue("@buyerid", order.payer_account_id);

                cmd.ExecuteNonQuery();
            }
        }

        enum payment_status
        {
            Pending,
            Withdrawn
        }


        bool check_if_order_exists(string transactionId, MySqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT count(*) FROM esc_orders WHERE txn_id=@txnId";
                cmd.Parameters.AddWithValue("@txnId", transactionId);

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int count = reader.GetInt32(0);
                    return count > 0;
                }
                else return false;
            }
        }
        decimal retrieve_order_price(int coachId, int gameId, int quantity, MySqlConnection connection)
        {
            decimal finalPrice = 0;

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT SalaryUSD FROM esc_profilegames WHERE accountId=@accid AND gameId=@gameid";
                cmd.Parameters.AddWithValue("@accid", coachId);
                cmd.Parameters.AddWithValue("@gameid", gameId);

                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    finalPrice = reader.GetDecimal("SalaryUSD");
                }
            }
            
            return finalPrice * quantity;
        }
    }
}