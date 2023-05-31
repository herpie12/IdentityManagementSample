using IdentityManagementSample.Model;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IdentityManagementSample
{
    public class Filestore
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
            //hash username and use it as filename, could be email etc.
            var hash = UserHash(user.Name);
            await using var writer = File.OpenWrite(hash);
            await JsonSerializer.SerializeAsync(writer, user);
        }
    }
}
