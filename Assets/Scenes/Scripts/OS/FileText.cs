// Какой текст игрок видит в файле
public static class FileText {
    public static string Visible(FileData f) {
        if (f == null) return "";

        // Незашифрованный файл — на языке игры
        if (!f.encrypted) {
            string ru = !string.IsNullOrEmpty(f.content) ? f.content : f.plainText ?? "";
            return Loc.T(ru, f.plainTextEn);
        }

        // Шифротекст всегда один (шифры на английском)
        if (!string.IsNullOrEmpty(f.content)) return f.content;

        // Забыли нажать «Зашифровать plainText → content» — шифруем на лету, чтобы не показать ответ
        try {
            return CipherFactory.Get(f.cipher).Encrypt(f.plainText ?? "", f.key);
        } catch {
            return "<ошибка шифрования: проверь key у файла>";
        }
    }
}
