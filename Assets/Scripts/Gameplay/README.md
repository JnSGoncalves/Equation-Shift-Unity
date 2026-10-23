# Movimentação da fase exemplo

Abra `Assets/Scenes/00-FaseExemplo.unity` e entre em Play.

- **WASD ou setas:** anda em quatro direções, uma célula por passo. Segurar repete os passos; toques rápidos são registrados entre atualizações de física.
- **Empurrar:** caminhe contra um bloco com `Is Movable` habilitado. O personagem ocupa a célula anterior do bloco enquanto ele avança uma célula na mesma direção.
- Paredes, ausência de chão, outros blocos e outros colliders sólidos impedem o movimento. Não há empurrão em cadeia ou movimento diagonal.

O movimento em células segue o GDD. Espaço e Shift ficam disponíveis para as futuras habilidades e transformações.

## Componentes

- `LevelGrid` no `Grid`: referências aos Tilemaps `Ground` e `Walls`; converte posições e consulta células ocupadas. Considera a diferença de profundidade Z dos Tilemaps apenas visual.
- `TopDownGridPlayer` no `Player`: leitura de teclado pelo Input System e deslocamento suave com Rigidbody2D cinemático.
- `GridPushBlock` em cada bloco: colisão sólida e método público `TryMove(Vector2Int direction)`, que retorna se o deslocamento pôde começar. `Cell`, `IsMoving` e `Occupies` permitem consultar seu estado. Origem e destino ficam reservados durante o deslocamento. `Value` expõe o caractere guardado no campo `value`; `IsMovable` controla se o bloco pode ser empurrado.
- `TilemapCollider2D` em `Walls`: colisão por célula pintada. Os antigos BoxColliders que cobriam todo o interior de `Walls` e do Tilemap decorativo vazio `Structs` foram removidos.

`Move Duration` no Inspector controla a duração de cada passo (padrão: 0,16 segundo). Mantenha o mesmo valor no personagem e nos blocos para sincronizar o empurrão. `Ground` não deve ter collider sólido: ele define as células permitidas, sem bloquear os atores.

Para adicionar outro bloco, duplique um dos blocos sob `Grid/Blocks` e posicione-o no centro de uma célula livre de `Ground`. Mantenha a referência ao mesmo `LevelGrid` e os componentes Rigidbody2D, BoxCollider2D e GridPushBlock. Preencha `Value` com um dígito de `0` a `9` ou um dos símbolos `+`, `-`, `*`, `/`, `=`. Os nomes `XX_Block_X` e as tags `MovableBlock`/`StaticBlock` organizam a cena; a lógica usa os campos do componente, preservando os valores mesmo quando o nome do objeto é diferente.

## Prefabs de blocos

Os 30 prefabs ficam em `Assets/Prefabs/Blocks/Static` e `Assets/Prefabs/Blocks/Movable`. Cada pasta contém os dígitos de 0 a 9 e cinco operadores. Exemplos:

- `Block_Static_Digit_5`: número 5 fixo.
- `Block_Movable_Digit_5`: número 5 que pode ser empurrado.
- `Block_Static_Operator_Plus`: adição fixa.
- `Block_Movable_Operator_Multiply`: multiplicação móvel.

Os operadores usam `Plus`, `Minus`, `Multiply`, `Divide` e `Equals`, correspondendo a `+`, `-`, `*`, `/` e `=`. Os nomes textuais evitam caracteres incompatíveis com arquivos do Windows.

Arraste os prefabs para `Grid/Blocks/StaticBlocks` ou `Grid/Blocks/MovableBlocks` da fase, alinhados ao centro das células de `Ground`. As instâncias existentes nesses grupos estão conectadas aos prefabs e preservam seus sprites. O campo `board` fica vazio no asset: o componente encontra o `LevelGrid` do pai ao entrar em Play. Nas instâncias já configuradas da cena, essa referência também está atribuída explicitamente.

Todos têm tag correspondente, `GridPushBlock`, `SpriteRenderer` e `BoxCollider2D` sólido de 0,9 × 0,9, com offset zero. Os móveis usam Rigidbody2D cinemático, sem gravidade, rotação travada, interpolação e detecção contínua; os fixos usam Rigidbody2D estático e `IsMovable` desabilitado. O passo padrão dura 0,16 segundo.

