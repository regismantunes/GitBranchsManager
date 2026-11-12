# Plano: branches de task por repositório

> Status: implementado e validado em 12/11/2026. O documento preserva as decisões e o diagnóstico que orientaram a implementação; o resultado da validação está na seção 16.

## 1. Objetivo e limites

Substituir a branch global de uma task por associações entre repositório e branch. Uma task poderá usar, por exemplo, `feature/PROJ-123-api` em Api e `fix/PROJ-123-ui` em Web. Cada comando deverá receber a branch concreta do repositório que está processando.

O trabalho inclui:

- Persistir uma coleção de pares repositório/branch, com no máximo uma branch por repositório.
- Criar branches usando o formato configurado em `-bn` e registrar somente nomes efetivamente criados ou confirmados.
- Importar tasks ausentes localmente, obtendo metadados no Jira e descobrindo branches em origin.
- Adicionar `gbm -rb <TaskId>` para redescobrir e substituir todos os vínculos.
- Adaptar cadastro, consulta, builders, comandos Git, build, PRs, DI, fakes, testes e documentação.

Ficam fora do escopo: migração automática do JSON antigo, múltiplas branches da mesma task no mesmo repo, remotos além de origin, renomeação/exclusão de branches remotas, revisão geral do motor de build, reconciliação de heads de PRs já abertos e transação distribuída entre repositórios Git.

## 2. Compatibilidade com o projeto

Esta tabela registra o diagnóstico anterior à implementação, para contextualizar as mudanças efetuadas.

| Arquivo/área | Estado observado | Consequência para a implementação |
|---|---|---|
| Gbm/Persistence/Entities/TaskInfo.cs | Já existe alteração local substituindo BranchName por List<RepositoryBranch> Repositories; RepositoryBranch está no mesmo arquivo. | Completar essa direção, preservando o trabalho local e definindo invariantes. |
| Gbm/Persistence/Repositories/Interfaces/ITaskInfoRepository.cs | Ainda recebe string taskBranch em SaveAsync; o diff atual é apenas a quebra de linha final. | A alteração funcional do contrato ainda precisa ser feita. |
| TaskInfoRepository.cs | Constrói TaskInfo com uma string e substitui todo o registro; leitura compara ID sem diferenciar caixa, mas escrita compara com caixa. | Atualizar construção e separar metadados, upsert de vínculo e substituição integral; uniformizar identidade. |
| IServiceCollectionExtensions.cs | O arquivo é Path.Combine(basePath, "tasksinfo.json"). | Corrigir a referência original a taskinfo.json: o nome real é **tasksinfo.json**, no plural. |
| BranchesCommandArgsBuilder.cs | Retorna TaskBranch, Repositories, BranchOrigin e NoBuild. | Trocar branch global e arrays paralelos por alvos tipados. |
| SaveTaskInfoCommand.cs | Calcula/interroga branch, salva e chama -n via ConsoleApp.Current. | Usar serviço compartilhado de criação para transportar o nome solicitado sem persistir branch global. |
| SaveTaskInfoCommand.cs / norepo | Mesmo com norepo, o fluxo chega à chamada de -n. | Encerrar após salvar metadados quando a criação não foi solicitada. |
| JiraClient.cs | As duas consultas fabricam feature/{TaskId}; GetTaskInfoAsync também consulta o armazenamento local. | Jira deve fornecer metadados sem inventar branch. Centralizar a orquestração fora do cliente. |
| FakeJiraClient.cs | Sem credenciais, consulta direta ao Jira retorna null; consulta combinada lê o armazenamento local. | Preservar uso offline de tasks salvas e diagnosticar importação indisponível. |
| GitTool.cs | Verifica uma branch remota, mas não enumera heads por task. RunGitAsync retorna stdout sem validar exit code. | Adicionar enumeração que distinga erro de sucesso vazio; confirmar sucesso antes de persistir criação. |
| GitTool.cs / DI | IGitTool é singleton com WorkingDirectory e ShowGitOutput mutáveis. | Novos fluxos devem ser sequenciais ou usar diretório explícito sem alterar estado compartilhado. |
| SetTaskBranchesCommand.cs | main/master têm tratamento especial; checkout remoto faz fetch e cria tracking. | Separar descoberta somente leitura do checkout mutante e preservar o atalho legado. |
| OpenPullRequestsTaskCommandArgsBuilder.cs | Sem repos explícitos, acessa taskInfo.BranchName sem tratar task nula. | Resolver task/alvos antes de montar argumentos. |
| OpenPullRequestsTaskCommand.cs | Busca a task novamente e usa branch global em build, checkout e PR. | Consumir um snapshot consistente da task e seus alvos. |
| IBuildTool.BuildBranchAsync | Já recebe repo e branchName. | Reutilizar o contrato; adaptar chamadores. |
| IGitHubClient.CreatePullRequestAsync | Já recebe branchName separada de TaskInfo. | Passar branch do alvo; nenhuma mudança de API externa é necessária. |
| SendTaskBranchesToDevelopCommand.cs | Ignora repos terminados em sdk e integra usando PullOriginAsync após checkout de develop. | Preservar essa semântica: a origem é remota, não um merge direto da branch local. |
| Testes existentes | Persistência/builders constroem TaskInfo com string; FakeGitTool costuma retornar sucesso fixo. | Adaptar fixtures e registrar chamadas com repo/branch, incluindo falhas configuráveis. |

