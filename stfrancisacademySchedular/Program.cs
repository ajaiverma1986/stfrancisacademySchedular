using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Web;

namespace stfrancisacademySchedular
{
    public class Program
    {
        static string reqHashKey = ConfigurationSettings.AppSettings["reqHashKey"].ToString();
        static string MerchantId = ConfigurationSettings.AppSettings["MerchantId"].ToString();
        static string _dbConnectionString = ConfigurationSettings.AppSettings["ConnectionString"].ToString();
        static string passphrase = ConfigurationSettings.AppSettings["passphrase"].ToString();
        static string salt = ConfigurationSettings.AppSettings["salt"].ToString();
        static string passphrase1 = ConfigurationSettings.AppSettings["passphrase"].ToString();
        static string salt1 = ConfigurationSettings.AppSettings["salt"].ToString();
        static string APIurl = ConfigurationSettings.AppSettings["APIurl"].ToString();
        static string ApiPassword = ConfigurationSettings.AppSettings["ApiPassword"].ToString();
        static string Gatewayurl = ConfigurationSettings.AppSettings["Gatewayurl"].ToString();
        static string APIname = ConfigurationSettings.AppSettings["APIname"].ToString();
        static string APIsource = ConfigurationSettings.AppSettings["APIsource"].ToString();
        static void Main(string[] args)
        {
            Program p = new Program();
            p.GetallPendngTxn();
        }
        private void UpdateGatwayTxnStatus(string CollectionCode, string Responsetext, object PaymentMode, object ReasonFailure, int Status, string RefNo)
        {
            var dt = new DataTable();
            using (SqlConnection sqlCon = new SqlConnection(_dbConnectionString))
            {
                using (SqlCommand SqlCmd = new SqlCommand("usp_updateccAvenueData", sqlCon))
                {
                    SqlCmd.CommandType = CommandType.StoredProcedure;
                    SqlCmd.Parameters.Add("@CollectionCode", SqlDbType.VarChar).Value = CollectionCode;
                    SqlCmd.Parameters.Add("@Responsetext", SqlDbType.VarChar).Value = Responsetext;
                    SqlCmd.Parameters.Add("@PaymentMode", SqlDbType.VarChar).Value = PaymentMode;
                    SqlCmd.Parameters.Add("@ReasonFailure", SqlDbType.VarChar).Value = ReasonFailure;
                    SqlCmd.Parameters.Add("@Status", SqlDbType.Int).Value = Status;
                    SqlCmd.Parameters.Add("@RefNo", SqlDbType.VarChar).Value = RefNo;
                    sqlCon.Open();
                    dt.Load(SqlCmd.ExecuteReader());
                }
            }
        }

