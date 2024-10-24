/*
 * ***********************************************************************************
 *
 * GERENCIANDO TOKENS PELA LINHA DE COMANDO COM dotnet user-jwts:
 *
 * dotnet user-jwts --help -> Comando para mostrar as opções de configuração;
 * dotnet user-jwts create -> Gera um tokenJWT(*) com um ID definido;
 * dotnet user-jwts list -> Lista todos os tokens JWT emitidos;
 * dotnet user-jwts key -> Obtem a chave de emissão do token JWT atual;
 * dotnet user-jwts remove ID -> Exclui o token JWT emitido através de seu ID;
 * dotnet user-jwts clear -> Limpa todos os tokens JWT emitidos;
 * dotnet user-jwts key --reset-force -> Redefine a chave de emissão do token
 * JWT atualmente em uso.
 *
 * (*) Registra autmaticamente um conjunto de números secretos <UserSecretsId>
 * exigidos pelo Secret Manager do .NET Core no arquivo de projeto .csproj e
 * define as configurações de autenticaçãono arquivo appsettings.json para o
 * desevolvimento.
 *
 * ***********************************************************************************
 *
 * INSTALAÇÃO DO ENTITY FRAMEWORK
 *
 * dotnet tool install --global dotnet-ef -> Instala o entity framework;
 * dotnet tool update --global dotnet-ef -> Atualiza o entity framework para
 * a versão mais estável;
 * dotnet ef -> Roda/ Verifica o entity framework instalado;
 *
 * ***********************************************************************************
 *
 * COMANDOS MIGRATIONS (LINHA DE COMANDO)
 *
 * dotnet ef migrations add NomeDaMigration -> Cria uma nova migration;
 * dotnet ef database update -> Atualiza o banco de dados com a migration criada;
 * dotnet ef migrations remove NomeDaMigration -> Remove a migration criada;
 *
 * ***********************************************************************************
 *
 * COMANDOS MIGRATIONS (CONSOLE DO GERENCIADOR DE PACOTES)
 *
 * Add-Migration NomeDaMigration  -> Cria uma nova migration;
 * Update-Database -> Atualiza o banco de dados com a migration criada;
 * Remove-Migration NomeDaMigration  -> Remove a migration criada;
 * 
 * ***********************************************************************************
 */