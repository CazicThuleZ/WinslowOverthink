using AutoMapper;
using DashboardService.DTOs;
using DashboardService.Entities;

namespace DashboardService.RequestHelpers;

public class MappingProfiles : Profile
{

    public MappingProfiles()
    {
        CreateMap<DietStat, DietStatDto>();
        CreateMap<DietStatDto, DietStat>();

        CreateMap<AccountBalance, AccountBalanceDto>();
        CreateMap<AccountBalanceDto, AccountBalance>();

        CreateMap<CryptoPrice, CryptoPriceDto>();
        CreateMap<CryptoPriceDto, CryptoPrice>();

        CreateMap<MealLog, MealLogDto>();
        CreateMap<MealLogDto, MealLog>();
    }
}
