using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Svg;
using System.Drawing.Imaging;
using System.Drawing;
using System.Text.RegularExpressions;
using SharpCompress.Common;
using System.Windows.Media.Imaging;
using MetadataExtractor.Formats.Exif;
using System.Text.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Interop;

using SharpCompress.Archives;



namespace ManualToolkit.Windows;

public static partial class FileManager
{

    //----VARS
    /// <summary>
    /// C:\Users\YO\AppData\Local
    /// </summary>
    public readonly static string APPDATA_LOCAL_PATH = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); //Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    /// <summary>
    /// C:\Users\YO
    /// </summary>
    public readonly static string USERPROFILE_PATH = Environment.GetEnvironmentVariable("USERPROFILE");





    //----------- FILES

    public static void OPENFOLDER(string path)
    {
        // Comprobar si el path dado corresponde a un archivo
        if (File.Exists(path))
        {
            // Si es un archivo, obtener el directorio que lo contiene
            string directoryPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                // Abrir el directorio que contiene el archivo
                OPEN(directoryPath);
            }
            else
            {
                // Manejar el caso donde no se puede obtener un directorio, opcional
                Console.WriteLine("Unable to get directory from path: " + path);
            }
        }
        else
        {
            // Si el path no corresponde a un archivo, pasarlo directamente
            OPEN(path);
        }
    }

    public static void OPEN(string path)
    {
        if (Directory.Exists(path))
        {
            // La URL es un directorio
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            });
        }
        else if (File.Exists(path))
        {
            // La URL es un archivo
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        else
        {
            // La URL no es ni un directorio válido ni un archivo
            Console.WriteLine("The provided URL does not exist as a file or directory: " + path);
            // Opcional: Puedes manejar este caso como creas más conveniente (mostrar un mensaje de erro
        }
    }

    public static string GetFolderName(string folderName)
    {
        return new DirectoryInfo(folderName).Name;
    }
    public static string GetFileFolderName(string filePath)
    {
        return new DirectoryInfo(Path.GetDirectoryName(filePath)).Name;
    }
    public static void DeleteFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true); // El segundo parámetro indica si se deben borrar subcarpetas y archivos
        }

    }
    public static void MoveFolder(string sourcePath, string destinationPath)
    {
        try
        {
            // Verificar si la carpeta de origen existe
            if (Directory.Exists(sourcePath))
            {
                // Si la carpeta de destino existe, eliminarla
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, true);
                    Console.WriteLine($"La carpeta existente en \"{destinationPath}\" ha sido eliminada.");
                }

                // Crear el directorio de destino
                Directory.CreateDirectory(destinationPath);

                // Obtener el nombre de la carpeta
                string folderName = new DirectoryInfo(sourcePath).Name;

                // Construir la ruta de destino completa
                string destinationFullPath = Path.Combine(destinationPath, folderName);

                // Mover la carpeta
                Directory.Move(sourcePath, destinationFullPath);

                Console.WriteLine($"La carpeta \"{folderName}\" ha sido movida a \"{destinationPath}\".");
            }
            else
            {
                Console.WriteLine($"La carpeta de origen \"{sourcePath}\" no existe.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al mover la carpeta: {ex.Message}");
        }
    }
    public static void MoveFolderContent(string sourceDir, string destDir)
    {
        // Asegúrate de que el directorio destino existe
        Directory.CreateDirectory(destDir);

        // Mover todos los archivos
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Move(file, destFile);
        }

        // Mover todos los directorios
        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
            Directory.Move(directory, destSubDir);
        }
    }
    public static void DeleteFolderContent(string dir)
    {
        // Borrar todos los archivos
        foreach (var file in Directory.GetFiles(dir))
        {
            File.Delete(file);
        }

        // Borrar todos los directorios y su contenido
        foreach (var directory in Directory.GetDirectories(dir))
        {
            Directory.Delete(directory, true);
        }
    }
    public static void ReplaceFolderContent(string sourceDir, string destDir)
    {
        DeleteFolderContent(destDir);
        MoveFolderContent(sourceDir, destDir);
    }

    //---------- TOKEN SAVE
    public static string GetUniqueFilePath(string basePath, string extension)
    {
        int counter = 1;
        string filePath;

        do
        {
            filePath = $"{basePath}{(counter > 1 ? $"_{counter}" : "")}{extension}";
            counter++;
        } while (File.Exists(filePath));

        return filePath;
    }

    public static void SaveToken(string token, string tokenName)
    {
        byte[] tokenBytes = System.Text.Encoding.Unicode.GetBytes(token);
        byte[] encryptedToken = ProtectedData.Protect(tokenBytes, null, DataProtectionScope.CurrentUser);
        System.IO.File.WriteAllBytes(tokenName, encryptedToken);
    }
    public static string LoadToken(string tokenName)
    {
        try
        {
            if (System.IO.File.Exists(tokenName))
            {
                byte[] encryptedToken = System.IO.File.ReadAllBytes(tokenName);
                byte[] tokenBytes = ProtectedData.Unprotect(encryptedToken, null, DataProtectionScope.CurrentUser);
                var token = System.Text.Encoding.Unicode.GetString(tokenBytes);
                return token;
            }
            return null; // o podría ser preferible lanzar una excepción aquí también
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            // Aquí se captura la excepción específica y se lanza una nueva con un mensaje personalizado
            throw new InvalidOperationException("The token could not be decrypted. This may occur if the data was encrypted on a different machine or by a different user.", ex);
        }
    }



    public static string RunProcess(string exePath, string arguments, bool openWindow = false)
    {
        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = !openWindow
        };

        using (Process process = new Process { StartInfo = processStartInfo })
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Puedes hacer algo con el resultado, como imprimirlo en la consola o manejarlo de otra manera.
            Console.WriteLine($"Output: {output}");
            Console.WriteLine($"Error: {error}");

            return output;
        }
    }
    public static async Task CMD(IEnumerable<string> commands, string workingDirectory, bool openWindow, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, Action exitCode = null)
    {
        // Construye el contenido del script
        var batContent = new StringBuilder();

        foreach (var command in commands)
        {
            batContent.AppendLine(command);
        }

        // Escribe el contenido a un archivo temporal .bat
        var tempBatPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".bat");
        File.WriteAllText(tempBatPath, batContent.ToString());

        // Ejecuta el archivo .bat
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = tempBatPath,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.OutputDataReceived += outputDataReceived;
        process.ErrorDataReceived += errorDataReceived;
        process.Exited += (sender, e) =>
        {
            TaskBar.Stop($"OpenBat Finished");
            exitCode?.Invoke(); //TODO: se llama 2 veces al final, por eso queda en 0 fixedProcess
            process.Close();
        };


        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        // Limpieza: elimina el archivo .bat temporal
        File.Delete(tempBatPath);

    }
    public static async Task CMD(IEnumerable<string> commands, string workingDirectory, bool openWindow = false)
    {
        await CMD(commands, workingDirectory, false, OutputDataReceived, ErrorDataReceived);
    }
    public static async Task CMD(string command, string workingDirectory, bool openWindow = false)
    {
        await CMD(new List<string> { command }, workingDirectory, openWindow, OutputDataReceived, ErrorDataReceived);
    }

    // ----------- BAT
    public static async Task OpenBat(string batFilePath, bool openWindow, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, Action exitCode = null)
    {
        await Task.Run(() =>
        {
            TaskBar.Indeterminate();
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + batFilePath);
            processInfo.CreateNoWindow = !openWindow;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.WorkingDirectory = Path.GetDirectoryName(batFilePath);

            var process = new Process //poner esto en otro metodo
            {
                StartInfo = processInfo,
                EnableRaisingEvents = true
            };
            


            process.OutputDataReceived += outputDataReceived;
            process.ErrorDataReceived += errorDataReceived;


            process.Exited += (sender, e) =>
            {
                TaskBar.Stop($"OpenBat Finished");
                exitCode?.Invoke(); //TODO: se llama 2 veces al final, por eso queda en 0 fixedProcess
                process.Close();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        });
    }
    public static async Task OpenBat(string batFilePath, bool openWindow = false)
    {
        await OpenBat(batFilePath, openWindow, OutputDataReceived, ErrorDataReceived);
        //  Output.Log("Count: " + Output.Count.ToString());
    }

    private static void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
          Debug.WriteLine("output>> " + e.Data);
    }
    private static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
           Debug.WriteLine("error>> " + e.Data);
    }


    public static string GetUserProfile()
    {
        string[] pathParts = USERPROFILE_PATH.Split('\\');
        string username = pathParts[pathParts.Length - 1];
        return username;
    }
    public static void SetBatVariable(string batFilePath, string variableName, string newValue)
    {
        // Leer todas las líneas del archivo .bat
        var lines = File.ReadAllLines(batFilePath).ToList();

        // Buscar la línea que contiene "set [variableName]="
        int lineIndex = lines.FindIndex(line => line.StartsWith($"set {variableName}="));

        // Si encontramos la línea, reemplazarla con el nuevo valor
        if (lineIndex != -1)
        {
            lines[lineIndex] = $"set {variableName}={newValue}";
        }

        // Guardar el archivo .bat modificado
        File.WriteAllLines(batFilePath, lines);
    }


    //----------------------------------------------------------------------------------- SVG

    // "C:\Program Files\Inkscape\inkscape.exe" "ruta/al/archivo.svg" --export-filename="ruta/al/archivo.png" --export-width=48 --export-height=48

    public static async Task<bool> ConvertSvgToPng(string svgContent, string outputPath, int width = 48, int height = 48)
    {
        try
        {
            svgContent = SvgChangeStrokeColor(svgContent, "white");

            var tempSvgPath = Path.GetTempFileName() + ".svg";
            File.WriteAllText(tempSvgPath, svgContent);

            string command = $"inkscape \"{tempSvgPath}\" --export-filename=\"{outputPath}\" --export-width={width} --export-height={height}";

            string inkscapeDirectory = Path.GetDirectoryName("C:/Program Files/Inkscape/bin/inkscape.exe");

            if (inkscapeDirectory == null)
                return false;

            await CMD(command, inkscapeDirectory);
            File.Delete(tempSvgPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
    static string SvgChangeStrokeColor(string svgContent, string newStrokeColor)
    {
        // Utiliza expresiones regulares para encontrar y reemplazar el valor del atributo stroke
        string pattern = @"stroke=""([^""]*)""";
        string replacement = $"stroke=\"{newStrokeColor}\"";
        string modifiedSvgContent = Regex.Replace(svgContent, pattern, replacement);

        return modifiedSvgContent;
    }


    public static List<MetadataEntry> ExtractMetadata(string filePath)
    {
        var metadata = new List<MetadataEntry>();

        try
        {
            var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(filePath);
            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags)
                {
                    metadata.Add(new MetadataEntry
                    {
                        DirectoryName = directory.Name,
                        TagName = tag.Name,
                        Description = tag.Description
                    });
                }

                if (directory.HasError)
                {
                    foreach (var error in directory.Errors)
                    {
                        metadata.Add(new MetadataEntry
                        {
                            DirectoryName = $"Error - {directory.Name}",
                            TagName = "Error",
                            Description = error
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            metadata.Add(new MetadataEntry
            {
                DirectoryName = "Exception",
                TagName = "Exception",
                Description = ex.Message
            });
        }

        return metadata;
    }

    public static string? ReadCustomMetadata(string imagePath, string tagName)
    {
        var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(imagePath);
        return FindMetadataTag(tagName, directories);
    }

    public static string? ReadCustomMetadata(byte[] image, string tagName)
    {
        using (MemoryStream stream = new MemoryStream(image))
        {
            var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(stream);
            return FindMetadataTag(tagName, directories);
        }
    }
    private static string? FindMetadataTag(string tagName, IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        foreach (var directory in directories)
        {
            foreach (var tag in directory.Tags)
            {
                if(tag.DirectoryName == tagName)
                {
                    var jsonText = tag.Description;//.Substring($"{tagName}:".Length).Trim();
                    return jsonText;
                }
                else if (tag.Description.StartsWith($"{tagName}:"))
                {
                    var jsonText = tag.Description.Substring($"{tagName}:".Length).Trim();
                    return jsonText;
                }
            }
        }
        return null;
    }
  
    public static T JsonToClass<T>(Dictionary<string, object> graphExtra, string key)
    {
        // Comprobar si la clave existe en el diccionario
        if (graphExtra.TryGetValue(key, out object value))
        {
            // Si el valor es una cadena JSON, deserialízalo directamente.
            if (value is string manualConfigJson && !string.IsNullOrEmpty(manualConfigJson))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(manualConfigJson);
                }
                catch (JsonReaderException ex)
                {
                    // Manejar o registrar el error de deserialización
                    Console.WriteLine($"Error deserializando JSON: {ex.Message}");
                }
            }
            // Si el valor ya es un JObject, conviértelo al tipo T deseado
            else if (value is JObject jObjectValue)
            {
                try
                {
                    return jObjectValue.ToObject<T>();
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    // Manejar o registrar el error de conversión
                    Console.WriteLine($"Error convirtiendo JObject a {typeof(T).Name}: {ex.Message}");
                }
            }
        }

        // Devolver el valor predeterminado para el tipo si la clave no existe o si hay algún problema en la deserialización o conversión
        return (T)value;
    }



    public static BitmapSource GetFileIcon(string filePath)
    {
        try
        {
            using (Icon sysicon = Icon.ExtractAssociatedIcon(filePath)) // Extrae el ícono
            {
                if (sysicon != null)
                {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        sysicon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error extracting icon: " + ex.Message);
        }
        return null;
    }

    //TODO: poner iconos
    public static ImageSource GetIconFromExtension(string extension)
    {
        switch (extension.ToLower())
        {
            case ".zip":
                return new BitmapImage(new Uri("pack://application:,,,/Images/zip_icon.png"));
            case ".exe":
                return new BitmapImage(new Uri("pack://application:,,,/Images/exe_icon.png"));
            case ".pdf":
                return new BitmapImage(new Uri("pack://application:,,,/Images/pdf_icon.png"));
            default:
                return new BitmapImage(new Uri("pack://application:,,,/Images/default_icon.png"));
        }
    }






    //COMPRESS

    public static async void Extract7zFile(string sourceArchive, string outputDirectory, Action? onFinalize = null)
    {

        // Asegúrate de que el directorio de salida existe
        if (!System.IO.Directory.Exists(outputDirectory))
        {
            System.IO.Directory.CreateDirectory(outputDirectory);
        }

        // Abrir el archivo 7z para extracción
        await Task.Run(() =>
        {
            using (var archiveFile = new SevenZipExtractor.ArchiveFile(sourceArchive))
            {
                archiveFile.Extract(outputDirectory); // Extrae todos los archivos al directorio especificado
            }
        });

        onFinalize?.Invoke();
    }


    public static string? GetFilePath(string directoryPath, string searchPattern, string extension)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"El directorio '{directoryPath}' no existe.");
        }

        var files = Directory.GetFiles(directoryPath, "*" + extension, SearchOption.TopDirectoryOnly)
                             .Where(f => Path.GetFileName(f).Contains(searchPattern))
                             .ToList();

        return files.FirstOrDefault();
    }


    public static string ToReadableJson(string content)
    {
        var jsonObject = JToken.Parse(content);
        var formattedContent = jsonObject.ToString(Formatting.Indented);
        return formattedContent;
    }

    public static string ToReadableJson(JToken content)
    {
        return ToReadableJson(content.ToString());
    }


    // Método para verificar si un proceso específico ya está en ejecución
    public static bool IsProcessRunning(string processName, string scriptName)
    {
        // Obtener todos los procesos con el nombre especificado
        var processes = Process.GetProcessesByName(processName);

        foreach (var process in processes)
        {
            try
            {
                // Verificar el título de la ventana o los argumentos del proceso
                if (!string.IsNullOrEmpty(process.MainWindowTitle) && process.MainWindowTitle.Contains(scriptName))
                {
                    return true;
                }
                else if (process.MainModule != null && process.MainModule.FileName.Contains(scriptName))
                {
                    return true;
                }
            }
            catch
            {
                // Puede haber procesos a los que no se tiene acceso, manejar excepciones según sea necesario
                continue;
            }
        }

        return false;
    }


}











//---------------------------------------------------------------------------------------------------------------------------------------------------- END OF FILE MANAGER
public class BatFile
{
    string FilePath;
    Process? BatProcess;


    DataReceivedEventHandler OutputDataReceived;
    DataReceivedEventHandler ErrorDataReceived;
    public BatFile()
    {
            
    }
    public BatFile(string bathFilePath)
    {
        FilePath = bathFilePath;
    }


    public async Task OpenPython(string python_embededPath, string programPath, string arguments, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, bool window = false)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = python_embededPath, //@".\python_embeded\python.exe",
            Arguments = programPath + " " + arguments, // @"ComfyUI\main.py --windows-standalone-build --multi-user",
            UseShellExecute = window, // Usar el shell para abrir en una ventana si se requiere
            CreateNoWindow = !window,
            RedirectStandardOutput = !window, // No redirigir si se desea una ventana con la salida visible
            RedirectStandardError = !window  // No redirigir si se desea una ventana con los errores visibles
        };

        BatProcess = new Process() { StartInfo = startInfo };
        BatProcess.Start();

        if (!window)
        {
            // Solo capturar la salida y los errores si no se está en modo ventana
            BatProcess.BeginOutputReadLine();
            BatProcess.BeginErrorReadLine();

            OutputDataReceived = outputDataReceived;
            ErrorDataReceived = errorDataReceived;

            BatProcess.OutputDataReceived += outputDataReceived;
            BatProcess.ErrorDataReceived += errorDataReceived;
        }
    }

    public static bool CheckPython()
    {
        try
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "python";
                process.StartInfo.Arguments = "--version";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                // Captura la salida del proceso
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                // Verifica si la salida contiene "Python"
                return output.Contains("Python") || error.Contains("Python");
            }
        }
        catch
        {
            return false; // Python no está instalado o hubo un error
        }
    }





    public async Task Open(bool openWindow, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, Action exitCode = null)
    {
        await Task.Run(() =>
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + FilePath);
            processInfo.CreateNoWindow = !openWindow;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.WorkingDirectory = Path.GetDirectoryName(FilePath);

            var process = new Process //poner esto en otro metodo
            {
                StartInfo = processInfo,
                EnableRaisingEvents = true
            };
            BatProcess = process;

            OutputDataReceived = outputDataReceived;
            ErrorDataReceived = errorDataReceived;

            process.OutputDataReceived += outputDataReceived;
            process.ErrorDataReceived += errorDataReceived;


            process.Exited += (sender, e) =>
            {
                exitCode?.Invoke(); //TODO: se llama 2 veces al final, por eso queda en 0 fixedProcess
                process.Close();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        });
    }


    public async void Close()
    {
        if (BatProcess != null && !BatProcess.HasExited)
        {
            BatProcess.OutputDataReceived -= OutputDataReceived;
            BatProcess.ErrorDataReceived -= ErrorDataReceived;


            IntPtr hWnd = BatProcess.MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                IPCManager.SendMessage(hWnd, IPCManager.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                BatProcess.Kill(); // Fallback si no se encuentra la ventana
            }
            await BatProcess.WaitForExitAsync(); // Opcional, espera a que el proceso termine realmente
            BatProcess = null;

        }
    }
    


    public void SetVariable(string variableName, string newValue)
    {
        // Leer todas las líneas del archivo .bat
        var lines = File.ReadAllLines(FilePath).ToList();

        // Buscar la línea que contiene "set [variableName]="
        int lineIndex = lines.FindIndex(line => line.StartsWith($"set {variableName}="));

        // Si encontramos la línea, reemplazarla con el nuevo valor
        if (lineIndex != -1)
        {
            lines[lineIndex] = $"set {variableName}={newValue}";
        }

        // Guardar el archivo .bat modificado
        File.WriteAllLines(FilePath, lines);
    }






}





//------------------------------------------------------------------------------------------------------------------------------------- JSON CONTRACT RESOLVER

public static partial class FileManager
{
   public class LowercaseContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLower();
        }
    }

    public class IgnorePropertiesResolver : DefaultContractResolver
    {
        private readonly HashSet<string> _ignoreProps;

        public IgnorePropertiesResolver(IEnumerable<string> propNamesToIgnore)
        {
            _ignoreProps = new HashSet<string>(propNamesToIgnore);
        }
        public IgnorePropertiesResolver(params string[] propNamesToIgnore)
        {
            _ignoreProps = new HashSet<string>(propNamesToIgnore);
        }

        protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            Newtonsoft.Json.Serialization.JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (_ignoreProps.Contains(property.PropertyName))
            {
                property.ShouldSerialize = instance => false;
            }

            return property;
        }
    }


}

public class MetadataEntry
{
    public string DirectoryName { get; set; }
    public string TagName { get; set; }
    public string Description { get; set; }
}