using APICatalogo.Context;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Filters;
using APICatalogo.Logging;
using APICatalogo.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Adi��o de servi�os e funcionalidades ao controlador.
builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(ApiExceptionFilter));
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();

//Adiciona o  servi�o do swagger a minha API
builder.Services.AddSwaggerGen();

/*
 * Cria a minha string de conex�o com o banco de dados a partir do meu 
 * arquivo de configura��o appsettings.json
 */
string mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");

/*
 * Cria o servi�o de conex�o com o meu banco de dados utilizando minha classe
 * de contexto do entityFramework baseado no RGBD escolhido e minha string de
 * conex�o.
 */
builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseMySql(mySqlConnection,
                    ServerVersion.AutoDetect(mySqlConnection)));

//Adiciona o filtro de log para o controlado.
builder.Services.AddScoped<ApiLoggingFilter>();

/**************************************************************/
/*Adiciona/Injeta os reposit�rios padr�es, gen�ricos e UnityOfWork a minha
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

var app = builder.Build();//Instancia da aplica��o WEB

//Respons�vel pela configura��o dos Middlewares.

/*  MIDLDEWARES
 *  -> Middlewares s�o trechos de c�digo que executam em cadeia sequencial para processar 
 *  a requisi��o e a resposta recebida pela API. Voc� pode criar e configurar quantas 
 *  middlewares quiser e adicionar quantos middlewares forem necess�rias para o fluxo
 *  da aplica��o.
 */


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())//Verificando se o ambiente � de desenvolvimento.
{
    app.UseSwagger();// Middleware do Swagger que fornece a documenta��o das API
    app.UseSwaggerUI();// Middleware que define a interface do usu�rio para interagir
                       // com a API
}

app.UseHttpsRedirection();// Redirecionar as requisi��es http para https.

app.UseAuthorization();// Define os n�veis de autoriza��o para verificar as permiss�es
                       // de acesso.

app.MapControllers();//Mapeamento dos controladores da aplica��o.

app.Run();// Middleware final -> Ponta da request.
