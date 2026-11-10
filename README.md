# MacroS — Automação de Propriedades e Documentação no CATIA V5

Conjunto de macros **CATScript** e uma interface **HTA (HTML Application)** para padronizar propriedades de peças e conjuntos no CATIA V5, editar metadados do modelo 3D a partir de um formulário, sincronizar campos de texto de um Drawing e exportar uma lista de materiais (BOM) em HTML.

> **Importante:** este README foi elaborado a partir dos arquivos presentes no diretório `MacroS.zip`. As macros não foram executadas dentro do CATIA; portanto, a documentação descreve o comportamento implementado no código, não uma validação funcional em ambiente real.

## Índice

- [Visão geral](#visão-geral)
- [Conteúdo do projeto](#conteúdo-do-projeto)
- [Macros e funcionalidades](#macros-e-funcionalidades)
- [Propriedades utilizadas](#propriedades-utilizadas)
- [Requisitos](#requisitos)
- [Instalação e configuração](#instalação-e-configuração)
- [Como executar](#como-executar)
- [Fluxo de dados](#fluxo-de-dados)
- [Limitações e pontos de atenção](#limitações-e-pontos-de-atenção)
- [Solução de problemas](#solução-de-problemas)
- [Boas práticas](#boas-práticas)

## Visão geral

O projeto reúne ferramentas para:

1. Criar propriedades personalizadas que estejam ausentes em peças e conjuntos.
2. Editar propriedades nativas e personalizadas por uma janela de formulário.
3. Atualizar campos de texto nomeados em um Drawing com dados do modelo 3D.
4. Numerar os componentes diretos de um conjunto por meio da propriedade `ITEM`.
5. Exportar a Bill of Materials (BOM) nativa do CATIA para HTML.
6. Analisar dimensões globais e principais de uma peça, atualizar propriedades dimensionais e calcular a massa.
7. Gerar um relatório de análise dimensional e estimativa do peso do bloco bruto sem alterar propriedades.
8. Identificar chapas e gravar automaticamente `PROCESSO` e `DIAM_ESP`.
9. Exportar arquivos STEP e DXF planificados das chapas de uma montagem.

As rotinas foram escritas em CATScript/VBScript e usam objetos da automação COM do CATIA. A interface gráfica é um arquivo HTA executado pelo `mshta.exe`, que troca dados com as macros por meio de `resultado.txt`.

## Conteúdo do projeto

| Arquivo | Finalidade |
|---|---|
| `AcessarProps.CATScript` | Abre o formulário de propriedades a partir de um Drawing, lê o modelo 3D associado, atualiza propriedades do modelo e textos específicos do Drawing e tenta salvar os documentos. |
| `ANALISE_DIMENSOES.CATScript` | Calcula dimensões nos eixos globais e principais de inércia, atualiza propriedades dimensionais e `PESO` quando a densidade é válida. |
| `BillOfMaterial.CATScript` | Exporta a BOM do conjunto ativo para um arquivo HTML na Área de Trabalho. |
| `DefinirCorteLaser.CATScript` | Percorre a montagem e define `PROCESSO` e `DIAM_ESP` para peças de chapa; cria `RELATORIO_PROCESSO.txt` junto ao CATProduct. |
| `Exportar_Chapas.CATScript` | Percorre a montagem e exporta STEP e DXF planificado das chapas para subpastas `STEP` e `DXF`, com relatório de processamento. |
| `JanelaPropriedades.hta` | Interface gráfica para visualizar e editar propriedades nativas e personalizadas. |
| `NumerarItens.CATScript` | Numera referências únicas entre os componentes diretos do conjunto ativo, gravando a propriedade personalizada `ITEM`. |
| `Propriedades.CATScript` | Cria propriedades padrão ausentes no modelo ativo e, em um conjunto, processa o conjunto raiz e seus componentes diretos. |
| `PropriedadesPeca.CATScript` | Abre o formulário a partir de uma peça ou de um componente selecionado na árvore de um conjunto e grava as propriedades retornadas. |
| `RelatorioAnaliseDimensoes.CATScript` | Calcula dimensões globais/principais, massa e estimativa de peso do bloco bruto; grava um relatório TXT sem alterar propriedades do CATIA. |
| `RelatorioAnaliseDimensoes.txt` | Exemplo/resultado de relatório da análise dimensional; é sobrescrito pela macro correspondente. |
| `resultado.txt` | Arquivo de intercâmbio temporário entre os scripts CATScript e a janela HTA. O arquivo incluído no ZIP contém dados de exemplo/execução anterior e não deve ser tratado como configuração permanente. |

## Macros e funcionalidades

### `AcessarProps.CATScript`

**Objetivo:** editar as propriedades do modelo 3D relacionado ao Drawing ativo e atualizar campos de texto do desenho.

Fluxo implementado:

1. Confirma que o documento ativo é um `DrawingDocument`.
2. Percorre as folhas e vistas em busca de uma vista generativa associada a um modelo 3D.
3. Identifica o objeto `Product` e tenta localizar o documento 3D proprietário.
4. Lê as propriedades nativas e personalizadas e grava os valores iniciais em `resultado.txt`.
5. Abre `JanelaPropriedades.hta` e aguarda seu fechamento.
6. Se o formulário retornar o status `OK`, grava as propriedades no modelo 3D.
7. Atualiza, pelo nome do objeto de texto no Drawing, os campos abaixo:
   - `NUM_PART` ← `PartNumber` do produto
   - `FOLHA_PDF` ← propriedade personalizada `PDF`
   - `ITEM` ← propriedade personalizada `ITEM`
   - `MATERIAL` ← propriedade nativa `DEFINITION`
   - `DESCRICAO` ← propriedade nativa `PRODUCT_DESCRIPTION`
   - `TRAT_SUP` ← propriedade personalizada `TRAT_SUP`
   - `TRAT_TERM` ← propriedade personalizada `TRAT_TERM`

   Os campos `Quant.` e `Obs.` do Drawing não são alterados pela macro.
8. Tenta salvar o documento 3D e, em seguida, o Drawing.

Os objetos de texto precisam ter exatamente os nomes `NUM_PART`, `FOLHA_PDF`, `ITEM`, `MATERIAL`, `DESCRICAO`, `TRAT_SUP` e `TRAT_TERM`. A macro percorre as folhas, vistas e coleções de textos do Drawing para localizá-los. A atualização só é considerada bem-sucedida quando o nome é encontrado e o texto é alterado.

**Atenção:** o código contém um caminho de pasta fixo na constante `PASTA`. Ele precisa ser ajustado para a instalação em que a macro será usada.

### `PropriedadesPeca.CATScript`

**Objetivo:** editar as propriedades de uma peça ou de um componente selecionado no CATIA.

- Em um `PartDocument`, usa o produto associado à peça aberta.
- Em um `ProductDocument`, exige que um objeto do tipo `Product` esteja selecionado na árvore.
- Carrega propriedades nativas (`Product Description`, `Nomenclature` e `Definition`) e personalizadas.
- Abre o formulário HTA, lê os dados retornados e tenta criar ou atualizar as propriedades.
- Exige que `ITEM` não esteja vazio.
- Ao final, informa a quantidade de propriedades contabilizadas e recomenda salvar o documento no CATIA.

**Atenção:** esta macro também utiliza caminhos absolutos específicos do computador para `JanelaPropriedades.hta` e `resultado.txt`. Altere-os antes de distribuir o projeto. A macro informa que o documento deve ser salvo, mas não chama `Save` no trecho analisado.

### `JanelaPropriedades.hta`

**Objetivo:** fornecer um formulário simples para editar os metadados.

O formulário contém:

- **Propriedades nativas:** `PRODUCT_DESCRIPTION`, `NOMENCLATURE` e `DEFINITION`.
- **Propriedades personalizadas:** `ITEM`, `REPOSICAO`, `TRAT_TERM`, `TRAT_SUP`, `DIAM_ESP`, `COMP`, `LARG`, `ALT`, `PROCESSO`, `PDF`, `DXF` e `STEP`.
- **OK:** valida `ITEM`, grava os valores com status `OK` e fecha a janela.
- **Cancelar:** grava o status `CANCELADO` e fecha a janela.
- **ESC:** tenta cancelar a operação e fechar a janela.

A interface e os scripts usam `resultado.txt` como protocolo simples de comunicação. O formulário carrega os dados iniciais somente quando a primeira linha do arquivo é `INICIAL`.

**Atenção:** o HTA contém uma constante `CAMINHO_RESULTADO` com caminho absoluto. Ela deve apontar para o mesmo arquivo configurado nas macros que o utilizam.

### `NumerarItens.CATScript`

**Objetivo:** atribuir valores sequenciais à propriedade personalizada `ITEM` dos componentes diretos do conjunto ativo.

- Requer um documento `ProductDocument`.
- Solicita um prefixo opcional. Por exemplo, `E` gera `E1`, `E2`, `E3`; sem prefixo, gera `1`, `2`, `3`.
- Percorre somente os filhos diretos de `doc.Product`.
- Não entra recursivamente nos componentes internos de subconjuntos.
- Evita numerar mais de uma vez a mesma referência de produto durante a execução.
- Atualiza a propriedade `ITEM` existente ou cria uma propriedade do tipo texto quando ela não existe.
- Apresenta a quantidade de referências processadas e o número de falhas reportadas.

**Importante:** a macro usa o produto raiz do documento ativo; ela não usa a seleção atual como raiz. Se a intenção for numerar um subconjunto selecionado, o código precisará ser adaptado para usar esse componente selecionado como raiz.

### `Propriedades.CATScript`

**Objetivo:** criar propriedades personalizadas padrão que estejam ausentes.

Em um documento de peça, processa o produto associado à peça. Em um documento de conjunto, processa o conjunto raiz e seus componentes diretos, sem percorrer recursivamente os níveis internos.

Propriedades comuns criadas quando ausentes:

- `ITEM` = `Item`
- `REPOSICAO` = `N/A`
- `TRAT_SUP` = `N/A`
- `TRAT_TERM` = `N/A`

Propriedades adicionais criadas quando o item é identificado como peça:

- `DIAM_ESP` = `-`
- `COMP` = `-`
- `LARG` = `-`
- `ALT` = `-`
- `PROCESSO` = `-`

A macro foi projetada para não sobrescrever propriedades que já existam.

### `BillOfMaterial.CATScript`

**Objetivo:** exportar a lista de materiais nativa do CATIA em HTML.

- Requer um documento `ProductDocument` ativo.
- Define colunas para propriedades nativas e personalizadas.
- Configura os formatos principal e secundário da BOM.
- Salva o HTML na Área de Trabalho do usuário, com nome semelhante a `BOM_<PartNumber>.html`.

Colunas configuradas:

`Part Number`, `Product Description`, `Nomenclature`, `Quantity`, `Definition`, `ITEM`, `REPOSICAO`, `TRAT_SUP`, `TRAT_TERM`, `DIAM_ESP`, `COMP`, `LARG`, `ALT`, `PROCESSO`, `PDF`, `DXF` e `STEP`.

A exportação utiliza o conversor nativo `BillOfMaterial` do CATIA. A disponibilidade e os nomes exibidos das propriedades dependem da configuração e da versão do CATIA.

### `ANALISE_DIMENSOES.CATScript`

**Objetivo:** calcular dimensões totais de um `CATPart` e atualizar propriedades do produto associado.

- Exige um `CATPart` ativo com geometria em `MainBody`.
- Calcula `COMP`, `LARG` e `ALT` nos eixos globais X, Y e Z, além de `DIM_PRINCIPAL_1`, `DIM_PRINCIPAL_2` e `DIM_PRINCIPAL_3` nos eixos principais de inércia. As dimensões são apresentadas em milímetros.
- Calcula a massa pela análise de inércia e atualiza `PESO` somente quando a densidade é positiva e diferente de 1, condição usada pelo script para considerar a densidade válida.
- Cria geometrias auxiliares temporárias para a bounding box e tenta removê-las ao final.
- Exibe os resultados em uma caixa de mensagem; não gera relatório TXT.

As propriedades são escritas no produto da peça. Revise os valores e salve o CATPart após a execução.

### `RelatorioAnaliseDimensoes.CATScript`

**Objetivo:** produzir um relatório dimensional e de massa para um `CATPart`, sem gravar propriedades no modelo.

- Calcula as mesmas dimensões globais e principais e a massa.
- Estima o peso do bloco bruto multiplicando o volume da bounding box global pela densidade; o valor é apresentado apenas quando a densidade é considerada válida.
- Exibe o relatório e o grava em `RelatorioAnaliseDimensoes.txt`, no caminho absoluto definido na constante `CAMINHO_RELATORIO`.
- Remove as geometrias auxiliares de análise ao final.

**Atenção:** ajuste `CAMINHO_RELATORIO` para a instalação em uso. A macro sobrescreve o TXT indicado a cada execução.

### `DefinirCorteLaser.CATScript`

**Objetivo:** analisar recursivamente as peças de uma montagem e classificar peças de chapa para atualizar propriedades de processo e espessura.

- Requer um `CATProduct` salvo. Percorre todos os níveis da árvore, processando referências de CATPart uma única vez.
- Para chapas sem dobra, grava `PROCESSO = LASER`; para chapas com dobra, grava `PROCESSO = LASER + DOBRA`.
- Lê `Thickness` do recurso Sheet Metal Parameter para preencher `DIAM_ESP`, preservando a unidade textual apresentada pelo CATIA.
- Grava as propriedades como texto em `Product.UserRefProperties` e salva os CATParts quando alguma propriedade é gravada.
- Ignora sólidos convencionais sem alterar suas propriedades. Não exporta STEP/DXF e não remove propriedades.
- Cria/sobrescreve `RELATORIO_PROCESSO.txt` na pasta do CATProduct, com contagens, erros e resultados.

A identificação depende dos recursos geométricos de chapa/dobra e do ambiente Generative Sheetmetal Design. Revise o relatório, especialmente entradas que não tiveram espessura identificada.

### `Exportar_Chapas.CATScript`

**Objetivo:** percorrer recursivamente um `CATProduct` salvo e exportar arquivos de fabricação das peças identificadas como chapa.

- Cria as pastas `DXF` e `STEP` ao lado do CATProduct, se necessário.
- Exporta cada chapa como STEP (`.stp`) e gera um DXF de planificação por meio de um Drawing temporário. A orientação do DXF usa os eixos H e V do primeiro Sketch de `PartBody`.
- Usa o nome-base do arquivo CATPart para nomear os exports e processa cada caminho de peça uma única vez.
- Classifica chapas sem dobra e com dobra apenas para o relatório; essa macro não atualiza propriedades CATIA.
- Para sólidos convencionais, não gera exports e remove STEP/DXF antigos de mesmo nome nas pastas de saída.
- Cria/sobrescreve `RELATORIO_PROCESSAMENTO.txt` junto ao CATProduct, listando contagens e falhas.

**Atenção:** arquivos de saída preexistentes com o mesmo nome são removidos antes da nova exportação; exports antigos também são removidos quando a peça deixa de ser reconhecida como chapa. Confira as pastas `DXF` e `STEP` após a execução.

## Propriedades utilizadas

| Nome no código | Tipo/uso | Valor padrão quando criado por `Propriedades.CATScript` |
|---|---|---|
| `PRODUCT_DESCRIPTION` | Propriedade nativa — descrição do produto | Não se aplica |
| `NOMENCLATURE` | Propriedade nativa — nomenclatura | Não se aplica |
| `DEFINITION` | Propriedade nativa — definição/material, conforme o padrão do projeto | Não se aplica |
| `ITEM` | Personalizada — identificação/número do item | `Item` |
| `REPOSICAO` | Personalizada — reposição | `N/A` |
| `TRAT_SUP` | Personalizada — tratamento superficial | `N/A` |
| `TRAT_TERM` | Personalizada — tratamento térmico | `N/A` |
| `DIAM_ESP` | Personalizada — diâmetro/espessura | `-` |
| `COMP` | Personalizada — comprimento | `-` |
| `LARG` | Personalizada — largura | `-` |
| `ALT` | Personalizada — altura | `-` |
| `PROCESSO` | Personalizada — processo de fabricação | `-` |
| `PDF` | Personalizada — referência/identificação de PDF | Sem padrão definido |
| `DXF` | Personalizada — referência/identificação de DXF | Sem padrão definido |
| `STEP` | Personalizada — referência/identificação de STEP | Sem padrão definido |
| `DIM_PRINCIPAL_1` | Personalizada — dimensão no primeiro eixo principal de inércia | Criada/atualizada por `ANALISE_DIMENSOES.CATScript`; sem padrão geral |
| `DIM_PRINCIPAL_2` | Personalizada — dimensão no segundo eixo principal de inércia | Criada/atualizada por `ANALISE_DIMENSOES.CATScript`; sem padrão geral |
| `DIM_PRINCIPAL_3` | Personalizada — dimensão no terceiro eixo principal de inércia | Criada/atualizada por `ANALISE_DIMENSOES.CATScript`; sem padrão geral |
| `PESO` | Personalizada — massa da peça, em kg conforme análise de inércia | Criada/atualizada por `ANALISE_DIMENSOES.CATScript` quando a densidade é válida |

Os nomes são identificadores usados diretamente no código. Preserve a grafia, os sublinhados e a ausência de acentos ao editar as macros ou configurar propriedades no CATIA.

## Requisitos

- Windows com CATIA V5 instalado.
- Permissão para executar macros CATScript e automação COM.
- `mshta.exe` disponível e permitido pelas políticas de segurança do Windows para as rotinas que usam a interface HTA.
- Permissão de leitura e gravação na pasta configurada para os scripts e para `resultado.txt`.
- Um documento compatível aberto no CATIA, conforme a macro executada.
- Para `AcessarProps.CATScript`, um Drawing com pelo menos uma vista generativa vinculada a um modelo 3D.
- Para `ANALISE_DIMENSOES.CATScript` e `RelatorioAnaliseDimensoes.CATScript`, um CATPart com geometria em `MainBody` e suporte do `SPAWorkbench`.
- Para `DefinirCorteLaser.CATScript` e `Exportar_Chapas.CATScript`, um CATProduct salvo com peças CATPart acessíveis e recursos de chapa compatíveis com Generative Sheetmetal Design.
- Para atualizar os textos do Drawing, os objetos de texto esperados precisam existir com os nomes configurados.

A disponibilidade de CATScript, HTA e automação pode variar conforme a versão do CATIA, a versão do Windows e as políticas corporativas.

## Instalação e configuração

1. Extraia os arquivos do ZIP para uma pasta local permanente. Evite executar o projeto diretamente de uma pasta temporária.
2. Escolha uma pasta para manter os arquivos, por exemplo:
   `C:\CAD\MacroS\`
3. Ajuste os caminhos absolutos existentes no código:
   - Em `AcessarProps.CATScript`, altere a constante `PASTA`.
   - Em `PropriedadesPeca.CATScript`, altere `caminhoJanela`, `caminhoResultado` e, se necessário, `caminhoMSHTA`.
   - Em `JanelaPropriedades.hta`, altere `CAMINHO_RESULTADO`.
4. Confirme que todos os arquivos que compartilham dados apontam para o **mesmo** `resultado.txt`.
5. Configure o acesso às macros no CATIA de acordo com as políticas da sua instalação.
6. Faça um teste inicial em cópias de uma peça, de um conjunto e de um Drawing.
7. Confirme os resultados e salve os documentos manualmente quando aplicável.

### Configuração recomendada dos caminhos

Sempre que possível, mantenha `JanelaPropriedades.hta` e `resultado.txt` na mesma pasta das macros e use um único caminho-base consistente. Os scripts atuais contêm caminhos absolutos; mover a pasta sem editar esses caminhos pode impedir a abertura da interface ou a troca de dados.

> O arquivo `resultado.txt` é um arquivo de trabalho sobrescrito a cada execução. Não o utilize como arquivo de histórico ou de armazenamento permanente.

## Como executar

### Preparação

1. Abra no CATIA o documento necessário.
2. Salve uma cópia de segurança antes de alterar propriedades em lote.
3. Execute a macro apropriada para o objetivo.
4. Leia as mensagens finais e verifique as propriedades no CATIA antes de considerar o processo concluído.

### Escolha da macro

| Necessidade | Macro |
|---|---|
| Editar propriedades e atualizar campos em um Drawing vinculado ao modelo 3D | `AcessarProps.CATScript` |
| Editar propriedades de uma peça aberta ou de um componente selecionado na árvore | `PropriedadesPeca.CATScript` |
| Criar propriedades padrão ausentes em peça/conjunto e componentes diretos | `Propriedades.CATScript` |
| Numerar `ITEM` dos componentes diretos do conjunto ativo | `NumerarItens.CATScript` |
| Exportar a BOM do conjunto ativo para HTML | `BillOfMaterial.CATScript` |
| Calcular dimensões e gravar dimensões/massa nas propriedades da peça | `ANALISE_DIMENSOES.CATScript` |
| Gerar relatório dimensional, massa e peso estimado do bloco bruto | `RelatorioAnaliseDimensoes.CATScript` |
| Classificar chapas e preencher `PROCESSO`/`DIAM_ESP` na montagem | `DefinirCorteLaser.CATScript` |
| Exportar STEP e DXF planificado das chapas de uma montagem | `Exportar_Chapas.CATScript` |

## Fluxo de dados

Nas macros que usam o formulário, o processo é:

1. O CATScript lê as propriedades atuais do CATIA.
2. O script grava um arquivo de texto com status `INICIAL` e pares `CHAVE=VALOR`.
3. A macro abre `JanelaPropriedades.hta` e aguarda a janela fechar.
4. A HTA lê os valores iniciais e exibe os campos.
5. Ao confirmar, a HTA grava status `OK` e os valores; ao cancelar, grava `CANCELADO`.
6. O CATScript lê o resultado e, quando o status é `OK`, tenta atualizar as propriedades no CATIA.

O formato é simples e não é adequado para valores que contenham quebras de linha. O código remove quebras de linha em alguns caminhos, mas não implementa um protocolo de escape completo para todos os caracteres especiais.

## Limitações e pontos de atenção

Estes pontos foram identificados pela leitura estática dos arquivos e devem ser considerados antes de usar o projeto em produção:

1. **Caminhos fixos:** além dos scripts de formulário (`AcessarProps.CATScript`, `PropriedadesPeca.CATScript` e `JanelaPropriedades.hta`), `RelatorioAnaliseDimensoes.CATScript` também usa um caminho absoluto para o TXT de saída.
2. **Arquivo compartilhado:** o uso de um único `resultado.txt` pressupõe que não haverá duas execuções simultâneas. Execuções concorrentes podem sobrescrever os dados umas das outras.
3. **HTA/MSHTA:** a interface depende de uma tecnologia antiga do Windows que pode estar bloqueada por políticas de segurança.
4. **Identificação do modelo no Drawing:** `AcessarProps.CATScript` usa a primeira vista generativa encontrada que permita obter um documento de origem. Em desenhos com várias vistas/modelos, confirme se essa seleção corresponde ao modelo que deseja editar.
5. **Nomes de textos fixos:** `NUM_PART`, `FOLHA_PDF`, `ITEM`, `MATERIAL`, `DESCRICAO`, `TRAT_SUP` e `TRAT_TERM` são nomes esperados pela macro no Drawing. Outros desenhos precisam usar esses nomes nos objetos correspondentes.
6. **Identificação de peças:** `Propriedades.CATScript` decide se um componente é peça com base no nome do objeto pai conter `.CATPART`. Essa heurística pode não funcionar em todos os ambientes ou estruturas de documentos.
7. **Contagem de falhas:** algumas rotinas usam `On Error Resume Next`. Isso permite continuar após certos erros, mas pode ocultar a causa original ou fazer com que uma mensagem de sucesso não garanta que todas as alterações foram efetivadas.
8. **Salvamento:** `AcessarProps.CATScript` tenta salvar o modelo 3D e o Drawing. `PropriedadesPeca.CATScript` informa que o usuário deve salvar, mas não salva explicitamente no código analisado. As demais macros também não salvam explicitamente todos os documentos modificados.
9. **Numeração por referência:** `NumerarItens.CATScript` evita processar novamente a mesma referência durante a execução. Instâncias repetidas de uma mesma referência podem, portanto, não receber números distintos; além disso, a propriedade é gravada na referência do produto, podendo afetar outras instâncias que compartilham essa referência.
10. **Conjunto raiz na numeração:** a macro numera os filhos diretos do produto raiz do documento ativo. Apesar do comentário mencionar conjunto selecionado, o código não lê a seleção atual.
11. **Validação de dados:** o formulário valida apenas que `ITEM` não esteja vazio. Não há validação de formato, unicidade global ou consistência entre `PDF`, `DXF` e `STEP`.
12. **Valores vazios:** `PropriedadesPeca.CATScript` ignora campos vazios ao gravar, o que impede usar um campo vazio para apagar o valor existente. Já `AcessarProps.CATScript` tenta gravar os valores retornados, inclusive strings vazias.
13. **Codificação:** o arquivo de intercâmbio é criado em Unicode em algumas rotinas. Mantenha a compatibilidade entre leitura e gravação ao modificar os scripts.
14. **`resultado.txt` incluído no ZIP:** contém dados de uma execução anterior. Ele será sobrescrito durante o uso; não dependa dos valores atualmente presentes.
15. **Saídas de fabricação:** `Exportar_Chapas.CATScript` substitui exports de mesmo nome e pode remover arquivos antigos de peças classificadas como sólido convencional. Verifique o relatório e os arquivos nas pastas `STEP` e `DXF`.
16. **Análise de chapa:** `DefinirCorteLaser.CATScript` salva os CATParts nos quais atualiza propriedades; confirme que os documentos estão disponíveis para gravação e revise `RELATORIO_PROCESSO.txt`.

## Solução de problemas

### A macro não encontra a janela HTA

- Verifique se `JanelaPropriedades.hta` existe na pasta indicada.
- Confira as constantes e variáveis de caminho em todos os arquivos.
- Garanta que os scripts e a HTA apontem para a mesma pasta.

### A janela abre, mas os campos não são preenchidos

- Confirme que a primeira linha de `resultado.txt` é `INICIAL`.
- Verifique se as chaves gravadas no arquivo correspondem aos IDs `txt_<CHAVE>` existentes no HTA.
- Confirme se o arquivo está sendo lido do mesmo caminho que a macro escreveu.

### A macro não atualiza um campo do Drawing

- Confirme que o documento ativo é um Drawing.
- Verifique se existe uma vista generativa vinculada ao modelo esperado.
- Confira os nomes exatos dos objetos de texto: `NUM_PART`, `FOLHA_PDF`, `ITEM`, `MATERIAL`, `DESCRICAO`, `TRAT_SUP` e `TRAT_TERM`.
- Verifique se os textos estão em alguma folha/vista percorrida pela macro.

### A macro não cria ou atualiza uma propriedade

- Confirme que o destino é um `Product` válido e que `UserRefProperties` está acessível.
- Confira a grafia do nome da propriedade.
- Verifique se o documento está editável e se não há restrições de acesso.
- Consulte a mensagem da macro e valide a propriedade diretamente no CATIA.

### A BOM não é gerada

- Confirme que o documento ativo é um `CATProduct`.
- Verifique se o usuário tem permissão de escrita na Área de Trabalho.
- Confira se a configuração da BOM e as propriedades personalizadas estão disponíveis na versão do CATIA utilizada.

### A numeração de itens parece incompleta

- A macro percorre apenas os filhos diretos do produto raiz.
- Ela não entra nos componentes internos dos subconjuntos.
- Referências repetidas são processadas uma única vez durante a execução.

## Boas práticas

- Faça backup dos arquivos CATPart, CATProduct e CATDrawing antes de executar macros que alterem propriedades.
- Teste primeiro em uma montagem pequena e controlada.
- Mantenha nomes de propriedades e nomes de objetos de texto padronizados.
- Centralize os caminhos em uma configuração única para facilitar a instalação em outros computadores.
- Evite executar duas macros que utilizem `resultado.txt` ao mesmo tempo.
- Após a execução, valide no CATIA os valores alterados e o estado de salvamento dos documentos.
- Ao distribuir o projeto, remova dados de exemplo de `resultado.txt` ou substitua-os por um arquivo de estado inicial apropriado.
- Registre a versão do CATIA e do Windows em que o conjunto de macros foi testado.

## Licença

Este projeto é distribuído sob a **Licença MIT**. Consulte o arquivo [`LICENSE`](LICENSE) para ler o texto integral da licença.

A licença MIT permite usar, copiar, modificar, mesclar, publicar, distribuir, sublicenciar e/ou vender cópias do software, desde que o aviso de copyright e o texto da licença sejam mantidos nas cópias ou partes substanciais do software. O software é fornecido “como está”, sem garantia de qualquer tipo, conforme os termos completos do arquivo `LICENSE`.

A licença do projeto não concede direitos sobre marcas registradas nem altera os termos de licenciamento do CATIA V5 ou de outros softwares de terceiros usados em conjunto com estas macros.

---

**Resumo:** `MacroS` reúne automações de propriedades, desenhos, numeração, BOM, análise dimensional e processamento de chapas (classificação, espessura e exportação STEP/DXF) para CATIA V5. Antes de distribuir ou usar em produção, ajuste os caminhos absolutos, valide os nomes dos objetos do Drawing e teste as rotinas em cópias dos documentos reais.
