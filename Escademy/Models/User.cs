namespace Escademy.Models
{
    public class User
    {
        public int Id
        {
            get;
            set;
        }
        public int Level
        {
            get;
            set;
        }

        public string Email
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }

        public string FirstName
        {
            get;
            set;
        }


        public string LastName
        {
            get;
            set;
        }

        public string Picture
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }
        public string Country
        {
            get;
            set;
        }

        public bool HasRole(UserLevel Level)
        {
            return UserLevelMananger.HasRole(this.Level, Level);
        }
    }
}