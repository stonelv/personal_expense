using System.Security.Cryptography;
using System.Text;

namespace PersonalExpense.Api.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10000;
    
    public string HashPassword(string password)
    {
        using var algorithm = new Rfc2898DeriveBytes(
            password, 
            SaltSize, 
            Iterations, 
            HashAlgorithmName.SHA256);
        
        var salt = algorithm.Salt;
        var hash = algorithm.GetBytes(HashSize);
        
        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);
        
        return Convert.ToBase64String(hashBytes);
    }
    
    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(hashedPassword);
            
            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);
            
            var storedHash = new byte[HashSize];
            Array.Copy(hashBytes, SaltSize, storedHash, 0, HashSize);
            
            using var algorithm = new Rfc2898DeriveBytes(
                password, 
                salt, 
                Iterations, 
                HashAlgorithmName.SHA256);
            
            var computedHash = algorithm.GetBytes(HashSize);
            
            return computedHash.SequenceEqual(storedHash);
        }
        catch
        {
            return false;
        }
    }
}