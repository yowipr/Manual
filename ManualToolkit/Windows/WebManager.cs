using ManualToolkit.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Printing;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.CodeDom;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using System.Web;
using System.Net.Mime;
using System.Data.SqlTypes;
using LibGit2Sharp.Handlers;

namespace ManualToolkit.Windows;

public static class WebManager
{
    /// <summary>
    /// opens a url in the main web
    /// </summary>
    /// <param name="url"></param>
    public static void OPEN(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });

    }

    public static async Task<string> TEST(int delay)
    {
        await Task.Delay(delay);
        return "Test";
    }

    public static string Parse(object obj)
    {
        string jsonString = System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        return jsonString;
    }


    public static JObject Parse(string json)
    {
        return JObject.Parse(json);
    }
   
    public static T GetJsonValue<T>(this string json, string variable)
    {
        var jsonObject = Parse(json);
        return jsonObject.Value<T>(variable);
    }

    public static async Task<JObject>POST(string url, object data, string? authToken = null)
    {
        using (var client = new HttpClient())
        {
            client.Timeout = Timeout.InfiniteTimeSpan;

            if (authToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            // Serializar el objeto de datos a JSON
            dynamic serialized = JsonConvert.SerializeObject(data, Formatting.Indented);
            var content = new StringContent(serialized, Encoding.UTF8, "application/json");

            try
            {
                // Realizar la solicitud POST
                var response = await client.PostAsync(url, content);

                // Asegúrate de que la respuesta es exitosa
                response.EnsureSuccessStatusCode();

                // Leer y deserializar la respuesta
                var r = await response.Content.ReadAsStringAsync();
                return JObject.Parse(r);
            }
            catch (Exception ex)
            {
                // Registrar la excepción para diagnóstico
                Debug.WriteLine("Error en la deserialización: " + ex.Message);
                throw; // O manejar la excepción de manera adecuada
            }
        }
    }
    public static async Task<T> POST<T>(string url, object data)
    {
        using HttpClient client = new();
        client.Timeout = Timeout.InfiniteTimeSpan;

        // Serializar el objeto de datos a JSON
        dynamic serialized = JsonConvert.SerializeObject(data, Formatting.Indented);
        var content = new StringContent(serialized, Encoding.UTF8, "application/json");

        try
        {
            // Realizar la solicitud POST
            var response = await client.PostAsync(url, content);

            // Asegúrate de que la respuesta es exitosa
            response.EnsureSuccessStatusCode();

            // Leer y deserializar la respuesta

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<dynamic>(responseString);
        }
        catch (Exception ex)
        {
            // Registrar la excepción para diagnóstico
            Debug.WriteLine("Error en la deserialización: " + ex.Message);
            throw; // O manejar la excepción de manera adecuada
        }

    }

    public static async Task<JObject> GET(string url, string? authToken = null, JObject Default = null)
    {
        using HttpClient client = new();

        try
        {
            if (authToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var r = await response.Content.ReadAsStringAsync();
                return JObject.Parse(r);
            }
        }
        catch (HttpRequestException ex)
        {
            return Default;
        }
        return Default;
    }
    public static async Task<T> GET<T>(string url, string? authToken = null, T? Default = default) // MAIN GET
    {
        using HttpClient client = new();

        if (authToken != null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        try
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();

                T result = JsonConvert.DeserializeObject<T>(json);//System.Text.Json.JsonSerializer.Deserialize<T>(json);
                return result;
            }
            else
            {
                return Default;
            }
        }
        catch
        {
            return Default;
        }
    }


    public static string Combine(string baseUrl, params string[] paths)
    {
        // Asegurarse de que la baseUrl termine con '/'
        baseUrl = baseUrl.TrimEnd('/') + "/";

        // Combinar las partes de la ruta
        var combinedPath = Path.Combine(new string[] { baseUrl }.Concat(paths).ToArray());

        // Reemplazar las barras invertidas '\' por barras normales '/'
        combinedPath = combinedPath.Replace('\\', '/');

        // Eliminar cualquier barra diagonal adicional en la raíz
        return combinedPath.TrimEnd('/');
    }





    public static string GetURLQuery(string url, string variable)
    {
        Uri uri = new Uri(url);
        string query = uri.Query.TrimStart('?');
        string[] parametros = query.Split('&');

        foreach (string parametro in parametros)
        {
            string[] partes = parametro.Split('=');
            if (partes.Length == 2 && partes[0] == variable)
            {
                return partes[1];
            }
        }

        return null;
    }



    //------------- DOWNLOAD
    static DateTime previousTime = DateTime.Now;
    static long previousBytesRead = 0L;
    public static async Task<bool> Download(string downloadUrl, string directoryDestinationPath, string? fileName = null)
    {
        try
        {
         
            //   Output.Log($"Downloading... {downloadUrl} to {destinationPath}");
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = Timeout.InfiniteTimeSpan;

                using (var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {


                    // Verifica el encabezado Content-Disposition para el nombre del archivo
                    ContentDispositionHeaderValue contentDisposition = response.Content.Headers.ContentDisposition;
                    if (fileName == null && contentDisposition != null && !string.IsNullOrWhiteSpace(contentDisposition.FileName))
                    {
                        fileName = contentDisposition.FileNameStar ?? contentDisposition.FileName;
                    }
                    else if (fileName == null)
                    {
                        fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                    }

                    string destinationPath = Path.Combine(directoryDestinationPath, fileName);


                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        var totalBytes = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : 0L;
                        var totalReadBytes = 0L;
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        do
                        {
                            var readBytes = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                            if (readBytes == 0)
                            {
                                isMoreToRead = false;
                                TaskBar.Set100(100); // 100% completado
                                continue;
                            }

                            await fileStream.WriteAsync(buffer, 0, readBytes);

                            totalReadBytes += readBytes;
                            float progress = totalBytes > 0 ? (float)totalReadBytes / totalBytes : -1; // between 0 and 1

                            if (progress < 0)
                            {
                                // No se conoce el progreso
                                TaskBar.Set(-1);
                                TaskBar.Indeterminate();
                            }
                            else // ----------- intermedio 
                            {
                                string speedStr = DownloadVelocity(totalReadBytes);
                                float progress100 = progress * 100;

                                string message = $"Downloading... {speedStr}/s   -   " +
                                    $"{totalReadBytes.ByteToReadableSize()} / {totalBytes.ByteToReadableSize()}" +
                                    $" ({progress100.ToString("F1")}%)";

                                TaskBar.Set(progress, message);
                                Debug.WriteLine(message);

                            }

                        } while (isMoreToRead);


                        //-------------------------------------------- 100% COMPLETED
                        // TaskBar.Notification();
                        //  Output.Log($"Download Finished! located in {destinationPath}");
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            //  Output.Log("Error al descargar: " + ex.Message);
            TaskBar.Notification();
            return false;
        }
    }
    static string DownloadVelocity(long totalReadBytes) // hay una ínfima probabilidad de que esto explote si el archivo pesa muchos bytes, por eso el "long"
    {
        DateTime now = DateTime.Now;
        TimeSpan timeDifference = now - previousTime;
        long bytesDifference = totalReadBytes - previousBytesRead;

        // Velocidad en bytes por segundo
        double speed = bytesDifference / timeDifference.TotalSeconds;

        string speedStr = $"{speed.ByteToReadableSize()}/s";


        previousTime = now;
        previousBytesRead = totalReadBytes;

        return speedStr;

    }
    public static async Task<byte[]> DownloadImageByte(string imageUrl, string jwtToken)
    {
        using (HttpClient client = new HttpClient())
        { 
            // Simular una solicitud de navegador añadiendo un User-Agent
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

            // Incluye el token JWT en la cabecera Authorization con el prefijo Bearer
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                return await client.GetByteArrayAsync(imageUrl);
            }
            catch (HttpRequestException e)
            {
                // Maneja el error adecuadamente
                Console.WriteLine($"Error al descargar la imagen: {e.Message}");
                return null;
            }
        }
    }

    public static async Task<byte[]> DownloadImageByte(string imageUrl)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetByteArrayAsync(imageUrl);
        }
    }
    public static async Task<BitmapImage> DownloadImage(string imageUrl)
    {
        try
        {
            using (System.Net.WebClient webClient = new System.Net.WebClient())
            {
                byte[] imageData = await webClient.DownloadDataTaskAsync(imageUrl);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new System.IO.MemoryStream(imageData);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// validate if its an image path, or find it, or null
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static async Task<byte[]?> ImageFromWebSafe(string url)
    {
        var realUrl = await ImageUrlFromWeb(url);

        using (HttpClient client = new HttpClient())
        {
            return await client.GetByteArrayAsync(realUrl);
        }

    }
    public static async Task<byte[]?> ImageFromWeb(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetByteArrayAsync(url);
        }

    }
    /// <summary>
    /// clean url of the actual image, or return null if it's not an image
    /// </summary>
    /// <returns></returns>
    public static async Task<string?> ImageUrlFromWeb(string url)
    {
        if (!string.IsNullOrWhiteSpace(url) &&
        (url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
         url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
         url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
        {
            return url;
        }
        if (url.Contains("pinterest"))
        {
            var html = await GetHtml(url);
            var newUrl = FindPinterestImageUrl(html);
            if (html != null && newUrl != null)
            {
                return newUrl;
            }
            else
                return null;
        }
       
        else
            return url;
    }
    public static async Task<string?> GetHtml(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                string html = await client.GetStringAsync(url);
                return html;
            }
            catch (HttpRequestException e)
            {
                // Manejar excepciones si no se puede obtener el HTML
                Console.WriteLine($"Error al obtener HTML: {e.Message}");
                return null;
            }
        }
    }
    public static string? FindPinterestImageUrl(string html)
    {
        if (html == null)
            return null;

        // Definir una expresión regular para buscar URLs de imágenes
        string pattern = "https:\\/\\/i\\.pinimg\\.com\\/[^\\s\"']+\\.(jpg|jpeg|png)";

        // Buscar en el HTML
        Match match = Regex.Match(html, pattern);
        if (match.Success)
        {
            return match.Value;
        }

        return null;
    }







    public static async Task<bool> GitClone(string sourceUrl, string destinationPath, CheckoutProgressHandler? progress = null)
    {
        await Task.Run(() =>
        {
            try
            {
                // Asegúrate de que la URL termina con .git
                if (!sourceUrl.EndsWith(".git"))
                {
                    sourceUrl += ".git";
                }

                // Extrae el nombre del repositorio de la URL
                var repoName = GetRepoNameFromUrl(sourceUrl);

                // Construye el path final incluyendo el nombre del repositorio
                var finalDestinationPath = Path.Combine(destinationPath, repoName);

                var cloneOptions = new CloneOptions();

                if(progress != null)
                cloneOptions.OnCheckoutProgress = progress;

                Debug.WriteLine($"Clonando {sourceUrl} en {finalDestinationPath}...");
                Repository.Clone(sourceUrl, finalDestinationPath);
                Debug.WriteLine("Git Cloned");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al clonar el repositorio: {ex.Message}");
                return false;
            }
        });

        return false;
    }

    private static string GetRepoNameFromUrl(string url)
    {
        // Elimina el sufijo opcional ".git"
        var strippedUrl = url.EndsWith(".git") ? url.Substring(0, url.Length - 4) : url;

        // Extrae el nombre después del último '/' en la URL
        var repoName = strippedUrl.Substring(strippedUrl.LastIndexOf('/') + 1);

        return repoName;
    }






    public static Dictionary<string, string> ExtractQuery(string urlString)
    {
        Uri uri = new Uri(urlString);
        string query = uri.Query.TrimStart('?');

        Dictionary<string, string> queryParams = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(query))
        {
            string[] pairs = query.Split('&');
            foreach (string pair in pairs)
            {
                string[] parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    string key = Uri.UnescapeDataString(parts[0]);
                    string value = Uri.UnescapeDataString(parts[1]);
                    queryParams[key] = value;
                }
            }
        }

        return queryParams;
    }


    public static string ExtractQuery(string urlString, string queryName)
    {
        // Parsea la URL para obtener la consulta
        Uri uri = new Uri(urlString);
        string query = uri.Query;

        // Extrae los pares clave-valor de la consulta
        var queryParts = HttpUtility.ParseQueryString(query);

        // Busca específicamente el valor para 'install_asset'
        string installAssetValue = queryParts[queryName];

        // Decodifica el valor para convertir los caracteres codificados en sus representaciones de caracteres
        string decodedInstallAssetValue = HttpUtility.UrlDecode(installAssetValue);

        return decodedInstallAssetValue;
    }

    // Método helper para remover caracteres no válidos
    public static string RemoveInvalidFileNameChars(string fileName)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c.ToString(), ""); // Puedes reemplazarlo con un guión bajo o cualquier otro caracter si lo prefieres
        }
        return fileName;
    }


    public static string GetFileNameWithoutExtension(string videoUrl)
    {
        try
        {
            // Crear una instancia de Uri para analizar la URL
            Uri uri = new Uri(videoUrl);

            // Obtener la última parte del path de la URL, que suele ser el nombre del archivo
            string fileName = Path.GetFileName(uri.LocalPath);

            // Devolver el nombre del archivo sin la extensión
            return Path.GetFileNameWithoutExtension(fileName);
        }
        catch (UriFormatException)
        {
            // Si la URL no es válida, manejar la excepción y devolver null
            return null;
        }
    }
    public static string GetFileName(string videoUrl)
    {
        try
        {
            // Crear una instancia de Uri para analizar la URL
            Uri uri = new Uri(videoUrl);

            // Obtener la última parte del path de la URL, que suele ser el nombre del archivo
            string fileName = Path.GetFileName(uri.LocalPath);

            // Devolver el nombre del archivo sin la extensión
            return Path.GetFileName(fileName);
        }
        catch (UriFormatException)
        {
            // Si la URL no es válida, manejar la excepción y devolver null
            return null;
        }
    }


    public static string GetUniqueFileName(string directory, string fileName)
    {
        string filePath = Path.Combine(directory, fileName);
        int count = 1;

        // Si el archivo ya existe, genera un nombre único
        while (File.Exists(filePath))
        {
            string tempFileName = $"{Path.GetFileNameWithoutExtension(fileName)}({count++}){Path.GetExtension(fileName)}";
            filePath = Path.Combine(directory, tempFileName);
        }

        return Path.GetFileName(filePath); // Devuelve solo el nombre del archivo, no el path completo
    }


}






