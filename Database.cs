using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IdentityManagementSample
{
    public class Database
    {
        private static string UserHash(string userName)
        {
            return Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(userName)));
        }

        public async Task<User> GetUserAsync(string userName)
        {
            var hash = UserHash(userName);

            if (!File.Exists(hash))
            {
                return null;
            }

            await using var reader = File.OpenRead(hash);
            return await JsonSerializer.DeserializeAsync<User>(reader);
        }


        public async Task PutAsync(User user)
        {
            var hash = UserHash(user.Name);
            await using var writer = File.OpenWrite(hash);
            await JsonSerializer.SerializeAsync(writer, user);
        }
    }

    public class User
    {
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public List<UserClaim> Claims { get; set; }
    }

    public class UserClaim
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
