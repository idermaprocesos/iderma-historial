# Iderma Capilar — Historial médico

Aplicación de escritorio **WinUI 3** para gestionar el historial clínico de pacientes de Iderma Capilar.

## Qué incluye

- Datos personales y número de expediente (`IDC-AÑO-0001`)
- Contacto de emergencia
- Datos médicos (alergias, medicamentos, antecedentes, grupo sanguíneo, IMC)
- Evaluación capilar (alopecia, Norwood/Ludwig, zona donante, diagnóstico y plan)
- Operaciones y procedimientos (ARTAS, FUE, PRP, láser, etc.)
- Observaciones de consulta y seguimiento
- Búsqueda, fotografía, tema claro/oscuro y respaldo de la base de datos

Los datos se guardan en este equipo, en:

`%LocalAppData%\IdermaCapilar\Historial\historial.db`

## Cómo ejecutar

Requisitos: Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download) y Windows App SDK (se restaura con NuGet).

```powershell
cd "c:\Users\Iderma Capilar 1\Documents\HISTORIAL"
dotnet restore
dotnet run -c Debug -r win-x64
```

También puede abrir `IdermaCapilar.Historial.sln` en Visual Studio 2022 y presionar F5 (plataforma x64). La aplicación es no empaquetada y lleva el runtime de Windows App SDK, para poder ejecutarla en el equipo de la clínica sin instalar MSIX.