        private DataTable GetallGatwayPeningTransaction(int PaymentId = 0)
        {
            var dt = new DataTable();
            using (SqlConnection sqlCon = new SqlConnection(_dbConnectionString))
            {
                using (SqlCommand SqlCmd = new SqlCommand("usp_getallPendingCCAvenueTransaction", sqlCon))
                {
                    SqlCmd.CommandType = CommandType.StoredProcedure;
                    if (PaymentId > 0)
                    {
                        SqlCmd.Parameters.Add("@PaymentId", SqlDbType.BigInt).Value = PaymentId;
                    }
                    else
                    {
                        SqlCmd.Parameters.Add("@PaymentId", SqlDbType.BigInt).Value = 0;
                    }

                    sqlCon.Open();
                    dt.Load(SqlCmd.ExecuteReader());
                }
            }
            return dt;
        }
        public void GetallPendngTxn()
        {
            string decrival = "";

            decrival = GetTxnStatusFromGatway("4494", "2024-06-25", 2700.00);

            return;
            DataTable dt = new DataTable();
            dt = GetallGatwayPeningTransaction();
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                  
                    //decrival= GetTxnStatusFromGatway(dt.Rows[i]["collectionCode"].ToString(), dt.Rows[i]["collectionCode"].ToString(),Convert.ToDouble( dt.Rows[i]["amount"].ToString()));
                   
                }
            }
            else
            {
                Console.WriteLine("No Pending Record Found");
            }
        }
        public  string GetTxnStatusFromGatway(string merchTxnId,string txndate,double Amount)
        {
            byte[] iv = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            int iterations = 65536;
            Pojo.Root rt = new Pojo.Root();
            Pojo.HeadDetails hd = new Pojo.HeadDetails();
            Pojo.MerchDetails md = new Pojo.MerchDetails();
            Pojo.PayDetails pd = new Pojo.PayDetails();
            Pojo.PayInstrument pi = new Pojo.PayInstrument();
            hd.api =APIname;
            hd.source = APIsource;
            md.merchId = MerchantId;
            md.password = ApiPassword;
            md.merchTxnId = merchTxnId;
            md.merchTxnDate = txndate;        // "2023-02-14";

            pd.amount = Amount; //10.00;
            pd.txnCurrency = "INR";

            string strsignature = md.merchId + md.password + md.merchTxnId + pd.amount + pd.txnCurrency + hd.api;
            byte[] bytes = Encoding.UTF8.GetBytes(reqHashKey);
            byte[] bt = new System.Security.Cryptography.HMACSHA512(bytes).ComputeHash(Encoding.UTF8.GetBytes(strsignature));
            string signature = byteToHexString(bt).ToLower();
            pd.signature = signature;
            pi.headDetails = hd;
            pi.merchDetails = md;
            pi.payDetails = pd;
            rt.payInstrument = pi;
            var json = new JavaScriptSerializer().Serialize(rt);
            string Encryptval = Encrypt(json, passphrase, salt, iv, iterations);
            string Link = APIurl+"/ots/payment/status?merchId=" + MerchantId + "&encData=" + Encryptval;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Link);

            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = "POST";
            request.ContentType = "application/json";
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            Encoding encoding = new UTF8Encoding();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(resStream);
            string responseFromServer = reader.ReadToEnd();
            var uri = new Uri(Gatewayurl + responseFromServer);
            var query = HttpUtility.ParseQueryString(uri.Query);

            string encData = query.Get("encData");
            string Decryptval = decrypt(encData, passphrase1, salt1, iv, iterations);
            return Decryptval;
        }
        public static string byteToHexString(byte[] byData)
        {
            StringBuilder sb = new StringBuilder((byData.Length * 2));
            for (int i = 0; (i < byData.Length); i++)
            {
                int v = (byData[i] & 255);
                if ((v < 16))
                {
                    sb.Append('0');
                }

                sb.Append(v.ToString("X"));

            }

            return sb.ToString();
        }
        public string Encrypt(string plainText, string passphrase, string salt, Byte[] iv, int iterations)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            string data = ByteArrayToHexString(Encrypt(plainBytes, GetSymmetricAlgorithm(passphrase, salt, iv, iterations))).ToUpper();


            return data;
        }
        public String decrypt(String plainText, String passphrase, String salt, Byte[] iv, int iterations)
        {
            byte[] str = HexStringToByte(plainText);

            string data1 = Encoding.UTF8.GetString(decrypt(str, GetSymmetricAlgorithm(passphrase, salt, iv, iterations)));
            return data1;
        }
        public byte[] Encrypt(byte[] plainBytes, SymmetricAlgorithm sa)
        {
            return sa.CreateEncryptor().TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        }
        public byte[] decrypt(byte[] plainBytes, SymmetricAlgorithm sa)
        {
            return sa.CreateDecryptor().TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }
        public SymmetricAlgorithm GetSymmetricAlgorithm(String passphrase, String salt, Byte[] iv, int iterations)
        {
            var saltBytes = new byte[16];
            var ivBytes = new byte[16];
            Rfc2898DeriveBytes rfcdb = new System.Security.Cryptography.Rfc2898DeriveBytes(passphrase, Encoding.UTF8.GetBytes(salt), iterations, HashAlgorithmName.SHA512);
            saltBytes = rfcdb.GetBytes(32);
            var tempBytes = iv;
            Array.Copy(tempBytes, ivBytes, Math.Min(ivBytes.Length, tempBytes.Length));
            var rij = new RijndaelManaged(); //SymmetricAlgorithm.Create();
            rij.Mode = CipherMode.CBC;
            rij.Padding = PaddingMode.PKCS7;
            rij.FeedbackSize = 128;
            rij.KeySize = 128;

            rij.BlockSize = 128;
            rij.Key = saltBytes;
            rij.IV = ivBytes;
            return rij;
        }
        protected static byte[] HexStringToByte(string hexString)
        {
            try
            {
                int bytesCount = (hexString.Length) / 2;
                byte[] bytes = new byte[bytesCount];
                for (int x = 0; x < bytesCount; ++x)
                {
                    bytes[x] = Convert.ToByte(hexString.Substring(x * 2, 2), 16);
                }
                return bytes;
            }
            catch
            {
                throw;
            }
        }
        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }
}
