using APICatalogo.Context;
using APICatalogo.Models;
using APICatalogo.Pagination;

namespace APICatalogo.Repositories;

public class ProdutoRepository : Repository<Produto>, IProdutoRepository
{
    public ProdutoRepository(AppDbContext context): base(context)
    {       
    }

    //public IEnumerable<Produto> GetProdutos(ProdutosParameters produtosParams)
    //{
    //    return GetAll()
    //        .OrderBy(x => x.Nome)
    //        .Skip((produtosParams.PageNumber - 1) * produtosParams.PageSize)
    //        .Take(produtosParams.PageSize).ToList();
    //}

    public async Task<PagedList<Produto>> GetProdutosAsync(ProdutosParameters produtosParams)
    {
        var produtos = await GetAllAsync();

        var produtosOrdenados = produtos.OrderBy(p => p.ProdutoId).AsQueryable();
        var resultado = PagedList<Produto>.ToPagedList(produtosOrdenados, produtosParams.PageNumber, produtosParams.PageSize);

        return resultado;
    }

    public async Task<PagedList<Produto>> GetProdutosFiltroPrecoAsync(ProdutosFiltroPreco produtosFiltroParams)
    {
        var produtos = await GetAllAsync();
        if(produtosFiltroParams.preco.HasValue && !string.IsNullOrEmpty(produtosFiltroParams.precoCriterio.ToString()))
        {
            if(produtosFiltroParams.precoCriterio.Equals("maior", StringComparison.OrdinalIgnoreCase)){

                produtos = produtos.Where(p => p.Preco > produtosFiltroParams.preco.Value).OrderBy(p => p.Preco);

            }else if(produtosFiltroParams.precoCriterio.Equals("menor", StringComparison.OrdinalIgnoreCase)){

                produtos = produtos.Where(p => p.Preco < produtosFiltroParams.preco.Value).OrderBy(p => p.Preco);

            }else if(produtosFiltroParams.precoCriterio.Equals("igual", StringComparison.OrdinalIgnoreCase)){

                produtos = produtos.Where(p => p.Preco == produtosFiltroParams.preco.Value).OrderBy(p => p.Preco);

            }
        }
        var produtosFiltrados = PagedList<Produto>.ToPagedList(produtos.AsQueryable(), produtosFiltroParams.PageNumber,
            produtosFiltroParams.PageSize);

        return produtosFiltrados;
    }

    public async Task<IEnumerable<Produto>> GetProdutosPorCategoriaAsync(int id)
    {
        var produtos = await GetAllAsync();

        return produtos.Where(p => p.CategoriaId == id);
    }
}