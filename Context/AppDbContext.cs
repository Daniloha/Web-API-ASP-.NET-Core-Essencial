using APICatalogo.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace APICatalogo.Context;

public class AppDbContext : IdentityDbContext<AplicationUser>
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /*Cria a collection Categorias do tipo Categoria para mapear a entidade e criar a
     * tabela no banco de dados*/
    public DbSet<Categoria> Categorias { get; set; }

    /*Cria a collection Produtos do tipo Produto para mapear a entidade e criar a
     * tabela no banco de dados*/
    public DbSet<Produto> Produtos { get; set; }

    // FLUENT API

    /* Fluent API -> Utilizado para editar e definir parâmetros das tabelas como tipos
     * e tamanhos de campos/propriedades. Sua função é semelhante a das Data Anotations
     * porém possui mais recursos, além de desacoplar a programação do BD. */
    protected override void OnModelCreating(ModelBuilder builder)
    {
        /*
         * OnModelCrearing -> configura as propriedades das classes
         * ApplyConfigurationsFromAssembly -> aplica as configurações das classes
         * */
        base.OnModelCreating(builder);
        //O que faz a linha abaixo: aplica as configurações das classes
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        //TABELA PRODUTOS

        //Define o limite de caracteres da propriedade Nome da tabela Produtos
        builder.Entity<Produto>().Property(p => p.Nome).
            HasMaxLength(80).IsRequired();

        //Define o limite de caracteres da propriedade Descricao da tabela Produtos
        builder.Entity<Produto>().Property(p => p.Descricao).
            HasMaxLength(300).IsRequired();

        //Define o limite de algarismos e precisão da propriedade preco da tabela Produtos
        builder.Entity<Produto>().Property(p => p.Preco).
            HasPrecision(10, 2).IsRequired();

        //Define o limite de caracteres da propriedade ImagemUrl da tabela Produtos
        builder.Entity<Produto>().Property(p => p.ImagemUrl).
            HasMaxLength(300).IsRequired();

        //Define o limite de caracteres e precisão da propriedade Estoque da tabela Produtos
        builder.Entity<Produto>().Property(p => p.Estoque).
            HasPrecision(6, 0).IsRequired();

        //TABELA CATEGORIAS

        //Define CategoriaId como chave primária
        builder.Entity<Categoria>().HasKey(c => c.CategoriaId);

        //Define o limite de caracteres da propriedade Nome da tabela Categoria
        builder.Entity<Categoria>().Property(c => c.Nome).
            HasMaxLength(80).IsRequired();

        //Define o limite de caracteres da propriedade ImagemUrl da tabela Categoria
        builder.Entity<Categoria>().Property(c => c.ImagemUrl).
            HasMaxLength(300).IsRequired();
    }
}