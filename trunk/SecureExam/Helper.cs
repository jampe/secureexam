﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;
using Ionic.Zip;

namespace SecureExam
{
    public class Helper
    {
        public static string ByteArrayToHexString( Byte[] array )
        {
            if (array == null)
                throw new ArgumentNullException();

            StringBuilder sb = new StringBuilder();
            String hex;
            foreach( Byte b in array)
            {
                hex = Convert.ToString(b, 16);
                if (hex.Length == 1) hex = "0" + hex;
                sb.Append( hex );
            }
            return sb.ToString().ToUpper();
        }

        public static string ByteArrayToBase64(Byte[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array null");

            return System.Convert.ToBase64String(array, 0, array.Length);
        }

        /// <summary>
        /// Generates a SHA256 with Salt and chaining
        /// </summary>
        /// <param name="data">actual data</param>
        /// <param name="salt">salt as byte array</param>
        /// <param name="iterations">iterations for hash chaining</param>
        /// <returns>Byte[]</returns>
        public static byte[] SHA256(string data, byte[] salt, int iterations)
        {
            if (data == null)
                throw new ArgumentNullException("data null");
            if (salt == null)
                throw new ArgumentNullException("iv null");
            if (salt.Length != BasicSettings.getInstance().Encryption.SHA256.SaltLength / 8)
                throw new ArgumentException("SHA256 IV length invalid");
            if (iterations <= 0)
                throw new ArgumentException("SHA256 Iterations invalid");

            using(SHA256 mySHA256 = SHA256Managed.Create())
            {
                String ivB64 = Convert.ToBase64String(salt);

                byte[] hash = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(data + ivB64));
                for( int i = 0; i < iterations -1; i++ )
                {
                    hash = mySHA256.ComputeHash(hash);
                }

                return hash;
            }
        }

        public static byte[] getSecureRandomBytes(int length)
        {
            if (length <= 0)
                throw new ArgumentException("length not valid");

            Byte[] array = new Byte[length];

            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(array);
            }

            return array;
        }

        public static byte[] encryptAES(string data, byte[] Key, byte[] IV)
        {
            if (data == null || data.Length <= 0)
                throw new ArgumentNullException("data");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(data);
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }
        }

        public static void unzip(string inputPath, string outputPath){
            using (ZipFile zip = ZipFile.Read(inputPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(outputPath);
                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(true);
                }

                zip.ExtractAll(outputPath);
            }
        }

        public static double dateTimeToMillisecondsSince1970ForJS(DateTime date)
        {
            if (date == null)
                throw new ArgumentNullException("date null");

            DateTime baseDate = new DateTime(1970, 1, 1);
            baseDate = baseDate.Add(new TimeSpan(1, 0, 0)); // JS FIX 
            TimeSpan diff = date - baseDate;
            return diff.TotalMilliseconds;
        }
    }
}
