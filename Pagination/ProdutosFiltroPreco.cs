namespace APICatalogo.Pagination
{
    public class ProdutosFiltroPreco : QueryStringParameters
    {
        public decimal? preco { get; set; }
        public string? precoCriterio { get; set; } // menor, maior ou igual
    }
}
