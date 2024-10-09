using APICatalogo.Context;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Filters;
using APICatalogo.Logging;
using APICatalogo.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Adição de serviços e funcionalidades ao controlador.
builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(ApiExceptionFilter));
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();

//Adiciona o  serviço do swagger a minha API
builder.Services.AddSwaggerGen();

/*
 * Cria a minha string de conexão com o banco de dados a partir do meu 
 * arquivo de configuração appsettings.json
 */
string mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");

/*
 * Cria o serviço de conexão com o meu banco de dados utilizando minha classe
 * de contexto do entityFramework baseado no RGBD escolhido e minha string de
 * conexão.
 */
builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseMySql(mySqlConnection,
                    ServerVersion.AutoDetect(mySqlConnection)));

//Adiciona o filtro de log para o controlado.
builder.Services.AddScoped<ApiLoggingFilter>();

/**************************************************************/
/*Adiciona/Injeta os repositórios padrões, genéricos e UnityOfWork a minha
 * API */
/**************************************************************/
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
/**************************************************************/

//Adicionando log personalizado
builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
{
    LogLevel = LogLevel.Information
}));

builder.Services.AddAutoMapper(typeof(ProdutoDTOMappingProfile));

var app = builder.Build();//Instancia da aplicação WEB

//Responsável pela configuração dos Middlewares.

/*  MIDLDEWARES
 *  -> Middlewares são trechos de código que executam em cadeia sequencial para processar 
 *  a requisição e a resposta recebida pela API. Você pode criar e configurar quantas 
 *  middlewares quiser e adicionar quantos middlewares forem necessárias para o fluxo
 *  da aplicação.
 */


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())//Verificando se o ambiente é de desenvolvimento.
{
    app.UseSwagger();// Middleware do Swagger que fornece a documentação das API
    app.UseSwaggerUI();// Middleware que define a interface do usuário para interagir
                       // com a API
}

app.UseHttpsRedirection();// Redirecionar as requisições http para https.

app.UseAuthorization();// Define os níveis de autorização para verificar as permissões
                       // de acesso.

app.MapControllers();//Mapeamento dos controladores da aplicação.

app.Run();// Middleware final -> Ponta da request.
