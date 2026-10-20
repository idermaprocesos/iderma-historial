using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Models;
using Windows.Storage;
using Windows.System;
using WinRT.Interop;

namespace IdermaCapilar_Historial.Helpers;

public static class ImpresionService
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");

    public static async Task ImprimirDatosPacienteAsync(Paciente paciente)
    {
        var html = Documento(
            "Datos del paciente",
            CuerpoDatosPaciente(paciente),
            $"{AppSettings.Current.NombreClinica} · Dermatología láser y trasplante capilar",
            "Ficha de identificación del expediente: datos personales y contactos de emergencia para ubicar al paciente y avisar en caso necesario.");
        await AbrirAsync(html, $"datos-{paciente.NumeroExpediente}");
    }

    public static async Task ImprimirHistorialClinicoAsync(int pacienteId)
    {
        var paciente = AppDatabase.Instance.GetPaciente(pacienteId)
            ?? throw new InvalidOperationException("No se encontró el expediente.");
        var html = Documento(
            "Historial clínico",
            CuerpoHistorial(paciente),
            $"{AppSettings.Current.NombreClinica} · Dermatología láser y trasplante capilar",
            "Este historial sirve para documentar y consultar, en un solo expediente, la identidad del paciente, antecedentes médicos, evaluación capilar, operaciones capilares y de reducción de grasa (con mapas de cabeza, frente y dorso) y el seguimiento en consulta.");
        await AbrirAsync(html, $"historial-{paciente.NumeroExpediente}");
    }

    public static async Task ImprimirCitasSemanaAsync(DateTime fechaReferencia)
    {
        var lunes = InicioSemana(fechaReferencia.Date);
        var citas = AppDatabase.Instance.GetCitasEnRango(lunes, lunes.AddDays(7));
        var titulo = $"Relación de citas · {lunes.ToString("d MMM", Cultura)} – {lunes.AddDays(6).ToString("d MMM yyyy", Cultura)}";
        var html = Documento(titulo, CuerpoSemana(lunes, citas));
        await AbrirAsync(html, $"citas-semana-{lunes:yyyyMMdd}");
    }

    public static DateTime InicioSemana(DateTime fecha)
    {
        var delta = ((int)fecha.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return fecha.Date.AddDays(-delta);
    }

    private static string CuerpoDatosPaciente(Paciente paciente)
    {
        var sb = new StringBuilder();
        EncabezadoPaciente(sb, paciente);
        sb.AppendLine("<h2>Datos personales</h2>");
        sb.AppendLine("<table>");
        Fila(sb, "Expediente", paciente.NumeroExpediente);
        Fila(sb, "Documento", Texto(paciente.TipoDocumento, paciente.NumeroDocumento));
        Fila(sb, "Fecha de nacimiento", paciente.FechaNacimiento?.ToString("dd/MM/yyyy"));
        Fila(sb, "Edad", paciente.EdadTexto);
        Fila(sb, "Sexo", paciente.Sexo);
        Fila(sb, "Estado civil", paciente.EstadoCivil);
        Fila(sb, "Nacionalidad", paciente.Nacionalidad);
        Fila(sb, "Ocupación", paciente.Ocupacion);
        Fila(sb, "Teléfono", TelefonoFormato.FormatearONull(paciente.Telefono));
        Fila(sb, "Teléfono alterno", TelefonoFormato.FormatearONull(paciente.TelefonoAlterno));
        Fila(sb, "Correo", paciente.Email);
        Fila(sb, "Dirección", Texto(paciente.Direccion, paciente.Distrito, paciente.Ciudad));
        Fila(sb, "Estado del expediente", paciente.Activo ? "Activo" : "Inactivo");
        Fila(sb, "Registro", paciente.FechaRegistro.ToString("dd/MM/yyyy"));
        Fila(sb, "Notas generales", paciente.NotasGenerales);
        sb.AppendLine("</table>");

        var contactos = AppDatabase.Instance.GetContactos(paciente.Id);
        sb.AppendLine("<h2>Contactos de emergencia</h2>");
        if (contactos.Count == 0)
        {
            sb.AppendLine("<p>Sin contactos registrados.</p>");
        }
        else
        {
            sb.AppendLine("<table><tr><th>Nombre</th><th>Relación</th><th>Teléfono</th><th>Correo</th></tr>");
            foreach (var c in contactos)
            {
                sb.AppendLine($"<tr><td>{E(c.Nombre)}</td><td>{E(c.Relacion)}</td><td>{E(TelefonoFormato.Formatear(c.Telefono))}</td><td>{E(c.Email)}</td></tr>");
            }

            sb.AppendLine("</table>");
        }

        return sb.ToString();
    }

    private static string CuerpoHistorial(Paciente paciente)
    {
        var datos = AppDatabase.Instance.GetDatosMedicos(paciente.Id);
        var procedimientos = AppDatabase.Instance.GetProcedimientos(paciente.Id);
        var observaciones = AppDatabase.Instance.GetObservaciones(paciente.Id);
        var citas = AppDatabase.Instance.GetCitasPaciente(paciente.Id);
        var doctores = AppDatabase.Instance.GetDoctores();
        var enfermeras = AppDatabase.Instance.GetEnfermeras();
        var sb = new StringBuilder();
        sb.Append(CuerpoDatosPaciente(paciente));

        sb.AppendLine("<h2>Datos médicos</h2><table>");
        Fila(sb, "Grupo sanguíneo", datos.GrupoSanguineo);
        Fila(sb, "Peso / talla", datos.PesoKg is double p && datos.TallaCm is double t ? $"{p:0.#} kg · {t:0.#} cm" : null);
        Fila(sb, "IMC", datos.ImcTexto);
        Fila(sb, "Presión arterial", datos.PresionArterial);
        Fila(sb, "Tabaco", datos.Fuma);
        Fila(sb, "Alcohol", datos.Alcohol);
        Fila(sb, "Alergias", datos.Alergias);
        Fila(sb, "Medicamentos actuales", datos.MedicamentosActuales);
        Fila(sb, "Antecedentes personales", datos.AntecedentesPersonales);
        Fila(sb, "Antecedentes familiares", datos.AntecedentesFamiliares);
        Fila(sb, "Enfermedades crónicas", datos.EnfermedadesCronicas);
        Fila(sb, "Cirugías previas", datos.CirugiasPrevias);
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Evaluación capilar</h2><table>");
        Fila(sb, "Tipo de alopecia", datos.TipoAlopecia);
        Fila(sb, "Escala Norwood", datos.EscalaNorwood);
        Fila(sb, "Escala Ludwig", datos.EscalaLudwig);
        Fila(sb, "Zona afectada", datos.ZonaAfectada);
        Fila(sb, "Densidad zona donante", datos.DensidadZonaDonante);
        Fila(sb, "Calidad / color / grosor", Texto(datos.CalidadCabello, datos.ColorCabello, datos.GrosorCabello));
        FichasDoctor(sb, "Médico responsable", datos.MedicoResponsable, doctores);
        Fila(sb, "Diagnóstico", datos.Diagnostico);
        Fila(sb, "Plan de tratamiento", datos.PlanTratamiento);
        Fila(sb, "Observaciones médicas", datos.ObservacionesMedicas);
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Citas</h2>");
        TablaCitas(sb, citas, doctores: doctores);

        var zonasResumen = MapaZonasCabeza.Unir(
            procedimientos
                .Where(p => !string.Equals(p.Estado, "Cancelado", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.ZonasOperadas));
        sb.AppendLine("<h2>Mapa de zonas tratadas</h2>");
        sb.AppendLine("<p class='meta'>Resumen de todas las operaciones del paciente (cabeza, frente y dorso) a lo largo de su tiempo en la clínica.</p>");
        var nombresResumen = MapaZonasCabeza.Nombres(zonasResumen);
        if (!string.IsNullOrWhiteSpace(nombresResumen))
        {
            sb.AppendLine($"<p>Zonas acumuladas: {E(nombresResumen)}</p>");
        }

        sb.AppendLine(MapaZonasCabeza.Html(zonasResumen, todasLasVistas: true));

        sb.AppendLine("<h2 class='operaciones-titulo'>Operaciones y tratamientos</h2>");
        if (procedimientos.Count == 0)
        {
            sb.AppendLine("<p>Sin procedimientos registrados.</p>");
        }
        else
        {
            foreach (var item in procedimientos)
            {
                sb.AppendLine("<section class='operacion-pagina'>");
                sb.AppendLine($"<h3>{E(item.Tipo)} · {E(item.FechaTexto)}</h3>");
                sb.AppendLine($"<p class='proposito'>{E(item.DescripcionProcedimiento)}</p>");
                sb.AppendLine("<table>");
                Fila(sb, "Estado", item.Estado);
                Fila(sb, "Área / zonas", string.IsNullOrWhiteSpace(item.ZonasTexto) ? item.AreaTratada : item.ZonasTexto);
                Fila(sb, "UF / injertos", item.UnidadesFoliculares is int uf ? $"{uf} UF" : item.NumeroInjertos?.ToString());
                FichasDoctor(sb, "Médico", item.Medico, doctores);
                FichasEnfermera(sb, "Enfermera", item.Enfermera, enfermeras);
                Fila(sb, "Anestesia", item.Anestesia);
                if (item.Sesiones.Count > 0)
                {
                    Fila(sb, "Sesiones", string.Join("; ", item.Tramos().Select(t =>
                        $"{t.Titulo}: {t.RangoTexto} ({t.DuracionTexto}" +
                        (t.EsPrincipal ? "" : $", {t.Estado}") +
                        (string.IsNullOrWhiteSpace(t.Notas) ? ")" : $", {t.Notas})"))));
                }
                else
                {
                    Fila(sb, "Duración", item.DuracionMinutos is int d ? $"{d} min" : null);
                }
                Fila(sb, "Resultado", item.Resultado);
                Fila(sb, "Complicaciones", item.Complicaciones);
                Fila(sb, "Costo", item.Costo is double c ? c.ToString("N2") : null);
                sb.AppendLine("</table>");
                sb.AppendLine("<h4>Observaciones del médico</h4>");
                sb.AppendLine(string.IsNullOrWhiteSpace(item.Observaciones)
                    ? "<p>Sin observaciones del médico en esta operación.</p>"
                    : $"<p>{E(item.Observaciones)}</p>");
                sb.AppendLine(MapaZonasCabeza.Html(item.ZonasOperadas));
                ListaChecklist(sb, item);
                sb.AppendLine("</section>");
            }
        }

        sb.AppendLine("<h2>Observaciones clínicas</h2>");
        if (observaciones.Count == 0)
        {
            sb.AppendLine("<p>Sin observaciones.</p>");
        }
        else
        {
            foreach (var obs in observaciones)
            {
                sb.AppendLine($"<h3>{E(obs.Encabezado)}</h3><p class='meta'>{E(obs.Pie)}</p><p>{E(obs.Contenido)}</p>");
            }
        }

        return sb.ToString();
    }

    private static string CuerpoSemana(DateTime lunes, List<Cita> citas)
    {
        var doctores = AppDatabase.Instance.GetDoctores();
        var sb = new StringBuilder();
        sb.AppendLine($"<p class='meta'>Impreso el {DateTime.Now:dd/MM/yyyy HH:mm} · {citas.Count} cita{(citas.Count == 1 ? "" : "s")} en la semana</p>");

        for (var i = 0; i < 7; i++)
        {
            var dia = lunes.AddDays(i);
            var delDia = citas.Where(c => c.FechaInicio.Date == dia).ToList();
            sb.AppendLine($"<h2>{E(Capitalizar(dia.ToString("dddd d 'de' MMMM", Cultura)))}</h2>");
            if (delDia.Count == 0)
            {
                sb.AppendLine("<p class='vacio'>Sin citas.</p>");
                continue;
            }

            TablaCitas(sb, delDia, incluirPaciente: true, doctores: doctores);
        }

        return sb.ToString();
    }

    private static void TablaCitas(StringBuilder sb, IReadOnlyList<Cita> citas, bool incluirPaciente = false, IReadOnlyList<Doctor>? doctores = null)
    {
        if (citas.Count == 0)
        {
            sb.AppendLine("<p>Sin citas.</p>");
            return;
        }

        sb.AppendLine(incluirPaciente ? "<table class='citas con-paciente'>" : "<table class='citas'>");
        if (incluirPaciente)
        {
            sb.AppendLine("<colgroup><col class='hora' /><col class='paciente' /><col class='expediente' /><col class='tipo' /><col class='estado' /><col class='medico' /></colgroup>");
        }
        else
        {
            sb.AppendLine("<colgroup><col class='hora' /><col class='tipo' /><col class='estado' /><col class='medico' /></colgroup>");
        }

        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Hora</th>");
        if (incluirPaciente)
        {
            sb.AppendLine("<th>Paciente</th><th>Expediente</th>");
        }

        sb.AppendLine("<th>Tipo</th><th>Estado</th><th>Médico</th></tr>");
        foreach (var cita in citas)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{E(cita.HorarioTexto)}</td>");
            if (incluirPaciente)
            {
                sb.AppendLine($"<td>{E(cita.NombrePaciente)}</td><td>{E(cita.NumeroExpediente)}</td>");
            }

            sb.AppendLine($"<td>{E(cita.Tipo)}</td><td>{E(cita.Estado)}</td><td>{E(ResumenDoctor(cita.Medico, doctores))}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
    }

    private static void EncabezadoPaciente(StringBuilder sb, Paciente paciente)
    {
        sb.AppendLine($"<p class='nombre-paciente'>{E(paciente.NombreCompleto)}</p>");
        if (!string.IsNullOrWhiteSpace(paciente.Subtitulo))
        {
            sb.AppendLine($"<p class='meta subtitulo-paciente'>{E(paciente.Subtitulo.Replace("  ·  ", " · "))}</p>");
            sb.AppendLine("<p class='meta'>El subtítulo identifica el expediente, la edad, el sexo y el teléfono de contacto.</p>");
        }
    }

    private static string Documento(string titulo, string cuerpo, string? subtitulo = null, string? proposito = null)
    {
        var logo = LogoHtml();
        var bloqueSubtitulo = string.IsNullOrWhiteSpace(subtitulo)
            ? string.Empty
            : $"<p class=\"subtitulo-doc\">{E(subtitulo)}</p>";
        var bloqueProposito = string.IsNullOrWhiteSpace(proposito)
            ? string.Empty
            : $"<p class=\"proposito\">{E(proposito)}</p>";
        return $$"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
            <meta charset="utf-8" />
            <title>{{E(titulo)}}</title>
            <style>
              * { box-sizing: border-box; }
              body {
                font-family: Segoe UI, Arial, sans-serif;
                color: #1b1b1b;
                margin: 12px 16px;
                font-size: 11.5px;
                line-height: 1.2;
              }
              .encabezado {
                text-align: center;
                padding: 0 0 4px;
                margin: 0 0 6px;
                border-bottom: 1.5px solid #C4A35A;
              }
              .encabezado img {
                display: block;
                margin: 0 auto;
                height: 56px;
                width: auto;
                max-width: 180px;
                object-fit: contain;
              }
              .encabezado h1 {
                color: #0E6B6B;
                font-size: 16px;
                margin: 0;
                line-height: 1.1;
              }
              .titulo-doc {
                color: #0A4F4F;
                font-size: 12.5px;
                margin: 0 0 2px;
                text-align: center;
                font-weight: 600;
                line-height: 1.2;
              }
              .subtitulo-doc {
                color: #0E6B6B;
                font-size: 11px;
                margin: 0 0 4px;
                text-align: center;
                line-height: 1.25;
              }
              .proposito {
                color: #555;
                font-size: 10.5px;
                margin: 0 auto 8px;
                max-width: 640px;
                text-align: center;
                line-height: 1.35;
              }
              .subtitulo-paciente { font-size: 11px; color: #333; }
              .nombre-paciente {
                color: #0A4F4F;
                font-size: 18px;
                font-weight: 700;
                text-align: center;
                margin: 0 0 2px;
                line-height: 1.2;
              }
              h2 {
                color: #0A4F4F;
                font-size: 12px;
                border-bottom: 1px solid #c4a35a;
                padding: 0 0 1px;
                margin: 8px 0 3px;
                line-height: 1.15;
              }
              h3 { font-size: 11.5px; margin: 6px 0 2px; line-height: 1.15; }
              .operacion-pagina {
                break-before: page;
                page-break-before: always;
              }
              .operacion-pagina:first-of-type {
                break-before: auto;
                page-break-before: auto;
              }
              h2.operaciones-titulo {
                break-before: page;
                page-break-before: always;
              }
              h4 { font-size: 11px; margin: 6px 0 2px; color: #0A4F4F; }
              .mapa-cabeza {
                margin: 6px 0 10px;
                page-break-inside: avoid;
              }
              .mapa-cabeza-fila {
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 20px;
                flex-wrap: wrap;
              }
              .mapa-cabeza-lienzo {
                position: relative;
                width: 240px;
                height: 240px;
                margin: 0;
                flex: 0 0 auto;
              }
              .mapa-cabeza-lienzo img {
                position: absolute;
                inset: 0;
                width: 100%;
                height: 100%;
                object-fit: contain;
              }
              table.zonas-operadas {
                width: 240px;
                margin: 0;
                text-align: left;
              }
              table.zonas-operadas td.si {
                font-weight: 600;
                color: #0E6B6B;
              }
              p { margin: 2px 0 4px; }
              p.vacio { margin: 2px 0 4px; color: #555; }
              .meta { color: #555; font-size: 10.5px; margin: 0 0 4px; line-height: 1.2; }
              table { width: 100%; border-collapse: collapse; margin: 2px 0 6px; font-size: 10.5px; line-height: 1.15; }
              table.citas { table-layout: fixed; }
              table.citas.con-paciente col.hora { width: 13%; }
              table.citas.con-paciente col.paciente { width: 28%; }
              table.citas.con-paciente col.expediente { width: 14%; }
              table.citas.con-paciente col.tipo { width: 20%; }
              table.citas.con-paciente col.estado { width: 12%; }
              table.citas.con-paciente col.medico { width: 13%; }
              table.citas:not(.con-paciente) col.hora { width: 16%; }
              table.citas:not(.con-paciente) col.tipo { width: 34%; }
              table.citas:not(.con-paciente) col.estado { width: 16%; }
              table.citas:not(.con-paciente) col.medico { width: 34%; }
              table.citas th, table.citas td { overflow: hidden; word-wrap: break-word; }
              th, td { border: 1px solid #d0d0d0; padding: 2px 5px; text-align: left; vertical-align: top; }
              th { background: #E7F3F3; font-weight: 600; }
              @media print {
                button { display: none; }
                body { margin: 0; }
                @page { margin: 10mm 12mm 12mm 12mm; }
              }
            </style>
            </head>
            <body>
            <button onclick="window.print()">Imprimir</button>
            {{logo}}
            <p class="titulo-doc">{{E(titulo)}}</p>
            {{bloqueSubtitulo}}
            {{bloqueProposito}}
            {{cuerpo}}
            <script>window.addEventListener('load', () => setTimeout(() => window.print(), 400));</script>
            </body>
            </html>
            """;
    }

    private static string LogoHtml()
    {
        var uri = LogoDataUri();
        return uri is null
            ? $"<header class='encabezado'><h1>{E(AppSettings.Current.NombreClinica)}</h1></header>"
            : $"<header class='encabezado'><img src='{uri}' alt='Iderma Capilar' /></header>";
    }

    private static string? LogoDataUri()
    {
        foreach (var ruta in RutasLogo())
        {
            if (!File.Exists(ruta))
            {
                continue;
            }

            var bytes = File.ReadAllBytes(ruta);
            if (bytes.Length == 0)
            {
                continue;
            }

            return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
        }

        return null;
    }

    private static IEnumerable<string> RutasLogo()
    {
        var carpeta = Path.Combine(AppContext.BaseDirectory, "img");
        yield return Path.Combine(carpeta, "IDERMA LOGO.jpg");
        if (!Directory.Exists(carpeta))
        {
            yield break;
        }

        foreach (var archivo in Directory.EnumerateFiles(carpeta, "*IDERMA*LOGO*.jpg"))
        {
            yield return archivo;
        }
    }

    public static async Task<string> EmitirComprobanteAsync(Cita cita, Paciente paciente, bool abrir)
    {
        AsignarNumero(cita);
        var html = HtmlComprobante(cita, paciente);
        var ruta = await EscribirHtmlAsync(html, $"comprobante-{cita.ComprobanteNumero}");
        if (cita.Id > 0)
        {
            AppDatabase.Instance.GuardarComprobante(cita);
        }

        if (abrir)
        {
            await AbrirRutaAsync(ruta);
        }

        return ruta;
    }

    public static string AsuntoComprobante(Cita cita)
    {
        var tipo = EsFactura(cita) ? "Factura" : "Boleta de venta";
        var numero = string.IsNullOrWhiteSpace(cita.ComprobanteNumero) ? string.Empty : " " + cita.ComprobanteNumero.Trim();
        return $"{tipo}{numero} — {AppSettings.Current.NombreClinica}";
    }

    public static string CuerpoCorreoComprobante(Cita cita, Paciente paciente)
    {
        var settings = AppSettings.Current;
        var total = (cita.ComprobanteMonto ?? 0).ToString("N2", Cultura);
        var lineas = new[]
        {
            $"Adjuntamos su {(EsFactura(cita) ? "factura" : "boleta de venta")} de {settings.NombreClinica}.",
            string.Empty,
            $"Comprobante: {cita.ComprobanteNumero}",
            $"Cliente: {cita.ComprobanteCliente}",
            $"Concepto: {cita.ComprobanteConcepto}",
            $"Cita: {cita.FechaTexto} {cita.HorarioTexto}",
            string.IsNullOrWhiteSpace(cita.Medico) ? null : $"Médico: {cita.Medico}",
            $"Forma de pago: {cita.ComprobanteFormaPago}",
            $"Total: S/ {total}",
            string.Empty,
            settings.NombreClinica,
            string.IsNullOrWhiteSpace(settings.DireccionClinica) ? null : settings.DireccionClinica.Trim(),
            string.IsNullOrWhiteSpace(settings.TelefonoClinica) ? null : TelefonoFormato.Formatear(settings.TelefonoClinica),
            string.IsNullOrWhiteSpace(settings.CorreoClinica) ? null : settings.CorreoClinica.Trim()
        };
        return string.Join(Environment.NewLine, lineas.Where(x => x is not null));
    }

    private static bool EsFactura(Cita cita)
        => string.Equals(cita.ComprobanteTipo, "Factura", StringComparison.OrdinalIgnoreCase);

    private static void AsignarNumero(Cita cita)
    {
        var settings = AppSettings.Current;
        var factura = EsFactura(cita);
        var serie = SerieComprobante(factura, settings);
        if (!string.IsNullOrWhiteSpace(cita.ComprobanteNumero)
            && cita.ComprobanteNumero.StartsWith(serie + "-", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var n = factura ? settings.CorrelativoFactura : settings.CorrelativoBoleta;
        if (n < 1)
        {
            n = 1;
        }

        cita.ComprobanteNumero = $"{serie}-{n:000000}";
        if (factura)
        {
            settings.CorrelativoFactura = n + 1;
        }
        else
        {
            settings.CorrelativoBoleta = n + 1;
        }

        settings.Save();
    }

    private static string SerieComprobante(bool factura, AppSettings settings)
    {
        var serie = (factura ? settings.SerieFactura : settings.SerieBoleta)?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(serie) ? (factura ? "F001" : "B001") : serie;
    }

    private static string HtmlComprobante(Cita cita, Paciente paciente)
    {
        var settings = AppSettings.Current;
        var factura = EsFactura(cita);
        var total = (decimal)(cita.ComprobanteMonto ?? 0);
        var baseImponible = Math.Round(total / 1.18m, 2, MidpointRounding.AwayFromZero);
        var igv = total - baseImponible;
        var titulo = factura ? "FACTURA" : "BOLETA DE VENTA";
        var asunto = AsuntoComprobante(cita);
        var cuerpo = CuerpoCorreoComprobante(cita, paciente);
        var href = $"mailto:{cita.ComprobanteCorreo?.Trim()}?subject={Uri.EscapeDataString(asunto)}&body={Uri.EscapeDataString(cuerpo)}";
        var datosClinica = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(settings.RucClinica))
        {
            datosClinica.Append($"<div>RUC {E(settings.RucClinica.Trim())}</div>");
        }

        if (!string.IsNullOrWhiteSpace(settings.DireccionClinica))
        {
            datosClinica.Append($"<div>{E(settings.DireccionClinica.Trim())}</div>");
        }

        var contacto = Texto(
            string.IsNullOrWhiteSpace(settings.TelefonoClinica) ? null : TelefonoFormato.Formatear(settings.TelefonoClinica),
            settings.CorreoClinica);
        if (!string.IsNullOrWhiteSpace(contacto))
        {
            datosClinica.Append($"<div>{E(contacto)}</div>");
        }

        var documentoEtiqueta = factura ? "RUC" : (string.IsNullOrWhiteSpace(paciente.TipoDocumento) ? "Documento" : paciente.TipoDocumento);
        var letras = MontoEnLetras(total);
        var bloqueLetras = letras is null ? string.Empty : $"<p class=\"letras\">{E(letras)}</p>";
        var logo = LogoDataUri();
        var logoHtml = logo is null
            ? string.Empty
            : $"<img src=\"{logo}\" alt=\"Iderma\" />";

        return $$"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
            <meta charset="utf-8" />
            <title>{{E(asunto)}}</title>
            <style>
              * { box-sizing: border-box; }
              body { font-family: Segoe UI, Arial, sans-serif; color: #1b1b1b; margin: 0; background: #f4f7f7; }
              .hoja { max-width: 760px; margin: 24px auto; background: #fff; padding: 28px 32px 22px; border-top: 6px solid #0E6B6B; }
              .marca { text-align: center; }
              .marca img { height: 78px; width: auto; max-width: 240px; object-fit: contain; }
              .marca h1 { margin: 8px 0 0; color: #0A4F4F; font-size: 22px; letter-spacing: 0.04em; }
              .marca .giro { color: #8a7340; margin-top: 2px; font-size: 13px; }
              .marca .datos { color: #444; font-size: 12.5px; line-height: 1.45; margin-top: 6px; }
              .tipo { margin: 18px 0 8px; text-align: center; border: 1px solid #C4A35A; padding: 10px 12px; }
              .tipo h2 { margin: 0; color: #0A4F4F; font-size: 20px; letter-spacing: 0.08em; }
              .tipo .numero { font-size: 16px; font-weight: 700; margin-top: 4px; }
              .tipo .fecha { color: #555; font-size: 12.5px; margin-top: 2px; }
              h3 { color: #0A4F4F; font-size: 13px; border-bottom: 1px solid #C4A35A; margin: 16px 0 6px; padding-bottom: 2px; }
              table { width: 100%; border-collapse: collapse; font-size: 13px; }
              th, td { border: 1px solid #d5d5d5; padding: 6px 8px; text-align: left; vertical-align: top; }
              th { background: #E7F3F3; width: 28%; font-weight: 600; }
              table.detalle th { width: auto; }
              td.importe, th.importe { text-align: right; white-space: nowrap; }
              table.totales { width: 280px; margin: 10px 0 0 auto; }
              table.totales th { width: 55%; }
              table.totales td { text-align: right; }
              tr.total th, tr.total td { background: #0E6B6B; color: #fff; font-size: 15px; }
              .pago { margin: 12px 0 0; font-size: 13px; }
              .letras { margin: 8px 0 0; font-size: 12.5px; font-weight: 600; letter-spacing: 0.02em; }
              .gracias { margin: 18px 0 0; text-align: center; color: #0A4F4F; font-size: 13px; }
              .acciones { max-width: 760px; margin: 0 auto 28px; display: flex; gap: 8px; justify-content: center; }
              button { background: #0E6B6B; color: #fff; border: 0; border-radius: 6px; padding: 10px 16px; font-size: 14px; cursor: pointer; }
              button.sec { background: #0A4F4F; }
              @media print {
                body { background: #fff; }
                .acciones, button { display: none; }
                .hoja { margin: 0; border-top: 6px solid #0E6B6B; }
                @page { margin: 12mm; }
              }
            </style>
            </head>
            <body>
            <article class="hoja">
              <header class="marca">
                {{logoHtml}}
                <h1>{{E(settings.NombreClinica)}}</h1>
                <div class="giro">Dermatología láser y trasplante capilar</div>
                <div class="datos">{{datosClinica}}</div>
              </header>
              <section class="tipo">
                <h2>{{titulo}}</h2>
                <div class="numero">N.° {{E(cita.ComprobanteNumero)}}</div>
                <div class="fecha">Fecha de emisión: {{DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cultura)}}</div>
              </section>
              <h3>Cliente</h3>
              <table>
                <tr><th>{{(factura ? "Razón social" : "Nombre")}}</th><td>{{E(cita.ComprobanteCliente)}}</td></tr>
                <tr><th>{{E(documentoEtiqueta)}}</th><td>{{E(cita.ComprobanteDocumento)}}</td></tr>
                <tr><th>Dirección</th><td>{{E(Texto(paciente.Direccion, paciente.Distrito, paciente.Ciudad))}}</td></tr>
                <tr><th>Teléfono</th><td>{{E(TelefonoFormato.FormatearONull(paciente.Telefono))}}</td></tr>
                <tr><th>Correo</th><td>{{E(cita.ComprobanteCorreo)}}</td></tr>
                <tr><th>Expediente</th><td>{{E(paciente.NumeroExpediente)}}</td></tr>
              </table>
              <h3>Detalle de lo que está pagando</h3>
              <table class="detalle">
                <tr>
                  <th>Descripción</th>
                  <th>Cita</th>
                  <th>Médico</th>
                  <th class="importe">Importe</th>
                </tr>
                <tr>
                  <td>{{E(cita.ComprobanteConcepto)}}</td>
                  <td>{{E(cita.FechaTexto + " " + cita.HorarioTexto)}}<br>{{E(cita.Tipo)}}</td>
                  <td>{{E(cita.Medico)}}</td>
                  <td class="importe">S/ {{total.ToString("N2", Cultura)}}</td>
                </tr>
              </table>
              <table class="totales">
                <tr><th>Op. gravada</th><td>S/ {{baseImponible.ToString("N2", Cultura)}}</td></tr>
                <tr><th>IGV (18%)</th><td>S/ {{igv.ToString("N2", Cultura)}}</td></tr>
                <tr class="total"><th>Total</th><td>S/ {{total.ToString("N2", Cultura)}}</td></tr>
              </table>
              <p class="pago">Forma de pago: <strong>{{E(cita.ComprobanteFormaPago)}}</strong></p>
              {{bloqueLetras}}
              <p class="gracias">Gracias por su preferencia.</p>
            </article>
            <div class="acciones">
              <button type="button" onclick="window.print()">Imprimir</button>
              <button type="button" class="sec" data-mail="{{E(href)}}" onclick="location.href=this.dataset.mail">Enviar por correo</button>
            </div>
            <script>window.addEventListener('load', () => setTimeout(() => window.print(), 400));</script>
            </body>
            </html>
            """;
    }

    private static string? MontoEnLetras(decimal total)
    {
        if (total < 0 || total >= 1_000_000_000m)
        {
            return null;
        }

        var soles = (int)Math.Floor(total);
        var centavos = (int)Math.Round((total - soles) * 100m, 0, MidpointRounding.AwayFromZero);
        if (centavos == 100)
        {
            soles++;
            centavos = 0;
        }

        return $"SON: {EnteroEnLetras(soles)} Y {centavos:00}/100 SOLES";
    }

    private static string EnteroEnLetras(int n)
    {
        if (n == 0)
        {
            return "CERO";
        }

        var millones = n / 1_000_000;
        var miles = n / 1000 % 1000;
        var resto = n % 1000;
        var partes = new List<string>();
        if (millones > 0)
        {
            partes.Add(millones == 1 ? "UN MILLÓN" : GrupoEnLetras(millones) + " MILLONES");
        }

        if (miles > 0)
        {
            partes.Add(miles == 1 ? "MIL" : GrupoEnLetras(miles) + " MIL");
        }

        if (resto > 0)
        {
            partes.Add(GrupoEnLetras(resto));
        }

        return string.Join(' ', partes);
    }

    private static string GrupoEnLetras(int n)
    {
        if (n == 100)
        {
            return "CIEN";
        }

        string[] unidades = ["", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE"];
        string[] decenas = ["", "", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"];
        string[] centenas = ["", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"];
        var texto = n >= 100 ? centenas[n / 100] : string.Empty;
        var resto = n % 100;
        if (resto == 0)
        {
            return texto;
        }

        string menores;
        if (resto < 20)
        {
            menores = unidades[resto];
        }
        else if (resto < 30)
        {
            menores = resto == 20 ? "VEINTE" : resto switch
            {
                21 => "VEINTIÚN",
                22 => "VEINTIDÓS",
                23 => "VEINTITRÉS",
                26 => "VEINTISÉIS",
                _ => "VEINTI" + unidades[resto - 20]
            };
        }
        else
        {
            var unidad = resto % 10;
            menores = decenas[resto / 10] + (unidad == 0 ? string.Empty : " Y " + unidades[unidad]);
        }

        return string.IsNullOrEmpty(texto) ? menores : texto + " " + menores;
    }

    private static async Task<string> EscribirHtmlAsync(string html, string nombre)
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "IdermaCapilar-impresion");
        Directory.CreateDirectory(carpeta);
        var seguro = NombreArchivoSeguro(nombre);
        var ruta = Path.Combine(carpeta, $"{seguro}-{DateTime.Now:yyyyMMddHHmmss}.html");
        await File.WriteAllTextAsync(ruta, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return ruta;
    }

    private static async Task AbrirAsync(string html, string nombre)
    {
        var ruta = await EscribirHtmlAsync(html, nombre);
        await AbrirRutaAsync(ruta);
    }

    private static async Task AbrirRutaAsync(string ruta)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ruta,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(ruta) ?? Path.GetTempPath(),
                Verb = "open"
            });
            return;
        }
        catch (Exception)
        {
            // En WinUI sin empaquetar LaunchFileAsync necesita la ventana; se usa como respaldo.
        }

        if (!await AbrirConLauncherAsync(ruta))
        {
            throw new InvalidOperationException(
                "No se pudo abrir el documento para imprimir. Compruebe que hay un navegador predeterminado e inténtelo de nuevo.");
        }
    }

    private static async Task<bool> AbrirConLauncherAsync(string ruta)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(ruta);
            var options = new LauncherOptions();
            if (global::IdermaCapilar_Historial.App.MainAppWindow is not null)
            {
                InitializeWithWindow.Initialize(options, WindowNative.GetWindowHandle(global::IdermaCapilar_Historial.App.MainAppWindow));
            }

            return await Launcher.LaunchFileAsync(file, options);
        }
        catch
        {
            return false;
        }
    }

    private static string NombreArchivoSeguro(string nombre)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var limpio = new string(nombre.Select(c => invalidos.Contains(c) ? '-' : c).ToArray());
        return string.IsNullOrWhiteSpace(limpio) ? "impresion" : limpio.Trim();
    }

    private static void Fila(StringBuilder sb, string etiqueta, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return;
        }

        sb.AppendLine($"<tr><th>{E(etiqueta)}</th><td>{E(valor)}</td></tr>");
    }

    private static void FichasDoctor(StringBuilder sb, string etiqueta, string? nombre, IReadOnlyList<Doctor> doctores)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }

        var doctor = BuscarDoctor(nombre, doctores);
        Fila(sb, etiqueta, doctor?.NombreMostrado ?? nombre.Trim());
        if (doctor is null)
        {
            return;
        }

        Fila(sb, "Especialidad", doctor.Especialidad);
        Fila(sb, "CMP / colegiatura", doctor.Cmp);
        Fila(sb, "Teléfono del médico", TelefonoFormato.FormatearONull(doctor.Telefono));
        Fila(sb, "Correo del médico", doctor.Email);
    }

    private static void FichasEnfermera(StringBuilder sb, string etiqueta, string? nombre, IReadOnlyList<Enfermera> enfermeras)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }

        var enfermera = BuscarEnfermera(nombre, enfermeras);
        Fila(sb, etiqueta, enfermera?.NombreMostrado ?? nombre.Trim());
        if (enfermera is null)
        {
            return;
        }

        Fila(sb, "Área de enfermería", enfermera.Especialidad);
        Fila(sb, "CEP / colegiatura", enfermera.Cep);
        Fila(sb, "Teléfono de la enfermera", TelefonoFormato.FormatearONull(enfermera.Telefono));
        Fila(sb, "Correo de la enfermera", enfermera.Email);
    }

    private static string? ResumenDoctor(string? nombre, IReadOnlyList<Doctor>? doctores)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        var doctor = BuscarDoctor(nombre, doctores ?? []);
        if (doctor is null)
        {
            return nombre.Trim();
        }

        return Texto(doctor.NombreMostrado, doctor.Especialidad, string.IsNullOrWhiteSpace(doctor.Cmp) ? null : "CMP " + doctor.Cmp);
    }

    private static Doctor? BuscarDoctor(string? nombre, IReadOnlyList<Doctor> doctores)
        => doctores.FirstOrDefault(d => MismoNombre(nombre, d.NombreMostrado, d.Nombres, d.Apellidos, d.Titulo));

    private static Enfermera? BuscarEnfermera(string? nombre, IReadOnlyList<Enfermera> enfermeras)
        => enfermeras.FirstOrDefault(e => MismoNombre(nombre, e.NombreMostrado, e.Nombres, e.Apellidos, e.Titulo));

    private static bool MismoNombre(string? buscado, string mostrado, string nombres, string apellidos, string? titulo)
    {
        if (string.IsNullOrWhiteSpace(buscado))
        {
            return false;
        }

        var a = NormalizarNombre(buscado);
        if (a.Length == 0)
        {
            return false;
        }

        if (a == NormalizarNombre(mostrado) || a == NormalizarNombre($"{nombres} {apellidos}"))
        {
            return true;
        }

        var sinTitulo = NormalizarNombre(buscado.Replace(titulo ?? string.Empty, string.Empty, StringComparison.OrdinalIgnoreCase));
        return sinTitulo.Length > 0 && sinTitulo == NormalizarNombre($"{nombres} {apellidos}");
    }

    private static string NormalizarNombre(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return string.Join(' ', valor.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static void ListaChecklist(StringBuilder sb, Procedimiento item)
    {
        var pasos = ChecklistOperacion.ParaTipo(item.Tipo);
        if (pasos.Count == 0)
        {
            return;
        }

        var hechos = ChecklistOperacion.IdsHechos(item.ChecklistPasos);
        sb.AppendLine($"<h4>Procedimiento paso a paso ({E(item.ChecklistResumen)})</h4><table>");
        string? fase = null;
        foreach (var paso in pasos)
        {
            if (paso.Fase != fase)
            {
                fase = paso.Fase;
                sb.AppendLine($"<tr><th colspan='2'>{E(fase)}</th></tr>");
            }

            var marca = hechos.Contains(paso.Id) ? "Sí" : "No";
            sb.AppendLine($"<tr><th>{E(paso.Titulo)}</th><td><strong>{marca}.</strong> {E(paso.Instruccion)}</td></tr>");
        }

        sb.AppendLine("</table>");
    }

    private static string? Texto(params string?[] partes)
    {
        var unidos = string.Join(" · ", partes.Where(p => !string.IsNullOrWhiteSpace(p))!);
        return string.IsNullOrWhiteSpace(unidos) ? null : unidos;
    }

    private static string E(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);

    private static string Capitalizar(string texto)
        => string.IsNullOrWhiteSpace(texto) ? texto : char.ToUpper(texto[0], Cultura) + texto[1..];
}
