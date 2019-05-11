using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Escademy.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using MySql.Data.MySqlClient;

namespace Escademy
{
    [HubName("chatHub")]
    public class ChatHub : Hub
    {
        #region---Data Members---
        static List<UserDetail> ConnectedUsers = new List<UserDetail>();
        static List<MessageDetail> CurrentMessage = new List<MessageDetail>();
        #endregion

        #region---Methods---

        public void Connect(string UserName, int UserID)
        {
            var id = Context.ConnectionId;
            if (ConnectedUsers.Count(x => x.ConnectionId == id) == 0)
            {
                ConnectedUsers.Add(new UserDetail { ConnectionId = id, UserName = UserName + "-" + UserID, UserID = UserID });
            }
            UserDetail CurrentUser = ConnectedUsers.Where(u => u.ConnectionId == id).FirstOrDefault();
            // send to caller           
            // Clients.Caller.onConnected(CurrentUser.UserID.ToString(), CurrentUser.UserName, ConnectedUsers, CurrentMessage, CurrentUser.UserID);
            // send to all except caller client           
            // Clients.AllExcept(CurrentUser.ConnectionId).onNewUserConnected(CurrentUser.UserID.ToString(), CurrentUser.UserName, CurrentUser.UserID);
        }


        public void SendPrivateMessage(string toUserId, string message)
        {
            try
            {
                DateTime Message_SentDate = DateTime.UtcNow;
                string fromconnectionid = Context.ConnectionId;
                string strfromUserId = (ConnectedUsers.Where(u => u.ConnectionId == Context.ConnectionId).Select(u => u.UserID).FirstOrDefault()).ToString();
                int _fromUserId = 0;
                int.TryParse(strfromUserId, out _fromUserId);
                int _toUserId = 0;
                int.TryParse(toUserId, out _toUserId);
                List<UserDetail> FromUsers = ConnectedUsers.Where(u => u.UserID == _fromUserId).ToList();
                List<UserDetail> ToUsers = ConnectedUsers.Where(x => x.UserID == _toUserId).ToList();

                // if (FromUsers.Count != 0 && ToUsers.Count() != 0)
                if (FromUsers.Count != 0)
                {
                    //--Save Message detail--
                    MessageDetail _MessageDeail = new MessageDetail { FromUserID = _fromUserId, ToUserID = _toUserId, Message = message, ExactMessageSentDateTime = Message_SentDate };
                    var message_id = AddMessageinCache(_MessageDeail);

                    foreach (var ToUser in ToUsers)
                    {
                        // send to                                                                                            //Chat Title
                        Clients.Client(ToUser.ConnectionId).sendPrivateMessage(_toUserId.ToString(), _fromUserId, message, Message_SentDate.ToString("MM/dd/yyyy HH:mm"), message_id);
                    }
                    foreach (var FromUser in FromUsers)
                    {
                        // send to caller user                                                                                //Chat Title
                        Clients.Client(FromUser.ConnectionId).sendPrivateMessage(_toUserId.ToString(), _fromUserId, message, Message_SentDate.ToString("MM/dd/yyyy HH:mm"), message_id);
                    }
                }
            }
            catch { }
        }

        public void RequestLastMessage(int FromUserID, int ToUserID)
        {
            List<MessageDetail> CurrentChatMessages = new List<MessageDetail>();
            var messages = new List<ChatMessage>();
            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();

                // Load Conversation
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM esc_chat c WHERE (c.Sender_Id=@sid and c.Receiver_id=@rid) or (c.Sender_Id=@rid and c.Receiver_id=@sid)";
                    cmd.Parameters.AddWithValue("@sid", FromUserID);
                    cmd.Parameters.AddWithValue("@rid", ToUserID);
                    cmd.Parameters.AddWithValue("@Sent", (int)ChatMessage.MessageStatus.Sent);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        CurrentChatMessages.Add(new MessageDetail()
                        {
                            Id = reader.GetInt32("Id"),
                            FromUserID = reader.GetInt32("Sender_Id"),
                            ToUserID = reader.GetInt32("Receiver_id"),
                            Message = reader.GetString("Message"),
                            SendDate = reader.GetDateTime("Created_at").ToString("MM/dd/yyyy HH:mm")
                        });

                    }
                }
                conn.Close();
            }
            //send to caller user
            Clients.Caller.GetLastMessages(ToUserID, CurrentChatMessages);
        }

        public void SendUserTypingRequest(string toUserId)
        {
            string strfromUserId = (ConnectedUsers.Where(u => u.ConnectionId == Context.ConnectionId).Select(u => u.UserID).FirstOrDefault()).ToString();

            int _toUserId = 0;
            int.TryParse(toUserId, out _toUserId);
            List<UserDetail> ToUsers = ConnectedUsers.Where(x => x.UserID == _toUserId).ToList();

            foreach (var ToUser in ToUsers)
            {
                // send to                                                                                            
                Clients.Client(ToUser.ConnectionId).ReceiveTypingRequest(strfromUserId);
            }
        }

        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            var item = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                ConnectedUsers.Remove(item);
                if (ConnectedUsers.Where(u => u.UserID == item.UserID).Count() == 0)
                {
                    var id = item.UserID.ToString();
                    Clients.All.onUserDisconnected(id, item.UserName);
                }
            }
            return base.OnDisconnected(stopCalled);
        }
        #endregion

        #region---private Messages---
        private int AddMessageinCache(MessageDetail _MessageDetail)
        {
            int Recent_id = 0;
            #region Save Message Detail in Database 
            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO esc_chat (Sender_Id, Receiver_id, Message, Status, Created_at) VALUES (@senderId, @receiverId, @message, @status, @createdAt)";

                    cmd.Parameters.AddWithValue("@senderId", _MessageDetail.FromUserID);
                    cmd.Parameters.AddWithValue("@receiverId", _MessageDetail.ToUserID);
                    cmd.Parameters.AddWithValue("@message", _MessageDetail.Message);
                    cmd.Parameters.AddWithValue("@status", 0);
                    cmd.Parameters.AddWithValue("@createdAt", _MessageDetail.ExactMessageSentDateTime);
                    cmd.ExecuteNonQuery();
                    Recent_id = Convert.ToInt32(cmd.LastInsertedId);
                }
                conn.Close();
            }
            #endregion

            #region Add Message in Cache
            CurrentMessage.Add(_MessageDetail);
            if (CurrentMessage.Count > 100)
                CurrentMessage.RemoveAt(0);
            #endregion

            return Recent_id;
        }
        #endregion
    }
}