/*
 * ************************************************************************************
 *                              API CATALOGO
 * ************************************************************************************
 * Descricão: API de Catalogo de Produtos.
 * Versão: 1.0.0
 * Arquivo: Program.cs
 * Autor: Danilo Holanda Araujo
 * E-mail: danilo.h.araujo@gmail.com
 * Repositório: https://github.com/Daniloha/Web-API-ASP-.NET-Core-Essencial
 * 
 * ************************************************************************************
 */

using APICatalogo.Context;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Filters;
using APICatalogo.Logging;
using APICatalogo.Models;
using APICatalogo.RateLimitOptions;
using APICatalogo.Repositories;
using APICatalogo.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// Adição de serviços e funcionalidades ao controlador.
builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(ApiExceptionFilter));
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
}).AddNewtonsoftJson();

/*
 * ************************************************************************************
 * CORS - Política de compartilhamento de recursos.
 * ************************************************************************************
 * -> Permite que minha API seja executada remotamente por outros sites
 * de origens diferentes de acordo com as regras que eu definir.
 * ************************************************************************************
 */

var OrigensComAcessoPermitido = "_origensComAcessoPermitido";//Nomeada -> AddPolicy

builder.Services.AddCors(options =>
{
    options.AddPolicy(OrigensComAcessoPermitido, //Torna nomeada 
        policy =>
        {
            policy.WithOrigins("https://localhost:7216"). //Habilita para esta origem
            WithMethods("GET", "POST"). // Habilita estes endpoints específicos.
            AllowAnyHeader().// Qualquer header.
            AllowCredentials(); //Quaisquer credenciais.
        });
});

// Configuração necessária apenas para as minimal APIs.
builder.Services.AddEndpointsApiExplorer();

/*
 * ************************************************************************************
 *                          Swashbuckle.AspNetCoreSwaggerGen
 * ************************************************************************************
 * Gerador Swagger que cria objetos SwaggerDocument de modelos, controladore s e rotas.
 * ************************************************************************************
 */

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", 
        new OpenApiInfo { Title = "Apicatalogo",
                          Version = "v1",
                          Description = "API referente ao curso Web API ASP .NET Core Essencial (.NET 8)\r\n",
                          Contact = new OpenApiContact
                          {
                              Name = "Danilo",
                              Email = "danilo.h.araujo@gmail.com",
                              Url = new Uri("https://github.com/Daniloha/Web-API-ASP-.NET-Core-Essencial")
                          }
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer JWT ",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
                    {
                          new OpenApiSecurityScheme
                          {
                              Reference = new OpenApiReference
                              {
                                  Type = ReferenceType.SecurityScheme,
                                  Id = "Bearer"
                              }
                          },
                         new string[] {}
                    }
    });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});


/* Adiciona o serviço de authorization para definir as políticas de nivel
 * de acesso aos usuários.
 */
builder.Services.AddAuthorization(options =>
{
    //Configura o nível de acesso Admin apenas para usuarios com a role Admin.
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    /*
     * Configura o nível de acesso SuperAdmin apenas para usuarios com as roles
     * Admin e a Claim (id , danilo)
     */
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("Admin").
                                        RequireClaim("id", "danilo"));

    //Configura o nível de acesso User apenas para usuarios com a role User.
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    /*
     * Configura o nível de acesso ExclusivePolicyOnly para usuarios que
     * possuem a claim definida ou a role SuperAdmin.
     */
    options.AddPolicy("ExclusivePolicyOnly", policy =>
    {
        policy.RequireAssertion(AppDbContext => AppDbContext.User.HasClaim(Claim =>
                                        Claim.Type == "id" && Claim.Value == "danilo") ||
                                        AppDbContext.User.IsInRole("SuperAdmin"));
    });

});

/*
 * CONFIGURAÇÂO DO IDENTITY
 * Adiciona as entidades IdentityUser como os usuarios e IdentityRole para
 * as funções correlatas.
 */
builder.Services.AddIdentity<AplicationUser, IdentityRole>().
    AddEntityFrameworkStores<AppDbContext>().
    AddDefaultTokenProviders();
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

