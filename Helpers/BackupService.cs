using System.IO.Compression;
using IdermaCapilar_Historial.Data;

namespace IdermaCapilar_Historial.Helpers;

public static class BackupService
{
    public static string AutoguardadoFolder => Path.Combine(AppDatabase.Instance.FolderPath, "Autoguardado");

    public static void Exportar(string zipPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);

        var temp = Path.Combine(Path.GetTempPath(), "iderma-historial-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            CopiarDatos(temp);
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(temp, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: false);
        }
        finally
        {
            TryDeleteDirectory(temp);
        }
    }

    public static void Importar(string zipPath)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException("No se encontró el archivo de importación.", zipPath);
        }

        Directory.CreateDirectory(AutoguardadoFolder);
        var seguridad = Path.Combine(AutoguardadoFolder, $"antes-importar-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
        Exportar(seguridad);

        var temp = Path.Combine(Path.GetTempPath(), "iderma-historial-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            ZipFile.ExtractToDirectory(zipPath, temp, overwriteFiles: true);
            var dbOrigen = Directory.GetFiles(temp, "historial.db", SearchOption.AllDirectories).FirstOrDefault();
            if (dbOrigen is null)
            {
                throw new InvalidOperationException("El archivo no contiene una base historial.db.");
            }

            AppDatabase.Instance.RestoreFrom(dbOrigen);
            CopiarFotos(Path.GetDirectoryName(dbOrigen)!, AppDatabase.Instance.FotosPath);
            CopiarAjustes(Path.GetDirectoryName(dbOrigen)!);
            AppDatabase.Instance.Initialize();
        }
        finally
        {
            TryDeleteDirectory(temp);
        }
    }

    public static string CrearCopiaAutoguardado()
    {
        Directory.CreateDirectory(AutoguardadoFolder);
        var destino = Path.Combine(AutoguardadoFolder, $"autoguardado-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
        Exportar(destino);
        var info = new FileInfo(destino);
        if (!info.Exists || info.Length < 1024)
        {
            TryDeleteFile(destino);
            throw new InvalidOperationException("La copia automática quedó vacía o dañada.");
        }

        LimpiarCopiasAntiguas();
        return destino;
    }

    private static void CopiarDatos(string destino)
    {
        Directory.CreateDirectory(destino);
        AppDatabase.Instance.BackupTo(Path.Combine(destino, "historial.db"));

        var ajustes = Path.Combine(AppDatabase.Instance.FolderPath, "ajustes.json");
        if (File.Exists(ajustes))
        {
            File.Copy(ajustes, Path.Combine(destino, "ajustes.json"), true);
        }

        CopiarFotos(AppDatabase.Instance.FotosPath, Path.Combine(destino, "Fotos"));
    }

    private static void CopiarFotos(string origen, string destino)
    {
        if (!Directory.Exists(origen))
        {
            return;
        }

        Directory.CreateDirectory(destino);
        foreach (var foto in Directory.GetFiles(origen))
        {
            try
            {
                File.Copy(foto, Path.Combine(destino, Path.GetFileName(foto)), true);
            }
            catch
            {
                // Una foto bloqueada no debe abortar toda la copia.
            }
        }
    }

    private static void CopiarAjustes(string carpetaOrigen)
    {
        var origen = Directory.GetFiles(carpetaOrigen, "ajustes.json", SearchOption.AllDirectories).FirstOrDefault();
        if (origen is null)
        {
            return;
        }

        try
        {
            File.Copy(origen, Path.Combine(AppDatabase.Instance.FolderPath, "ajustes.json"), true);
            AppSettings.Load();
        }
        catch
        {
            // Se conservan los ajustes actuales si el archivo importado no se puede leer.
        }
    }

    private static void LimpiarCopiasAntiguas()
    {
        if (!Directory.Exists(AutoguardadoFolder))
        {
            return;
        }

        foreach (var vacio in Directory.GetFiles(AutoguardadoFolder, "autoguardado-*.zip")
                     .Select(f => new FileInfo(f))
                     .Where(f => f.Length < 1024))
        {
            TryDeleteFile(vacio.FullName);
        }

        var archivos = Directory.GetFiles(AutoguardadoFolder, "autoguardado-*.zip")
            .Select(f => new FileInfo(f))
            .Where(f => f.Length >= 1024)
            .OrderByDescending(f => f.CreationTimeUtc)
            .Skip(12)
            .ToList();
        foreach (var archivo in archivos)
        {
            TryDeleteFile(archivo.FullName);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // El archivo puede estar en uso.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // La carpeta temporal se limpia en el siguiente arranque del sistema.
        }
    }
}
