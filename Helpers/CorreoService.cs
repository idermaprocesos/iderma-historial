using System.Runtime.InteropServices;
using WinRT.Interop;

namespace IdermaCapilar_Historial.Helpers;

public enum ResultadoCorreo
{
    AbiertoConAdjunto,
    AbiertoSinAdjunto,
    Cancelado,
    Fallo
}

public static class CorreoService
{
    public static ResultadoCorreo Enviar(string? para, string asunto, string cuerpo, string rutaAdjunto)
    {
        var destino = string.IsNullOrWhiteSpace(para) ? string.Empty : para.Trim();
        if (IntentarOutlook(destino, asunto, cuerpo, rutaAdjunto))
        {
            return ResultadoCorreo.AbiertoConAdjunto;
        }

        var mapi = IntentarMapi(destino, asunto, cuerpo, rutaAdjunto);
        if (mapi == 0)
        {
            return ResultadoCorreo.AbiertoConAdjunto;
        }

        if (mapi == 1)
        {
            return ResultadoCorreo.Cancelado;
        }

        return IntentarMailto(destino, asunto, cuerpo)
            ? ResultadoCorreo.AbiertoSinAdjunto
            : ResultadoCorreo.Fallo;
    }

    private static bool IntentarOutlook(string para, string asunto, string cuerpo, string rutaAdjunto)
    {
        try
        {
            var tipo = Type.GetTypeFromProgID("Outlook.Application");
            if (tipo is null)
            {
                return false;
            }

            dynamic app = Activator.CreateInstance(tipo)!;
            dynamic mail = app.CreateItem(0);
            mail.To = para;
            mail.Subject = asunto;
            mail.Body = cuerpo;
            if (File.Exists(rutaAdjunto))
            {
                mail.Attachments.Add(rutaAdjunto);
            }

            mail.Display(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int IntentarMapi(string para, string asunto, string cuerpo, string rutaAdjunto)
    {
        var recipPtr = IntPtr.Zero;
        var filePtr = IntPtr.Zero;
        try
        {
            var mensaje = new MapiMessage
            {
                subject = asunto,
                noteText = cuerpo
            };

            if (!string.IsNullOrWhiteSpace(para))
            {
                var recip = new MapiRecipDesc
                {
                    recipClass = 1,
                    name = para,
                    address = "SMTP:" + para
                };
                recipPtr = Marshal.AllocHGlobal(Marshal.SizeOf<MapiRecipDesc>());
                Marshal.StructureToPtr(recip, recipPtr, false);
                mensaje.recipCount = 1;
                mensaje.recips = recipPtr;
            }

            if (File.Exists(rutaAdjunto))
            {
                var archivo = new MapiFileDesc
                {
                    position = -1,
                    pathName = rutaAdjunto,
                    fileName = Path.GetFileName(rutaAdjunto)
                };
                filePtr = Marshal.AllocHGlobal(Marshal.SizeOf<MapiFileDesc>());
                Marshal.StructureToPtr(archivo, filePtr, false);
                mensaje.fileCount = 1;
                mensaje.files = filePtr;
            }

            var hwnd = IntPtr.Zero;
            if (global::IdermaCapilar_Historial.App.MainAppWindow is not null)
            {
                hwnd = WindowNative.GetWindowHandle(global::IdermaCapilar_Historial.App.MainAppWindow);
            }

            return MAPISendMailW(IntPtr.Zero, hwnd, ref mensaje, 0x00000001 | 0x00000008, 0);
        }
        catch
        {
            return -1;
        }
        finally
        {
            if (recipPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(recipPtr);
            }

            if (filePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(filePtr);
            }
        }
    }

    private static bool IntentarMailto(string para, string asunto, string cuerpo)
    {
        try
        {
            var href = $"mailto:{para}?subject={Uri.EscapeDataString(asunto)}&body={Uri.EscapeDataString(cuerpo)}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = href,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("MAPI32.DLL", CharSet = CharSet.Unicode, EntryPoint = "MAPISendMailW")]
    private static extern int MAPISendMailW(IntPtr session, IntPtr hwnd, ref MapiMessage message, int flags, int reserved);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MapiRecipDesc
    {
        public int reserved;
        public int recipClass;
        public string name;
        public string address;
        public int eIDSize;
        public IntPtr entryID;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MapiFileDesc
    {
        public int reserved;
        public int flags;
        public int position;
        public string pathName;
        public string fileName;
        public IntPtr fileType;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MapiMessage
    {
        public int reserved;
        public string subject;
        public string noteText;
        public string messageType;
        public string dateReceived;
        public string conversationID;
        public int flags;
        public IntPtr originator;
        public int recipCount;
        public IntPtr recips;
        public int fileCount;
        public IntPtr files;
    }
}