/*
 * ************************************************************************************
 *                           RATE LIMITING
 * -> Limita a quantidade de requisições baseado nas configurações definidas.
 * ************************************************************************************
 *                       TIPOS DE RATE LIMITERS
 *
 * Fixed Window Limiting (Limitação de Janela Fixa) ->  Este tipo de limitação
 * de taxa considera um intervalo de tempo fixo (por exemplo, 1 minuto) e permite
 * um número definido de requisições dentro dessa janela de tempo. Se o número de
 * requisições exceder o limite antes que a janela de tempo termine, as requisições
 * adicionais são bloqueadas até que uma nova janela comece.
 *
 * Sliding Window Limiting (Limitação de Janela Deslizante) -> Este método combina
 * aspectos do Fixed Window e Token Bucket. A janela de tempo é considerada em uma
 * base contínua, recalculando o limite à medida que o tempo passa. Isso oferece um
 * controle mais dinâmico, pois considera a quantidade de requisições que aconteceram
 * nos últimos N segundos.
 *
 * Leaky Bucket (Balde Furado) -> Essa abordagem utiliza uma analogia de um balde
 * que “vaza” de forma constante. As requisições entram no balde e são processadas
 * a uma taxa constante, e o balde tem uma capacidade máxima. Se o balde encher, as
 * requisições adicionais são descartadas.
 *
 * Token Bucket (Balde de Tokens) -> Nesse método, um “balde” contém tokens e cada
 * requisição consome um token. Os tokens são repostos a uma taxa fixa. Se não há
 * tokens suficientes, a requisição é bloqueada até que tokens sejam repostos.
 *
 * Concurrency Limit (Limite de Concorrência) -> Em vez de limitar o número de
 * requisições em um período de tempo, este método limita o número de requisições
 * que podem ser processadas simultaneamente. Isso é útil para evitar sobrecarga
 * no servidor por múltiplas requisições sendo processadas ao mesmo tempo.
 *
 * ************************************************************************************
 */
var myOptions = new MyRateLimitOptions();

builder.Configuration.GetSection(MyRateLimitOptions.MyRateLimit).Bind(myOptions);

builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddFixedWindowLimiter(policyName: "fixedwindow", options =>
    {
        options.PermitLimit = myOptions.PermitLimit;//1;
        options.Window = TimeSpan.FromSeconds(myOptions.Window);
        options.QueueLimit = myOptions.QueueLimit;//2;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

/*
 * ************************************************************************************
 *                                VERSIONAMENTO DA API
 * ************************************************************************************
 *
 * -> O versionamento de software é um sistema de numeração que ajuda a identificar
 * diferentes versões de um software ao longo do seu ciclo de desenvolvimento e
 * distribuição. Uma das convenções mais populares para versionamento é o SemVer
 * (Versionamento Semântico), que utiliza três números separados por pontos:
 * MAJOR.MINOR.PATCH.
 *
 * MAJOR (Versão Major) -> Refere-se a mudanças significativas no software que,
 * geralmente, resultam em incompatibilidade com versões anteriores. Incrementar
 * a versão major indica que a nova versão possui modificações que quebram a
 * compatibilidade com a API anterior ou introduzem mudanças fundamentais.
 *
 * MINOR (Versão Minor) -> Refere-se à adição de novas funcionalidades que são
 * compatíveis com as versões anteriores, sem modificar ou quebrar a API existente.
 * Incrementar a versão minor significa que houve uma expansão das funcionalidades
 * do software, mas de forma que os usuários existentes não precisam mudar seus
 * códigos para aproveitar as novidades.
 *
 * PATCH (Correção) -> Refere-se a correções de bugs, melhorias de segurança
 * ou pequenos ajustes que não alteram a API ou a funcionalidade existente de
 * forma significativa. Incrementar a versão patch significa que o software foi
 * atualizado para corrigir erros, mantendo a compatibilidade total com a
 * versão anterior.
 *
 * ************************************************************************************
 */

var temp = builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0); //Versão padrão da API (1, 0) = 1.0
    o.AssumeDefaultVersionWhenUnspecified = true; //Se não fornecermos outra versão, se usa a padrão.
    o.ReportApiVersions = true; //Permite informar as versões suportadas nos headers de response.
    o.ApiVersionReader = ApiVersionReader.Combine( //Informa como ler a versão da API.
                          new QueryStringApiVersionReader(),//Versionamento por Query string.
                          new UrlSegmentApiVersionReader());//Versionamento pela URL.
}).AddApiExplorer(options => //Corrige e adapta o versionamento no swagger.
{
    options.GroupNameFormat = "'v'VVV"; //modelo de formatação da versão "'v'major[.minor][-status]".
    options.SubstituteApiVersionInUrl = true;//Substitui a versão na URL no swagger.
});


