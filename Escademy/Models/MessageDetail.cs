using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Escademy.Models
{
    public class MessageDetail
    {
        public int Id { get; set; }
        public int FromUserID { get; set; }
        public string FromUserName { get; set; }
        public int ToUserID { get; set; }
        public string ToUserName { get; set; }
        public string Message { get; set; }
        public string SendDate { get; set; }
        public DateTime ExactMessageSentDateTime { get; set; }
    }
}