using APICatalogo.Context;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Filters;
using APICatalogo.Logging;
using APICatalogo.Models;
using APICatalogo.RateLimitOptions;
using APICatalogo.Repositories;
using APICatalogo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

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

/*
 * CORS - Pol�tica de compartilhamento de recursos.
 * -> Permite que minha API seja executada remotamente por outros sites
 * de origens diferentes de acordo com as regras que eu definir.
 */

var OrigensComAcessoPermitido = "_origensComAcessoPermitido";//Nomeada -> AddPolicy

builder.Services.AddCors(options =>
{
    options.AddPolicy(OrigensComAcessoPermitido, //Torna nomeada 
        policy =>
        {
            policy.WithOrigins("https://localhost:7216"). //Habilita para esta origem
            WithMethods("GET", "POST"). // Habilita estes endpoints espec�ficos.
            AllowAnyHeader().// Qualquer header.
            AllowCredentials(); //Quaisquer credenciais.
        });
});

//Adiciona o servi�o de endpoints a minha API.
builder.Services.AddEndpointsApiExplorer();

//Adiciona o  servi�o do swagger a minha API.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "apicatalogo", Version = "v1" });

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
});


/* Adiciona o servi�o de authorization para definir as pol�ticas de nivel
 * de acesso aos usu�rios.
 */
builder.Services.AddAuthorization(options =>
{
    //Configura o n�vel de acesso Admin apenas para usuarios com a role Admin.
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    /*
     * Configura o n�vel de acesso SuperAdmin apenas para usuarios com as roles
     * Admin e a Claim (id , danilo)
     */
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("Admin").
                                        RequireClaim("id", "danilo"));

    //Configura o n�vel de acesso User apenas para usuarios com a role User.
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    /*
     * Configura o n�vel de acesso ExclusivePolicyOnly para usuarios que
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
 * CONFIGURA��O DO IDENTITY
 * Adiciona as entidades IdentityUser como os usuarios e IdentityRole para
 * as fun��es correlatas.
 */
builder.Services.AddIdentity<AplicationUser, IdentityRole>().
    AddEntityFrameworkStores<AppDbContext>().
    AddDefaultTokenProviders();
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

/*
 * ************************************************************************************
 *                           RATE LIMITING
 * -> Limita a quantidade de requisi��es baseado nas configura��es definidas.
 * ************************************************************************************
 *                       TIPOS DE RATE LIMITERS
 *
 * Fixed Window Limiting (Limita��o de Janela Fixa) ->  Este tipo de limita��o
 * de taxa considera um intervalo de tempo fixo (por exemplo, 1 minuto) e permite
 * um n�mero definido de requisi��es dentro dessa janela de tempo. Se o n�mero de
 * requisi��es exceder o limite antes que a janela de tempo termine, as requisi��es
 * adicionais s�o bloqueadas at� que uma nova janela comece.
 *
 * Sliding Window Limiting (Limita��o de Janela Deslizante) -> Este m�todo combina
 * aspectos do Fixed Window e Token Bucket. A janela de tempo � considerada em uma
 * base cont�nua, recalculando o limite � medida que o tempo passa. Isso oferece um
 * controle mais din�mico, pois considera a quantidade de requisi��es que aconteceram
 * nos �ltimos N segundos.
 *
 * Leaky Bucket (Balde Furado) -> Essa abordagem utiliza uma analogia de um balde
 * que �vaza� de forma constante. As requisi��es entram no balde e s�o processadas
 * a uma taxa constante, e o balde tem uma capacidade m�xima. Se o balde encher, as
 * requisi��es adicionais s�o descartadas.
 *
 * Token Bucket (Balde de Tokens) -> Nesse m�todo, um �balde� cont�m tokens e cada
 * requisi��o consome um token. Os tokens s�o repostos a uma taxa fixa. Se n�o h�
 * tokens suficientes, a requisi��o � bloqueada at� que tokens sejam repostos.
 *
 * Concurrency Limit (Limite de Concorr�ncia) -> Em vez de limitar o n�mero de
 * requisi��es em um per�odo de tempo, este m�todo limita o n�mero de requisi��es
 * que podem ser processadas simultaneamente. Isso � �til para evitar sobrecarga
 * no servidor por m�ltiplas requisi��es sendo processadas ao mesmo tempo.
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
 *                              IMPEMENTA��O DO TOKEN JWT
 * ************************************************************************************
 */

/*
 * Obtem a SecretKey definida na se��o JWT de meu appsettings.json e salva 
 * em secretKey
 */

var secretKey = builder.Configuration["JWT:SecretKey"]
                  ?? throw new ArgumentException("Invalid secret key!");
/*
 * Adiciona o servi�o de autentica��o via token JWT a minha API
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
 *                  INJE��O DE DEPEND�NCIAS
 *
 * Adiciona/Injeta os reposit�rios padr�es, gen�ricos e UnityOfWork
 * a minha API.
 * ************************************************************************************
 */
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();
/* ************************************************************************************
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
 * ************************************************************************************
*/

//Adicionando logs personalizados a minha aplica��o.
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

app.UseStaticFiles();//Habilitar o suporte para arquivos est�ticos.

/*
 *                         USE ROUTING()
 *
 * Habilita o roteamento de requisi��es dentro da aplica��o. Ele configura o
 * middleware para que o ASP.NET Core possa mapear as requisi��es de entrada para 
 * os endpoints apropriados, que s�o definidos nos controladores (Controllers) ou 
 * nos endpoints diretamente configurados no c�digo.
 */
app.UseRouting(); // 

app.UseRateLimiter();

app.UseCors(OrigensComAcessoPermitido);//N�o nomeada -> AddDefaultPolicy.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())//Verificando se o ambiente � de desenvolvimento.
{
    app.UseSwagger();// Middleware do Swagger que fornece a documenta��o das API
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyAPI");
        c.InjectStylesheet("/swagger-ui/SwaggerDark.css");
    });// Middleware que define a interface do usu�rio para interagir
                       // com a API
}

app.UseHttpsRedirection();// Redirecionar as requisi��es http para https.

app.UseAuthorization();// Define os n�veis de autoriza��o para verificar as permiss�es
                       // de acesso.

app.MapControllers();//Mapeamento dos controladores da aplica��o.

app.Run();// Middleware final -> Ponta da request.