/*
 * ************************************************************************************
 *                              IMPEMENTAÇÂO DO TOKEN JWT
 * ************************************************************************************
 */

/*
 * Obtem a SecretKey definida na seção JWT de meu appsettings.json e salva 
 * em secretKey
 */

var secretKey = builder.Configuration["JWT:SecretKey"]
                  ?? throw new ArgumentException("Invalid secret key!");
/*
 * Adiciona o serviço de autenticação via token JWT a minha API
 */
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
                           Encoding.UTF8.GetBytes(secretKey))
    };
});

/* ************************************************************************************
 *                  INJEÇÃO DE DEPENDÊNCIAS
 *
 * Adiciona/Injeta os repositórios padrões, genéricos e UnityOfWork
 * a minha API.
 * ************************************************************************************
 */
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();
/* ************************************************************************************
/*TEMPOS DE VIDA DE UMA INSTÂNCIA
 * Singleton -> Uma única instância é criada para todo o tempo de vida da aplicação
 * Essa mesma instância é reutilizada sempre que a dependência é requisitada.
 * Ideal para serviços que precisam compartilhar dados entre todas as requisições
 * e que são seguros para acesso simultâneo (thread-safe).
 *
 * Scoped -> Uma nova instância é criada para cada scope de requisição.
 * Em aplicações web, um novo scope é criado a cada requisição HTTP,
 * ou seja, a mesma instância é compartilhada durante toda a requisição.
 * Ideal para objetos que mantêm estado durante o processamento de uma
 * única requisição, mas não entre diferentes requisições.
 *
 * Transient -> Cada vez que a dependência é requisitada, uma nova instância é criada.
 * Usado para objetos que não mantêm estado e são leves, ou que precisam ser criados
 * sempre que requisitados.
 * É ideal para serviços que são de curta duração e que não precisam manter estado entre
 * as requisições.
 *
 * ************************************************************************************
*/

//Adicionando logs personalizados a minha aplicação.
builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
{
    LogLevel = LogLevel.Information
}));

/*
 * Injeta o serviço de mapeamento automático em minha API para a entidade Produto
 * e Produto DTO.
 */

builder.Services.AddAutoMapper(typeof(ProdutoDTOMappingProfile));


var app = builder.Build();//Instancia da aplicação WEB

//Responsável pela configuração dos Middlewares.

/*  MIDLDEWARES
 *  -> Middlewares são trechos de código que executam em cadeia sequencial para processar 
 *  a requisição e a resposta recebida pela API. Você pode criar e configurar quantas 
 *  middlewares quiser e adicionar quantos middlewares forem necessárias para o fluxo
 *  da aplicação.
 */

app.UseStaticFiles();//Habilitar o suporte para arquivos estáticos.

/*
 *                         USE ROUTING()
 *
 * Habilita o roteamento de requisições dentro da aplicação. Ele configura o
 * middleware para que o ASP.NET Core possa mapear as requisições de entrada para 
 * os endpoints apropriados, que são definidos nos controladores (Controllers) ou 
 * nos endpoints diretamente configurados no código.
 */
app.UseRouting(); // 

app.UseRateLimiter();

app.UseCors(OrigensComAcessoPermitido);//Não nomeada -> AddDefaultPolicy.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())//Verificando se o ambiente é de desenvolvimento.
{
    /*
     *                        Swashbuckle.AspNetCore.Swagger
     * Middleware do Swagger que fornece os objetos SwaggerDocument como endpoints JSON.
     */
    app.UseSwagger();

    /*
   *                        Swashbuckle.AspNetCore.SwaggerUI
   * Versão embutida da ferramenta de interface do usuário Swagger.
   */
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiCatalogo");//Adição de endpoints.
        c.InjectStylesheet("/swagger-ui/SwaggerDark.css");//Implementa darkMode ao swagger.
    });// Middleware que define a interface do usuário para interagir
                       // com a API
}

app.UseHttpsRedirection();// Redirecionar as requisições http para https.

app.UseAuthorization();// Define os níveis de autorização para verificar as permissões
                       // de acesso.

app.MapControllers();//Mapeamento dos controladores da aplicação.
// Mapeamento das rotas de meus endpoints.
app.MapControllerRoute("DefaultApi", "{controller=values}/v{version=apiVersion}/{id?}");

app.Run();// Middleware final -> Ponta da request.
