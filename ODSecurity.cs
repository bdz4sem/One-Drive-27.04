using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace OneDrive
{
    class ODSecurity // To be created for unique user of this app
    {
        public byte[] Key = new byte[32];
        public byte[] IV = new byte[16];
        public string guid;

        public ODSecurity() // Generates new ODSecurity
        {
            Random random = new Random();
            //random.NextBytes(Key);
            //random.NextBytes(IV);
            Key = Encoding.ASCII.GetBytes("01234567890123456789012345678901");
            IV = Encoding.ASCII.GetBytes("9876543210987654");
            guid = System.Guid.NewGuid().ToString().Replace("-","");
        }
        public void SaveODSecurity(string HashedPass) // Encrypts and saves ODSecurity
        {
            FileStream fs = new FileStream("mySecFile.ods", FileMode.Create);
            Rijndael rijndael = Rijndael.Create();
            rijndael.Key = Encoding.ASCII.GetBytes(HashedPass.Substring(0,32)); // To be entered
            rijndael.IV = Encoding.ASCII.GetBytes(HashedPass.Substring(32,16));
            rijndael.Padding = PaddingMode.None;
            ICryptoTransform iCryptoTransform = rijndael.CreateEncryptor();
            CryptoStream cryptoStream = new CryptoStream(fs, iCryptoTransform, CryptoStreamMode.Write);
            string temp = Encoding.ASCII.GetString(Key) + Encoding.ASCII.GetString(IV) + guid; // добавить проверку хэш-суммы
            cryptoStream.Write(Encoding.ASCII.GetBytes(temp), 0, temp.Length);
            fs.Close();
        }
        public ODSecurity OpenODSecurity(string HashedPass)  // Decrypts and opens ODSecurity
        {
            ODSecurity mySecurity = new ODSecurity();
            FileStream fs = new FileStream("mySecFile.ods", FileMode.Open);
            Rijndael rijndael = Rijndael.Create();
            rijndael.Key = Encoding.ASCII.GetBytes(HashedPass.Substring(0, 32)); // To be entered
            rijndael.IV = Encoding.ASCII.GetBytes(HashedPass.Substring(32, 16));
            rijndael.Padding = PaddingMode.None;
            ICryptoTransform iCryptoTransform = rijndael.CreateDecryptor();
            CryptoStream cryptoStream = new CryptoStream(fs, iCryptoTransform, CryptoStreamMode.Read);
            byte[] buffer = new byte[80];
            cryptoStream.Read(buffer, 0, buffer.Length);
            for (int iter = 0; iter < Key.Length; iter++)
            {
                mySecurity.Key[iter] = buffer[iter];
            }
            for (int iter = 0; iter < IV.Length; iter++)
            {
                mySecurity.IV[iter] = buffer[iter + Key.Length];
            }
            mySecurity.guid = Encoding.ASCII.GetString(buffer).Substring(48,32);
            fs.Close();
            return mySecurity;
        }
    }
}
