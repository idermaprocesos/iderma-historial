using System.Globalization;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.Data.Sqlite;

namespace IdermaCapilar_Historial.Data;

public sealed class AppDatabase
{
    public const int PaginaPacientes = 80;
    public const int PaginaOperaciones = 80;
    public const int SugerenciasPaciente = 12;

    public static AppDatabase Instance { get; } = new();
    private readonly object _sync = new();
    private bool _ftsPacientes;

    public string FolderPath { get; }
    public string DbPath { get; }
    public string FotosPath { get; }

    private AppDatabase()
    {
        FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IdermaCapilar",
            "Historial");
        Directory.CreateDirectory(FolderPath);
        FotosPath = Path.Combine(FolderPath, "Fotos");
        Directory.CreateDirectory(FotosPath);
        DbPath = Path.Combine(FolderPath, "historial.db");
    }

    public void Initialize()
    {
        lock (_sync)
        {
            try
            {
                InitializeCore();
            }
            catch (Exception ex) when (EsCorrupcion(ex))
            {
                RepararCorrupcion();
                InitializeCore();
            }
        }
    }

    private void InitializeCore()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Pacientes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NumeroExpediente TEXT NOT NULL UNIQUE,
                Nombres TEXT NOT NULL,
                Apellidos TEXT NOT NULL,
                TipoDocumento TEXT,
                NumeroDocumento TEXT,
                FechaNacimiento TEXT,
                Sexo TEXT,
                EstadoCivil TEXT,
                Nacionalidad TEXT,
                Ocupacion TEXT,
                Direccion TEXT,
                Distrito TEXT,
                Ciudad TEXT,
                Telefono TEXT,
                TelefonoAlterno TEXT,
                Email TEXT,
                FotoRuta TEXT,
                FechaRegistro TEXT NOT NULL,
                FechaActualizacion TEXT,
                Activo INTEGER NOT NULL DEFAULT 1,
                NotasGenerales TEXT
            );

            CREATE TABLE IF NOT EXISTS ContactosEmergencia (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PacienteId INTEGER NOT NULL,
                Nombre TEXT NOT NULL,
                Relacion TEXT,
                Telefono TEXT,
                TelefonoAlterno TEXT,
                Email TEXT,
                Direccion TEXT,
                FOREIGN KEY (PacienteId) REFERENCES Pacientes(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS DatosMedicos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PacienteId INTEGER NOT NULL UNIQUE,
                GrupoSanguineo TEXT,
                Alergias TEXT,
                MedicamentosActuales TEXT,
                AntecedentesPersonales TEXT,
                AntecedentesFamiliares TEXT,
                EnfermedadesCronicas TEXT,
                CirugiasPrevias TEXT,
                Fuma TEXT,
                Alcohol TEXT,
                PesoKg REAL,
                TallaCm REAL,
                PresionArterial TEXT,
                TipoAlopecia TEXT,
                EscalaNorwood TEXT,
                EscalaLudwig TEXT,
                ZonaAfectada TEXT,
                DensidadZonaDonante TEXT,
                CalidadCabello TEXT,
                ColorCabello TEXT,
                GrosorCabello TEXT,
                Diagnostico TEXT,
                PlanTratamiento TEXT,
                MedicoResponsable TEXT,
                ObservacionesMedicas TEXT,
                FOREIGN KEY (PacienteId) REFERENCES Pacientes(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Procedimientos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PacienteId INTEGER NOT NULL,
                Fecha TEXT NOT NULL,
                Tipo TEXT NOT NULL,
                AreaTratada TEXT,
                UnidadesFoliculares INTEGER,
                NumeroInjertos INTEGER,
                Medico TEXT,
                Enfermera TEXT,
                Anestesia TEXT,
                DuracionMinutos INTEGER,
                Estado TEXT,
                Resultado TEXT,
                Complicaciones TEXT,
                Observaciones TEXT,
                ChecklistPasos TEXT,
                ZonasOperadas TEXT,
                Costo REAL,
                FOREIGN KEY (PacienteId) REFERENCES Pacientes(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Observaciones (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PacienteId INTEGER NOT NULL,
                Fecha TEXT NOT NULL,
                Tipo TEXT,
                Titulo TEXT,
                Contenido TEXT NOT NULL,
                Autor TEXT,
                FOREIGN KEY (PacienteId) REFERENCES Pacientes(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Doctores (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Titulo TEXT,
                Nombres TEXT NOT NULL,
                Apellidos TEXT NOT NULL,
                Especialidad TEXT,
                Cmp TEXT,
                Telefono TEXT,
                Email TEXT,
                Activo INTEGER NOT NULL DEFAULT 1,
                EsPredeterminado INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Enfermeras (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Titulo TEXT,
                Nombres TEXT NOT NULL,
                Apellidos TEXT NOT NULL,
                Especialidad TEXT,
                Cep TEXT,
                Telefono TEXT,
                Email TEXT,
                Activo INTEGER NOT NULL DEFAULT 1,
                EsPredeterminada INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Citas (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PacienteId INTEGER NOT NULL,
                FechaInicio TEXT NOT NULL,
                DuracionMinutos INTEGER NOT NULL DEFAULT 30,
                Tipo TEXT NOT NULL,
                Estado TEXT NOT NULL DEFAULT 'Programada',
                Medico TEXT,
                Notas TEXT,
                Temperamento TEXT NOT NULL DEFAULT 'Amable',
                ComprobanteTipo TEXT,
                ComprobanteNumero TEXT,
                ComprobanteConcepto TEXT,
                ComprobanteMonto REAL,
                ComprobanteFormaPago TEXT,
                ComprobanteCliente TEXT,
                ComprobanteDocumento TEXT,
                ComprobanteCorreo TEXT,
                FOREIGN KEY (PacienteId) REFERENCES Pacientes(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Pacientes_Nombre ON Pacientes(Apellidos, Nombres);
            CREATE INDEX IF NOT EXISTS IX_Pacientes_Documento ON Pacientes(NumeroDocumento);
            CREATE INDEX IF NOT EXISTS IX_Pacientes_Expediente ON Pacientes(NumeroExpediente);
            CREATE INDEX IF NOT EXISTS IX_Pacientes_ActivoNombre ON Pacientes(Activo, Apellidos, Nombres);
            CREATE INDEX IF NOT EXISTS IX_Pacientes_Telefono ON Pacientes(Telefono);
            CREATE INDEX IF NOT EXISTS IX_Pacientes_Email ON Pacientes(Email);
            CREATE INDEX IF NOT EXISTS IX_Pacientes_FechaRegistro ON Pacientes(FechaRegistro);
            CREATE INDEX IF NOT EXISTS IX_Contactos_Paciente ON ContactosEmergencia(PacienteId);
            CREATE INDEX IF NOT EXISTS IX_Procedimientos_Paciente ON Procedimientos(PacienteId);
            CREATE INDEX IF NOT EXISTS IX_Procedimientos_Fecha ON Procedimientos(Fecha);
            CREATE INDEX IF NOT EXISTS IX_Procedimientos_EstadoFecha ON Procedimientos(Estado, Fecha);
            CREATE INDEX IF NOT EXISTS IX_Observaciones_Paciente ON Observaciones(PacienteId);
            CREATE TABLE IF NOT EXISTS SesionesOperacion (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcedimientoId INTEGER NOT NULL,
                FechaInicio TEXT NOT NULL,
                DuracionMinutos INTEGER NOT NULL DEFAULT 60,
                Estado TEXT NOT NULL DEFAULT 'Programada',
                Notas TEXT,
                FOREIGN KEY (ProcedimientoId) REFERENCES Procedimientos(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Citas_Fecha ON Citas(FechaInicio);
            CREATE INDEX IF NOT EXISTS IX_Citas_Paciente ON Citas(PacienteId);
            CREATE INDEX IF NOT EXISTS IX_Citas_EstadoFecha ON Citas(Estado, FechaInicio);
            CREATE INDEX IF NOT EXISTS IX_Sesiones_Procedimiento ON SesionesOperacion(ProcedimientoId);
            CREATE INDEX IF NOT EXISTS IX_Sesiones_Fecha ON SesionesOperacion(FechaInicio);
            """;
        command.ExecuteNonQuery();
        AsegurarColumna(connection, "Citas", "Temperamento", "TEXT NOT NULL DEFAULT 'Amable'");
        AsegurarColumna(connection, "Citas", "ComprobanteTipo", "TEXT");
        AsegurarColumna(connection, "Citas", "ComprobanteNumero", "TEXT");
        AsegurarColumna(connection, "Citas", "ComprobanteConcepto", "TEXT");
        AsegurarColumna(connection, "Citas", "ComprobanteMonto", "REAL");
        AsegurarColumna(connection, "Citas", "ComprobanteFormaPago", "TEXT");
        AsegurarColumna(connection, "Citas", "ComprobanteCliente", "TEXT");
        AsegurarColumna(connection, "Citas", "ComprobanteDocumento", "TEXT");
        AsegurarColumna(connection, "Citas", "ComprobanteCorreo", "TEXT");
        AsegurarColumna(connection, "Procedimientos", "ChecklistPasos", "TEXT");
        AsegurarColumna(connection, "Procedimientos", "ZonasOperadas", "TEXT");
        AsegurarColumna(connection, "Procedimientos", "Enfermera", "TEXT");
        NormalizarFechas(connection);
        _ftsPacientes = false;
        QuitarIndiceFts(connection);
        SembrarDoctorInicial(connection);
    }

    public void BackupTo(string destinationPath)
    {
        lock (_sync)
        {
            var carpeta = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            TryDelete(destinationPath);
            using var source = Open();
            using var command = source.CreateCommand();
            var ruta = destinationPath.Replace("'", "''", StringComparison.Ordinal);
            command.CommandText = $"VACUUM INTO '{ruta}';";
            command.ExecuteNonQuery();
        }
    }

    public void RestoreFrom(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("No se encontró historial.db en la copia.", sourcePath);
        }

        lock (_sync)
        {
            using var dest = Open();
            using var source = new SqliteConnection($"Data Source={sourcePath}");
            source.Open();
            source.BackupDatabase(dest);
            using var checkpoint = dest.CreateCommand();
            checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            checkpoint.ExecuteNonQuery();
        }
    }

    private static void SembrarDoctorInicial(SqliteConnection connection)
    {
        using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM Doctores;";
        var total = Convert.ToInt32(count.ExecuteScalar());
        if (total > 0)
        {
            return;
        }

        var predeterminado = Helpers.AppSettings.Current.MedicoPredeterminado;
        if (string.IsNullOrWhiteSpace(predeterminado))
        {
            return;
        }

        var partes = predeterminado.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var titulo = "Dr.";
        var inicio = 0;
        if (partes.Length > 0 && (partes[0].StartsWith("Dr", StringComparison.OrdinalIgnoreCase) || partes[0] is "Mg." or "Lic."))
        {
            titulo = partes[0].EndsWith('.') ? partes[0] : partes[0] + ".";
            inicio = 1;
        }

        var resto = partes.Skip(inicio).ToArray();
        var nombres = resto.Length == 0 ? predeterminado : resto[0];
        var apellidos = resto.Length <= 1 ? string.Empty : string.Join(' ', resto.Skip(1));

        using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO Doctores (Titulo, Nombres, Apellidos, Activo, EsPredeterminado)
            VALUES ($titulo, $nombres, $apellidos, 1, 1);
            """;
        insert.Parameters.AddWithValue("$titulo", titulo);
        insert.Parameters.AddWithValue("$nombres", nombres.Trim());
        insert.Parameters.AddWithValue("$apellidos", apellidos.Trim());
        insert.ExecuteNonQuery();
    }

    public List<Doctor> GetDoctores(bool soloActivos = false)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = soloActivos
            ? "SELECT * FROM Doctores WHERE Activo = 1 ORDER BY Apellidos, Nombres;"
            : "SELECT * FROM Doctores ORDER BY Activo DESC, Apellidos, Nombres;";
        var lista = new List<Doctor>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(MapDoctor(reader));
        }

        return lista;
    }

    public int GuardarDoctor(Doctor doctor)
    {
        lock (_sync)
        {
            return GuardarDoctorCore(doctor);
        }
    }

    private int GuardarDoctorCore(Doctor doctor)
    {
        if (GetDoctores().Count == 0)
        {
            doctor.EsPredeterminado = true;
            doctor.Activo = true;
        }
        using var connection = Open();
        if (doctor.EsPredeterminado)
        {
            using var clear = connection.CreateCommand();
            clear.CommandText = "UPDATE Doctores SET EsPredeterminado = 0;";
            clear.ExecuteNonQuery();
        }

        using var command = connection.CreateCommand();
        if (doctor.Id == 0)
        {
            command.CommandText = """
                INSERT INTO Doctores (Titulo, Nombres, Apellidos, Especialidad, Cmp, Telefono, Email, Activo, EsPredeterminado)
                VALUES ($titulo, $nombres, $apellidos, $especialidad, $cmp, $telefono, $email, $activo, $predeterminado);
                SELECT last_insert_rowid();
                """;
            BindDoctor(command, doctor);
            doctor.Id = Convert.ToInt32(command.ExecuteScalar());
        }
        else
        {
            command.CommandText = """
                UPDATE Doctores SET
                    Titulo = $titulo, Nombres = $nombres, Apellidos = $apellidos, Especialidad = $especialidad,
                    Cmp = $cmp, Telefono = $telefono, Email = $email, Activo = $activo, EsPredeterminado = $predeterminado
                WHERE Id = $id;
                """;
            BindDoctor(command, doctor);
            command.Parameters.AddWithValue("$id", doctor.Id);
            command.ExecuteNonQuery();
        }

        if (doctor.EsPredeterminado)
        {
            Helpers.AppSettings.Current.MedicoPredeterminado = doctor.NombreMostrado;
            Helpers.AppSettings.Current.Save();
        }
        else if (Helpers.AppSettings.Current.MedicoPredeterminado == doctor.NombreMostrado)
        {
            var otro = GetDoctores(soloActivos: true).FirstOrDefault(d => d.EsPredeterminado)
                ?? GetDoctores(soloActivos: true).FirstOrDefault();
            Helpers.AppSettings.Current.MedicoPredeterminado = otro?.NombreMostrado ?? string.Empty;
            Helpers.AppSettings.Current.Save();
        }

        return doctor.Id;
    }

    public void EliminarDoctor(int id)
    {
        lock (_sync)
        {
            EliminarDoctorCore(id);
        }
    }

    private void EliminarDoctorCore(int id)
    {
        var actual = GetDoctores().FirstOrDefault(d => d.Id == id);
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Doctores WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();

        if (actual is not null && Helpers.AppSettings.Current.MedicoPredeterminado == actual.NombreMostrado)
        {
            var otro = GetDoctores(soloActivos: true).FirstOrDefault();
            Helpers.AppSettings.Current.MedicoPredeterminado = otro?.NombreMostrado ?? string.Empty;
            Helpers.AppSettings.Current.Save();
        }
    }

    public List<Enfermera> GetEnfermeras(bool soloActivas = false)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = soloActivas
            ? "SELECT * FROM Enfermeras WHERE Activo = 1 ORDER BY Apellidos, Nombres;"
            : "SELECT * FROM Enfermeras ORDER BY Activo DESC, Apellidos, Nombres;";
        var lista = new List<Enfermera>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(MapEnfermera(reader));
        }

        return lista;
    }

    public int GuardarEnfermera(Enfermera enfermera)
    {
        lock (_sync)
        {
            return GuardarEnfermeraCore(enfermera);
        }
    }

    private int GuardarEnfermeraCore(Enfermera enfermera)
    {
        if (GetEnfermeras().Count == 0)
        {
            enfermera.EsPredeterminada = true;
            enfermera.Activo = true;
        }

        using var connection = Open();
        if (enfermera.EsPredeterminada)
        {
            using var clear = connection.CreateCommand();
            clear.CommandText = "UPDATE Enfermeras SET EsPredeterminada = 0;";
            clear.ExecuteNonQuery();
        }

        using var command = connection.CreateCommand();
        if (enfermera.Id == 0)
        {
            command.CommandText = """
                INSERT INTO Enfermeras (Titulo, Nombres, Apellidos, Especialidad, Cep, Telefono, Email, Activo, EsPredeterminada)
                VALUES ($titulo, $nombres, $apellidos, $especialidad, $cep, $telefono, $email, $activo, $predeterminada);
                SELECT last_insert_rowid();
                """;
            BindEnfermera(command, enfermera);
            enfermera.Id = Convert.ToInt32(command.ExecuteScalar());
        }
        else
        {
            command.CommandText = """
                UPDATE Enfermeras SET
                    Titulo = $titulo, Nombres = $nombres, Apellidos = $apellidos, Especialidad = $especialidad,
                    Cep = $cep, Telefono = $telefono, Email = $email, Activo = $activo, EsPredeterminada = $predeterminada
                WHERE Id = $id;
                """;
            BindEnfermera(command, enfermera);
            command.Parameters.AddWithValue("$id", enfermera.Id);
            command.ExecuteNonQuery();
        }

        if (enfermera.EsPredeterminada)
        {
            Helpers.AppSettings.Current.EnfermeraPredeterminada = enfermera.NombreMostrado;
            Helpers.AppSettings.Current.Save();
        }
        else if (Helpers.AppSettings.Current.EnfermeraPredeterminada == enfermera.NombreMostrado)
        {
            var otra = GetEnfermeras(soloActivas: true).FirstOrDefault(d => d.EsPredeterminada)
                ?? GetEnfermeras(soloActivas: true).FirstOrDefault();
            Helpers.AppSettings.Current.EnfermeraPredeterminada = otra?.NombreMostrado ?? string.Empty;
            Helpers.AppSettings.Current.Save();
        }

        return enfermera.Id;
    }

    public void EliminarEnfermera(int id)
    {
        lock (_sync)
        {
            EliminarEnfermeraCore(id);
        }
    }

    private void EliminarEnfermeraCore(int id)
    {
        var actual = GetEnfermeras().FirstOrDefault(d => d.Id == id);
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Enfermeras WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();

        if (actual is not null && Helpers.AppSettings.Current.EnfermeraPredeterminada == actual.NombreMostrado)
        {
            var otra = GetEnfermeras(soloActivas: true).FirstOrDefault();
            Helpers.AppSettings.Current.EnfermeraPredeterminada = otra?.NombreMostrado ?? string.Empty;
            Helpers.AppSettings.Current.Save();
        }
    }

    public string SiguienteExpediente()
    {
        lock (_sync)
        {
            using var connection = Open();
            return CalcularSiguienteExpediente(connection);
        }
    }

    private static string CalcularSiguienteExpediente(SqliteConnection connection, SqliteTransaction? transaccion = null)
    {
        var anio = DateTime.Now.Year;
        var prefijo = $"IDC-{anio}-";
        using var command = connection.CreateCommand();
        command.Transaction = transaccion;
        command.CommandText = """
            SELECT NumeroExpediente
            FROM Pacientes
            WHERE NumeroExpediente LIKE $prefijo
            ORDER BY NumeroExpediente DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$prefijo", prefijo + "%");
        var ultimo = command.ExecuteScalar() as string;
        var siguiente = 1;
        if (!string.IsNullOrWhiteSpace(ultimo) && ultimo.Length >= prefijo.Length + 4)
        {
            if (int.TryParse(ultimo[prefijo.Length..], out var n))
            {
                siguiente = n + 1;
            }
        }

        return $"{prefijo}{siguiente:000000}";
    }

    public DashboardStats GetDashboardStats()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        var inicioMes = SqliteDate.FormatValue(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var hoy = SqliteDate.FormatValue(DateTime.Today);
        var manana = SqliteDate.FormatValue(DateTime.Today.AddDays(1));
        command.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM Pacientes) AS Total,
                (SELECT COUNT(*) FROM Pacientes WHERE Activo = 1) AS Activos,
                (SELECT COUNT(*) FROM Procedimientos WHERE Fecha >= $inicioMes AND Estado != 'Cancelado') AS Mes,
                (SELECT COUNT(*) FROM Procedimientos WHERE Estado != 'Cancelado') AS Operaciones,
                (SELECT COUNT(*) FROM Citas WHERE Estado IN ('Programada', 'Confirmada') AND FechaInicio >= $hoy) AS Pendientes,
                (SELECT COUNT(*) FROM Citas WHERE Estado NOT IN ('Cancelada') AND FechaInicio >= $hoy AND FechaInicio < $manana) AS Hoy,
                (SELECT COUNT(*) FROM Citas WHERE Estado IN ('Programada', 'Confirmada') AND FechaInicio >= $hoy) AS Proximas;
            """;
        command.Parameters.AddWithValue("$inicioMes", inicioMes);
        command.Parameters.AddWithValue("$hoy", hoy);
        command.Parameters.AddWithValue("$manana", manana);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new DashboardStats
            {
                TotalPacientes = reader.GetInt32(0),
                ExpedientesActivos = reader.GetInt32(1),
                ProcedimientosMes = reader.GetInt32(2),
                TotalOperaciones = reader.GetInt32(3),
                SeguimientosPendientes = reader.GetInt32(4),
                CitasHoy = reader.GetInt32(5),
                CitasProximas = reader.GetInt32(6)
            };
        }

        return new DashboardStats();
    }

    public List<Paciente> GetPacientesRecientes(int cantidad = 8)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM Pacientes
            ORDER BY FechaRegistro DESC
            LIMIT $cantidad;
            """;
        command.Parameters.AddWithValue("$cantidad", cantidad);
        return ReadPacientes(command);
    }

    public List<Paciente> BuscarPacientes(string? texto, int limite = SugerenciasPaciente)
        => BuscarPacientesPagina(texto, soloActivos: false, limite, 0).Items;

    public PaginaConsulta<Paciente> BuscarPacientesPagina(string? texto, bool soloActivos, int limite, int offset)
    {
        limite = Math.Clamp(limite, 1, 200);
        offset = Math.Max(0, offset);
        try
        {
            return ConsultarPacientes(texto, soloActivos, limite, offset, usarFts: true);
        }
        catch
        {
            return ConsultarPacientes(texto, soloActivos, limite, offset, usarFts: false);
        }
    }

    private PaginaConsulta<Paciente> ConsultarPacientes(string? texto, bool soloActivos, int limite, int offset, bool usarFts)
    {
        using var connection = Open();
        var fts = usarFts ? ConsultaFts(texto) : null;
        using var count = connection.CreateCommand();
        using var command = connection.CreateCommand();
        PrepararConsultaPacientes(count, soloActivos, texto, fts, contar: true, limite, offset);
        var total = Convert.ToInt32(count.ExecuteScalar());
        PrepararConsultaPacientes(command, soloActivos, texto, fts, contar: false, limite, offset);
        return new PaginaConsulta<Paciente>
        {
            Items = ReadPacientes(command),
            Total = total
        };
    }

    private void PrepararConsultaPacientes(
        SqliteCommand command,
        bool soloActivos,
        string? texto,
        string? fts,
        bool contar,
        int limite,
        int offset)
    {
        command.Parameters.Clear();
        var tel = TelefonoFormato.Digitos(texto);
        var usarFts = _ftsPacientes && !string.IsNullOrWhiteSpace(fts);
        if (usarFts)
        {
            command.CommandText = contar
                ? """
                    SELECT COUNT(*)
                    FROM Pacientes p
                    WHERE ($solo = 0 OR p.Activo = 1)
                      AND (
                        ($tel != '' AND replace(ifnull(p.Telefono,''), ' ', '') LIKE $tel)
                        OR ($tel != '' AND replace(ifnull(p.TelefonoAlterno,''), ' ', '') LIKE $tel)
                        OR p.Id IN (SELECT rowid FROM PacientesFts WHERE PacientesFts MATCH $fts)
                      );
                    """
                : """
                    SELECT p.*
                    FROM Pacientes p
                    WHERE ($solo = 0 OR p.Activo = 1)
                      AND (
                        ($tel != '' AND replace(ifnull(p.Telefono,''), ' ', '') LIKE $tel)
                        OR ($tel != '' AND replace(ifnull(p.TelefonoAlterno,''), ' ', '') LIKE $tel)
                        OR p.Id IN (SELECT rowid FROM PacientesFts WHERE PacientesFts MATCH $fts)
                      )
                    ORDER BY p.Apellidos, p.Nombres
                    LIMIT $limite OFFSET $offset;
                    """;
            command.Parameters.AddWithValue("$fts", fts);
            command.Parameters.AddWithValue("$tel", string.IsNullOrEmpty(tel) ? "" : $"%{tel}%");
        }
        else if (string.IsNullOrWhiteSpace(texto))
        {
            command.CommandText = contar
                ? "SELECT COUNT(*) FROM Pacientes WHERE ($solo = 0 OR Activo = 1);"
                : """
                    SELECT * FROM Pacientes
                    WHERE ($solo = 0 OR Activo = 1)
                    ORDER BY Apellidos, Nombres
                    LIMIT $limite OFFSET $offset;
                    """;
        }
        else
        {
            command.CommandText = contar
                ? """
                    SELECT COUNT(*) FROM Pacientes
                    WHERE ($solo = 0 OR Activo = 1)
                      AND (Nombres LIKE $q OR Apellidos LIKE $q OR NumeroExpediente LIKE $q
                           OR NumeroDocumento LIKE $q OR Telefono LIKE $q OR Email LIKE $q
                           OR (Nombres || ' ' || Apellidos) LIKE $q
                           OR ($tel != '' AND replace(ifnull(Telefono,''), ' ', '') LIKE $tel)
                           OR ($tel != '' AND replace(ifnull(TelefonoAlterno,''), ' ', '') LIKE $tel));
                    """
                : """
                    SELECT * FROM Pacientes
                    WHERE ($solo = 0 OR Activo = 1)
                      AND (Nombres LIKE $q OR Apellidos LIKE $q OR NumeroExpediente LIKE $q
                           OR NumeroDocumento LIKE $q OR Telefono LIKE $q OR Email LIKE $q
                           OR (Nombres || ' ' || Apellidos) LIKE $q
                           OR ($tel != '' AND replace(ifnull(Telefono,''), ' ', '') LIKE $tel)
                           OR ($tel != '' AND replace(ifnull(TelefonoAlterno,''), ' ', '') LIKE $tel))
                    ORDER BY Apellidos, Nombres
                    LIMIT $limite OFFSET $offset;
                    """;
            command.Parameters.AddWithValue("$q", $"%{texto.Trim()}%");
            command.Parameters.AddWithValue("$tel", string.IsNullOrEmpty(tel) ? "" : $"%{tel}%");
        }

        command.Parameters.AddWithValue("$solo", soloActivos ? 1 : 0);
        if (!contar)
        {
            command.Parameters.AddWithValue("$limite", limite);
            command.Parameters.AddWithValue("$offset", offset);
        }
    }

    public Paciente? GetPaciente(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Pacientes WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return ReadPacientes(command).FirstOrDefault();
    }

    public int GuardarPaciente(Paciente paciente)
    {
        lock (_sync)
        {
            try
            {
                return GuardarPacienteCore(paciente);
            }
            catch (Exception ex) when (EsCorrupcion(ex))
            {
                RepararCorrupcion();
                return GuardarPacienteCore(paciente);
            }
        }
    }

    private int GuardarPacienteCore(Paciente paciente)
    {
        paciente.FechaActualizacion = DateTimeOffset.Now;
        using var connection = Open();
        using var transaccion = connection.BeginTransaction();
        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaccion;
            if (paciente.Id == 0)
            {
                if (string.IsNullOrWhiteSpace(paciente.NumeroExpediente))
                {
                    paciente.NumeroExpediente = CalcularSiguienteExpediente(connection, transaccion);
                }

                    command.CommandText = """
                    INSERT INTO Pacientes (
                        NumeroExpediente, Nombres, Apellidos, TipoDocumento, NumeroDocumento,
                        FechaNacimiento, Sexo, EstadoCivil, Nacionalidad, Ocupacion, Direccion,
                        Distrito, Ciudad, Telefono, TelefonoAlterno, Email, FotoRuta,
                        FechaRegistro, FechaActualizacion, Activo, NotasGenerales)
                    VALUES (
                        $numero, $nombres, $apellidos, $tipoDoc, $numDoc, $nacimiento, $sexo,
                        $estadoCivil, $nacionalidad, $ocupacion, $direccion, $distrito, $ciudad,
                        $telefono, $telefonoAlt, $email, $foto, $registro, $actualizacion, $activo, $notas);
                    """;
                BindPaciente(command, paciente);
                command.ExecuteNonQuery();
                command.CommandText = "SELECT last_insert_rowid();";
                paciente.Id = Convert.ToInt32(command.ExecuteScalar());
                transaccion.Commit();
                return paciente.Id;
            }

            command.CommandText = """
                UPDATE Pacientes SET
                    NumeroExpediente = $numero, Nombres = $nombres, Apellidos = $apellidos,
                    TipoDocumento = $tipoDoc, NumeroDocumento = $numDoc, FechaNacimiento = $nacimiento,
                    Sexo = $sexo, EstadoCivil = $estadoCivil, Nacionalidad = $nacionalidad,
                    Ocupacion = $ocupacion, Direccion = $direccion, Distrito = $distrito,
                    Ciudad = $ciudad, Telefono = $telefono, TelefonoAlterno = $telefonoAlt,
                    Email = $email, FotoRuta = $foto, FechaRegistro = $registro,
                    FechaActualizacion = $actualizacion, Activo = $activo, NotasGenerales = $notas
                WHERE Id = $id;
                """;
            BindPaciente(command, paciente);
            command.Parameters.AddWithValue("$id", paciente.Id);
            command.ExecuteNonQuery();
            transaccion.Commit();
            return paciente.Id;
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public void EliminarPaciente(int id)
    {
        lock (_sync)
        {
            using var connection = Open();
            string? foto = null;
            using (var lookup = connection.CreateCommand())
            {
                lookup.CommandText = "SELECT FotoRuta FROM Pacientes WHERE Id = $id;";
                lookup.Parameters.AddWithValue("$id", id);
                foto = lookup.ExecuteScalar() as string;
            }

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Pacientes WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
            BorrarFotoLocal(foto);
        }
    }

    private void BorrarFotoLocal(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
        {
            return;
        }

        try
        {
            var archivo = Path.GetFullPath(ruta);
            var carpeta = Path.GetFullPath(FotosPath);
            if (archivo.StartsWith(carpeta, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(archivo);
            }
        }
        catch
        {
            // La foto puede estar en uso por la UI.
        }
    }

    public DatosMedicos GetDatosMedicos(int pacienteId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM DatosMedicos WHERE PacienteId = $id;";
        command.Parameters.AddWithValue("$id", pacienteId);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapDatosMedicos(reader);
        }

        return new DatosMedicos { PacienteId = pacienteId };
    }

    public void GuardarDatosMedicos(DatosMedicos datos)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DatosMedicos (
                PacienteId, GrupoSanguineo, Alergias, MedicamentosActuales, AntecedentesPersonales,
                AntecedentesFamiliares, EnfermedadesCronicas, CirugiasPrevias, Fuma, Alcohol,
                PesoKg, TallaCm, PresionArterial, TipoAlopecia, EscalaNorwood, EscalaLudwig,
                ZonaAfectada, DensidadZonaDonante, CalidadCabello, ColorCabello, GrosorCabello,
                Diagnostico, PlanTratamiento, MedicoResponsable, ObservacionesMedicas)
            VALUES (
                $pacienteId, $grupo, $alergias, $medicamentos, $antPers, $antFam, $cronicas,
                $cirugias, $fuma, $alcohol, $peso, $talla, $presion, $alopecia, $norwood,
                $ludwig, $zona, $densidad, $calidad, $color, $grosor, $diagnostico, $plan,
                $medico, $obs)
            ON CONFLICT(PacienteId) DO UPDATE SET
                GrupoSanguineo = excluded.GrupoSanguineo,
                Alergias = excluded.Alergias,
                MedicamentosActuales = excluded.MedicamentosActuales,
                AntecedentesPersonales = excluded.AntecedentesPersonales,
                AntecedentesFamiliares = excluded.AntecedentesFamiliares,
                EnfermedadesCronicas = excluded.EnfermedadesCronicas,
                CirugiasPrevias = excluded.CirugiasPrevias,
                Fuma = excluded.Fuma,
                Alcohol = excluded.Alcohol,
                PesoKg = excluded.PesoKg,
                TallaCm = excluded.TallaCm,
                PresionArterial = excluded.PresionArterial,
                TipoAlopecia = excluded.TipoAlopecia,
                EscalaNorwood = excluded.EscalaNorwood,
                EscalaLudwig = excluded.EscalaLudwig,
                ZonaAfectada = excluded.ZonaAfectada,
                DensidadZonaDonante = excluded.DensidadZonaDonante,
                CalidadCabello = excluded.CalidadCabello,
                ColorCabello = excluded.ColorCabello,
                GrosorCabello = excluded.GrosorCabello,
                Diagnostico = excluded.Diagnostico,
                PlanTratamiento = excluded.PlanTratamiento,
                MedicoResponsable = excluded.MedicoResponsable,
                ObservacionesMedicas = excluded.ObservacionesMedicas;
            """;
        command.Parameters.AddWithValue("$pacienteId", datos.PacienteId);
        command.Parameters.AddWithValue("$grupo", Nv(datos.GrupoSanguineo));
        command.Parameters.AddWithValue("$alergias", Nv(datos.Alergias));
        command.Parameters.AddWithValue("$medicamentos", Nv(datos.MedicamentosActuales));
        command.Parameters.AddWithValue("$antPers", Nv(datos.AntecedentesPersonales));
        command.Parameters.AddWithValue("$antFam", Nv(datos.AntecedentesFamiliares));
        command.Parameters.AddWithValue("$cronicas", Nv(datos.EnfermedadesCronicas));
        command.Parameters.AddWithValue("$cirugias", Nv(datos.CirugiasPrevias));
        command.Parameters.AddWithValue("$fuma", Nv(datos.Fuma));
        command.Parameters.AddWithValue("$alcohol", Nv(datos.Alcohol));
        command.Parameters.AddWithValue("$peso", Nd(datos.PesoKg));
        command.Parameters.AddWithValue("$talla", Nd(datos.TallaCm));
        command.Parameters.AddWithValue("$presion", Nv(datos.PresionArterial));
        command.Parameters.AddWithValue("$alopecia", Nv(datos.TipoAlopecia));
        command.Parameters.AddWithValue("$norwood", Nv(datos.EscalaNorwood));
        command.Parameters.AddWithValue("$ludwig", Nv(datos.EscalaLudwig));
        command.Parameters.AddWithValue("$zona", Nv(datos.ZonaAfectada));
        command.Parameters.AddWithValue("$densidad", Nv(datos.DensidadZonaDonante));
        command.Parameters.AddWithValue("$calidad", Nv(datos.CalidadCabello));
        command.Parameters.AddWithValue("$color", Nv(datos.ColorCabello));
        command.Parameters.AddWithValue("$grosor", Nv(datos.GrosorCabello));
        command.Parameters.AddWithValue("$diagnostico", Nv(datos.Diagnostico));
        command.Parameters.AddWithValue("$plan", Nv(datos.PlanTratamiento));
        command.Parameters.AddWithValue("$medico", Nv(datos.MedicoResponsable));
        command.Parameters.AddWithValue("$obs", Nv(datos.ObservacionesMedicas));
        command.ExecuteNonQuery();
    }

    public List<ContactoEmergencia> GetContactos(int pacienteId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM ContactosEmergencia WHERE PacienteId = $id ORDER BY Nombre;";
        command.Parameters.AddWithValue("$id", pacienteId);
        var lista = new List<ContactoEmergencia>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new ContactoEmergencia
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteId")),
                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                Relacion = GetString(reader, "Relacion"),
                Telefono = GetString(reader, "Telefono"),
                TelefonoAlterno = GetString(reader, "TelefonoAlterno"),
                Email = GetString(reader, "Email"),
                Direccion = GetString(reader, "Direccion")
            });
        }

        return lista;
    }

    public int GuardarContacto(ContactoEmergencia contacto)
    {
        lock (_sync)
        {
            using var connection = Open();
        using var command = connection.CreateCommand();
        if (contacto.Id == 0)
        {
            command.CommandText = """
                INSERT INTO ContactosEmergencia (PacienteId, Nombre, Relacion, Telefono, TelefonoAlterno, Email, Direccion)
                VALUES ($pacienteId, $nombre, $relacion, $telefono, $telefonoAlt, $email, $direccion);
                SELECT last_insert_rowid();
                """;
            BindContacto(command, contacto);
            contacto.Id = Convert.ToInt32(command.ExecuteScalar());
            return contacto.Id;
        }

        command.CommandText = """
            UPDATE ContactosEmergencia SET
                Nombre = $nombre, Relacion = $relacion, Telefono = $telefono,
                TelefonoAlterno = $telefonoAlt, Email = $email, Direccion = $direccion
            WHERE Id = $id;
            """;
        BindContacto(command, contacto);
        command.Parameters.AddWithValue("$id", contacto.Id);
        command.ExecuteNonQuery();
        return contacto.Id;
        }
    }

    public void EliminarContacto(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ContactosEmergencia WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public List<Procedimiento> GetProcedimientos(int pacienteId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM Procedimientos
            WHERE PacienteId = $id
            ORDER BY Fecha DESC;
            """;
        command.Parameters.AddWithValue("$id", pacienteId);
        return ReadProcedimientos(command);
    }

    public Procedimiento? GetProcedimiento(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.*, pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente
            FROM Procedimientos p
            LEFT JOIN Pacientes pac ON pac.Id = p.PacienteId
            WHERE p.Id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id);
        return ReadProcedimientos(command, incluirNombre: true).FirstOrDefault();
    }

    public List<Procedimiento> GetProcedimientosEnRango(DateTime inicio, DateTime fin)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.*, pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente
            FROM Procedimientos p
            INNER JOIN Pacientes pac ON pac.Id = p.PacienteId
            WHERE p.Fecha >= $inicio
              AND p.Fecha < $fin
            ORDER BY p.Fecha;
            """;
        command.Parameters.AddWithValue("$inicio", SqliteDate.FormatValue(inicio));
        command.Parameters.AddWithValue("$fin", SqliteDate.FormatValue(fin));
        return ReadProcedimientos(command, incluirNombre: true);
    }

    public List<Procedimiento> GetProximosProcedimientos(int cantidad = 8)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.*, pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente
            FROM Procedimientos p
            INNER JOIN Pacientes pac ON pac.Id = p.PacienteId
            WHERE p.Estado IN ('Programado', 'Seguimiento')
            ORDER BY p.Fecha
            LIMIT $cantidad;
            """;
        command.Parameters.AddWithValue("$cantidad", cantidad);
        return ReadProcedimientos(command, incluirNombre: true);
    }

    public List<Procedimiento> BuscarProcedimientos(string? texto, int limite = PaginaOperaciones, int offset = 0)
        => BuscarProcedimientosPagina(texto, limite, offset).Items;

    public PaginaConsulta<Procedimiento> BuscarProcedimientosPagina(string? texto, int limite, int offset)
    {
        limite = Math.Clamp(limite, 1, 200);
        offset = Math.Max(0, offset);
        using var connection = Open();
        var q = texto?.Trim() ?? string.Empty;
        using var count = connection.CreateCommand();
        count.CommandText = """
            SELECT COUNT(*)
            FROM Procedimientos p
            INNER JOIN Pacientes pac ON pac.Id = p.PacienteId
            WHERE ($q = ''
               OR p.Tipo LIKE $like
               OR p.Estado LIKE $like
               OR p.Medico LIKE $like
               OR p.Enfermera LIKE $like
               OR p.AreaTratada LIKE $like
               OR pac.Nombres LIKE $like
               OR pac.Apellidos LIKE $like
               OR pac.NumeroExpediente LIKE $like);
            """;
        count.Parameters.AddWithValue("$q", q);
        count.Parameters.AddWithValue("$like", $"%{q}%");
        var total = Convert.ToInt32(count.ExecuteScalar());

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.*, pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente
            FROM Procedimientos p
            INNER JOIN Pacientes pac ON pac.Id = p.PacienteId
            WHERE ($q = ''
               OR p.Tipo LIKE $like
               OR p.Estado LIKE $like
               OR p.Medico LIKE $like
               OR p.Enfermera LIKE $like
               OR p.AreaTratada LIKE $like
               OR pac.Nombres LIKE $like
               OR pac.Apellidos LIKE $like
               OR pac.NumeroExpediente LIKE $like)
            ORDER BY p.Fecha DESC
            LIMIT $limite OFFSET $offset;
            """;
        command.Parameters.AddWithValue("$q", q);
        command.Parameters.AddWithValue("$like", $"%{q}%");
        command.Parameters.AddWithValue("$limite", limite);
        command.Parameters.AddWithValue("$offset", offset);
        return new PaginaConsulta<Procedimiento>
        {
            Items = ReadProcedimientos(command, incluirNombre: true),
            Total = total
        };
    }

    public int GuardarProcedimiento(Procedimiento procedimiento)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        if (procedimiento.Id == 0)
        {
            command.CommandText = """
                INSERT INTO Procedimientos (
                    PacienteId, Fecha, Tipo, AreaTratada, UnidadesFoliculares, NumeroInjertos,
                    Medico, Enfermera, Anestesia, DuracionMinutos, Estado, Resultado, Complicaciones, Observaciones, ChecklistPasos, ZonasOperadas, Costo)
                VALUES (
                    $pacienteId, $fecha, $tipo, $area, $uf, $injertos, $medico, $enfermera, $anestesia,
                    $duracion, $estado, $resultado, $complicaciones, $obs, $checklist, $zonas, $costo);
                SELECT last_insert_rowid();
                """;
            BindProcedimiento(command, procedimiento);
            procedimiento.Id = Convert.ToInt32(command.ExecuteScalar());
            return procedimiento.Id;
        }

        command.CommandText = """
            UPDATE Procedimientos SET
                Fecha = $fecha, Tipo = $tipo, AreaTratada = $area, UnidadesFoliculares = $uf,
                NumeroInjertos = $injertos, Medico = $medico, Enfermera = $enfermera, Anestesia = $anestesia,
                DuracionMinutos = $duracion, Estado = $estado, Resultado = $resultado,
                Complicaciones = $complicaciones, Observaciones = $obs, ChecklistPasos = $checklist, ZonasOperadas = $zonas, Costo = $costo
            WHERE Id = $id;
            """;
        BindProcedimiento(command, procedimiento);
        command.Parameters.AddWithValue("$id", procedimiento.Id);
        command.ExecuteNonQuery();
        return procedimiento.Id;
    }

    public void EliminarProcedimiento(int id)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var sesiones = connection.CreateCommand();
        sesiones.Transaction = transaction;
        sesiones.CommandText = "DELETE FROM SesionesOperacion WHERE ProcedimientoId = $id;";
        sesiones.Parameters.AddWithValue("$id", id);
        sesiones.ExecuteNonQuery();

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM Procedimientos WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public SesionOperacion? GetSesion(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.Id, s.ProcedimientoId, s.FechaInicio, s.DuracionMinutos, s.Estado, s.Notas,
                   p.PacienteId, p.Tipo, p.Medico, p.Estado AS EstadoOperacion,
                   pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente
            FROM SesionesOperacion s
            INNER JOIN Procedimientos p ON p.Id = s.ProcedimientoId
            INNER JOIN Pacientes pac ON pac.Id = p.PacienteId
            WHERE s.Id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? LeerSesion(reader) : null;
    }

    public List<SesionOperacion> GetSesionesEnRango(DateTime inicio, DateTime fin)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.Id, s.ProcedimientoId, s.FechaInicio, s.DuracionMinutos, s.Estado, s.Notas,
                   p.PacienteId, p.Tipo, p.Medico, p.Estado AS EstadoOperacion,
                   pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente
            FROM SesionesOperacion s
            INNER JOIN Procedimientos p ON p.Id = s.ProcedimientoId
            INNER JOIN Pacientes pac ON pac.Id = p.PacienteId
            WHERE s.FechaInicio >= $inicio
              AND s.FechaInicio < $fin
            ORDER BY s.FechaInicio;
            """;
        command.Parameters.AddWithValue("$inicio", SqliteDate.FormatValue(inicio));
        command.Parameters.AddWithValue("$fin", SqliteDate.FormatValue(fin));
        var lista = new List<SesionOperacion>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(LeerSesion(reader));
        }

        return lista;
    }

    public int GuardarSesion(SesionOperacion sesion)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        if (sesion.Id == 0)
        {
            command.CommandText = """
                INSERT INTO SesionesOperacion (ProcedimientoId, FechaInicio, DuracionMinutos, Estado, Notas)
                VALUES ($procedimientoId, $inicio, $duracion, $estado, $notas);
                SELECT last_insert_rowid();
                """;
            BindSesion(command, sesion);
            sesion.Id = Convert.ToInt32(command.ExecuteScalar());
            return sesion.Id;
        }

        command.CommandText = """
            UPDATE SesionesOperacion SET
                FechaInicio = $inicio, DuracionMinutos = $duracion, Estado = $estado, Notas = $notas
            WHERE Id = $id;
            """;
        BindSesion(command, sesion);
        command.Parameters.AddWithValue("$id", sesion.Id);
        command.ExecuteNonQuery();
        return sesion.Id;
    }

    public void EliminarSesion(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM SesionesOperacion WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public string? BloqueoTramo(int procedimientoId, string? medico, string? estado, DateTimeOffset inicio, int duracionMinutos, int excluirSesionId, bool excluirPrincipal)
    {
        if (estado is "Cancelado" or "Cancelada")
        {
            return null;
        }

        var duracion = Math.Max(5, duracionMinutos);
        var fin = inicio.AddMinutes(duracion);
        if (procedimientoId > 0 && HayTramoSolapado(procedimientoId, inicio, fin, excluirSesionId, excluirPrincipal))
        {
            return "Esta operación ya tiene otra sesión en ese horario.";
        }

        if (string.IsNullOrWhiteSpace(medico))
        {
            return null;
        }

        var excluirProcedimientoId = excluirPrincipal ? procedimientoId : 0;
        var ocupado = GetOcupacionMedico(medico, inicio.Date, 0, excluirProcedimientoId, excluirSesionId)
            .Any(o => o.Inicio < fin && o.Fin > inicio);
        return ocupado
            ? $"{medico.Trim()} ya tiene una cita u operación en ese horario. Elija otra hora."
            : null;
    }

    private bool HayTramoSolapado(int procedimientoId, DateTimeOffset inicio, DateTimeOffset fin, int excluirSesionId, bool excluirPrincipal)
    {
        var procedimiento = GetProcedimiento(procedimientoId);
        if (procedimiento is null || procedimiento.Estado is "Cancelado")
        {
            return false;
        }

        if (!excluirPrincipal)
        {
            var duracion = Math.Max(5, procedimiento.DuracionMinutos ?? 60);
            var finPrincipal = procedimiento.Fecha.AddMinutes(duracion);
            if (procedimiento.Fecha < fin && finPrincipal > inicio)
            {
                return true;
            }
        }

        foreach (var sesion in procedimiento.Sesiones)
        {
            if (sesion.Id == excluirSesionId || sesion.Estado is "Cancelada")
            {
                continue;
            }

            if (sesion.FechaInicio < fin && sesion.FechaFin > inicio)
            {
                return true;
            }
        }

        return false;
    }

    public List<Observacion> GetObservaciones(int pacienteId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM Observaciones
            WHERE PacienteId = $id
            ORDER BY Fecha DESC;
            """;
        command.Parameters.AddWithValue("$id", pacienteId);
        var lista = new List<Observacion>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new Observacion
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteId")),
                Fecha = ReadDate(reader, "Fecha") ?? DateTimeOffset.Now,
                Tipo = GetString(reader, "Tipo") ?? "Consulta",
                Titulo = GetString(reader, "Titulo"),
                Contenido = GetString(reader, "Contenido") ?? string.Empty,
                Autor = GetString(reader, "Autor")
            });
        }

        return lista;
    }

    public int GuardarObservacion(Observacion observacion)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        if (observacion.Id == 0)
        {
            command.CommandText = """
                INSERT INTO Observaciones (PacienteId, Fecha, Tipo, Titulo, Contenido, Autor)
                VALUES ($pacienteId, $fecha, $tipo, $titulo, $contenido, $autor);
                SELECT last_insert_rowid();
                """;
            BindObservacion(command, observacion);
            observacion.Id = Convert.ToInt32(command.ExecuteScalar());
            return observacion.Id;
        }

        command.CommandText = """
            UPDATE Observaciones SET
                Fecha = $fecha, Tipo = $tipo, Titulo = $titulo, Contenido = $contenido, Autor = $autor
            WHERE Id = $id;
            """;
        BindObservacion(command, observacion);
        command.Parameters.AddWithValue("$id", observacion.Id);
        command.ExecuteNonQuery();
        return observacion.Id;
    }

    public void EliminarObservacion(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Observaciones WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public List<Cita> GetCitasEnRango(DateTime inicio, DateTime fin)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.*, pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente,
                   pac.NumeroExpediente, pac.Telefono AS TelefonoPaciente
            FROM Citas c
            INNER JOIN Pacientes pac ON pac.Id = c.PacienteId
            WHERE c.FechaInicio >= $inicio
              AND c.FechaInicio < $fin
            ORDER BY c.FechaInicio;
            """;
        command.Parameters.AddWithValue("$inicio", SqliteDate.FormatValue(inicio));
        command.Parameters.AddWithValue("$fin", SqliteDate.FormatValue(fin));
        return ReadCitas(command);
    }

    public List<Cita> GetCitasPaciente(int pacienteId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.*, pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente,
                   pac.NumeroExpediente, pac.Telefono AS TelefonoPaciente
            FROM Citas c
            INNER JOIN Pacientes pac ON pac.Id = c.PacienteId
            WHERE c.PacienteId = $id
            ORDER BY c.FechaInicio DESC;
            """;
        command.Parameters.AddWithValue("$id", pacienteId);
        return ReadCitas(command);
    }

    public List<Cita> GetProximasCitas(int cantidad = 10)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.*, pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente,
                   pac.NumeroExpediente, pac.Telefono AS TelefonoPaciente
            FROM Citas c
            INNER JOIN Pacientes pac ON pac.Id = c.PacienteId
            WHERE c.FechaInicio >= $hoy
              AND c.Estado IN ('Programada', 'Confirmada')
            ORDER BY c.FechaInicio
            LIMIT $cantidad;
            """;
        command.Parameters.AddWithValue("$hoy", SqliteDate.FormatValue(DateTime.Today));
        command.Parameters.AddWithValue("$cantidad", cantidad);
        return ReadCitas(command);
    }

    public Cita? GetCita(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.*, pac.Nombres || ' ' || pac.Apellidos AS NombrePaciente,
                   pac.NumeroExpediente, pac.Telefono AS TelefonoPaciente
            FROM Citas c
            INNER JOIN Pacientes pac ON pac.Id = c.PacienteId
            WHERE c.Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);
        return ReadCitas(command).FirstOrDefault();
    }

    public bool HayPacienteSolapado(Cita cita)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM Citas
            WHERE Id != $id
              AND PacienteId = $pacienteId
              AND Estado NOT IN ('Cancelada', 'No asistió')
              AND FechaInicio < $fin
              AND datetime(replace(substr(FechaInicio, 1, 19), 'T', ' '), '+' || DuracionMinutos || ' minutes') > datetime($inicio);
            """;
        command.Parameters.AddWithValue("$id", cita.Id);
        command.Parameters.AddWithValue("$pacienteId", cita.PacienteId);
        command.Parameters.AddWithValue("$inicio", SqliteDate.FormatValue(cita.FechaInicio));
        command.Parameters.AddWithValue("$fin", SqliteDate.FormatValue(cita.FechaFin));
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool HayMedicoOcupado(Cita cita)
    {
        if (string.IsNullOrWhiteSpace(cita.Medico) || cita.Estado is "Cancelada" or "No asistió")
        {
            return false;
        }

        return GetOcupacionMedico(cita.Medico, cita.FechaInicio.Date, cita.Id)
            .Any(o => o.Inicio < cita.FechaFin && o.Fin > cita.FechaInicio);
    }

    public bool HayMedicoOcupado(Procedimiento procedimiento)
    {
        if (string.IsNullOrWhiteSpace(procedimiento.Medico) || procedimiento.Estado is "Cancelado")
        {
            return false;
        }

        var duracion = Math.Max(5, procedimiento.DuracionMinutos ?? 60);
        var inicio = procedimiento.Fecha;
        var fin = inicio.AddMinutes(duracion);
        return GetOcupacionMedico(procedimiento.Medico, procedimiento.Fecha.Date, 0, procedimiento.Id)
            .Any(o => o.Inicio < fin && o.Fin > inicio);
    }

    public List<(DateTimeOffset Inicio, DateTimeOffset Fin)> GetOcupacionMedico(string? medico, DateTime dia, int excluirCitaId = 0, int excluirProcedimientoId = 0, int excluirSesionId = 0)
    {
        var lista = new List<(DateTimeOffset Inicio, DateTimeOffset Fin)>();
        if (string.IsNullOrWhiteSpace(medico))
        {
            return lista;
        }

        var inicioDia = dia.Date;
        var finDia = inicioDia.AddDays(1);
        foreach (var cita in GetCitasEnRango(inicioDia, finDia))
        {
            if (cita.Id == excluirCitaId || cita.Estado is "Cancelada" or "No asistió" || !MismoMedico(cita.Medico, medico))
            {
                continue;
            }

            lista.Add((cita.FechaInicio, cita.FechaFin));
        }

        foreach (var procedimiento in GetProcedimientosEnRango(inicioDia, finDia))
        {
            if (procedimiento.Id == excluirProcedimientoId || procedimiento.Estado is "Cancelado" || !MismoMedico(procedimiento.Medico, medico))
            {
                continue;
            }

            var duracion = Math.Max(5, procedimiento.DuracionMinutos ?? 60);
            lista.Add((procedimiento.Fecha, procedimiento.Fecha.AddMinutes(duracion)));
        }

        foreach (var sesion in GetSesionesEnRango(inicioDia, finDia))
        {
            if (sesion.Id == excluirSesionId
                || sesion.Estado is "Cancelada"
                || sesion.EstadoOperacion is "Cancelado"
                || !MismoMedico(sesion.Medico, medico))
            {
                continue;
            }

            lista.Add((sesion.FechaInicio, sesion.FechaFin));
        }

        return lista;
    }

    private static bool MismoMedico(string? a, string? b)
        => !string.IsNullOrWhiteSpace(a)
           && !string.IsNullOrWhiteSpace(b)
           && string.Equals(a.Trim(), b.Trim(), StringComparison.CurrentCultureIgnoreCase);

    public int GuardarCita(Cita cita)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        if (cita.Id == 0)
        {
            command.CommandText = """
                INSERT INTO Citas (
                    PacienteId, FechaInicio, DuracionMinutos, Tipo, Estado, Medico, Notas, Temperamento,
                    ComprobanteTipo, ComprobanteNumero, ComprobanteConcepto, ComprobanteMonto,
                    ComprobanteFormaPago, ComprobanteCliente, ComprobanteDocumento, ComprobanteCorreo)
                VALUES (
                    $pacienteId, $inicio, $duracion, $tipo, $estado, $medico, $notas, $temperamento,
                    $compTipo, $compNumero, $compConcepto, $compMonto,
                    $compPago, $compCliente, $compDocumento, $compCorreo);
                SELECT last_insert_rowid();
                """;
            BindCita(command, cita);
            cita.Id = Convert.ToInt32(command.ExecuteScalar());
            return cita.Id;
        }

        command.CommandText = """
            UPDATE Citas SET
                PacienteId = $pacienteId, FechaInicio = $inicio, DuracionMinutos = $duracion,
                Tipo = $tipo, Estado = $estado, Medico = $medico, Notas = $notas, Temperamento = $temperamento,
                ComprobanteTipo = $compTipo, ComprobanteNumero = $compNumero, ComprobanteConcepto = $compConcepto,
                ComprobanteMonto = $compMonto, ComprobanteFormaPago = $compPago, ComprobanteCliente = $compCliente,
                ComprobanteDocumento = $compDocumento, ComprobanteCorreo = $compCorreo
            WHERE Id = $id;
            """;
        BindCita(command, cita);
        command.Parameters.AddWithValue("$id", cita.Id);
        command.ExecuteNonQuery();
        return cita.Id;
    }

    public void GuardarComprobante(Cita cita)
    {
        if (cita.Id <= 0)
        {
            return;
        }

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Citas SET
                ComprobanteTipo = $compTipo, ComprobanteNumero = $compNumero, ComprobanteConcepto = $compConcepto,
                ComprobanteMonto = $compMonto, ComprobanteFormaPago = $compPago, ComprobanteCliente = $compCliente,
                ComprobanteDocumento = $compDocumento, ComprobanteCorreo = $compCorreo
            WHERE Id = $id;
            """;
        BindComprobante(command, cita);
        command.Parameters.AddWithValue("$id", cita.Id);
        command.ExecuteNonQuery();
    }

    public void EliminarCita(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Citas WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void Checkpoint()
    {
        lock (_sync)
        {
            CheckpointCore();
        }
    }

    private void CheckpointCore()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
        command.ExecuteNonQuery();
    }

    private static void AsegurarColumna(SqliteConnection connection, string tabla, string columna, string definicion)
    {
        using var info = connection.CreateCommand();
        info.CommandText = $"PRAGMA table_info({tabla});";
        var existe = false;
        using (var reader = info.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columna, StringComparison.OrdinalIgnoreCase))
                {
                    existe = true;
                    break;
                }
            }
        }

        if (existe)
        {
            return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tabla} ADD COLUMN {columna} {definicion};";
        alter.ExecuteNonQuery();
    }

    private static void NormalizarFechas(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Pacientes SET FechaRegistro = replace(substr(FechaRegistro, 1, 19), 'T', ' ')
            WHERE FechaRegistro LIKE '%T%';
            UPDATE Pacientes SET FechaActualizacion = replace(substr(FechaActualizacion, 1, 19), 'T', ' ')
            WHERE FechaActualizacion LIKE '%T%';
            UPDATE Pacientes SET FechaNacimiento = replace(substr(FechaNacimiento, 1, 19), 'T', ' ')
            WHERE FechaNacimiento LIKE '%T%';
            UPDATE Procedimientos SET Fecha = replace(substr(Fecha, 1, 19), 'T', ' ')
            WHERE Fecha LIKE '%T%';
            UPDATE Observaciones SET Fecha = replace(substr(Fecha, 1, 19), 'T', ' ')
            WHERE Fecha LIKE '%T%';
            UPDATE Citas SET FechaInicio = replace(substr(FechaInicio, 1, 19), 'T', ' ')
            WHERE FechaInicio LIKE '%T%';
            """;
        command.ExecuteNonQuery();
    }

    private static bool EsCorrupcion(Exception ex)
        => ex is SqliteException sqlite
           && sqlite.SqliteErrorCode is 11 or 26;

    private void RepararCorrupcion()
    {
        SqliteConnection.ClearAllPools();
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var dañado = Path.Combine(FolderPath, $"historial-danado-{stamp}.db");
        var limpio = Path.Combine(FolderPath, $"historial-limpio-{stamp}.db");
        if (File.Exists(DbPath))
        {
            File.Copy(DbPath, dañado, overwrite: true);
        }

        var recuperado = false;
        try
        {
            recuperado = IntentarVacuum(limpio);
        }
        catch
        {
            recuperado = false;
        }

        if (!recuperado)
        {
            try
            {
                recuperado = IntentarCopiaSqlite(limpio);
            }
            catch
            {
                recuperado = false;
            }
        }

        if (recuperado && File.Exists(limpio) && new FileInfo(limpio).Length > 0)
        {
            ReemplazarBase(limpio);
        }

        try
        {
            using var connection = Open();
            QuitarIndiceFts(connection);
        }
        catch
        {
            // El esquema se recrea al volver a inicializar.
        }
    }

    private bool IntentarVacuum(string destino)
    {
        TryDelete(destino);
        using var connection = Open();
        using var command = connection.CreateCommand();
        var ruta = destino.Replace("'", "''", StringComparison.Ordinal);
        command.CommandText = $"VACUUM INTO '{ruta}';";
        command.ExecuteNonQuery();
        return File.Exists(destino) && new FileInfo(destino).Length > 0;
    }

    private bool IntentarCopiaSqlite(string destino)
    {
        TryDelete(destino);
        using var source = Open();
        using var dest = new SqliteConnection($"Data Source={destino}");
        dest.Open();
        source.BackupDatabase(dest);
        return File.Exists(destino) && new FileInfo(destino).Length > 0;
    }

    private void ReemplazarBase(string origen)
    {
        SqliteConnection.ClearAllPools();
        TryDelete(DbPath + "-wal");
        TryDelete(DbPath + "-shm");
        TryDelete(DbPath);
        File.Move(origen, DbPath);
    }

    private static void TryDelete(string path)
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
        }
    }

    private static void QuitarIndiceFts(SqliteConnection connection)
    {
        using var drop = connection.CreateCommand();
        drop.CommandText = """
            DROP TRIGGER IF EXISTS PacientesFts_ai;
            DROP TRIGGER IF EXISTS PacientesFts_ad;
            DROP TRIGGER IF EXISTS PacientesFts_au;
            DROP TABLE IF EXISTS PacientesFts;
            """;
        drop.ExecuteNonQuery();
    }

    private static bool AsegurarBusquedaPacientes(SqliteConnection connection)
    {
        try
        {
            using var create = connection.CreateCommand();
            create.CommandText = """
                CREATE VIRTUAL TABLE IF NOT EXISTS PacientesFts USING fts5(
                    Nombres, Apellidos, NumeroExpediente, NumeroDocumento, Telefono, Email,
                    content='Pacientes', content_rowid='Id',
                    tokenize='unicode61 remove_diacritics 2');
                CREATE TRIGGER IF NOT EXISTS PacientesFts_ai AFTER INSERT ON Pacientes BEGIN
                    INSERT INTO PacientesFts(rowid, Nombres, Apellidos, NumeroExpediente, NumeroDocumento, Telefono, Email)
                    VALUES (new.Id, new.Nombres, new.Apellidos, new.NumeroExpediente, new.NumeroDocumento, new.Telefono, new.Email);
                END;
                CREATE TRIGGER IF NOT EXISTS PacientesFts_ad AFTER DELETE ON Pacientes BEGIN
                    INSERT INTO PacientesFts(PacientesFts, rowid, Nombres, Apellidos, NumeroExpediente, NumeroDocumento, Telefono, Email)
                    VALUES ('delete', old.Id, old.Nombres, old.Apellidos, old.NumeroExpediente, old.NumeroDocumento, old.Telefono, old.Email);
                END;
                CREATE TRIGGER IF NOT EXISTS PacientesFts_au AFTER UPDATE ON Pacientes BEGIN
                    INSERT INTO PacientesFts(PacientesFts, rowid, Nombres, Apellidos, NumeroExpediente, NumeroDocumento, Telefono, Email)
                    VALUES ('delete', old.Id, old.Nombres, old.Apellidos, old.NumeroExpediente, old.NumeroDocumento, old.Telefono, old.Email);
                    INSERT INTO PacientesFts(rowid, Nombres, Apellidos, NumeroExpediente, NumeroDocumento, Telefono, Email)
                    VALUES (new.Id, new.Nombres, new.Apellidos, new.NumeroExpediente, new.NumeroDocumento, new.Telefono, new.Email);
                END;
                """;
            create.ExecuteNonQuery();

            using var fts = connection.CreateCommand();
            fts.CommandText = "SELECT COUNT(*) FROM PacientesFts;";
            var enIndice = Convert.ToInt32(fts.ExecuteScalar());
            using var pacientes = connection.CreateCommand();
            pacientes.CommandText = "SELECT COUNT(*) FROM Pacientes;";
            var total = Convert.ToInt32(pacientes.ExecuteScalar());
            if (enIndice != total)
            {
                using var rebuild = connection.CreateCommand();
                rebuild.CommandText = "INSERT INTO PacientesFts(PacientesFts) VALUES('rebuild');";
                rebuild.ExecuteNonQuery();
            }

            using var integrity = connection.CreateCommand();
            integrity.CommandText = "INSERT INTO PacientesFts(PacientesFts) VALUES('integrity-check');";
            integrity.ExecuteNonQuery();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? ConsultaFts(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var partes = texto.Trim().Split([' ', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var terminos = new List<string>();
        foreach (var parte in partes)
        {
            var limpio = new string(parte.Where(c => char.IsLetterOrDigit(c) || c is '-' or '.' or '@').ToArray());
            if (limpio.Length > 0)
            {
                terminos.Add(limpio + "*");
            }
        }

        return terminos.Count == 0 ? null : string.Join(" AND ", terminos);
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection($"Data Source={DbPath};Pooling=False");
        connection.Open();
        EjecutarPragma(connection, "PRAGMA foreign_keys = ON;");
        EjecutarPragma(connection, "PRAGMA busy_timeout = 8000;");
        EjecutarPragma(connection, "PRAGMA journal_mode = DELETE;");
        EjecutarPragma(connection, "PRAGMA synchronous = FULL;");
        EjecutarPragma(connection, "PRAGMA temp_store = MEMORY;");
        EjecutarPragma(connection, "PRAGMA cache_size = -20000;");
        EjecutarPragma(connection, "PRAGMA mmap_size = 0;");
        return connection;
    }

    private static void EjecutarPragma(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static List<Paciente> ReadPacientes(SqliteCommand command)
    {
        var lista = new List<Paciente>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                lista.Add(new Paciente
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    NumeroExpediente = reader.GetString(reader.GetOrdinal("NumeroExpediente")),
                    Nombres = reader.GetString(reader.GetOrdinal("Nombres")),
                    Apellidos = reader.GetString(reader.GetOrdinal("Apellidos")),
                    TipoDocumento = GetString(reader, "TipoDocumento"),
                    NumeroDocumento = GetString(reader, "NumeroDocumento"),
                    FechaNacimiento = ReadDate(reader, "FechaNacimiento"),
                    Sexo = GetString(reader, "Sexo"),
                    EstadoCivil = GetString(reader, "EstadoCivil"),
                    Nacionalidad = GetString(reader, "Nacionalidad"),
                    Ocupacion = GetString(reader, "Ocupacion"),
                    Direccion = GetString(reader, "Direccion"),
                    Distrito = GetString(reader, "Distrito"),
                    Ciudad = GetString(reader, "Ciudad"),
                    Telefono = GetString(reader, "Telefono"),
                    TelefonoAlterno = GetString(reader, "TelefonoAlterno"),
                    Email = GetString(reader, "Email"),
                    FotoRuta = GetString(reader, "FotoRuta"),
                    FechaRegistro = ReadDate(reader, "FechaRegistro") ?? DateTimeOffset.Now,
                    FechaActualizacion = ReadDate(reader, "FechaActualizacion"),
                    Activo = reader.GetInt32(reader.GetOrdinal("Activo")) == 1,
                    NotasGenerales = GetString(reader, "NotasGenerales")
                });
            }
            catch
            {
                // Un expediente con datos dañados no debe impedir cargar el resto.
            }
        }

        return lista;
    }

    private static List<Procedimiento> ReadProcedimientos(SqliteCommand command, bool incluirNombre = false)
    {
        var lista = new List<Procedimiento>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                lista.Add(new Procedimiento
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteId")),
                    Fecha = ReadDate(reader, "Fecha") ?? DateTimeOffset.Now,
                    Tipo = reader.GetString(reader.GetOrdinal("Tipo")),
                    AreaTratada = GetString(reader, "AreaTratada"),
                    UnidadesFoliculares = GetInt(reader, "UnidadesFoliculares"),
                    NumeroInjertos = GetInt(reader, "NumeroInjertos"),
                    Medico = GetString(reader, "Medico"),
                    Enfermera = HasColumn(reader, "Enfermera") ? GetString(reader, "Enfermera") : null,
                    Anestesia = GetString(reader, "Anestesia"),
                    DuracionMinutos = GetInt(reader, "DuracionMinutos"),
                    Estado = GetString(reader, "Estado") ?? "Programado",
                    Resultado = GetString(reader, "Resultado"),
                    Complicaciones = GetString(reader, "Complicaciones"),
                    Observaciones = GetString(reader, "Observaciones"),
                    ChecklistPasos = HasColumn(reader, "ChecklistPasos") ? GetString(reader, "ChecklistPasos") : null,
                    ZonasOperadas = HasColumn(reader, "ZonasOperadas") ? GetString(reader, "ZonasOperadas") : null,
                    Costo = GetDouble(reader, "Costo"),
                    NombrePaciente = incluirNombre ? GetString(reader, "NombrePaciente") : null
                });
            }
        }

        AdjuntarSesiones(command.Connection, lista);
        return lista;
    }

    private static void AdjuntarSesiones(SqliteConnection? connection, List<Procedimiento> lista)
    {
        if (connection is null || lista.Count == 0)
        {
            return;
        }

        var ids = lista.Select(p => p.Id).Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT Id, ProcedimientoId, FechaInicio, DuracionMinutos, Estado, Notas
            FROM SesionesOperacion
            WHERE ProcedimientoId IN ({string.Join(",", ids)})
            ORDER BY FechaInicio;
            """;
        var porProcedimiento = new Dictionary<int, List<SesionOperacion>>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var sesion = LeerSesion(reader);
            if (!porProcedimiento.TryGetValue(sesion.ProcedimientoId, out var grupo))
            {
                grupo = [];
                porProcedimiento[sesion.ProcedimientoId] = grupo;
            }

            grupo.Add(sesion);
        }

        foreach (var procedimiento in lista)
        {
            if (porProcedimiento.TryGetValue(procedimiento.Id, out var sesiones))
            {
                procedimiento.Sesiones = sesiones;
            }
        }
    }

    private static SesionOperacion LeerSesion(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        ProcedimientoId = reader.GetInt32(reader.GetOrdinal("ProcedimientoId")),
        FechaInicio = ReadDate(reader, "FechaInicio") ?? DateTimeOffset.Now,
        DuracionMinutos = Math.Max(5, reader.GetInt32(reader.GetOrdinal("DuracionMinutos"))),
        Estado = GetString(reader, "Estado") ?? "Programada",
        Notas = GetString(reader, "Notas"),
        PacienteId = HasColumn(reader, "PacienteId") && !reader.IsDBNull(reader.GetOrdinal("PacienteId"))
            ? reader.GetInt32(reader.GetOrdinal("PacienteId"))
            : 0,
        Tipo = HasColumn(reader, "Tipo") ? GetString(reader, "Tipo") ?? string.Empty : string.Empty,
        Medico = HasColumn(reader, "Medico") ? GetString(reader, "Medico") : null,
        NombrePaciente = HasColumn(reader, "NombrePaciente") ? GetString(reader, "NombrePaciente") : null,
        EstadoOperacion = HasColumn(reader, "EstadoOperacion")
            ? GetString(reader, "EstadoOperacion") ?? "Programado"
            : "Programado"
    };

    private static void BindSesion(SqliteCommand command, SesionOperacion sesion)
    {
        command.Parameters.AddWithValue("$procedimientoId", sesion.ProcedimientoId);
        command.Parameters.AddWithValue("$inicio", SqliteDate.FormatValue(sesion.FechaInicio));
        command.Parameters.AddWithValue("$duracion", Math.Max(5, sesion.DuracionMinutos));
        command.Parameters.AddWithValue("$estado", string.IsNullOrWhiteSpace(sesion.Estado) ? "Programada" : sesion.Estado.Trim());
        command.Parameters.AddWithValue("$notas", Nv(sesion.Notas));
    }

    private static DatosMedicos MapDatosMedicos(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteId")),
        GrupoSanguineo = GetString(reader, "GrupoSanguineo"),
        Alergias = GetString(reader, "Alergias"),
        MedicamentosActuales = GetString(reader, "MedicamentosActuales"),
        AntecedentesPersonales = GetString(reader, "AntecedentesPersonales"),
        AntecedentesFamiliares = GetString(reader, "AntecedentesFamiliares"),
        EnfermedadesCronicas = GetString(reader, "EnfermedadesCronicas"),
        CirugiasPrevias = GetString(reader, "CirugiasPrevias"),
        Fuma = GetString(reader, "Fuma"),
        Alcohol = GetString(reader, "Alcohol"),
        PesoKg = GetDouble(reader, "PesoKg"),
        TallaCm = GetDouble(reader, "TallaCm"),
        PresionArterial = GetString(reader, "PresionArterial"),
        TipoAlopecia = GetString(reader, "TipoAlopecia"),
        EscalaNorwood = GetString(reader, "EscalaNorwood"),
        EscalaLudwig = GetString(reader, "EscalaLudwig"),
        ZonaAfectada = GetString(reader, "ZonaAfectada"),
        DensidadZonaDonante = GetString(reader, "DensidadZonaDonante"),
        CalidadCabello = GetString(reader, "CalidadCabello"),
        ColorCabello = GetString(reader, "ColorCabello"),
        GrosorCabello = GetString(reader, "GrosorCabello"),
        Diagnostico = GetString(reader, "Diagnostico"),
        PlanTratamiento = GetString(reader, "PlanTratamiento"),
        MedicoResponsable = GetString(reader, "MedicoResponsable"),
        ObservacionesMedicas = GetString(reader, "ObservacionesMedicas")
    };

    private static void BindPaciente(SqliteCommand command, Paciente paciente)
    {
        command.Parameters.AddWithValue("$numero", paciente.NumeroExpediente);
        command.Parameters.AddWithValue("$nombres", (paciente.Nombres ?? string.Empty).Trim());
        command.Parameters.AddWithValue("$apellidos", (paciente.Apellidos ?? string.Empty).Trim());
        command.Parameters.AddWithValue("$tipoDoc", Nv(paciente.TipoDocumento));
        command.Parameters.AddWithValue("$numDoc", Nv(paciente.NumeroDocumento));
        command.Parameters.AddWithValue("$nacimiento", Nd(paciente.FechaNacimiento));
        command.Parameters.AddWithValue("$sexo", Nv(paciente.Sexo));
        command.Parameters.AddWithValue("$estadoCivil", Nv(paciente.EstadoCivil));
        command.Parameters.AddWithValue("$nacionalidad", Nv(paciente.Nacionalidad));
        command.Parameters.AddWithValue("$ocupacion", Nv(paciente.Ocupacion));
        command.Parameters.AddWithValue("$direccion", Nv(paciente.Direccion));
        command.Parameters.AddWithValue("$distrito", Nv(paciente.Distrito));
        command.Parameters.AddWithValue("$ciudad", Nv(paciente.Ciudad));
        command.Parameters.AddWithValue("$telefono", Nv(paciente.Telefono));
        command.Parameters.AddWithValue("$telefonoAlt", Nv(paciente.TelefonoAlterno));
        command.Parameters.AddWithValue("$email", Nv(paciente.Email));
        command.Parameters.AddWithValue("$foto", Nv(paciente.FotoRuta));
        command.Parameters.AddWithValue("$registro", SqliteDate.FormatValue(paciente.FechaRegistro));
        command.Parameters.AddWithValue("$actualizacion", Nd(paciente.FechaActualizacion));
        command.Parameters.AddWithValue("$activo", paciente.Activo ? 1 : 0);
        command.Parameters.AddWithValue("$notas", Nv(paciente.NotasGenerales));
    }

    private static void BindContacto(SqliteCommand command, ContactoEmergencia contacto)
    {
        command.Parameters.AddWithValue("$pacienteId", contacto.PacienteId);
        command.Parameters.AddWithValue("$nombre", (contacto.Nombre ?? string.Empty).Trim());
        command.Parameters.AddWithValue("$relacion", Nv(contacto.Relacion));
        command.Parameters.AddWithValue("$telefono", Nv(contacto.Telefono));
        command.Parameters.AddWithValue("$telefonoAlt", Nv(contacto.TelefonoAlterno));
        command.Parameters.AddWithValue("$email", Nv(contacto.Email));
        command.Parameters.AddWithValue("$direccion", Nv(contacto.Direccion));
    }

    private static void BindProcedimiento(SqliteCommand command, Procedimiento procedimiento)
    {
        command.Parameters.AddWithValue("$pacienteId", procedimiento.PacienteId);
        command.Parameters.AddWithValue("$fecha", SqliteDate.FormatValue(procedimiento.Fecha));
        command.Parameters.AddWithValue("$tipo", procedimiento.Tipo);
        command.Parameters.AddWithValue("$area", Nv(procedimiento.AreaTratada));
        command.Parameters.AddWithValue("$uf", Ni(procedimiento.UnidadesFoliculares));
        command.Parameters.AddWithValue("$injertos", Ni(procedimiento.NumeroInjertos));
        command.Parameters.AddWithValue("$medico", Nv(procedimiento.Medico));
        command.Parameters.AddWithValue("$enfermera", Nv(procedimiento.Enfermera));
        command.Parameters.AddWithValue("$anestesia", Nv(procedimiento.Anestesia));
        command.Parameters.AddWithValue("$duracion", Ni(procedimiento.DuracionMinutos));
        command.Parameters.AddWithValue("$estado", Nv(procedimiento.Estado));
        command.Parameters.AddWithValue("$resultado", Nv(procedimiento.Resultado));
        command.Parameters.AddWithValue("$complicaciones", Nv(procedimiento.Complicaciones));
        command.Parameters.AddWithValue("$obs", Nv(procedimiento.Observaciones));
        command.Parameters.AddWithValue("$checklist", Nv(procedimiento.ChecklistPasos));
        command.Parameters.AddWithValue("$zonas", Nv(procedimiento.ZonasOperadas));
        command.Parameters.AddWithValue("$costo", Nd(procedimiento.Costo));
    }

    private static void BindObservacion(SqliteCommand command, Observacion observacion)
    {
        command.Parameters.AddWithValue("$pacienteId", observacion.PacienteId);
        command.Parameters.AddWithValue("$fecha", SqliteDate.FormatValue(observacion.Fecha));
        command.Parameters.AddWithValue("$tipo", Nv(observacion.Tipo));
        command.Parameters.AddWithValue("$titulo", Nv(observacion.Titulo));
        command.Parameters.AddWithValue("$contenido", observacion.Contenido ?? string.Empty);
        command.Parameters.AddWithValue("$autor", Nv(observacion.Autor));
    }

    private static void BindCita(SqliteCommand command, Cita cita)
    {
        command.Parameters.AddWithValue("$pacienteId", cita.PacienteId);
        command.Parameters.AddWithValue("$inicio", SqliteDate.FormatValue(cita.FechaInicio));
        command.Parameters.AddWithValue("$duracion", Math.Max(5, cita.DuracionMinutos));
        command.Parameters.AddWithValue("$tipo", cita.Tipo);
        command.Parameters.AddWithValue("$estado", Nv(cita.Estado));
        command.Parameters.AddWithValue("$medico", Nv(cita.Medico));
        command.Parameters.AddWithValue("$notas", Nv(cita.Notas));
        command.Parameters.AddWithValue("$temperamento", string.IsNullOrWhiteSpace(cita.Temperamento) ? "Amable" : cita.Temperamento.Trim());
        BindComprobante(command, cita);
    }

    private static void BindComprobante(SqliteCommand command, Cita cita)
    {
        command.Parameters.AddWithValue("$compTipo", Nv(cita.ComprobanteTipo));
        command.Parameters.AddWithValue("$compNumero", Nv(cita.ComprobanteNumero));
        command.Parameters.AddWithValue("$compConcepto", Nv(cita.ComprobanteConcepto));
        command.Parameters.AddWithValue("$compMonto", Nd(cita.ComprobanteMonto));
        command.Parameters.AddWithValue("$compPago", Nv(cita.ComprobanteFormaPago));
        command.Parameters.AddWithValue("$compCliente", Nv(cita.ComprobanteCliente));
        command.Parameters.AddWithValue("$compDocumento", Nv(cita.ComprobanteDocumento));
        command.Parameters.AddWithValue("$compCorreo", Nv(cita.ComprobanteCorreo));
    }

    private static List<Cita> ReadCitas(SqliteCommand command)
    {
        var lista = new List<Cita>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new Cita
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteId")),
                FechaInicio = ReadDate(reader, "FechaInicio") ?? DateTimeOffset.Now,
                DuracionMinutos = reader.GetInt32(reader.GetOrdinal("DuracionMinutos")),
                Tipo = reader.GetString(reader.GetOrdinal("Tipo")),
                Estado = GetString(reader, "Estado") ?? "Programada",
                Medico = GetString(reader, "Medico"),
                Notas = GetString(reader, "Notas"),
                Temperamento = HasColumn(reader, "Temperamento")
                    ? GetString(reader, "Temperamento") ?? "Amable"
                    : "Amable",
                NombrePaciente = GetString(reader, "NombrePaciente"),
                NumeroExpediente = GetString(reader, "NumeroExpediente"),
                TelefonoPaciente = GetString(reader, "TelefonoPaciente"),
                ComprobanteTipo = HasColumn(reader, "ComprobanteTipo") ? GetString(reader, "ComprobanteTipo") : null,
                ComprobanteNumero = HasColumn(reader, "ComprobanteNumero") ? GetString(reader, "ComprobanteNumero") : null,
                ComprobanteConcepto = HasColumn(reader, "ComprobanteConcepto") ? GetString(reader, "ComprobanteConcepto") : null,
                ComprobanteMonto = HasColumn(reader, "ComprobanteMonto") ? GetDouble(reader, "ComprobanteMonto") : null,
                ComprobanteFormaPago = HasColumn(reader, "ComprobanteFormaPago") ? GetString(reader, "ComprobanteFormaPago") : null,
                ComprobanteCliente = HasColumn(reader, "ComprobanteCliente") ? GetString(reader, "ComprobanteCliente") : null,
                ComprobanteDocumento = HasColumn(reader, "ComprobanteDocumento") ? GetString(reader, "ComprobanteDocumento") : null,
                ComprobanteCorreo = HasColumn(reader, "ComprobanteCorreo") ? GetString(reader, "ComprobanteCorreo") : null
            });
        }

        return lista;
    }

    private static void BindDoctor(SqliteCommand command, Doctor doctor)
    {
        command.Parameters.AddWithValue("$titulo", Nv(doctor.Titulo));
        command.Parameters.AddWithValue("$nombres", (doctor.Nombres ?? string.Empty).Trim());
        command.Parameters.AddWithValue("$apellidos", (doctor.Apellidos ?? string.Empty).Trim());
        command.Parameters.AddWithValue("$especialidad", Nv(doctor.Especialidad));
        command.Parameters.AddWithValue("$cmp", Nv(doctor.Cmp));
        command.Parameters.AddWithValue("$telefono", Nv(doctor.Telefono));
        command.Parameters.AddWithValue("$email", Nv(doctor.Email));
        command.Parameters.AddWithValue("$activo", doctor.Activo ? 1 : 0);
        command.Parameters.AddWithValue("$predeterminado", doctor.EsPredeterminado ? 1 : 0);
    }

    private static Doctor MapDoctor(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Titulo = GetString(reader, "Titulo") ?? "Dr.",
        Nombres = reader.GetString(reader.GetOrdinal("Nombres")),
        Apellidos = reader.GetString(reader.GetOrdinal("Apellidos")),
        Especialidad = GetString(reader, "Especialidad"),
        Cmp = GetString(reader, "Cmp"),
        Telefono = GetString(reader, "Telefono"),
        Email = GetString(reader, "Email"),
        Activo = reader.GetInt32(reader.GetOrdinal("Activo")) == 1,
        EsPredeterminado = reader.GetInt32(reader.GetOrdinal("EsPredeterminado")) == 1
    };

    private static void BindEnfermera(SqliteCommand command, Enfermera enfermera)
    {
        command.Parameters.AddWithValue("$titulo", Nv(enfermera.Titulo));
        command.Parameters.AddWithValue("$nombres", (enfermera.Nombres ?? string.Empty).Trim());
        command.Parameters.AddWithValue("$apellidos", (enfermera.Apellidos ?? string.Empty).Trim());
        command.Parameters.AddWithValue("$especialidad", Nv(enfermera.Especialidad));
        command.Parameters.AddWithValue("$cep", Nv(enfermera.Cep));
        command.Parameters.AddWithValue("$telefono", Nv(enfermera.Telefono));
        command.Parameters.AddWithValue("$email", Nv(enfermera.Email));
        command.Parameters.AddWithValue("$activo", enfermera.Activo ? 1 : 0);
        command.Parameters.AddWithValue("$predeterminada", enfermera.EsPredeterminada ? 1 : 0);
    }

    private static Enfermera MapEnfermera(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Titulo = GetString(reader, "Titulo") ?? "Enf.",
        Nombres = reader.GetString(reader.GetOrdinal("Nombres")),
        Apellidos = reader.GetString(reader.GetOrdinal("Apellidos")),
        Especialidad = GetString(reader, "Especialidad"),
        Cep = GetString(reader, "Cep"),
        Telefono = GetString(reader, "Telefono"),
        Email = GetString(reader, "Email"),
        Activo = reader.GetInt32(reader.GetOrdinal("Activo")) == 1,
        EsPredeterminada = reader.GetInt32(reader.GetOrdinal("EsPredeterminada")) == 1
    };

    private static object Nv(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    private static object Nd(DateTimeOffset? value) => SqliteDate.FormatOrDbNull(value);
    private static object Nd(double? value) => value is null ? DBNull.Value : value.Value;
    private static object Ni(int? value) => value is null ? DBNull.Value : value.Value;

    private static bool HasColumn(SqliteDataReader reader, string column)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetString(SqliteDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        return reader.IsDBNull(i) ? null : reader.GetString(i);
    }

    private static DateTimeOffset? ReadDate(SqliteDataReader reader, string column)
    {
        var raw = GetString(reader, column);
        return SqliteDate.Parse(raw);
    }

    private static int? GetInt(SqliteDataReader reader, string column)
    {
        try
        {
            var i = reader.GetOrdinal(column);
            if (reader.IsDBNull(i))
            {
                return null;
            }

            return Convert.ToInt32(reader.GetValue(i), CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static double? GetDouble(SqliteDataReader reader, string column)
    {
        try
        {
            var i = reader.GetOrdinal(column);
            if (reader.IsDBNull(i))
            {
                return null;
            }

            return Convert.ToDouble(reader.GetValue(i), CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
