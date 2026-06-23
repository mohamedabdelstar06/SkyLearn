using SkyLearnApi.DTOs.Announcements;
using SkyLearnApi.DTOs.Common;
using System.Threading;
using System.Threading.Tasks;

namespace SkyLearnApi.Services.Interfaces
{
    public interface IAnnouncementService
    {
        Task<AnnouncementDetailsDto> CreateAnnouncementAsync(int userId, CreateAnnouncementRequestDto request, CancellationToken cancellationToken = default);
        Task<AnnouncementDetailsDto> UpdateAnnouncementAsync(int id, UpdateAnnouncementRequestDto request, CancellationToken cancellationToken = default);
        Task DeleteAnnouncementAsync(int id, CancellationToken cancellationToken = default);
        
        Task<AnnouncementDetailsDto> GetAnnouncementByIdAsync(int id, CancellationToken cancellationToken = default);
        
        Task<PaginatedList<AnnouncementListDto>> GetAllAnnouncementsAdminAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        
        Task<PaginatedList<AnnouncementListDto>> GetAllAnnouncementsForUserAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
        
        Task<PaginatedList<AnnouncementListDto>> GetActiveAnnouncementsForUserAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    }
}
