using System.Threading.Tasks;

namespace SkyLearnApi.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<object> GetAdminDashboardStatsAsync();
        Task<object> GetAdminOverviewAsync();
        Task<object> GetStudentDashboardStatsAsync(int studentId);
    }
}