O diagnóstico inicial identificou a incompatibilidade entre o modelo proposto e consumidores da branch global. A implementação atualizou esses consumidores conjuntamente; o build e os testes finais confirmam a integração dos contratos novos.

## 3. Decisões de comportamento

As decisões abaixo completam lacunas do plano inicial. São escolhas de implementação propostas nesta revisão, e não descrições de funcionalidades já existentes.

### 3.1 Identidade e fonte de verdade

- Comparar IDs de task e nomes de repos com StringComparer.OrdinalIgnoreCase, tanto na leitura quanto na escrita/seleção.
- Preservar grafia canônica do repo enumerado sob BasePath e do ID já armazenado ou retornado pelo Jira.
- Preservar caixa e nome exato das branches; não aplicar ToLower(). Branches que diferem apenas por caixa são candidatas distintas.
- Armazenar BranchName relativo a refs/heads/, sem prefixo origin/ ou refs/heads/.
- Enumerar apenas repos Git filhos diretos de BasePath, como GetAllRepositoriesAsync já faz; não introduzir busca recursiva.
- Validar repos explícitos contra esse inventário. Rejeitar caminhos absolutos, travessia por .. e nomes inexistentes antes de mutações.
- Task salva usa seus vínculos como fonte de verdade. Não adivinhar outro nome quando uma branch mapeada desaparecer.
- Importar automaticamente somente quando a task estiver ausente. Uma task salva com lista vazia exige -rb ou criação explícita; não repetir descoberta em toda chamada.
- -bn serve para criação e nunca restringe descoberta ou reescreve vínculos existentes.
- -r remove somente a branch local e preserva o vínculo, permitindo novo checkout remoto por -s.

### 3.2 Task versus branch literal

A regra antiga de -s (“não está salvo, então é branch literal”) conflita com a importação automática. Adotar:

1. Task já salva tem precedência, salvo modo literal explícito.
2. main/master, como entradas literais legadas de -s, preservam o atalho para a principal disponível em cada repo.
3. Entrada não salva compatível com ID Jira, pelo padrão ^[A-Za-z][A-Za-z0-9_]*-[0-9]+$, segue importação de task.
4. Outras entradas são branches literais, preservando caixa e nome completo.
5. Acrescentar -s --branch <Branch> [Repos...] para forçar branch literal, inclusive quando chamada PROJ-123 ou homônima de task salva. Nesse modo, main/master também são nomes exatos; não consultar Jira nem persistir task.

Aplicar a mesma classificação a -u --origin <TaskId/Branch> e acrescentar --origin-branch <Branch> para forçar origem literal. As duas opções de origem são mutuamente exclusivas. Os comandos que recebem somente TaskId não precisam da heurística: consultam o ID diretamente.

Essas opções resolvem ambiguidades introduzidas pela importação. Atualizar ajuda e testes do parser. O padrão acima é apenas classificação de entrada ambígua; o filtro remoto continua sendo substring, sem regex.

### 3.3 Criação versus vínculo existente

- Em -n, reutilizar a branch mapeada quando o repo já tiver vínculo.
- Para repo sem vínculo, gerar nome usando -bn.
- Alterar -bn não deve criar uma segunda branch para repos já associados.
- -t -branch <nome> é uma solicitação explícita de criação/vinculação nos repos selecionados. Não cria uma propriedade global da task.
- Se essa solicitação substituir um vínculo, manter a branch antiga no Git. Renomeação/exclusão não faz parte desse comando.

## 4. Modelo e persistência

### 4.1 Estrutura

Manter a direção da edição local:

```csharp
public record TaskInfo(
    string Id,
    string Summary,
    string Description,
    string Url,
    List<RepositoryBranch> Repositories);

public record RepositoryBranch(string Repository, string BranchName);
```

RepositoryBranch pode ir para arquivo próprio. Copiar coleções nas fronteiras de leitura/escrita, evitando que um consumidor altere acidentalmente o snapshot de outro.

Exemplo de <BasePath>/tasksinfo.json:

```json
[
  {
    "Id": "PROJ-123",
    "Summary": "Ajustar autenticação",
    "Description": "Descrição da tarefa",
    "Url": "https://example.atlassian.net/browse/PROJ-123",
    "Repositories": [
      { "Repository": "Api", "BranchName": "feature/PROJ-123-api" },
      { "Repository": "Web", "BranchName": "fix/PROJ-123-ui" }
    ]
  }
]
```

Invariantes:

- Repositories nunca é null; lista vazia é válida.
- Repo/branch não podem ser vazios ou somente espaços.
- No máximo um vínculo por repo, sem diferenciar caixa. Duplicatas inconsistentes no JSON devem gerar diagnóstico, sem escolher valor arbitrário.
- No máximo um registro por TaskId, sem diferenciar caixa.
- Alterar um vínculo preserva demais repos, demais tasks e metadados.
- Description nula do Jira vira string.Empty, como hoje.
- A URL continua derivada do domínio Jira configurado; não introduzir configuração obrigatória nova.

