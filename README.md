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

As rotinas foram escritas em CATScript/VBScript e usam objetos da automação COM do CATIA. A interface gráfica é um arquivo HTA executado pelo `mshta.exe`, que troca dados com as macros por meio de `resultado.txt`.

## Conteúdo do projeto

| Arquivo | Finalidade |
|---|---|
| `AcessarProps.CATScript` | Abre o formulário de propriedades a partir de um Drawing, lê o modelo 3D associado, atualiza propriedades do modelo e textos específicos do Drawing e tenta salvar os documentos. |
| `BillOfMaterial.CATScript` | Exporta a BOM do conjunto ativo para um arquivo HTML na Área de Trabalho. |
| `JanelaPropriedades.hta` | Interface gráfica para visualizar e editar propriedades nativas e personalizadas. |
| `NumerarItens.CATScript` | Numera referências únicas entre os componentes diretos do conjunto ativo, gravando a propriedade personalizada `ITEM`. |
| `Propriedades.CATScript` | Cria propriedades padrão ausentes no modelo ativo e, em um conjunto, processa o conjunto raiz e seus componentes diretos. |
| `PropriedadesPeca.CATScript` | Abre o formulário a partir de uma peça ou de um componente selecionado na árvore de um conjunto e grava as propriedades retornadas. |
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
7. Atualiza os textos do Drawing identificados pelos nomes abaixo:
   - `Text.524` ← `PartNumber`
   - `Text.525` ← `PDF`
   - `Text.526` ← `ITEM`
   - `Text.527` ← `DEFINITION`
   - `Text.528` ← `PRODUCT_DESCRIPTION`
8. Tenta salvar o documento 3D e, em seguida, o Drawing.

Os nomes `Text.524` a `Text.528` precisam corresponder exatamente aos nomes dos objetos de texto existentes no Drawing. A macro percorre folhas, vistas e coleções de textos para localizar esses objetos.

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

Os nomes são identificadores usados diretamente no código. Preserve a grafia, os sublinhados e a ausência de acentos ao editar as macros ou configurar propriedades no CATIA.

## Requisitos

- Windows com CATIA V5 instalado.
- Permissão para executar macros CATScript e automação COM.
- `mshta.exe` disponível e permitido pelas políticas de segurança do Windows para as rotinas que usam a interface HTA.
- Permissão de leitura e gravação na pasta configurada para os scripts e para `resultado.txt`.
- Um documento compatível aberto no CATIA, conforme a macro executada.
- Para `AcessarProps.CATScript`, um Drawing com pelo menos uma vista generativa vinculada a um modelo 3D.
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

1. **Caminhos fixos:** `AcessarProps.CATScript`, `PropriedadesPeca.CATScript` e `JanelaPropriedades.hta` dependem de caminhos absolutos associados a um usuário/pasta específicos.
2. **Arquivo compartilhado:** o uso de um único `resultado.txt` pressupõe que não haverá duas execuções simultâneas. Execuções concorrentes podem sobrescrever os dados umas das outras.
3. **HTA/MSHTA:** a interface depende de uma tecnologia antiga do Windows que pode estar bloqueada por políticas de segurança.
4. **Identificação do modelo no Drawing:** `AcessarProps.CATScript` usa a primeira vista generativa encontrada que permita obter um documento de origem. Em desenhos com várias vistas/modelos, confirme se essa seleção corresponde ao modelo que deseja editar.
5. **Nomes de textos fixos:** `Text.524` a `Text.528` são nomes específicos do Drawing usado como referência. Outros desenhos podem ter nomes diferentes.
6. **Identificação de peças:** `Propriedades.CATScript` decide se um componente é peça com base no nome do objeto pai conter `.CATPART`. Essa heurística pode não funcionar em todos os ambientes ou estruturas de documentos.
7. **Contagem de falhas:** algumas rotinas usam `On Error Resume Next`. Isso permite continuar após certos erros, mas pode ocultar a causa original ou fazer com que uma mensagem de sucesso não garanta que todas as alterações foram efetivadas.
8. **Salvamento:** `AcessarProps.CATScript` tenta salvar o modelo 3D e o Drawing. `PropriedadesPeca.CATScript` informa que o usuário deve salvar, mas não salva explicitamente no código analisado. As demais macros também não salvam explicitamente todos os documentos modificados.
9. **Numeração por referência:** `NumerarItens.CATScript` evita processar novamente a mesma referência durante a execução. Instâncias repetidas de uma mesma referência podem, portanto, não receber números distintos; além disso, a propriedade é gravada na referência do produto, podendo afetar outras instâncias que compartilham essa referência.
10. **Conjunto raiz na numeração:** a macro numera os filhos diretos do produto raiz do documento ativo. Apesar do comentário mencionar conjunto selecionado, o código não lê a seleção atual.
11. **Validação de dados:** o formulário valida apenas que `ITEM` não esteja vazio. Não há validação de formato, unicidade global ou consistência entre `PDF`, `DXF` e `STEP`.
12. **Valores vazios:** `PropriedadesPeca.CATScript` ignora campos vazios ao gravar, o que impede usar um campo vazio para apagar o valor existente. Já `AcessarProps.CATScript` tenta gravar os valores retornados, inclusive strings vazias.
13. **Codificação:** o arquivo de intercâmbio é criado em Unicode em algumas rotinas. Mantenha a compatibilidade entre leitura e gravação ao modificar os scripts.
14. **`resultado.txt` incluído no ZIP:** contém dados de uma execução anterior. Ele será sobrescrito durante o uso; não dependa dos valores atualmente presentes.

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
- Confira os nomes exatos dos textos: `Text.524`, `Text.525`, `Text.526`, `Text.527` e `Text.528`.
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

**Resumo:** `MacroS` é um conjunto de automações para propriedades de produto, atualização de campos em desenhos, numeração de itens e exportação de BOM no CATIA V5. Antes de distribuir ou usar em produção, ajuste os caminhos absolutos, valide os nomes dos objetos do Drawing e teste as rotinas em cópias dos documentos reais.
