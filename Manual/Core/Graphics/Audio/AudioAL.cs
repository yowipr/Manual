using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Manual.Core.Graphics.Audio;



public class AudioPlayer
{
    private int buffer;
    private int source;
    private int state;

    public AudioPlayer()
    {
        string defaultDevice = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);

        // Inicializa OpenAL
        var device = ALC.OpenDevice(null);
        if (device == IntPtr.Zero)
        {
            throw new Exception("Failed to open the default OpenAL device.");
        }

        var context = ALC.CreateContext(device, (int[])null);
        if (context == IntPtr.Zero)
        {
            throw new Exception("Failed to create OpenAL context.");
        }

        ALC.MakeContextCurrent(context);

        // Genera un buffer y una fuente
        buffer = AL.GenBuffer();
        source = AL.GenSource();
    }

    public async void LoadAndPlayAudio(string audioFilePath)
    {
        if (!File.Exists(audioFilePath)) throw new Exception("audio doesn't exist");

        // Carga el archivo WAV
        var audioData = LoadWaveCorrectly(File.Open(audioFilePath, FileMode.Open), out int channels, out int bits, out int rate);

        // Fija el arreglo en la memoria
   //     GCHandle handle = GCHandle.Alloc(audioData, GCHandleType.Pinned);
   //     IntPtr dataPtr = handle.AddrOfPinnedObject();

        // Calcula el tamaño de los datos en función de los bits por muestra
   //     int bufferSize = audioData.Length; // audioData ya es un byte array, así que su longitud es correcta en bytes

        // Asigna datos al buffer usando un puntero nativo
        var format = GetSoundFormat(channels, bits);

        AL.BufferData(buffer, format, audioData, rate);
      //  AL.BufferData(buffer, format, dataPtr, bufferSize, rate);
        CheckOpenALError("BufferData");
        
      
        // Libera el handle una vez que hemos pasado los datos
     //   handle.Free();

        // Asigna el buffer a la fuente
        AL.Source(source, ALSourcei.Buffer, buffer);
        CheckOpenALError("Source buffer assignment");

        // Reproduce el audio
        AL.SourcePlay(source);
        CheckOpenALError("SourcePlay");

        // Espera a que termine la reproducción
        do
        {
          //  Core.Output.Log(".");
            Thread.Sleep(150);
            AL.GetSource(source, ALGetSourcei.SourceState, out state);
        } while ((ALSourceState)state == ALSourceState.Playing);

        Output.Log("\nReproducción completa.");

        // Limpieza
        AL.SourceStop(source);
        AL.DeleteSource(source);
        AL.DeleteBuffer(buffer);
        ALC.DestroyContext(ALC.GetCurrentContext());
        ALC.CloseDevice(ALC.GetContextsDevice(ALC.GetCurrentContext()));
    }

    private byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
    {
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Lee el encabezado del archivo WAV
            reader.ReadChars(4); // "RIFF"
            reader.ReadInt32(); // Chunk size
            reader.ReadChars(4); // "WAVE"
            reader.ReadChars(4); // "fmt "
            reader.ReadInt32(); // Subchunk1Size
            reader.ReadInt16(); // AudioFormat
            channels = reader.ReadInt16(); // NumChannels
            rate = reader.ReadInt32(); // SampleRate
            reader.ReadInt32(); // ByteRate
            reader.ReadInt16(); // BlockAlign
            bits = reader.ReadInt16(); // BitsPerSample

            reader.ReadChars(4); // "data"
            int dataSize = reader.ReadInt32(); // Tamaño del subchunk de datos

            // Lee los datos del audio
            return reader.ReadBytes((int)reader.BaseStream.Length);
        }
    }
    private byte[] LoadWaveCorrectly(Stream stream, out int channels, out int bits, out int rate)
    {
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Lee el encabezado del archivo WAV
            string chunkID = new string(reader.ReadChars(4)); // "RIFF"
            if (chunkID != "RIFF")
                throw new NotSupportedException("El archivo no es un archivo WAV válido.");

            int fileSize = reader.ReadInt32(); // Chunk size

            string riffType = new string(reader.ReadChars(4)); // "WAVE"
            if (riffType != "WAVE")
                throw new NotSupportedException("El archivo no es un archivo WAV válido.");

            // Lee el subchunk1 "fmt "
            string fmtID = new string(reader.ReadChars(4)); // "fmt "
            int fmtSize = reader.ReadInt32(); // Subchunk1Size
            int audioFormat = reader.ReadInt16(); // AudioFormat
            channels = reader.ReadInt16(); // NumChannels
            rate = reader.ReadInt32(); // SampleRate
            int byteRate = reader.ReadInt32(); // ByteRate
            int blockAlign = reader.ReadInt16(); // BlockAlign
            bits = reader.ReadInt16(); // BitsPerSample

            if (fmtID != "fmt " || audioFormat != 1) // Asegurarse de que es formato PCM
                throw new NotSupportedException("El archivo WAV no está en formato PCM.");

            // Leer hasta encontrar el chunk "data"
            string dataID;
            int dataSize = 0;

            // Salta chunks desconocidos hasta encontrar el chunk "data"
            do
            {
                dataID = new string(reader.ReadChars(4));
                dataSize = reader.ReadInt32();

                if (dataID != "data")
                {
                    // Salta este chunk si no es "data"
                    reader.BaseStream.Seek(dataSize, SeekOrigin.Current);
                }
            } while (dataID != "data" && reader.BaseStream.Position < reader.BaseStream.Length);

            if (dataID != "data")
                throw new NotSupportedException("El archivo WAV no contiene una sección de datos válida.");

            // Lee los datos de audio
            return reader.ReadBytes(dataSize);
        }
    }




    private ALFormat GetSoundFormat(int channels, int bits)
    {
        switch (channels)
        {
            case 1: return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
            case 2: return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
            default: throw new NotSupportedException("The specified sound format is not supported.");
        }
    }

    private static void CheckOpenALError(string operation)
    {
        ALError error = AL.GetError();
        if (error != ALError.NoError)
        {
            Output.Log($"OpenAL Error ({operation}): {AL.GetErrorString(error)}");
        }
    }
}