### 4.2 Contratos propostos

Substituir SaveAsync(..., string taskBranch, ...) por operações com intenção explícita:

```csharp
Task<TaskInfo?> GetAsync(string taskId, CancellationToken cancellationToken = default);
Task SaveMetadataAsync(string taskId, string summary, string description,
    CancellationToken cancellationToken = default);
Task UpsertRepositoryBranchAsync(string taskId, RepositoryBranch repositoryBranch,
    CancellationToken cancellationToken = default);
Task SaveSnapshotAsync(TaskInfo taskInfo, CancellationToken cancellationToken = default);
```

| Operação | Semântica |
|---|---|
| GetAsync | Só lê; não consulta Git/Jira, não cria arquivo nem migra. |
| SaveMetadataAsync | Cria com lista vazia ou altera metadados/URL preservando vínculos. |
| UpsertRepositoryBranchAsync | Exige task existente; altera somente o vínculo indicado. |
| SaveSnapshotAsync | Substitui metadados e toda a lista da task após importação/redescoberta completa. |

Os nomes podem mudar, mas essas semânticas não devem ser confundidas por um único parâmetro opcional cuja ausência apague vínculos.

### 4.3 Gravação, cancelamento e formato antigo

- Ler e validar o arquivo inteiro antes de modificá-lo.
- Serializar o estado novo com WriteIndented, gravar temporário exclusivo no mesmo diretório e substituir o destino apenas após escrita completa.
- Em erro, limpar somente o temporário da operação; não remover arquivos alheios.
- Verificar cancelamento antes da substituição. Depois dela, a escrita está concluída; não prometer desfazê-la.
- Serializar read-modify-write dentro da instância do repositório para evitar perda de atualização no mesmo processo.
- Coordenação entre dois processos gbm concorrentes fica fora do escopo inicial e deve ser documentada: substituição atômica sozinha não evita lost updates.

**Não implementar migração automática**, conforme a premissa original. Porém, incompatibilidade deve ser detectada:

- Registro antigo com BranchName na raiz da task e sem Repositories deve gerar erro de formato, orientando preservar cópia e recriar os dados com a versão nova.
- Não aceitar desserialização permissiva que produza Repositories null e a converta silenciosamente em lista vazia.
- Não sobrescrever formato legado, JSON malformado, raiz null, mistura de formatos ou registros inválidos.
- Arquivo inexistente, array [] e task nova com Repositories: [] são válidos.
- A mudança é incompatível com o arquivo antigo; retornar à versão anterior requer a cópia correspondente. Não prometer compatibilidade reversa.

## 5. Serviços e dependências

Introduzir componentes pequenos em Gbm/Services/Tasks/ (nomes propostos):

| Componente | Responsabilidade |
|---|---|
| ITaskResolver / TaskResolver | Obter task salva, importar ausente e coordenar redescoberta explícita. |
| ITaskBranchDiscovery / TaskBranchDiscovery | Enumerar repos e obter candidatos/diagnósticos, sem persistir ou alterar refs. |
| IBranchSelection / ConsoleBranchSelection | Selecionar candidata via MyConsole; permitir fake nos testes. |
| ITaskBranchTargetResolver / TaskBranchTargetResolver | Resolver alvos e origens conforme política do comando. |
| ITaskBranchCreator / TaskBranchCreator | Compartilhar criação e confirmação de vínculos entre -n e -t. |
| TaskBranchNameFactory | Gerar nomes de formato/entrada explícita, sem I/O. |

Não transformar cada DTO/helper em serviço com interface. Usar interfaces nas fronteiras que precisam de substituição em testes.

Dependências: comando/builder -> serviços de task -> Git/Jira/repositório. Persistência e Jira não dependem do resolver, evitando ciclos. Registrar serviços em AddAllServices e preservar atributos de comandos e IArgsBuilderAsync. Propagar CancellationToken em todo I/O.

Manter as configurações atuais: BasePath e JiraDomain para dados locais; credenciais Jira apenas para consultas Jira; token/owner GitHub apenas para PRs. Adicionar resolver aos comandos não pode tornar GitHub obrigatório em -l, -n, -ti ou -rb.

## 6. Descoberta remota

### 6.1 Contrato Git

Adicionar ao IGitTool uma operação como:

```csharp
public record RemoteBranchDiscoveryResult(
    bool HasOrigin,
    IReadOnlyList<string> BranchNames);

Task<RemoteBranchDiscoveryResult> GetRemoteBranchNamesAsync(
    string repository, CancellationToken cancellationToken = default);
```

Consultar git ls-remote --heads origin com diretório explícito, examinando exit code, stdout e stderr. HasOrigin false representa ausência comprovada do remoto; HasOrigin true com lista vazia representa consulta bem-sucedida sem heads. Falhas de consulta lançam exceção com contexto e não retornam nenhum desses resultados. O record e a assinatura acima representam, respectivamente, o DTO e o membro da interface; devem ficar nas declarações apropriadas.

