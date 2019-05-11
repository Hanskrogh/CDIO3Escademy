namespace Escademy.Models
{
    public class UserLevelMananger
    {
        public static bool HasRole(int Level, UserLevel Role)
        {
            return Level >= (int)Role;
        }

        public static string GetRoleName(int Level)
        {
            switch (Level)
            {
                case (int)UserLevel.Admin: return "Staff Member";
                case (int)UserLevel.Coach: return "Coach";
                case (int)UserLevel.Default: return "Default User";
            }
            
            return ((UserLevel)Level).ToString();
        }
    }
}