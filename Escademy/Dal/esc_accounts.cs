//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Escademy.Dal
{
    using System;
    using System.Collections.Generic;
    
    public partial class esc_accounts
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int Level { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int verified { get; set; }
        public byte[] ProfilePicture { get; set; }
        public System.DateTime Created_at { get; set; }
        public string Description { get; set; }
        public string Country { get; set; }
    }
}
