using APICatalogo.Models;
using AutoMapper;

namespace APICatalogo.DTOs.Mappings;

// Classe responsável pelo mapeamento automático de PRODUTOS e conversão de PRODUTOS DTO
public class ProdutoDTOMappingProfile : Profile
{
    public ProdutoDTOMappingProfile()
    {
        CreateMap<Produto, ProdutoDTO>().ReverseMap();
        CreateMap<Categoria, CategoriaDTO>().ReverseMap();
        CreateMap<Produto, ProdutoDTOUpdateRequest>().ReverseMap(); 
        CreateMap<Produto, ProdutoDTOUpdateResponse>().ReverseMap(); 
    }
}
