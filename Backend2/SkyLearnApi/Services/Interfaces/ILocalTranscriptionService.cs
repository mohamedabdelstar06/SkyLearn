using System.Threading.Tasks;

namespace SkyLearnApi.Services.Interfaces
{
    public interface ILocalTranscriptionService
    {
        Task<string> ConvertToWavAsync(string inputFilePath);
        Task<string> TranscribeAudioAsync(string wavFilePath);
    }
}
