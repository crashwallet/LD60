using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public enum CipherType { Caesar = 0, Vigenere = 1, Des = 3 }

public interface ICipher {
    string Decrypt(string text, string key);
    string Encrypt(string text, string key);
}

public class CaesarCipher : ICipher {
    public string Encrypt(string t, string key) => Shift(t, int.Parse(key));
    public string Decrypt(string t, string key) => Shift(t, -int.Parse(key));

    static string Shift(string t, int s) {
        var sb = new StringBuilder();
        foreach (char c in t) {
            if (char.IsLetter(c)) {
                char b = char.IsUpper(c) ? 'A' : 'a';
                sb.Append((char)(b + ((c - b + s) % 26 + 26) % 26));
            } else sb.Append(c);
        }
        return sb.ToString();
    }
}

public class VigenereCipher : ICipher {
    public string Encrypt(string t, string k) => Run(t, k, 1);
    public string Decrypt(string t, string k) => Run(t, k, -1);

    static string Run(string t, string k, int dir) {
        var sb = new StringBuilder();
        int i = 0;
        foreach (char c in t) {
            if (!char.IsLetter(c)) { sb.Append(c); continue; }
            char b = char.IsUpper(c) ? 'A' : 'a';
            int s = (char.ToUpper(k[i++ % k.Length]) - 'A') * dir;
            sb.Append((char)(b + ((c - b + s) % 26 + 26) % 26));
        }
        return sb.ToString();
    }
}

// «DES»: XOR с SHA256 от ключа, вывод в hex
public class FakeDesCipher : ICipher {
    static byte[] K(string key) { using var s = SHA256.Create(); return s.ComputeHash(Encoding.UTF8.GetBytes(key)); }
    public string Encrypt(string t, string key) => Hex.To(Hex.Xor(Encoding.UTF8.GetBytes(t), K(key)));
    public string Decrypt(string t, string key) => Encoding.UTF8.GetString(Hex.Xor(Hex.From(t), K(key)));
}

static class Hex {
    public static byte[] Xor(byte[] d, byte[] k) {
        var r = new byte[d.Length];
        for (int i = 0; i < d.Length; i++) r[i] = (byte)(d[i] ^ k[i % k.Length]);
        return r;
    }
    public static byte[] From(string h) {
        h = h.Replace(" ", "");
        var b = new byte[h.Length / 2];
        for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
        return b;
    }
    public static string To(byte[] b) => BitConverter.ToString(b).Replace("-", " ");
}

public static class CipherFactory {
    static readonly Dictionary<CipherType, ICipher> map = new() {
        { CipherType.Caesar,   new CaesarCipher() },
        { CipherType.Vigenere, new VigenereCipher() },
        { CipherType.Des,      new FakeDesCipher() },
    };
    public static ICipher Get(CipherType t) => map[t];
}