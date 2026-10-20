namespace IdermaCapilar_Historial.Models;

public sealed class DashboardStats
{
    public int TotalPacientes { get; init; }
    public int ExpedientesActivos { get; init; }
    public int ProcedimientosMes { get; init; }
    public int TotalOperaciones { get; init; }
    public int SeguimientosPendientes { get; init; }
    public int CitasHoy { get; init; }
    public int CitasProximas { get; init; }
}

public static class Catalogos
{
    public static IReadOnlyList<string> TiposDocumento { get; } =
        ["DNI", "Carné de extranjería", "Pasaporte", "RUC", "Otro"];

    public static IReadOnlyList<string> Sexos { get; } =
        ["Femenino", "Masculino", "Otro"];

    public static IReadOnlyList<string> EstadosCiviles { get; } =
        ["Soltero/a", "Casado/a", "Conviviente", "Divorciado/a", "Viudo/a"];

    public static IReadOnlyList<string> GruposSanguineos { get; } =
        ["O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-"];

    public static IReadOnlyList<string> Habitos { get; } =
        ["No", "Ocasional", "Frecuente"];

    public static IReadOnlyList<string> TiposAlopecia { get; } =
        ["Androgenética", "Areata", "Efluvio telógeno", "Cicatricial", "Traumática / tracción", "Difusa", "Otra"];

    public static IReadOnlyList<string> EscalasNorwood { get; } =
        ["I", "II", "IIA", "III", "III Vertex", "IV", "V", "VI", "VII"];

    public static IReadOnlyList<string> EscalasLudwig { get; } =
        ["I", "II", "III"];

    public static IReadOnlyList<string> TiposProcedimiento =>
        OpcionesTipoProcedimiento.Select(t => t.Nombre).ToList();

    public static IReadOnlyList<string> EstadosProcedimiento { get; } =
        ["Programado", "Realizado", "Seguimiento", "Cancelado"];

    public static IReadOnlyList<string> TiposObservacion { get; } =
        ["Consulta", "Preoperatorio", "Intraoperatorio", "Postoperatorio", "Seguimiento", "Administrativo"];

    public static IReadOnlyList<string> RelacionesEmergencia { get; } =
        ["Cónyuge", "Padre/Madre", "Hijo/a", "Hermano/a", "Amigo/a", "Otro"];

    public static IReadOnlyList<string> TitulosMedicos { get; } =
        ["Dr.", "Dra.", "Mg.", "Lic."];

    public static IReadOnlyList<string> TitulosEnfermeria { get; } =
        ["Enf.", "Lic.", "Tec.", "Mg."];

    public static IReadOnlyList<string> EspecialidadesEnfermeria { get; } =
        ["Enfermería", "Instrumentación quirúrgica", "Anestesia", "Cuidados intensivos", "Otra"];

    public static IReadOnlyList<string> EspecialidadesMedicas { get; } =
    [
        "Dermatología",
        "Cirugía capilar",
        "Medicina estética",
        "Tricología",
        "Anestesiología",
        "Cirugía plástica",
        "Medicina general",
        "Otra"
    ];

    public static IReadOnlyList<TipoCitaOpcion> MotivosCitaPredeterminados { get; } =
    [
        new("Consulta", 92, 128, 126),
        new("Evaluación capilar", 98, 124, 148),
        new("Control postoperatorio", 110, 140, 118),
        new("PRP / mesoterapia", 124, 114, 148),
        new("Láser capilar", 168, 128, 108),
        new("Trasplante", 176, 158, 122),
        new("Retiro de vendaje", 104, 138, 136),
        new("Lavado", 120, 146, 128),
        new("Administrativa", 122, 128, 136),
        new("Otro", 154, 140, 118)
    ];

