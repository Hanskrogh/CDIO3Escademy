using Escademy.Models;
using Escademy.Helpers;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Http;
using Escademy.ViewModels;

namespace Escademy.Controllers
{
    public class UserController : ApiController
    {
        [HttpGet]
        public User GetUserByMail([FromUri]string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            // Ensures that the user is an admin.
            if (EnsureUserRights(UserLevel.Admin)) {
                
                // Load the user 
                MySqlConnection con = new MySqlConnection();
                con.ConnectionString = ConnectionString.Get("EscademyMDB");
                con.Open();

                MySqlCommand cmd = con.CreateCommand();
                cmd.CommandText = "SELECT * FROM esc_accounts WHERE Email=@email LIMIT 1";
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("@email", email);

                MySqlDataReader reader = cmd.ExecuteReader();

                User user = null;

                if (reader.Read())
                {
                    user = new User()
                    {
                        Email = reader.GetString("Email"),
                        FirstName = reader.GetString("FirstName"),
                        LastName = reader.GetString("LastName"),
                        Level = reader.GetInt32("Level"),
                    };
                }
                    
                cmd.Dispose();
                con.Close();
                    
                // Return user after connectons has been closed.
                return user;
                
            }

            return null; // no permission.
        }

        [HttpPost]
        public bool UpdateUser([FromBody]User user)
        {
            if (!EnsureUserRights(UserLevel.Admin)) return false;


            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                bool success = false;
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Level", user.Level);
                    cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", user.LastName);
                    
                    if (string.IsNullOrWhiteSpace(user.Password))
                    {
                        cmd.CommandText = "UPDATE esc_accounts SET Level=@Level, FirstName=@FirstName, LastName=@LastName WHERE Email=@Email";
                    } else
                    {
                        cmd.Parameters.AddWithValue("@Password", user.Password.ToSHA512());
                        cmd.CommandText = "UPDATE esc_accounts SET Password=@Password, Level=@Level, FirstName=@FirstName, LastName=@LastName WHERE Email=@Email";
                    }
                    
                    success = cmd.ExecuteNonQuery() >= 1;
                }