- Parsear linhas <oid><whitespace>refs/heads/<nome>, removendo somente refs/heads/.
- Não assumir comprimento fixo do hash; aceitar apenas heads.
- Filtrar nomes com Contains(taskId, StringComparison.OrdinalIgnoreCase) depois da consulta.
- Manter a regra original de substring: PROJ-12 também encontra PROJ-123. Documentar e testar esse caso.
- Não usar padrões remotos do Git para simular filtro sem diferenciar caixa.
- Preservar nomes exatos, deduplicar com comparação ordinal e ordenar candidatas deterministicamente.
- Ordenar repos sem diferenciar caixa.
- Não executar fetch/checkout/pull/push/criação/exclusão durante descoberta.
- Não depender somente do cache refs/remotes/origin/*, que pode estar desatualizado.
- Detectar ausência de origin por inspeção explícita dos remotos; reportar separadamente como “sem remoto”.
- Sucesso com zero heads é resultado vazio; falha de rede/autenticação/processo é erro. Não converter falha em lista vazia.
- Saída inválida é erro de leitura, evitando remoções baseadas em descoberta incompleta.

Reutilizar o executor atual, acrescentando validação de exit code no caminho novo. Nos caminhos de criação/checkout utilizados pela feature, confirmar também sucesso real; os helpers atuais nem sempre o garantem. Evitar refatorar todas as operações Git sem necessidade.

Usar argumentos separados/escape adequado para entradas; validar branches destinadas à criação com git check-ref-format --branch. Não interpolar nomes como comandos de shell.

### 6.2 Escolha entre candidatas

| Quantidade no repo | Resultado |
|---|---|
| Zero | Nenhum vínculo; diagnóstico do repo. |
| Uma | Selecionar automaticamente. |
| Mais de uma | Exibir opções 1..N e 0 para cancelar toda a operação. |

Entrada vazia, não numérica ou fora do intervalo repete a pergunta sem gravar. Cancelar no segundo repo descarta também a escolha do primeiro. Não oferecer descarte silencioso de ambiguidade.

EOF deve cancelar, sem loop infinito. MyConsole.ReadLine atualmente converte EOF em string vazia: acrescentar leitura que preserve a distinção, mantendo acesso direto a Console encapsulado em MyConsole. Sem entrada disponível, uma ambiguidade deve terminar sem salvar.

Propagar cancelamento por token na enumeração, consulta e seleção. Se algum serviço alterar ShowGitOutput temporariamente, restaurar em finally.

### 6.3 Importação de task ausente

1. Validar sintaxe e repos explícitos antes de consultas externas.
2. Consultar GetAsync uma vez; se existir, retornar snapshot salvo.
3. Consultar IJiraClient.GetTaskInfoFromJiraAsync.
4. Retornar metadados com Repositories vazio (ou DTO próprio), removendo a fabricação de feature/{TaskId}.
5. Falta de autenticação, task inexistente ou falha Jira gera diagnóstico; não inventar metadados.
6. Descobrir em todos os repos elegíveis de BasePath, mesmo quando o comando recebeu filtro de repos.
7. Resolver todas as ambiguidades em memória.
8. Salvar um único snapshot de metadados e vínculos.
9. Aplicar o filtro de repos e a política do comando para montar os alvos.

Uma busca completa sem correspondências salva os metadados com lista vazia. Comando que exige branch deve interromper com orientação para -n/-rb; -ti pode exibir metadados e retornar sucesso; -n pode continuar para criação.

Falha de consulta ou cancelamento antes do commit não salva nem metadados nem escolhas parciais. Isso é diferente de busca concluída com zero matches. Task salva vazia não dispara nova importação implícita.

## 7. Alvos e políticas dos comandos

Substituir branch global e arrays paralelos por DTO:

```csharp
public record RepositoryBranchTarget(
    string Repository,
    string BranchName,
    string? OriginBranchName = null);
```

Builders retornam Targets e o contexto/flags necessários: TaskId/TaskInfo, NoBuild e PushLocalChanges. Chaves do dicionário devem corresponder aos parâmetros reais dos comandos. Incluir teste de binding pelo framework, não apenas inspeção do dicionário.

Representar separadamente o atalho legado de principal de -s: uma task cujo vínculo seja main deve continuar usando o nome exato, sem acionar esse atalho.

### 7.1 Seleção de repos

- Explícitos: deduplicar, validar todos e exigir vínculo para operações sobre branches existentes. Ausência de vínculo é erro, sem fallback para -bn.
- Automáticos: partir dos vínculos e verificar a branch correta em cada repo; não usar GetRepositoriesWithBranchAsync com um único nome global.
- Pasta mapeada ausente: reportar e excluir na seleção automática; falhar na seleção explícita. Não apagar vínculo automaticamente.
- Erro técnico de consulta aborta resolução; não significa branch inexistente.
- -n exige pelo menos um repo explícito e aceita repo ainda não vinculado.
- -rb não aceita filtro: substitui o conjunto completo.
- Resolver todos os alvos antes da primeira operação mutante. Uma falha de resolução não pode resultar em checkout/merge parcial.

Para -s em modo literal, os alvos são transitórios e não exigem vínculos persistidos. Sem repos explícitos, o atalho legado main/master considera todos os repos do inventário; outras branches literais selecionam os repos onde o nome exato existe localmente ou em origin. Com --branch, main/master são pesquisados como nomes exatos. Com repos explícitos, validar disponibilidade em todos os solicitados. Nunca salvar esses alvos em uma task apenas porque o nome literal contém um ID.

### 7.2 Disponibilidade e execução

| Comando | Pré-condição / seleção automática | Comportamento |
|---|---|---|
| -l | Vínculo com branch local. | Mostrar repo -> branch; não alterar vínculos. |
| -s | Branch local ou em origin. | Checkout, tracking se necessário e pull. |
| -u | Destino local; origem explícita disponível em origin. | Checkout e atualização pela principal/origem do repo. |
| -build | Branch local. | BuildBranchAsync(repo, branch). |
| -push | Branch local. | Todos os builds habilitados antes de qualquer push; depois checkout/push por alvo. |
| -pull | Branch local. | Checkout e pull; não criar tracking implicitamente. |
| -d | Branch local e disponível em origin; excluir *sdk. | Checkout/pull develop, PullOriginAsync(branch), build opcional e push. |
| -r | Branch local. | Excluir branch local exata; manter vínculo. |
| -pr | Branch local para novas operações de PR. | Build, checkout, push opcional e criação com branch do alvo. |

Preservar o requisito local dos comandos atuais, exceto -s, que já considera remotos. Orientar -s quando só houver branch remota. Em seleção automática, relatar vínculos ignorados; em seleção explícita, pré-condição ausente é erro.

Se nenhum alvo elegível restar, terminar com mensagem específica sem mutações. Em -d, *sdk é exclusão deliberada mesmo quando repo foi explícito; se todos forem excluídos por essa regra, retornar 0 como no-op informado.

Importação pode estar salva antes de se descobrir que nenhum alvo tem branch local. Isso não é execução parcial de Git; o usuário poderá usar -s com os vínculos importados.

## 8. Criação e cadastro

### 8.1 -n

1. Validar repos e carregar/importar task.
2. Reutilizar vínculo existente; para repo não vinculado, gerar nome com BranchDefaultNameFormat substituindo {TaskId}/{TaskSummary}.
3. Se formato ausente ou nome inválido, falhar antes de mutações e orientar -bn ou -t -branch. Não inventar feature/{TaskId}.
4. Não introduzir slugificação silenciosa: se summary gerar nome inválido, diagnosticar.
5. Validar todos os nomes antes de criar a primeira branch.
6. Processar repos sequencialmente:
   - Local existente: checkout, verificar sucesso e confirmar vínculo.
   - Somente remota: criar tracking com o fluxo de checkout remoto; não criar branch divergente da principal.
   - Inexistente: checkout da principal do repo, pull bem-sucedido e criação.
   - Persistir vínculo somente depois de sucesso confirmado.
7. Se repo posterior falhar, manter vínculos dos concluídos e reportar quais faltam; não excluir branches como compensação.

Aqui a persistência é incremental porque Git já sofreu efeitos por repo. Descoberta, em contraste, permanece integral antes de salvar. Falha de gravação após sucesso Git deve informar repo/branch; repetição precisa reconhecer branch existente e ser idempotente.

### 8.2 -t

Preservar cadastro manual, consulta Jira para metadados faltantes, clipboard e seleção interativa de repos. Não fazer descoberta remota no simples cadastro manual de metadados.

- Carregar registro existente e preservar vínculos ao atualizar summary/description.
- norepo salva somente metadados, retorna 0 e não chama criação nem ConsoleApp.Current.RunCommandAsync.
- Sem repos e com resposta negativa à criação, salvar metadados e encerrar.
- -branch usa nome exato nos repos selecionados e substitui vínculo somente após criação/confirmação.
- usedefaultbranch exige formato válido e aplica esse formato aos repos selecionados.
- Sem opção de branch, perguntar nome apenas se houver criação; oferecer formato configurado quando existir.
- Rejeitar -branch com usedefaultbranch e norepo com repos/opções de branch. Sem repo não existe onde armazenar o nome; documentar essa incompatibilidade com o cadastro antigo de branch global.
- Validar combinações antes de salvar. Após metadados salvos, falha na criação pode deixar metadados e vínculos concluídos persistidos; informar o resultado.
- Substituir chamada indireta de -n pelo serviço compartilhado de criação, preservando o nome escolhido.

Ao adaptar SaveTaskInfoCommandArgsBuilder, corrigir os problemas adjacentes do parser: Contains/IndexOf precisam da mesma comparação, e a validação de valor ausente deve usar o tamanho da lista em processamento, não o array original. Opções sem valor devem gerar ArgsValidationException, sem erro de índice.

## 9. Origem por repositório em -u

- Sem --origin, manter GetMainChangesAsync para cada repo.
- Com origem task, resolver/importar uma única vez e buscar seu vínculo pelo nome de cada repo de destino.
- Exemplo: destino Api -> feature/PROJ-123-api; origem Api -> release/PROJ-100-api. Checkout da primeira e PullOriginAsync da segunda.
- Não unir por posição de array nem usar a branch da origem em Web na operação em Api.
- Origem sem vínculo em qualquer repo de destino falha antes do primeiro checkout/merge; não substituir por main nem omitir silenciosamente.
- Origem literal aplica nome exato em todos os repos selecionados; preservar --origin main/develop.
- Como a operação é pull origin <branch>, origem somente local não satisfaz o contrato; validar disponibilidade remota.
- Origem igual ao destino continua significando atualização a partir de origin.

## 10. Pull requests

O builder deve resolver task e alvos, inclusive para task ausente. O comando não consulta novamente Jira para obter outra branch.

Preservar as fases:

1. Identificar PRs já registrados por task/repo.
2. Validar builds dos repos com PR novo, salvo nobuild, usando branches dos alvos.
3. Somente após todos passarem, realizar checkout, push opcional e criação.
4. Salvar cada PR e atualizar links relacionados, incluindo PRs previamente salvos.

Regras:

- nopush suprime somente push; nobuild suprime somente build. Manter ordem documentada [nopush] [nobuild] [Repos...]; combinações inválidas devem gerar erro de argumento.
- CreatePullRequestAsync recebe target.BranchName; base continua sendo a principal do repo.
- Checkout falso impede push/criação, evitando operar sobre a branch já ativa.
- Preservar a prevenção de duplicata por IPullRequestInfoRepository.ExistsAsync(taskId, repo).
- PRs já existentes continuam podendo receber atualização dos links; não exigir build/checkout para essa atualização isolada.
- O arquivo de PRs permanece identificado por task/repo. -rb não muda head de PR existente; -pr continua tratando o PR salvo como existente após remapeamento. Reconciliar/substituir esse PR fica fora do escopo e deve estar documentado.
- Falha tardia não desfaz pushes/PRs já criados; reportar sucesso parcial.
- -pri continua consultando registros de PR sem importar task ou descobrir branches.

## 11. Redefinição e exibição

### 11.1 -rb

Criar ResetTaskBranchesCommand e builder em Commands/TaskInfoCommands, grupo Tasks, ordem proposta 2, após -ti. Rejeitar argumentos excedentes.

1. Ler metadados salvos; se ausentes, consultar Jira.
2. Enumerar todos os repos Git filhos diretos de BasePath.
3. Consultar origin e resolver todas as ambiguidades.
4. Montar uma lista inteiramente nova.
5. Mostrar vínculos adicionados, mantidos, alterados/removidos e repos sem match/origin.
6. Gravar um único snapshot após sucesso completo.
7. Retornar 0 mesmo com lista vazia, informando explicitamente as remoções.

Vínculos de pastas removidas de BasePath também desaparecem após redescoberta integral. BasePath inexistente/inacessível aborta sem salvar. Diretório acessível com zero repos é conjunto vazio válido e deve ser reportado.

Repo sem origin é excluído com diagnóstico; falha ao consultar origin configurado aborta toda a operação. Em erro/cancelamento antes da gravação, o JSON anterior fica byte a byte intacto.

-rb altera somente tasksinfo.json: não modifica checkout, HEAD, índice, arquivos, refs locais/de tracking ou remotas. Não fazer fetch durante redescoberta.

### 11.2 -ti

Usar resolver para carregar/importar task. Exibir summary, description, URL e uma linha repo -> branch para cada vínculo, ordenada por repo. Lista vazia aparece como “nenhum repositório vinculado” e retorna sucesso se os metadados foram obtidos.

## 12. Falhas e códigos de retorno

Toda saída usa MyConsole. Não capturar cancelamento como resultado vazio ou sucesso.

| Condição | Resultado |
|---|---|
| Argumentos ausentes/inválidos, repo explícito inválido/sem vínculo | ArgsValidationException -> middleware retorna 2. |
| Nenhum alvo elegível para operação de branch | Diagnóstico de resolução, retorno 2; metadados importados podem já estar salvos. |
| Jira indisponível/não configurado para importação, erro Git/I/O/JSON | Falha operacional, retorno 1, com contexto. |
| Cancelamento da seleção | Retorno 1 e nenhuma gravação parcial da descoberta. |
| Cancelamento por token | Propagar até fronteira que reporte cancelamento/retorno 1, sem sucesso. |
| -rb concluído sem matches | Retorno 0 e lista vazia persistida, remoções informadas. |
| -ti ou -t norepo com lista vazia | Retorno 0. |

Falha de rede não é erro de argumento. Checkout falso impede a próxima operação naquele repo. Em mutações multi-repo já iniciadas, registrar concluídos e não concluídos sem prometer rollback global.

## 13. Arquivos afetados

| Arquivos | Trabalho |
|---|---|
| Persistence/Entities/TaskInfo.cs; possível RepositoryBranch.cs | Modelo e invariantes. |
| Persistence/Repositories/Interfaces/ITaskInfoRepository.cs; TaskInfoRepository.cs | Contratos, validação, atualização parcial/snapshot e escrita segura. |
| Services/Tasks/ (novo) | Resolver, descoberta, seleção, criação, geração de nomes, DTOs. |
| Services/Git/IGitTool.cs; GitTool.cs | Enumeração remota e verificação de sucesso nos caminhos necessários. |
| Services/Jira/IJiraClient.cs; JiraClient.cs; FakeJiraClient.cs | Remover branch fabricada; preservar caminho offline. Remover consulta combinada antiga se não houver consumidores. |
| Services/Initialization/IServiceCollectionExtensions.cs | Registro de serviços sem ciclos nem credenciais extras. |
| Commands/BranchesCommands/BranchesCommandArgsBuilder.cs | Parser, literal/task, alvos e origens. Separar builder de criação se simplificar políticas. |
| Todos os comandos em Commands/BranchesCommands/ | Consumir alvos e respeitar falhas antes de operações seguintes. |
| Commands/TaskInfoCommands/SaveTaskInfoCommand*.cs | Cadastro sem branch global, norepo, parser e criação compartilhada. |
| Commands/TaskInfoCommands/ShowTaskInfoCommand.cs | Exibir/importar vínculos. |
| Commands/TaskInfoCommands/ResetTaskBranchesCommand*.cs (novo) | -rb e validação. |
| Commands/PullRequestCommands/OpenPullRequestsTaskCommand*.cs | Contexto único e branch por repo em todas as fases. |
| MyConsole.cs; Services/Middleware/CommandsMiddleware.cs | EOF/cancelamento onde necessário, preservando convenções de retorno. |
| Gbm.Unit.Tests/Shared/Fakes/ | Novos contratos, falhas configuráveis e histórico de chamadas. |
| Gbm.Unit.Tests/Commands/, Persistence/, Services/ | Testes da seção 15. |
| README.md, AGENTS.md e atributos dos comandos | Sintaxe, modelo, descoberta, incompatibilidades e limitações. |

Caminhos sem prefixo referem-se a Gbm/. Não alterar PullRequestInfo ou contrato do build somente porque TaskInfo mudou.

## 14. Ordem de implementação

1. Registrar diff inicial, mapear consumidores e eventual falha inicial de compilação. Não reverter arquivos do usuário.
2. Fechar modelo/persistência; adaptar construtores, implementações e fakes, sem manter duas fontes de verdade como solução final.
3. Implementar enumeração Git com erros distintos de vazio; remover branch fabricada no Jira; confirmar sucesso real de criação/checkout.
4. Implementar serviços de resolução, seleção, importação, classificação e alvos/origens.
5. Adaptar -t, -n, -ti e adicionar -rb; validar norepo, nomes explícitos e idempotência.
6. Migrar operações Git/build, preservando flags e exclusão sdk.
7. Migrar PRs, preservando fases, registros existentes e cross-links.
8. Executar verificações, revisar binding e atualizar documentação.

A troca de modelo atravessa muitos consumidores e exige adaptação coordenada. A entrega final não pode conter fallback silencioso para branch global.

## 15. Matriz de testes

### 15.1 Persistência

- Round-trip de várias tasks e repos com branches distintas, verificando todos os pares/metadados.
- Lista vazia válida; Repositories null/ausente, formato legado/misto e JSON malformado rejeitados sem alterar arquivo.
- IDs e repos duplicados com caixa diferente tratados explicitamente.
- Atualização de metadados preserva vínculos; upsert de api altera somente Api; snapshot remove ausentes.
- Salvar proj-123 não cria duplicata de PROJ-123.
- Falha/cancelamento antes da substituição preserva o arquivo e não remove temporários alheios.

### 15.2 Descoberta e resolução

- Parse de heads com barras/caixa e hashes válidos sem comprimento fixo; saída inválida gera erro.
- Filtro sem diferenciar caixa e caso de substring PROJ-12/PROJ-123.
- Zero refs, repo sem origin, erro de autenticação/rede e cancelamento são resultados distintos.
- Ordenação/deduplicação não mescla branches que diferem por caixa.
- Zero/uma/múltiplas candidatas; input inválido repete; 0 cancela; EOF não entra em loop.
- Cancelar no segundo repo após escolher no primeiro não grava.
- Task ausente consulta Jira e todos os repos; task salva não consulta Jira nem redescobre.
- Zero matches salva metadados vazios; falha em uma consulta não salva snapshot parcial.
- Sem credenciais Jira, tasks salvas funcionam e importação falha claramente.

### 15.3 Builders e binding

- Alvos Api -> feature/PROJ-123-api e Web -> fix/PROJ-123-ui em todos os comandos.
- Filtro de repo explícito, identidade sem caixa e deduplicação.
- Repo inexistente, fora de BasePath ou sem vínculo falha antes de mutações.
- Seleção local/remota segue a política por comando.
- -n exige repo; -rb rejeita argumentos excedentes.
- -s main/master, literal com caixa, task salva/desconhecida e --branch seguem precedência.
- --origin resolve por nome de repo; vínculo ausente falha antes de checkout; --origin-branch força literal.
- Opções sem valor, em caixa diferente, conflitantes ou duplicadas têm validação previsível.
- Teste pelo framework confirma binding de Targets, contexto e flags aos métodos com dependências fake.

### 15.4 Cadastro e criação

- -t norepo preserva vínculos, salva metadados e não chama Git/criação.
- -t -branch afeta somente repos selecionados, sem apagar a branch anteriormente vinculada.
- usedefaultbranch exige formato; combinações inválidas são rejeitadas antes da escrita.
- -n gera para repo novo e reutiliza vínculo existente mesmo após alteração de -bn.
- Local existente, somente remota e nova seguem caminhos distintos.
- Falha de pull/criação/checkout não persiste vínculo fictício.
- Falha de gravação após Git é reportada; repetição reconhece branch existente.
- Falha no segundo repo mantém primeiro confirmado e não altera repos ainda não processados.

### 15.5 Comandos e PRs

- Assertar argumentos e ordem por repo, não somente retorno 0/contagem total.
- Build recebe branch correta; push não publica nada se qualquer build prévio falhar; nobuild elimina builds.
- Checkout falso impede pull/push/PR na branch errada.
- -d ignora *sdk sem diferenciar caixa; PullOriginAsync recebe branch correta; build ocorre em develop após integração.
- -r exclui somente branch local e preserva vínculo.
- -pr usa branch correta em build/checkout/head; base vem do repo correspondente.
- nopush, nobuild, PR salvo, links relacionados e falhas parciais preservam semântica.
- -ti exibe todos os vínculos/URL/lista vazia; -pri permanece independente de descoberta.

### 15.6 Integração Git e -rb

Usar diretórios temporários e origins bare locais, sem Jira/GitHub de produção:

1. Criar dois repos com branches diferentes contendo o mesmo ID.
2. Criar branch apenas no remoto para comprovar independência de fetch prévio.
3. Descobrir/redescobrir e verificar snapshot.
4. Comparar antes/depois branch atual, HEAD, refs locais/de tracking e status --porcelain: -rb não altera nenhum.
5. Remover uma head remota e repetir; remover vínculo após sucesso completo.
6. Tornar origin inacessível; falhar mantendo JSON byte a byte.
7. Cobrir repo sem origin, pasta removida e conjunto vazio.
8. Executar -s na branch somente remota e verificar tracking; essa mutação pertence ao checkout, não à descoberta.

Expandir FakeGitTool para armazenar repo selecionado, disponibilidades e histórico ordenado (operação, repo, branch). Adicionar fakes de Jira, GitHub, build e seleção conforme necessário. Evitar rede/stdin real em testes unitários.

## 16. Validação e conclusão da entrega

O projeto usa net10.0, xunit.v3, Microsoft.NET.Test.Sdk e xunit.runner.visualstudio. No repositório inspecionado não há configuração versionada selecionando runner MTP; UseMicrosoftTestingPlatformRunner está comentado no projeto de testes. Não mudar de runner nesta feature.

Após implementação, com SDK .NET 10 compatível e respeitando eventuais configurações herdadas do ambiente:

```powershell
rtk dotnet build GitBranchManager.sln
rtk dotnet test GitBranchManager.sln --no-build
rtk git diff --check
```

Usar --no-build somente após build bem-sucedido da mesma configuração e sem edições posteriores. Falha de build/restauração não é falha de teste. Registrar testes executados/aprovados/falhos e limitações do ambiente.

Revisar referências restantes a BranchName, TaskBranch, BranchOrigin, new TaskInfo e SaveAsync. RepositoryBranch.BranchName e parâmetros locais de Git/build continuam válidos; não fazer substituição cega.

Checklist:

- [x] Nenhum consumidor depende de TaskInfo.BranchName ou fabrica branch para descoberta.
- [x] Metadados, upsert e snapshot têm semânticas distintas.
- [x] Formato antigo é rejeitado sem migração/sobrescrita silenciosa.
- [x] Importação/redescoberta só salva após consulta e seleção completas.
- [x] -rb não altera estado Git e distingue erro remoto de ausência de matches.
- [x] -t norepo, criação explícita e geração por formato estão documentados/testados.
- [x] -s/-u possuem desambiguação e preservam caixa.
- [x] Cada operação usa branch correta do repo e respeita falha de checkout.
- [x] DI/binding funcionam sem credenciais de serviços não usados.
- [x] Testes unitários e cenários temporários passam com resultados registrados.
- [x] Ajuda, README e AGENTS refletem mudanças e limites, inclusive PRs existentes.

Validação executada em 12/11/2026: `rtk proxy dotnet test GitBranchManager.sln --nologo --no-restore --verbosity quiet`, incluindo compilação da solução. Resultado: **Passed: 100, Failed: 0, Skipped: 0**. Os cenários cobrem persistência, resolução, criação, comandos, binding/DI pelo framework e Git real com origins bare temporários. O SDK ativo era 11.0.100, compilando e executando o target net10.0.

O build ainda emite avisos de nulabilidade/captura de parâmetros, recomendações xUnit sobre tokens de cancelamento em testes e o aviso NU1903 da dependência transitiva Newtonsoft.Json 10.0.3. Nenhuma dependência foi atualizada nesta feature. Jira/GitHub foram substituídos por fakes nos testes; não houve acesso aos serviços reais nem execução nos repositórios configurados pelo usuário.
