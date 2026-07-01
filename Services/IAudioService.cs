using System.Threading.Tasks;

namespace TaskNote.Services
{
    public interface IAudioService
    {
        Task PlayStartSoundAsync();
        Task PlayFinishSoundAsync();
        Task StopSoundsAsync();
    }
}