                conn.Clone();
                return success;
            }
        }

        [HttpPost]
        public object UpdateLanguage([FromBody]UserLanguage language)
        {
            if (EnsureUserRights(UserLevel.Default))
            {
                var user = GetCurrentUser();
                bool success = false;

                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO esc_profilelanguages (account_id, language_id, level) VALUES(@uid,@language,@level) ON DUPLICATE KEY UPDATE language_id = @language, level = @level";
                        cmd.Parameters.AddWithValue("@uid", user.Id);
                        cmd.Parameters.AddWithValue("@language", language.Language);
                        cmd.Parameters.AddWithValue("@level", language.Level);

                        success = cmd.ExecuteNonQuery() >= 1;
                    }

                    conn.Close();
                }

                return Json(new { auth = "OK", success, language = language.Language, level = language.Level, levelText = UserLanguage.GetLevelText(language.Level), languageText = UserLanguage.GetLanguageString(language.Language) });
            }
            return Json(new { auth = "NO_AUTH" });
        }

        [HttpGet]
        public object DeleteLanguage(int language) {
            if (EnsureUserRights(UserLevel.Default))
            {
                var user = GetCurrentUser();
                bool success = false;

                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB"))) {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM esc_profilelanguages WHERE account_id=@accountId AND language_id=@languageId LIMIT 1";
                        cmd.Parameters.AddWithValue("@accountId", user.Id);
                        cmd.Parameters.AddWithValue("@languageId", language);
                        success = cmd.ExecuteNonQuery() >= 1;
                    }
                    conn.Close();
                }

                return Json(new { auth = "OK", success });
            }
            return Json(new { auth = "NO_AUTH" });
        }

        //http://localhost:15855/api/user/SendMessage?Receiver_id=1&message=Test

        [HttpGet]
        public object SendMessage(int Receiver_id, string message) {
            if (EnsureUserRights(UserLevel.Default)) // all users are allowed to send messages.
            {
                int Sender_id = GetCurrentUser().Id;

                var outgoingMsg = new ChatMessage()
                {
                    Message = message,
                    Receiver_id = Receiver_id,
                    Sender_id = Sender_id,
                    Status = ChatMessage.MessageStatus.Sent,
                    Created_at = DateTime.UtcNow.AddHours(1)
                };
                
                SendChatMessage(outgoingMsg);

                return Json(new { auth = "OK", sender_id = Sender_id, receiver_id = outgoingMsg.Receiver_id });
            } else return Json(new { auth = "NO_AUTH" });
        }

        [HttpGet]
        public object GetUnreadMessages(int sender_id)
        {
            if (EnsureUserRights(UserLevel.Default))
            {
                var user = GetCurrentUser();
                var receiver_id = user.Id;

                var messages = new List<ChatMessage>();
                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();

                    // Load Conversation
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM esc_chat WHERE Receiver_id=@RID AND Sender_Id=@SID AND Status=@Sent GROUP BY Id";
                        cmd.Parameters.AddWithValue("@SID", sender_id);
                        cmd.Parameters.AddWithValue("@RID", receiver_id);
                        cmd.Parameters.AddWithValue("@Sent", (int)ChatMessage.MessageStatus.Sent);

                        var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            messages.Add(new ChatMessage()
                            {
                                Id = reader.GetInt32("Id"),
                                Sender_id = reader.GetInt32("Sender_Id"),
                                Receiver_id = reader.GetInt32("Receiver_id"),
                                Status = (ChatMessage.MessageStatus)reader.GetInt32("Status"),
                                Created_at = reader.GetDateTime("Created_at"),
                                Message = reader.GetString("Message")
                            });

                        }
                    }

                    // Set messages receieved to ChatMessage.MessageStatus.Delivered
                    if (messages.Count > 0)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "UPDATE esc_chat SET Status=@Delivered WHERE Receiver_id=@RID AND Sender_Id=@SID";
                            cmd.Parameters.AddWithValue("@SID", sender_id);
                            cmd.Parameters.AddWithValue("@RID", receiver_id);
                            cmd.Parameters.AddWithValue("@Delivered", (int)ChatMessage.MessageStatus.Delivered);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    conn.Close();
                }

                return Json(new { auth = "OK", messages = messages.ToArray(), size = messages.Count });
            }
            
            return Json(new { auth = "NO_AUTH" });
        }

        [HttpGet]
        public object UpdateDescription(string newDescription)
        {
            if (EnsureUserRights(UserLevel.Default))
            {
                bool success = false;

                var id = GetCurrentUser().Id;

                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE esc_accounts SET Description=@description WHERE Id=@accountId";
                        cmd.Parameters.AddWithValue("@description", newDescription);
                        cmd.Parameters.AddWithValue("@accountId", id);

                        success = cmd.ExecuteNonQuery() >= 1;
                    }

                    conn.Close();
                }

                return Json(new
                {
                    auth = "OK",
                    success,
                    description = newDescription
                });
            }
            else return Json(new { auth = "NO_AUTH" });
        }

        [HttpGet]
        public object GetConversations()
        {
            if (EnsureUserRights(UserLevel.Default)) {
                var user = GetCurrentUser();

                var receiver_id = user.Id;

                var conversations = new List<ChatMessage>();
                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();

                    // Default Conversation Load.
                    using (var cmd = conn.CreateCommand())
                    {
                        //cmd.CommandText = "SELECT t1.*, accs.Email FROM esc_chat AS t1 LEFT OUTER JOIN esc_chat AS t2 ON t1.Sender_Id = t2.Sender_Id AND (t1.Created_at < t2.Created_at OR (t1.Created_at = t2.Created_at AND t1.Id < t2.Id)) LEFT JOIN esc_accounts AS accs ON accs.Id=t1.Sender_Id WHERE t2.Sender_Id IS NULL AND t1.Receiver_id=" + receiver_id;
                        cmd.CommandText = "SELECT a.*, c.Email, c.FirstName, c.LastName, c.ProfilePicture FROM esc_chat a INNER JOIN ( SELECT MAX(`Id`) AS Id FROM esc_chat AS `alt` WHERE `alt`.`Sender_Id` =@sid OR `alt`.`Receiver_id` =@sid GROUP BY least(`Receiver_id` , `Sender_Id`), greatest(`Receiver_id` , `Sender_Id`) )b ON a.Id = b.Id INNER JOIN esc_accounts c ON c.Id = IF(a.Sender_Id=@sid, a.Receiver_id, a.Sender_Id) GROUP BY a.Created_at DESC";
                        cmd.Parameters.AddWithValue("@sid", user.Id);
                        var reader = cmd.ExecuteReader();
                        
                        while (reader.Read())
                        {
                            var sid = reader.GetInt32("Sender_Id");
                            var rid = reader.GetInt32("Receiver_id");
                            
                            conversations.Add(new ChatMessageResponse() {
                                Id = reader.GetInt32("Id"),
                                Sender_id = rid == user.Id ? sid : rid,
                                Receiver_id = user.Id,
                                Status = (ChatMessage.MessageStatus)reader.GetInt32("Status"),
                                Created_at = reader.GetDateTime("Created_at"),
                                Email = reader.GetString("Email"),
                                FirstName = reader.GetString("FirstName"),
                                LastName = reader.GetString("LastName"),
                                Message = reader.GetString("Message"),
                                Profile = reader.GetString("ProfilePicture")
                            });
                        }
                    }

                    conn.Close();
                }
                
                return Json(new { auth = "OK", conversations = conversations.ToArray() });
            }
            return Json(new { auth = "NO_AUTH" });
        }
        public object GetFullConversation(int sender_id)
        {
            if (EnsureUserRights(UserLevel.Default))
            {
                var user = GetCurrentUser();
                var receiver_id = user.Id;

                var messages = new List<ChatMessage>();
                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();

                    // Load Conversation
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM esc_chat WHERE (Receiver_id=@RID AND Sender_Id=@SID) OR (Sender_Id=@RID AND Receiver_id=@SID) GROUP BY Id";
                        cmd.Parameters.AddWithValue("@SID", sender_id);
                        cmd.Parameters.AddWithValue("@RID", receiver_id);

                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            
                            messages.Add(new ChatMessage()
                            {
                                Id = reader.GetInt32("Id"),
                                Sender_id = reader.GetInt32("Sender_Id"),
                                Receiver_id = reader.GetInt32("Receiver_id"),
                                Status = (ChatMessage.MessageStatus)reader.GetInt32("Status"),
                                Created_at = reader.GetDateTime("Created_at"),
                                Message = reader.GetString("Message")
                            });

                        }
                    }

                    // Set messages receieved to ChatMessage.MessageStatus.Delivered
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE esc_chat SET Status=@Delivered WHERE Receiver_id=@RID AND Sender_Id=@SID";
                        cmd.Parameters.AddWithValue("@SID", sender_id);
                        cmd.Parameters.AddWithValue("@RID", receiver_id);
                        cmd.Parameters.AddWithValue("@Delivered", (int)ChatMessage.MessageStatus.Delivered);
                        cmd.ExecuteNonQuery();
                    }


                    conn.Close();
                }

                return Json(new { auth = "OK", messages = messages.ToArray() });
            }
            return Json(new { auth = "NO_AUTH" });
        }

        private bool SendChatMessage(ChatMessage message)
        {
            bool success = false;
            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO esc_chat(Sender_Id, Receiver_Id, Message, Status, Created_at) VALUES (@senderId, @receiverId, @message, @status, @created)";
                    cmd.Parameters.AddWithValue("@senderId", message.Sender_id);
                    cmd.Parameters.AddWithValue("@receiverId", message.Receiver_id);
                    cmd.Parameters.AddWithValue("@message", message.Message);
                    cmd.Parameters.AddWithValue("@status", (int)message.Status);
                    cmd.Parameters.AddWithValue("@created", message.Created_at);

                    success = cmd.ExecuteNonQuery() >= 1;
                }

                conn.Close();
            }
            return success;
        }

        [HttpGet]
        public object GetAllContacts()
        {
            try
            {
                if (EnsureUserRights(UserLevel.Default))
                {
                    var user = GetCurrentUser();
                    var conversations = new List<ChatMessage>();
                    using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "select acc.Id,acc.Email,acc.FirstName,acc.LastName,acc.ProfilePicture,(select count(c.Id) from esc_chat c where c.Receiver_id=@sid and c.Sender_Id=acc.Id and c.Status=0) as TotalUnreadMessagesCount from esc_accounts acc where acc.Id!=@sid and (acc.Id in (select c1.Sender_Id from esc_chat c1 where c1.Receiver_id=@sid) or acc.Id in (select c2.Receiver_id from esc_chat c2 where c2.Sender_Id=@sid))";
                            cmd.Parameters.AddWithValue("@sid", user.Id);
                            var reader = cmd.ExecuteReader();

                            while (reader.Read())
                            {
                                conversations.Add(new ChatMessageResponse()
                                {
                                    Id = reader.GetInt32("Id"),
                                    Email = reader.GetString("Email"),
                                    FirstName = reader.GetString("FirstName"),
                                    LastName = reader.GetString("LastName"),
                                    Profile = reader.GetString("ProfilePicture"),
                                    TotalUnreadMessagesCount = reader.GetInt32("TotalUnreadMessagesCount")
                                });
                            }
                        }
                        conn.Close();
                    }
                    return Json(new { auth = "OK", conversations = conversations.ToArray() });
                }
                else return Json(new { auth = "NO_AUTH" });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public object GetUserDetialById(int UserId)
        {
            try
            {
                if (EnsureUserRights(UserLevel.Default))
                {
                    var user = new List<AccountViewModel>();
                    using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "select acc.Id,acc.Email,acc.FirstName,acc.LastName,acc.ProfilePicture from esc_accounts acc where acc.Id=@uid";
                            cmd.Parameters.AddWithValue("@uid", UserId);
                            var reader = cmd.ExecuteReader();

                            while (reader.Read())
                            {
                                user.Add(new AccountViewModel()
                                {
                                    Id = reader.GetInt32("Id"),
                                    Email = reader.GetString("Email"),
                                    FirstName = reader.GetString("FirstName"),
                                    LastName = reader.GetString("LastName"),
                                    ProfilePicture = reader.GetString("ProfilePicture")
                                });
                            }
                        }
                        conn.Close();
                    }
                    return Json(new { auth = "OK", UserDetail = user.ToArray() });
                }
                else return Json(new { auth = "NO_AUTH" });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public object UpdateMessageSeenStatus(int MessageId)
        {
            try
            {
                if (EnsureUserRights(UserLevel.Default))
                {
                    using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "update esc_chat set Status=1 where Id=@msgId";
                            cmd.Parameters.AddWithValue("@msgId", MessageId);
                            cmd.ExecuteNonQuery();
                        }
                        conn.Close();
                    }
                    return Json(new { auth = "OK", ReturnStatus = "1" });
                }
                else return Json(new { auth = "NO_AUTH" });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public object UpdateAllMessageSeenStatus(int ToUserid, int FromUserId)
        {
            try
            {
                if (EnsureUserRights(UserLevel.Default))
                {
                    using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "update esc_chat set Status=1 where Sender_Id=@sid and Receiver_id=@rid";
                            cmd.Parameters.AddWithValue("@sid", FromUserId);
                            cmd.Parameters.AddWithValue("@rid", ToUserid);
                            cmd.ExecuteNonQuery();
                        }
                        conn.Close();
                    }
                    return Json(new { auth = "OK", ReturnStatus = "1" });
                }
                else return Json(new { auth = "NO_AUTH" });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public object GetLoggedInUserDetail()
        {
            if (EnsureUserRights(UserLevel.Default))
            {
                var user = GetCurrentUser();
                return Json(new { auth = "OK", UserDetail = user });
            }
            else return Json(new { auth = "NO_AUTH" });
        }

        private bool EnsureUserRights(UserLevel rights)
        {
            if (HttpContext.Current.Session["user"] != null)
            {
                if ((HttpContext.Current.Session["user"] as User).HasRole(rights)) // REQUIRES ADMIN ROLE
                {
                    return true;
                }
            }
            return false;
        }
        private User GetCurrentUser()
        {
            return (User)HttpContext.Current.Session["user"];
        }
    }
}
