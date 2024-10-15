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
}).AddNewtonsoftJson();

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
/*TEMPOS DE VIDA DE UMA INST�NCIA
 * Singleton -> Uma �nica inst�ncia � criada para todo o tempo de vida da aplica��o
 * Essa mesma inst�ncia � reutilizada sempre que a depend�ncia � requisitada.
 * Ideal para servi�os que precisam compartilhar dados entre todas as requisi��es
 * e que s�o seguros para acesso simult�neo (thread-safe).
 * 
 * Scoped -> Uma nova inst�ncia � criada para cada scope de requisi��o.
 * Em aplica��es web, um novo scope � criado a cada requisi��o HTTP, 
 * ou seja, a mesma inst�ncia � compartilhada durante toda a requisi��o.
 * Ideal para objetos que mant�m estado durante o processamento de uma 
 * �nica requisi��o, mas n�o entre diferentes requisi��es.
 * 
 * Transient -> Cada vez que a depend�ncia � requisitada, uma nova inst�ncia � criada.
 * Usado para objetos que n�o mant�m estado e s�o leves, ou que precisam ser criados 
 * sempre que requisitados.
 * � ideal para servi�os que s�o de curta dura��o e que n�o precisam manter estado entre
 * as requisi��es.
 * 
/**************************************************************/

//Adicionando logs personalizados
builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
{
    LogLevel = LogLevel.Information
}));

/*
 * Injeta o servi�o de mapeamento autom�tico em minha API para a entidade Produto
 * e Produto DTO.
 */
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
