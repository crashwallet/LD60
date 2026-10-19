// Какой текст игрок видит в файле
public static class FileText {
    public static string Visible(FileData f) {
        if (f == null) return "";
        if (!string.IsNullOrEmpty(f.content)) return f.content;
        if (!f.encrypted) return f.plainText ?? "";

        // Забыли нажать «Зашифровать plainText → content» — шифруем на лету, чтобы не показать ответ
        try {
            return CipherFactory.Get(f.cipher).Encrypt(f.plainText ?? "", f.key);
        } catch {
            return "<ошибка шифрования: проверь key у файла>";
        }
    }
}
