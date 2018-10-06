using AutoMapper;
using WebAPIs.Dtos;
using WebAPIs.Models;

namespace WebAPIs.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
        }
    }
}
