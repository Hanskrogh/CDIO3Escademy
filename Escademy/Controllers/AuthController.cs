using Escademy.Helpers;
using Escademy.Models;
using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web.Mvc;

namespace Escademy.Controllers
{
    public class AuthController : Controller
    {
        // GET: Auth
        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }

        [HttpPost]
        public ActionResult Login(User user)
        {
            bool auth = false;

            MySqlConnection con = new MySqlConnection();
            con.ConnectionString = ConnectionString.Get("EscademyMDB");
            con.Open();

            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "SELECT * FROM esc_accounts WHERE Email=@email AND Password=@password LIMIT 1";
            cmd.Connection = con;
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@password", user.Password.ToSHA512());

            MySqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                auth = true;

                Session["user"] = new User() {
                    Id = reader.GetInt32("Id"),
                    Email = user.Email,
                    Password = user.Password.ToSHA512(),
                    FirstName = reader.GetString("FirstName"),
                    LastName = reader.GetString("LastName"),
                    Level = reader.GetInt32("Level"),
                    Picture = reader.GetString("ProfilePicture"),
                    Description = reader.GetString("Description")
                };
            }

            cmd.Dispose();
            con.Close();
            

            // Success:
            if (auth)
                return RedirectToAction(actionName: "Index", controllerName: "Home");
            else
            {
                ViewBag.success = false;
                return View();
            }
        }

        public ActionResult Login()
        {
            ViewBag.success = true;
            return View();
        }
        public ActionResult Register()
        {
            ViewBag.success = true;
            return View();
        }

        [HttpPost]
        public ActionResult Register(User user)
        {
            ViewBag.success = false;

            // ignored for now ..
            /*
            if (!VerifyCapatcha(ConnectionString.Get("capatcha"), Request["g-recaptcha-response"]))
                return View();
            */

            // Verify input
            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password) || string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
                return View();

            bool verified = false;

            // INSERT INTO esc_accounts (Email, Password, FirstName, Level) VALUES (@Email, @Password, @FirstName, 1)
            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO esc_accounts (Email, Password, Level, FirstName, LastName, verified, Created_at, Country) VALUES (@Email, @Password, 1, @FirstName, @LastName, 0, @CreationDate, @Country)";

                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password.ToSHA512());
                    cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", user.LastName);
                    cmd.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@Country", user.Country);

                    try
                    {
                        if (cmd.ExecuteNonQuery() >= 1)
                        {
                            verified = true;
                        }
                    } catch (MySqlException)
                    {
                        //duplicate entry exception..
                    }
                }

                if (verified)
                {
                    string key = KeyGenerator.GetUniqueKey(new Random().Next(15, 30));
                    var reglink = "https://www.escademy.com/auth/confirm_mail?reg="+key;

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO esc_verificationcodes(VerificationCode, Email) VALUES (@VerificationCode, @Email)";
                        cmd.Parameters.AddWithValue("@VerificationCode", key);
                        cmd.Parameters.AddWithValue("@Email", user.Email);
                        cmd.ExecuteNonQuery();
                    }

                    var fileContents = System.IO.File.ReadAllText(Server.MapPath(@"~/App_Data/bf_confirm_mail.html"))
                        .Replace("REP_ACTIVACTION_URL", reglink)
                        .Replace("{FirstName}", user.FirstName);


                    using (var mFactory = new MailFactory())
                    {
                        mFactory.SendMail(
                            "Welcome to Escademy",
                            //"<html><body><h1>Tak for din registrering hos Escademy.</h1>Tryk på linket forneden for at færdigøre registreringen<br /><a href=\"" + reglink + "\">" + reglink + "</a></body></html>",
                            fileContents,
                            new MailAddress(user.Email)
                        );
                    }
                }

                conn.Close();

                if (verified)
                    return RedirectToAction("Success");
                else
                    return View();
            }   
        }
        public ActionResult confirm_mail(string reg)
        {
            ViewBag.verified = false;
            
            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                var email = string.Empty;

                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM esc_verificationcodes WHERE VerificationCode=@VerificationCode";
                    cmd.Parameters.AddWithValue("@VerificationCode", reg);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        email = reader.GetString("Email");
                    }
                }

                if (email != string.Empty)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM esc_verificationcodes WHERE VerificationCode=@VerificationCode";
                        cmd.Parameters.AddWithValue("@VerificationCode", reg);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE esc_accounts SET verified=1 WHERE Email=@Email";
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.ExecuteNonQuery();
                    }

                    ViewBag.email = email;
                    ViewBag.verified = true;
                }
            }

            return View();
        }

        [HttpPost]
        public ActionResult SetPicture()
        {
            if (Session["user"] != null)
            {
                var user = (User)Session["user"];
                if (Request.Files.Count > 0)
                {
                    var file = Request.Files[0];
                    if (file != null && file.ContentLength > 0)
                    {
                        // Load Img..
                        byte[] thePictureAsBytes = new byte[file.ContentLength];
                        using (BinaryReader theReader = new BinaryReader(file.InputStream))
                        {
                            thePictureAsBytes = theReader.ReadBytes(file.ContentLength);
                        }

                        // Convert img to bitmap and back
                        thePictureAsBytes = GetRectangularBMP(thePictureAsBytes);

                        // Convert to base64 and set img.
                        string thePictureDataAsString = Convert.ToBase64String(thePictureAsBytes);
                        using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "UPDATE esc_accounts SET ProfilePicture=@profilePicture WHERE Email=@email";
                                cmd.Parameters.AddWithValue("@email", user.Email);
                                cmd.Parameters.AddWithValue("@profilePicture", thePictureDataAsString);
                                cmd.ExecuteNonQuery();
                            }
                            conn.Close();
                        }

                        // Set image on local profile
                        user.Picture = thePictureDataAsString;
                    }
                }
            }
            
            return RedirectToAction(actionName: "Index", controllerName: "Home");
        }
        
        public ActionResult Forgotpass()
        {
            ViewBag.mail_sent = false;
            return View();
        }

        [HttpPost]
        public ActionResult Forgotpass(string Email)
        {
            string key = KeyGenerator.GetUniqueKey(new Random().Next(15, 30));

            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO esc_userpasswordreset(PasswordResetToken, PasswordResetExpiration, Email) VALUES (@token, @exp, @mail)";
                    cmd.Parameters.AddWithValue("@token", key);
                    cmd.Parameters.AddWithValue("@exp", DateTime.UtcNow.AddDays(5));
                    cmd.Parameters.AddWithValue("@mail", Email);

                    if (cmd.ExecuteNonQuery() >= 1)
                    {
                        // SEND MAIL TO USER
                        var reset_link = "https://www.escademy.com/auth/reset_password?token="+key;

                        var fileContents = System.IO.File.ReadAllText(Server.MapPath(@"~/App_Data/bf_resetpassword.html"))
                            .Replace("REP_RESET_LINK", reset_link)
                            .Replace("{USERNAME}", "User");


                        using (var mFactory = new MailFactory())
                        {
                            mFactory.SendMail(
                                "Password Reset",
                                fileContents,
                                new MailAddress(Email)
                            );
                        }
                    }
                }
                conn.Close();
            }

            ViewBag.mail_sent = true;
            return View();
        }

        public ActionResult Success()
        {
            return View();
        }

        public ActionResult reset_password(string token)
        {
            ViewBag.token = token;
            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM esc_userpasswordreset WHERE PasswordResetToken=@token";
                    cmd.Parameters.AddWithValue("@token", token);

                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        DateTime dateExpire = reader.GetDateTime("PasswordResetExpiration");
                        if (dateExpire > DateTime.UtcNow)
                        {
                            string mail = reader.GetString("Email");
                            ViewBag.mail = mail;
                        }
                    }
                }
                
                conn.Close();
            }
            

            return View();
        }

        [HttpPost]
        public ActionResult reset_password(string token, string password)
        {
            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();

                string mail = null;
                using (var cmd = conn.CreateCommand())
                {
                    // READS CURRENT TOKEN.
                    cmd.CommandText = "SELECT * FROM esc_userpasswordreset WHERE PasswordResetToken=@token";
                    cmd.Parameters.AddWithValue("@token", token);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        DateTime dateExpire = reader.GetDateTime("PasswordResetExpiration");
                        if (dateExpire > DateTime.UtcNow)
                        {
                            mail = reader.GetString("Email"); 
                        }
                    }
                }

                using (var cmd = conn.CreateCommand())
                {
                    // DELETES TOKEN.
                    cmd.CommandText = "DELETE FROM esc_userpasswordreset WHERE PasswordResetToken=@token";
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.ExecuteNonQuery();
                }

                if (mail != null)
                {
                    // IF VALID TOKEN, RESET PASSWORD.
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE esc_accounts SET `Password` = @newPass WHERE `Email`= @mail; ";
                        cmd.Parameters.AddWithValue("@newPass", password.ToSHA512());
                        cmd.Parameters.AddWithValue("@mail", mail);

                        cmd.ExecuteNonQuery();
                    }
                }


                conn.Close();
            }

            return RedirectToAction("Login");
        }


        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction(actionName: "Index", controllerName: "Home");
        }
        public ActionResult Checkout()
        {
            return View();
        }
        public ActionResult Applycoach()
        {
            return View();
        }


        private byte[] GetRectangularBMP(byte[] thePictureAsBytes)
        {
            Bitmap bmp;
            using (var ms = new MemoryStream(thePictureAsBytes))
            {
                bmp = new Bitmap(ms);
            }

            var newBmp = MakeSquarePhoto(bmp, Math.Min(bmp.Width, bmp.Height));

            using (var stream = new MemoryStream())
            {
                newBmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                thePictureAsBytes = stream.ToArray();
            }

            bmp.Dispose();
            newBmp.Dispose();

            return thePictureAsBytes;
        }
        private Bitmap MakeSquarePhoto(Bitmap bmp, int size)
        {
            Bitmap res = new Bitmap(size, size);
            Graphics g = Graphics.FromImage(res);
            g.FillRectangle(new SolidBrush(Color.White), 0, 0, size, size);
            int t = 0, l = 0;
            if (bmp.Height > bmp.Width)
                t = (bmp.Height - bmp.Width) / 2;
            else
                l = (bmp.Width - bmp.Height) / 2;
            g.DrawImage(bmp, new Rectangle(0, 0, size, size), new Rectangle(l, t, bmp.Width - l * 2, bmp.Height - t * 2), GraphicsUnit.Pixel);
            return res;
        }
        private bool VerifyCapatcha(string capatchaCode, string userResponse)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.google.com/recaptcha/api/siteverify");

            var postData = "secret=" + capatchaCode;
            postData += "&response=" + userResponse;
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return true;
        }
    }
}