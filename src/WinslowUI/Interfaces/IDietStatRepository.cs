using WinslowUI.DTOs;

namespace WinslowUI;

public interface IDietStatRepository
{
    Task<List<DietStatDto>> GetDietStatsAsync(DateTime beginDate, DateTime endDate);
}
