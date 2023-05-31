namespace IdentityManagementSample.Model
{
    public class User
    {
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public List<UserClaim> Claims { get; set; } = new();
    }
}
