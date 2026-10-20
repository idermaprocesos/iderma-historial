namespace IdermaCapilar_Historial.Models;

public sealed record PasoOperacionDef(string Id, string Fase, string Titulo, string Instruccion);

public static class ChecklistOperacion
{
    public const char Separador = '|';

    public static IReadOnlyList<PasoOperacionDef> ParaTipo(string? tipo)
    {
        var nombre = (tipo ?? string.Empty).Trim();
        if (PorNombre().TryGetValue(nombre, out var exacto))
        {
            return exacto;
        }

        if (EsTrasplante(nombre))
        {
            return Trasplante;
        }

        if (EsLiposuccion(nombre))
        {
            return Liposuccion;
        }

        if (EsCorporalNoInvasivo(nombre))
        {
            return Criolipolisis;
        }

        if (EsControl(nombre))
        {
            return Control;
        }

        return TratamientoConsultorio;
    }

    public static HashSet<string> IdsHechos(string? guardado)
    {
        if (string.IsNullOrWhiteSpace(guardado))
        {
            return [];
        }

        return guardado
            .Split(Separador, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static string Serializar(IEnumerable<string> idsHechos)
    {
        var set = idsHechos
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return set.Count == 0 ? string.Empty : string.Join(Separador, set);
    }

    public static string Resumen(string? tipo, string? guardado)
    {
        var pasos = ParaTipo(tipo);
        if (pasos.Count == 0)
        {
            return "Sin pasos";
        }

        var hechos = IdsHechos(guardado);
        var hechosEnLista = pasos.Count(p => hechos.Contains(p.Id));
        return $"{hechosEnLista}/{pasos.Count} pasos";
    }

    public static bool EsTrasplante(string tipo) =>
        tipo.Contains("Trasplante", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("ARTAS", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("FUE", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("FUT", StringComparison.OrdinalIgnoreCase);

    public static bool EsLiposuccion(string tipo) =>
        tipo.Contains("Liposucc", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Lipoescult", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Lipoláser", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Lipolaser", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("lipo ", StringComparison.OrdinalIgnoreCase);

    public static bool EsCorporalNoInvasivo(string tipo) =>
        tipo.Contains("Criolipólisis", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Criolipolisis", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Cavitación", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Cavitacion", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Radiofrecuencia", StringComparison.OrdinalIgnoreCase);

    public static bool EsControl(string tipo) =>
        tipo.Contains("Control", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Consulta", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Seguimiento", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("postoperatorio", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<PasoOperacionDef> Trasplante => TrasplanteFue;

    public static IReadOnlyList<PasoOperacionDef> TrasplanteFue { get; } =
    [
        new("fue01", "Ingreso",
            "Identidad, alertas y signos",
            "Confirmar paciente, alergias, ayuno, medicación y PA. No iniciar si hay alerta sin valoración."),
        new("fue02", "Ingreso",
            "Consentimiento y plan FUE",
            "Consentimiento firmado. Acordar extracción FUE, zonas receptoras y expectativa."),
        new("fue03", "Ingreso",
            "Fotos y diseño aprobado",
            "Fotos pre (frente, perfil, vértex, donante). Marcar línea y donante; el paciente lo ve y acepta."),
        new("fue04", "Preparación",
            "Sala, punches e implanters",
            "Mesa lista, punches, implanters, solución fría de injertos y hoja de conteo."),
        new("fue05", "Preparación",
            "Recorte, campo y anestesia donante",
            "Rasurar donante, antisepsia, infiltrar y probar sensibilidad antes de extraer."),
        new("fue06", "Cirugía",
            "Extracción FUE y conteo",
            "Extraer de forma ordenada, irrigar, controlar sangrado y anotar UF en tiempo real."),
        new("fue07", "Cirugía",
            "Procesar y clasificar injertos",
            "Mantener húmedos y fríos. Limpiar, separar 1-2-3 pelos y registrar viables."),
        new("fue08", "Cirugía",
            "Receptora: anestesia e incisiones",
            "Antisepsia, infiltrar, slits con ángulo y densidad del diseño. Línea solo con 1 pelo."),
        new("fue09", "Cirugía",
            "Implantar y cuadrar recuento",
            "Colocar por zonas, hidratar, reponer pop-up y cuadrar extraídos vs. implantados."),
        new("fue10", "Alta",
            "Cura, fotos y medicación",
            "Limpiar, vendaje de donante, fotos inmediatas y entregar analgésico/antibiótico/spray."),
        new("fue11", "Alta",
            "Indicaciones, alarma y control",
            "Explicar 72 h, signos de alarma, primer lavado y agendar control. Egreso estable con acompañante.")
    ];

    public static IReadOnlyList<PasoOperacionDef> TrasplanteArtas { get; } =
    [
        new("art01", "Ingreso",
            "Identidad, alertas y signos",
            "Confirmar paciente, alergias, ayuno, medicación y PA. No iniciar si hay alerta sin valoración."),
        new("art02", "Ingreso",
            "Consentimiento y plan ARTAS",
            "Consentimiento firmado. Explicar extracción robótica, zonas y expectativa."),
        new("art03", "Ingreso",
            "Fotos, diseño y calibración",
            "Fotos pre. Marcar línea y donante. Verificar que el paciente acepte el diseño."),
        new("art04", "Preparación",
            "Robot, mesa y conservación",
            "ARTAS listo, punches/implanters de apoyo, solución fría y hoja de conteo."),
        new("art05", "Preparación",
            "Recorte, campo y anestesia donante",
            "Rasurar donante al largo del robot, antisepsia, infiltrar y probar sensibilidad."),
        new("art06", "Cirugía",
            "Extracción ARTAS y conteo",
            "Supervisar el robot, irrigar, controlar sangrado y anotar UF en tiempo real."),
        new("art07", "Cirugía",
            "Procesar y clasificar injertos",
            "Mantener húmedos y fríos. Limpiar, separar 1-2-3 pelos y registrar viables."),
        new("art08", "Cirugía",
            "Receptora: anestesia e incisiones",
            "Antisepsia, infiltrar, slits con ángulo y densidad del diseño."),
        new("art09", "Cirugía",
            "Implantar y cuadrar recuento",
            "Colocar por zonas, hidratar y cuadrar extraídos vs. implantados."),
        new("art10", "Alta",
            "Cura, fotos y medicación",
            "Limpiar, vendaje de donante, fotos inmediatas y medicación de alta."),
        new("art11", "Alta",
            "Indicaciones y control",
            "Explicar 72 h, signos de alarma, primer lavado y agendar control.")
    ];

    public static IReadOnlyList<PasoOperacionDef> TrasplanteFut { get; } =
    [
        new("fut01", "Ingreso",
            "Identidad, alertas y signos",
            "Confirmar paciente, alergias, ayuno, medicación y PA. No iniciar si hay alerta sin valoración."),
        new("fut02", "Ingreso",
            "Consentimiento y plan FUT",
            "Consentimiento firmado. Explicar tira donante, cicatriz y zonas receptoras."),
        new("fut03", "Ingreso",
            "Fotos y diseño aprobado",
            "Fotos pre. Marcar línea receptora y elíptica donante; el paciente lo ve y acepta."),
        new("fut04", "Preparación",
            "Sala, bisturí y conservación",
            "Mesa lista, material de tira, sutura, solución fría y hoja de conteo."),
        new("fut05", "Preparación",
            "Campo y anestesia donante",
            "Antisepsia, infiltrar tira y probar sensibilidad antes de la incisión."),
        new("fut06", "Cirugía",
            "Toma de tira y cierre donante",
            "Disecar la tira, hemostasia y cierre. Vigilar tensión de la cicatriz."),
        new("fut07", "Cirugía",
            "Disecar y clasificar injertos",
            "Mantener húmedos y fríos. Separar UF 1-2-3 pelos y registrar viables."),
        new("fut08", "Cirugía",
            "Receptora: anestesia e incisiones",
            "Antisepsia, infiltrar, slits con ángulo y densidad del diseño."),
        new("fut09", "Cirugía",
            "Implantar y cuadrar recuento",
            "Colocar por zonas, hidratar y cuadrar disecados vs. implantados."),
        new("fut10", "Alta",
            "Cura, fotos y medicación",
            "Limpiar, vendaje de donante, fotos inmediatas y medicación de alta."),
        new("fut11", "Alta",
            "Indicaciones y retiro de puntos",
            "Explicar cuidados de cicatriz, alarma y fecha de retiro de puntos.")
    ];

    public static IReadOnlyList<PasoOperacionDef> PrpCapilar { get; } =
    [
        new("prp01", "Preparación",
            "Identidad y contraindicaciones",
            "Confirmar paciente, anticoagulantes, infección, anemia y consentimiento del PRP."),
        new("prp02", "Preparación",
            "Fotos y toma de sangre",
            "Fotos de control. Extraer sangre, centrifugar y separar el plasma rico."),
        new("prp03", "Aplicación",
            "Antisepsia y mapa de inyección",
            "Limpiar cuero cabelludo. Infiltrar PRP según mapa y dosis acordada."),
        new("prp04", "Aplicación",
            "Vigilar reacción",
            "Detener si hay mareo, sangrado excesivo, urticaria o dolor desproporcionado."),
        new("prp05", "Cierre",
            "Indicaciones y siguiente sesión",
            "No lavar el día. Anotar ml, zonas, tolerancia y agendar la siguiente sesión.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Mesoterapia { get; } =
    [
        new("mes01", "Preparación",
            "Identidad y contraindicaciones",
            "Confirmar paciente, alergias al vial, infección activa y consentimiento."),
        new("mes02", "Preparación",
            "Fotos, lote y mesa",
            "Fotos de control. Verificar lote/caducidad del cóctel y armar la mesa."),
        new("mes03", "Aplicación",
            "Antisepsia e infiltración",
            "Limpiar cuero cabelludo. Inyectar mesoterapia según mapa, profundidad y dosis."),
        new("mes04", "Aplicación",
            "Vigilar reacción",
            "Detener si hay mareo, habón excesivo, urticaria o dolor desproporcionado."),
        new("mes05", "Cierre",
            "Indicaciones y siguiente sesión",
            "Limpieza suave. Anotar producto, ml, zonas y agendar control.")
    ];

    public static IReadOnlyList<PasoOperacionDef> LaserCapilar { get; } =
    [
        new("las01", "Preparación",
            "Identidad y contraindicaciones",
            "Confirmar paciente, fotosensibilidad, embarazo, infección y consentimiento."),
        new("las02", "Preparación",
            "Fotos y parámetros",
            "Fotos de control. Verificar equipo, gafas y parámetros del protocolo."),
        new("las03", "Aplicación",
            "Sesión de láser",
            "Aplicar en cuero cabelludo según mapa. No sobretratar una misma zona."),
        new("las04", "Aplicación",
            "Vigilar reacción",
            "Detener si hay eritema intenso, quemadura o dolor desproporcionado."),
        new("las05", "Cierre",
            "Indicaciones y siguiente sesión",
            "Fotoprotección si aplica. Anotar parámetros, tiempo y agendar control.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Bioestimulacion { get; } =
    [
        new("bio01", "Preparación",
            "Identidad y contraindicaciones",
            "Confirmar paciente, alergias, infección activa y consentimiento."),
        new("bio02", "Preparación",
            "Fotos y material",
            "Fotos de control. Verificar lote del bioestimulante y armar la mesa."),
        new("bio03", "Aplicación",
            "Antisepsia y el procedimiento",
            "Limpiar. Aplicar según técnica (inyección o dispositivo) y mapa."),
        new("bio04", "Aplicación",
            "Vigilar reacción",
            "Detener si hay mareo, urticaria, sangrado excesivo o dolor desproporcionado."),
        new("bio05", "Cierre",
            "Indicaciones y siguiente sesión",
            "Cuidados del día. Anotar producto, zonas, tolerancia y próximo control.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Micropigmentacion { get; } =
    [
        new("mic01", "Preparación",
            "Identidad y consentimiento estético",
            "Confirmar paciente, alergias a pigmento, infección y expectativa del resultado."),
        new("mic02", "Preparación",
            "Fotos, diseño y color",
            "Fotos pre. Probar color, marcar densidad y obtener visto bueno del diseño."),
        new("mic03", "Aplicación",
            "Antisepsia y pigmentación",
            "Campo limpio. Pigmentar por zonas siguiendo el diseño aprobado."),
        new("mic04", "Aplicación",
            "Vigilar reacción",
            "Detener si hay sangrado excesivo, reacción al pigmento o dolor desproporcionado."),
        new("mic05", "Cierre",
            "Cura e indicaciones",
            "Limpiar, explicar costras y retoque. Anotar color, zonas y fecha de revisión.")
    ];

    public static IReadOnlyList<PasoOperacionDef> TratamientoConsultorio { get; } =
    [
        new("px01", "Preparación",
            "Identidad y contraindicaciones",
            "Confirmar paciente, alergias, infección activa, embarazo y consentimiento del acto."),
        new("px02", "Preparación",
            "Fotos y material",
            "Fotos de control. Verificar lote/caducidad, armar mesa y preparar la zona."),
        new("px03", "Aplicación",
            "Antisepsia y el procedimiento",
            "Antiséptico, anestesia o frío si aplica. Ejecutar según mapa y dosis."),
        new("px04", "Aplicación",
            "Vigilar reacción",
            "Detener si hay mareo, sangrado excesivo, urticaria o dolor desproporcionado."),
        new("px05", "Cierre",
            "Indicaciones y siguiente sesión",
            "Limpieza suave, qué no hacer hoy, agendar control y anotar zonas/tolerancia.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Consulta { get; } =
    [
        new("co01", "Evaluación",
            "Motivo y antecedentes",
            "Registrar motivo, alergias, cirugías previas y expectativa del paciente."),
        new("co02", "Evaluación",
            "Exploración y fotos",
            "Explorar zona (capilar o corporal). Fotos y, si aplica, marcar mapa de zonas."),
        new("co03", "Plan",
            "Opciones y riesgos",
            "Explicar técnicas, tiempos, riesgos y costo. Dejar plan por escrito."),
        new("co04", "Plan",
            "Siguiente paso",
            "Agendar procedimiento o control. Entregar indicaciones preoperatorias si procede.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Control { get; } =
    [
        new("sx01", "Evaluación",
            "Motivo y síntomas",
            "Confirmar operación o sesión previa. Dolor, fiebre, sueño y si cumplió indicaciones."),
        new("sx02", "Evaluación",
            "Inspección y fotos",
            "Revisar zona tratada (edema, costra, infección, cicatriz, hematomas). Fotos de control."),
        new("sx03", "Conducta",
            "Lavado, cura o ajuste",
            "Hacer el gesto del día (lavado, vendaje, faja, puntos) y reforzar la técnica en casa."),
        new("sx04", "Conducta",
            "Plan y próximo control",
            "Ajustar medicación, explicar tiempos de resultado y agendar la siguiente visita.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Liposuccion { get; } =
    [
        new("lp01", "Ingreso",
            "Identidad, ayuno y alertas",
            "Confirmar paciente, alergias, ayuno, anticoagulantes, IMC y PA. No iniciar si hay alerta."),
        new("lp02", "Ingreso",
            "Consentimiento y mapa corporal",
            "Consentimiento firmado. Marcar zonas en frente y dorso con el paciente de pie."),
        new("lp03", "Ingreso",
            "Fotos preoperatorias",
            "Fotos frente, perfil y dorso. El paciente ve las marcas y acepta el plan."),
        new("lp04", "Preparación",
            "Sala, cánulas y tumescencia",
            "Cánulas, lipoaspirador, solución tumescente, compresión y hoja de volúmenes."),
        new("lp05", "Preparación",
            "Campo y anestesia",
            "Antisepsia, infiltrar tumescencia y esperar el tiempo de vasoconstricción."),
        new("lp06", "Cirugía",
            "Aspiración por zonas",
            "Lipoaspirar según mapa. Anotar ml por zona. Vigilar simetría y sangrado."),
        new("lp07", "Cirugía",
            "Balance de líquidos",
            "Registrar infiltrado vs. aspirado. Alertar si hay inestabilidad hemodinámica."),
        new("lp08", "Cierre",
            "Curas y faja",
            "Cerrar puertos, fotos inmediatas y colocar faja o prenda de compresión."),
        new("lp09", "Alta",
            "Indicaciones y alarma",
            "Explicar faja, drenaje, deambulación, signos de alarma y control. Egreso con acompañante.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Lipoescultura { get; } =
    [
        new("le01", "Ingreso",
            "Identidad, ayuno y alertas",
            "Confirmar paciente, alergias, ayuno, anticoagulantes, IMC y PA."),
        new("le02", "Ingreso",
            "Consentimiento y diseño",
            "Consentimiento. Marcar extracción y, si aplica, injerto graso (glúteo u otra zona)."),
        new("le03", "Ingreso",
            "Fotos preoperatorias",
            "Fotos frente, perfil y dorso con marcas. El paciente acepta el diseño."),
        new("le04", "Preparación",
            "Sala, cánulas y conservación de grasa",
            "Cánulas, sistema de aspiración, decantación/lavado de grasa y prenda de compresión."),
        new("le05", "Preparación",
            "Campo y tumescencia",
            "Antisepsia, infiltrar y esperar vasoconstricción."),
        new("le06", "Cirugía",
            "Lipoaspiración y modelado",
            "Aspirar y definir contorno según mapa. Anotar ml extraídos por zona."),
        new("le07", "Cirugía",
            "Injerto graso si aplica",
            "Procesar grasa y colocar en zonas de aumento. Evitar sobrecorrección."),
        new("le08", "Cierre",
            "Curas y faja",
            "Cerrar puertos, fotos inmediatas y colocar faja."),
        new("le09", "Alta",
            "Indicaciones y alarma",
            "Faja, no sentarse sobre injerto si aplica, signos de alarma y control.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Lipolaser { get; } =
    [
        new("ll01", "Ingreso",
            "Identidad y contraindicaciones",
            "Confirmar paciente, alergias, ayuno si aplica, marcapasos/fotosensibilidad y PA."),
        new("ll02", "Ingreso",
            "Consentimiento y mapa",
            "Consentimiento. Marcar zonas de lipoláser en frente y/o dorso."),
        new("ll03", "Ingreso",
            "Fotos pre",
            "Fotos frente, perfil y dorso. El paciente acepta las marcas."),
        new("ll04", "Preparación",
            "Equipo láser y protección",
            "Verificar láser, cánulas, gafas, tumescencia y prenda de compresión."),
        new("ll05", "Preparación",
            "Campo y anestesia",
            "Antisepsia, infiltrar y probar sensibilidad."),
        new("ll06", "Cirugía",
            "Emisión láser por zonas",
            "Aplicar energía según protocolo. Vigilar temperatura cutánea y simetría."),
        new("ll07", "Cirugía",
            "Aspiración si el protocolo lo incluye",
            "Aspirar emulsión si corresponde. Anotar energía y ml por zona."),
        new("ll08", "Cierre",
            "Curas y faja",
            "Cerrar puertos, fotos y faja."),
        new("ll09", "Alta",
            "Indicaciones y control",
            "Explicar edema, faja, alarma (quemadura, fiebre) y próximo control.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Criolipolisis { get; } =
    [
        new("cr01", "Preparación",
            "Identidad y contraindicaciones",
            "Confirmar paciente, frío extremo, hernia, crioglobulinemia, embarazo y consentimiento."),
        new("cr02", "Preparación",
            "Fotos y marcado",
            "Fotos frente y dorso. Marcar rollos o pliegues a tratar en el mapa corporal."),
        new("cr03", "Aplicación",
            "Colocar aplicador",
            "Gel/membrana, succión y tiempo del protocolo. Verificar sellado y confort."),
        new("cr04", "Aplicación",
            "Vigilar durante el ciclo",
            "Detener si hay dolor intenso, palidez extrema o fallo del equipo."),
        new("cr05", "Cierre",
            "Masaje e indicaciones",
            "Masaje post si el protocolo lo indica. Explicar adormecimiento, tiempos y control.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Cavitacion { get; } =
    [
        new("cv01", "Preparación",
            "Identidad y contraindicaciones",
            "Confirmar paciente, embarazo, marcapasos, infección, hernia y consentimiento."),
        new("cv02", "Preparación",
            "Fotos y mapa",
            "Fotos. Marcar abdomen, flancos u otras zonas en frente o dorso."),
        new("cv03", "Aplicación",
            "Gel y cabezal",
            "Aplicar gel. Pasar ultrasonido según minutos y potencia del protocolo."),
        new("cv04", "Aplicación",
            "Vigilar reacción",
            "Detener si hay dolor óseo, quemazón o mareo."),
        new("cv05", "Cierre",
            "Hidratación e indicaciones",
            "Limpiar. Indicar agua, actividad y próxima sesión. Anotar zonas y minutos.")
    ];

    public static IReadOnlyList<PasoOperacionDef> Radiofrecuencia { get; } =
    [
        new("rf01", "Preparación",
            "Identidad y contraindicaciones",
            "Confirmar paciente, metal, marcapasos, embarazo, infección y consentimiento."),
        new("rf02", "Preparación",
            "Fotos y mapa",
            "Fotos. Marcar zonas de flacidez o grasa en frente o dorso."),
        new("rf03", "Aplicación",
            "Parámetros y pases",
            "Gel o aceite. Aplicar RF hasta temperatura objetivo sin quemar."),
        new("rf04", "Aplicación",
            "Vigilar piel",
            "Detener si hay eritema intenso, ampolla o dolor desproporcionado."),
        new("rf05", "Cierre",
            "Indicaciones y control",
            "Hidratación y fotoprotección si aplica. Anotar zonas, temperatura y próxima sesión.")
    ];

    private static readonly Lazy<Dictionary<string, IReadOnlyList<PasoOperacionDef>>> PorNombreLazy = new(() =>
        new Dictionary<string, IReadOnlyList<PasoOperacionDef>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Trasplante ARTAS"] = TrasplanteArtas,
            ["Trasplante FUE"] = TrasplanteFue,
            ["Trasplante FUT"] = TrasplanteFut,
            ["PRP capilar"] = PrpCapilar,
            ["Mesoterapia"] = Mesoterapia,
            ["Láser capilar"] = LaserCapilar,
            ["Bioestimulación"] = Bioestimulacion,
            ["Micropigmentación"] = Micropigmentacion,
            ["Consulta"] = Consulta,
            ["Control postoperatorio"] = Control,
            ["Liposucción"] = Liposuccion,
            ["Lipoescultura"] = Lipoescultura,
            ["Lipoláser"] = Lipolaser,
            ["Criolipólisis"] = Criolipolisis,
            ["Cavitación"] = Cavitacion,
            ["Radiofrecuencia corporal"] = Radiofrecuencia,
            ["Otro"] = TratamientoConsultorio
        });

    private static Dictionary<string, IReadOnlyList<PasoOperacionDef>> PorNombre() => PorNombreLazy.Value;
}