    public static IReadOnlyList<TipoCitaOpcion> MotivosOperacionPredeterminados { get; } =
    [
        new("Consulta", 86, 122, 132),
        new("Trasplante ARTAS", 156, 118, 96),
        new("Trasplante FUE", 168, 128, 92),
        new("Trasplante FUT", 148, 116, 84),
        new("PRP capilar", 118, 108, 140),
        new("Mesoterapia", 128, 112, 144),
        new("Láser capilar", 158, 118, 98),
        new("Bioestimulación", 104, 132, 148),
        new("Micropigmentación", 132, 108, 128),
        new("Control postoperatorio", 108, 136, 112),
        new("Liposucción", 150, 96, 92),
        new("Lipoescultura", 168, 110, 98),
        new("Lipoláser", 176, 122, 88),
        new("Criolipólisis", 92, 132, 148),
        new("Cavitación", 88, 140, 136),
        new("Radiofrecuencia corporal", 118, 124, 150),
        new("Otro", 140, 120, 102)
    ];

    public static IReadOnlyList<string> MotivosOperacionCorporal { get; } =
    [
        "Liposucción",
        "Lipoescultura",
        "Lipoláser",
        "Criolipólisis",
        "Cavitación",
        "Radiofrecuencia corporal"
    ];

    public static string DescripcionProcedimiento(string? tipo)
    {
        var nombre = tipo?.Trim() ?? string.Empty;
        if (DescripcionesProcedimiento.TryGetValue(nombre, out var texto))
        {
            return texto;
        }

        if (ChecklistOperacion.EsTrasplante(nombre))
        {
            return DescripcionesProcedimiento["Trasplante FUE"];
        }

        if (ChecklistOperacion.EsLiposuccion(nombre))
        {
            return DescripcionesProcedimiento["Liposucción"];
        }

        if (nombre.Contains("Criolipólisis", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("Criolipolisis", StringComparison.OrdinalIgnoreCase))
        {
            return DescripcionesProcedimiento["Criolipólisis"];
        }

        if (nombre.Contains("Cavitación", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("Cavitacion", StringComparison.OrdinalIgnoreCase))
        {
            return DescripcionesProcedimiento["Cavitación"];
        }

        if (nombre.Contains("Radiofrecuencia", StringComparison.OrdinalIgnoreCase))
        {
            return DescripcionesProcedimiento["Radiofrecuencia corporal"];
        }

        if (ChecklistOperacion.EsControl(nombre))
        {
            return DescripcionesProcedimiento["Control postoperatorio"];
        }

        return "Procedimiento clínico documentado en el expediente: se registra la técnica, las zonas tratadas, la tolerancia y el plan de seguimiento.";
    }

    private static readonly Dictionary<string, string> DescripcionesProcedimiento = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Consulta"] =
            "Evaluación médica para diagnosticar, explicar opciones (capilares o de contorno corporal), riesgos y un plan. No es una cirugía: sirve para decidir el procedimiento y dejar indicaciones.",
        ["Trasplante ARTAS"] =
            "Trasplante capilar con extracción robótica (ARTAS) de unidades foliculares de la zona donante y su implante en las zonas de alopecia, para restaurar densidad con un diseño acordado.",
        ["Trasplante FUE"] =
            "Trasplante capilar FUE: extracción folículo a folículo de la zona donante e implante en la zona receptora. Permite redistribuir cabello propio sin una tira de piel.",
        ["Trasplante FUT"] =
            "Trasplante capilar FUT: se toma una tira de cuero cabelludo donante, se disecan las unidades foliculares y se implantan en la zona de alopecia. Deja una cicatriz lineal en donante.",
        ["PRP capilar"] =
            "Plasma rico en plaquetas obtenido de la sangre del paciente e infiltrado en el cuero cabelludo para estimular el folículo y apoyar el crecimiento o el mantenimiento del cabello.",
        ["Mesoterapia"] =
            "Infiltración de un cóctel vitamínico o medicamentos en el cuero cabelludo, en microdosis, para nutrir el folículo y complementar el tratamiento de la alopecia.",
        ["Láser capilar"] =
            "Aplicación de láser de baja intensidad en el cuero cabelludo para estimular el folículo, mejorar el ambiente del cuero cabelludo y apoyar otros tratamientos capilares.",
        ["Bioestimulación"] =
            "Tratamiento que usa productos o dispositivos para reactivar el folículo y mejorar la calidad del cabello o de la piel de la zona tratada, según el protocolo de la sesión.",
        ["Micropigmentación"] =
            "Pigmentación cosmética del cuero cabelludo (o zona acordada) para simular densidad capilar. No implanta cabello: dibuja puntos de color con un diseño previo.",
        ["Control postoperatorio"] =
            "Revisión después de una cirugía o sesión: se valora cicatrización, inflamación, cumplimiento de indicaciones, fotos de control y se ajusta el tratamiento o la próxima visita.",
        ["Liposucción"] =
            "Cirugía de reducción de grasa: se infiltra solución tumescente y se aspira el tejido adiposo de las zonas marcadas (abdomen, flancos, muslos u otras) para reducir volumen localizado.",
        ["Lipoescultura"] =
            "Cirugía de contorno: combina liposucción para quitar grasa y, si el plan lo incluye, reinyectar grasa en otra zona (por ejemplo glúteos) para modelar la silueta.",
        ["Lipoláser"] =
            "Reducción de grasa con energía láser bajo la piel para emulsionar el adipocito; según el protocolo puede aspirarse después. Busca contorno con menos agresión que una liposucción clásica.",
        ["Criolipólisis"] =
            "Tratamiento no quirúrgico que congela de forma controlada el pliegue de grasa (aplicador con succión). Las células grasas se eliminan de forma gradual en las semanas siguientes.",
        ["Cavitación"] =
            "Ultrasonido estético sobre la grasa localizada para fragmentar adipocitos de forma no invasiva. Suele requerir varias sesiones y no sustituye a una liposucción.",
        ["Radiofrecuencia corporal"] =
            "Energía de radiofrecuencia sobre la piel para calentar el tejido, mejorar flacidez y apoyar la reducción de grasa superficial. Es un procedimiento de consultorio, no una cirugía.",
        ["Otro"] =
            "Procedimiento distinto a los protocolos habituales. En observaciones del médico debe quedar claro qué se hizo, en qué zonas y cuál es el seguimiento."
    };

    public static IReadOnlyList<TipoCitaOpcion> OpcionesTipoCita =>
        Helpers.AppSettings.Current.MotivosCita.Select(t => t.ToOpcion()).ToList();

    public static IReadOnlyList<TipoCitaOpcion> OpcionesTipoProcedimiento =>
        Helpers.AppSettings.Current.MotivosOperacion.Select(t => t.ToOpcion()).ToList();

    public static IReadOnlyList<TipoCitaOpcion> LeyendaCalendario =>
        OpcionesTipoCita.Concat(OpcionesTipoProcedimiento.Select(t => new TipoCitaOpcion($"Op. {t.Nombre}", t.R, t.G, t.B))).ToList();

    public static IReadOnlyList<string> TiposCita =>
        OpcionesTipoCita.Select(t => t.Nombre).ToList();

    public static TipoCitaOpcion TipoCitaPorNombre(string? tipo)
    {
        try
        {
            var lista = OpcionesTipoCita;
            return lista.FirstOrDefault(t => t.Nombre == tipo)
                ?? lista.FirstOrDefault()
                ?? new TipoCitaOpcion(tipo ?? "Otro", 154, 140, 118);
        }
        catch
        {
            return new TipoCitaOpcion(tipo ?? "Otro", 154, 140, 118);
        }
    }

    public static TipoCitaOpcion TipoProcedimientoPorNombre(string? tipo)
    {
        var lista = OpcionesTipoProcedimiento;
        return lista.FirstOrDefault(t => t.Nombre == tipo)
            ?? lista.FirstOrDefault()
            ?? new TipoCitaOpcion(tipo ?? "Otro", 140, 120, 102);
    }

    public static IReadOnlyList<string> EstadosCita { get; } =
        ["Programada", "Confirmada", "Completada", "Cancelada", "No asistió"];

    public static IReadOnlyList<string> TemperamentosPaciente { get; } =
        ["Amable", "Intenso", "Ansioso", "Reservado", "Impaciente", "Colaborador", "Exigente", "Tranquilo", "Otro"];

    public static IReadOnlyList<string> VistasCalendario { get; } =
        ["Día", "Semana", "Mes", "Agenda"];

    public static string? Alinear(IReadOnlyList<string> opciones, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || opciones.Count == 0)
        {
            return null;
        }

        var recorte = valor.Trim();
        return opciones.FirstOrDefault(o => string.Equals(o, recorte, StringComparison.OrdinalIgnoreCase));
    }
}
