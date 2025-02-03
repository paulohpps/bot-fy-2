﻿using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos.Streams;

namespace bot_fy.Service
{
    public class AudioService
    {
        private readonly YoutubeClient youtube = new();

        public async Task<Process> ConvertAudioToPcm(string path_music, CancellationToken cancellationToken)
        {
            if (!File.Exists(path_music))
            {
                throw new FileNotFoundException($"O arquivo especificado não foi encontrado: {path_music}");
            }

            Process? ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = GetFfmpeg(),
                Arguments = $@"-i ""{path_music}"" -vn -q:a 2 -ac 2 -f s16le -ar 48000 -loglevel error -b:a 96k pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            ffmpeg.Exited += (s, e) => ffmpeg.Dispose();

            cancellationToken.Register(ffmpeg.Kill);

            return ffmpeg;
        }

        public string GetFfmpeg()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return $"{Directory.GetCurrentDirectory()}//ffmpeg//ffmpeg.exe";
            }
            return "ffmpeg";
        }

        private async Task<string> GetAudioUrl(string url)
        {
            try
            {
                StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);
                IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                return streamInfo.Url;
            }
            catch (VideoUnplayableException)
            {
                return await youtube.Videos.Streams.GetHttpLiveStreamUrlAsync(url);
            }
        }
    }
}
