using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Whisper.net;
using Whisper.net.Ggml;
using NAudio.Wave;
using SkyLearnApi.Services.Interfaces;

namespace SkyLearnApi.Services.Implementation
{
    public class LocalTranscriptionService : ILocalTranscriptionService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocalTranscriptionService> _logger;
        private readonly string _modelPath;
        private static bool _isModelReady = false;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public LocalTranscriptionService(IWebHostEnvironment env, ILogger<LocalTranscriptionService> logger)
        {
            _env = env;
            _logger = logger;
            // Place model in a dedicated folder
            _modelPath = Path.Combine(_env.ContentRootPath, "WhisperModels", "ggml-tiny.bin");
            Directory.CreateDirectory(Path.Combine(_env.ContentRootPath, "WhisperModels"));
        }

        private async Task EnsureDependenciesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_isModelReady)
                {
                    if (!File.Exists(_modelPath))
                    {
                        _logger.LogInformation("Downloading Whisper tiny model (approx 39MB) to improve performance...");
                        using var httpClient = new HttpClient();
                        httpClient.Timeout = TimeSpan.FromMinutes(10);
                        var response = await httpClient.GetAsync("https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin");
                        response.EnsureSuccessStatusCode();
                        
                        using var modelStream = await response.Content.ReadAsStreamAsync();
                        using var fileWriter = File.OpenWrite(_modelPath);
                        await modelStream.CopyToAsync(fileWriter);
                        _logger.LogInformation("Whisper model downloaded successfully.");
                    }
                    _isModelReady = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<string> ConvertToWavAsync(string inputFilePath)
        {
            await EnsureDependenciesAsync();

            var tempWavPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            
            _logger.LogInformation("Converting {Input} to 16kHz mono WAV at {Output} using pure C# NAudio...", inputFilePath, tempWavPath);

            // Run synchronously on a background thread. Force garbage collection afterwards to save web server memory.
            await Task.Run(() => 
            {
                try
                {
                    using var reader = new MediaFoundationReader(inputFilePath);
                    var targetFormat = new WaveFormat(16000, 16, 1);
                    using var resampler = new MediaFoundationResampler(reader, targetFormat)
                    {
                        ResamplerQuality = 60
                    };
                    WaveFileWriter.CreateWaveFile(tempWavPath, resampler);
                }
                finally
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            });
            
            _logger.LogInformation("Conversion completed.");
            return tempWavPath;
        }

        public async Task<string> TranscribeAudioAsync(string wavFilePath)
        {
            await EnsureDependenciesAsync();

            _logger.LogInformation("Starting local transcription for {File}...", wavFilePath);

            var transcriptBuilder = new System.Text.StringBuilder();

            // Scope everything forcefully to release huge unmanaged objects (Whisper AI weights) as soon as possible
            await Task.Run(async () =>
            {
                try
                {
                    using var whisperFactory = WhisperFactory.FromPath(_modelPath);
                    using var processor = whisperFactory.CreateBuilder()
                        .WithLanguage("auto")
                        .Build();

                    using var fileStream = File.OpenRead(wavFilePath);
                    await foreach (var segment in processor.ProcessAsync(fileStream))
                    {
                        transcriptBuilder.AppendLine($"[{segment.Start}] -> [{segment.End}]: {segment.Text}");
                    }
                }
                finally
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            });

            _logger.LogInformation("Transcription completed successfully.");
            
            return transcriptBuilder.ToString();
        }
    }
}