O menu **Equation Shift > Padronizar blocos e criar prefabs** reconstrói ou atualiza os assets a partir dos filhos `block_<valor>` de `StaticBlocks` e `MovableBlocks` nas cenas de `Assets/Scenes`. Ele reconhece números e os sufixos `plus`, `minus`, `mult`, `divis`, `equal` (também aceita os símbolos literais). O nome define o valor, e o grupo define se é móvel. O comando move os dois grupos para `Grid/Blocks`, alinha os blocos ao chão, preserva os sprites e conecta as instâncias aos prefabs. Salve suas cenas antes de executar o comando.

## Validação das equações

`EquationValidator` está no objeto `Blocks` da fase exemplo, com referências ao `LevelGrid` e ao pai dos blocos. Para outras cenas, adicione esse componente e atribua essas duas referências.

- A leitura segue células consecutivas: horizontal da esquerda para a direita e vertical de cima para baixo. Uma célula vazia interrompe a sequência. Dígitos consecutivos formam um único número em ambas as direções: `1`, `2`, `+`, `3`, `=`, `1`, `5` resulta em `12+3=15`.
- Todas as sequências com pelo menos dois blocos e um `=` são verificadas, incluindo cruzamentos. A sequência inteira deve formar uma equação, com exatamente um `=` e expressões válidas dos dois lados. Blocos sem uma equação não geram resultados.
- Multiplicação e divisão têm prioridade sobre adição e subtração; operações de mesma prioridade são resolvidas da esquerda para a direita. São aceitos números negativos com `-`. Divisão por zero, símbolos desconhecidos e contas incompletas retornam erro. Frações e inteiros grandes são calculados exatamente, sem arredondamento de ponto flutuante (`1/3*3=1` é verdadeiro).
- Blocos fixos também participam. Durante a movimentação de qualquer bloco monitorado, o resultado fica suspenso. O componente recalcula após o fim do passo e quando valores, quantidade ou ativação dos blocos mudam. Dois blocos na mesma célula invalidam a configuração.

No Inspector, `Results` mostra texto, direção, célula inicial, validade sintática, resultado e eventual erro. `Log Results On Change` habilita mensagens no Console. O menu de contexto **Validar equacoes** permite conferir o layout também fora do Play Mode.

Para consultar por código, use `ValidateEquations()` (retorna se há alguma equação verdadeira), `HasTrueEquation`, `AllEquationsTrue`, `Results`, `IsWaitingForBlocks` e `ValidationError`. `AllEquationsTrue` exige pelo menos uma equação e que todas as sequências encontradas sejam verdadeiras. Cada `EquationResult` contém `Expression`, `StartCell`, `Direction`, `IsWellFormed`, `IsTrue` e `Error`.

Os UnityEvents `On Equation Satisfied` e `On Equation Invalidated` são disparados nas transições entre nenhuma equação verdadeira e pelo menos uma verdadeira. Uma movimentação suspende o resultado e pode disparar a invalidação. Use-os para ligar portas ou feedbacks reversíveis; a conclusão definitiva da fase pode consultar `AllEquationsTrue` quando necessário. O evento C# `ValidationChanged` avisa sobre cada recálculo.

O menu **Equation Shift > Configurar Fase Exemplo** reaplica a configuração à cena salva. Use-o somente para reconstruir a configuração inicial: ele abre a cena do exemplo e salva as alterações.

## Verificação

Os testes em `Assets/Tests/PlayMode/GridMovementTests.cs` podem ser executados pelo Unity Test Runner em PlayMode. Cobrem deslocamento e alinhamento, colisão com paredes, ausência de chão, bloqueio por outro bloco, rejeição de diagonais, proteção durante deslocamento, colisão com o personagem e integração com a cena do exemplo.

`EquationValidatorTests.cs` cobre leitura nas duas direções, dezenas e centenas, precedência, frações, contas inválidas, lacunas, cruzamentos, blocos fixos, sobreposição, revalidação após movimento e integração com o pai `Blocks` da cena.
