# Movimentação da fase exemplo

Para montar novas salas pelo editor, comece pelo [guia de criação de fases](../../../README.md). Este documento reúne os detalhes técnicos dos componentes e das animações.

Abra `Assets/Scenes/00-FaseExemplo_1.unity` e entre em Play.

- **WASD ou setas:** anda em quatro direções, uma célula por passo. Segurar repete os passos; toques rápidos são registrados entre atualizações de física.
- **Tab:** troca a forma uma vez por pressionamento, no ciclo `Default → Equal → Plus → Minus → Mult → Divis → Default`, pulando as formas ainda não coletadas. O Player começa somente com `Default`.
- **Coletar habilidade:** termine um passo na célula de um objeto `habilitie_<forma>` em `Grid/Interactables` para liberar aquela transformação.
- **Empurrar:** caminhe contra um bloco com `Is Movable` habilitado. O personagem ocupa a célula anterior do bloco enquanto ele avança uma célula na mesma direção.
- **R:** restaura os blocos às posições e valores iniciais da sala e devolve o Player à entrada usada na última transição. Na primeira sala, usa a posição inicial da cena. O portão da entrada abre para receber o Player e aguarda sua saída antes de fechar novamente. Cancela passos em andamento e revalida as contas. As habilidades já obtidas, inclusive em salas anteriores, e a forma selecionada são preservadas. O reset aguarda o fim de transições e animações que bloqueiam o movimento; pressione R novamente quando terminarem. Por código, use `TopDownGridPlayer.TryResetLevel()`.
- Paredes, ausência de chão, outros blocos e outros colliders sólidos impedem o movimento. Não há empurrão em cadeia ou movimento diagonal.

O movimento em células segue o GDD. Espaço e Shift ficam disponíveis para as futuras habilidades.

## Componentes

- `LevelGrid` no `Grid`: referências aos Tilemaps `Ground` e `Walls`; converte posições e consulta células ocupadas. Considera a diferença de profundidade Z dos Tilemaps apenas visual.
- `TopDownGridPlayer` no `Player`: leitura de teclado pelo Input System e deslocamento suave com Rigidbody2D cinemático.
- `AbilityPickup` nos coletáveis de `Grid/Interactables`: libera a forma indicada em `Form` e executa o efeito de coleta.
- `GridPushBlock` em cada bloco: colisão sólida e método público `TryMove(Vector2Int direction)`, que retorna se o deslocamento pôde começar. `Cell`, `IsMoving` e `Occupies` permitem consultar seu estado. Origem e destino ficam reservados durante o deslocamento. `Value` expõe o caractere guardado no campo `value`; `IsMovable` controla se o bloco pode ser empurrado.
- `TilemapCollider2D` em `Walls`: colisão por célula pintada. Os antigos BoxColliders que cobriam todo o interior de `Walls` e do Tilemap decorativo vazio `Structs` foram removidos.

`Move Duration` no Inspector controla a duração de cada passo (padrão: 0,16 segundo). Mantenha o mesmo valor no personagem e nos blocos para sincronizar o empurrão. `Ground` não deve ter collider sólido: ele define as células permitidas, sem bloquear os atores.

`Current Form` no Inspector mostra a forma atual e pode ser alterado durante o Play Mode entre as formas liberadas. O Player inicia em `Default`, sem transformações desbloqueadas. Por código, use `UnlockForm(PlayerForm)` para liberar uma forma, `IsFormUnlocked(form)` para consultá-la e `CurrentForm` para selecioná-la, ou `CycleForm()` para avançar no ciclo. `UnlockedForms` lista as transformações coletadas; liberar novamente a mesma forma não duplica a entrada. Atribuições de formas bloqueadas são ignoradas, e a validação das equações também as rejeita. Desativar e reativar o Player preserva os desbloqueios; uma nova instância começa sem eles.

Fora de `Default`, o Player representa um símbolo nas equações: `Equal` = `=`, `Plus` = `+`, `Minus` = `-`, `Mult` = `*` e `Divis` = `/`. `TryGetEquationSymbol(out char symbol)` expõe esse símbolo e retorna `false` em `Default`. Todas as formas mantêm o mesmo movimento e empurrão.

O Animator do Player usa `Assets/Animations/Player/Player.controller`, com seis estados e clipes `player_<forma>_idle.anim`. Cada clipe repete quatro frames de 20 × 20 a 12 fps, seguindo os grupos de quatro sprites de `player_animations_80x100.png`: default (0–3), equal (4–7), plus (8–11), minus (12–15), mult (16–19) e divis (20–23). A troca reinicia o clipe correspondente e atualiza o sprite imediatamente; bloquear o movimento durante a abertura dos portões não bloqueia a troca de forma.

## Coletáveis de habilidades

Os cinco objetos `habilitie_equal`, `habilitie_minus`, `habilitie_plus`, `habilitie_mult` e `habilitie_divis` em `Grid/Interactables` têm `AbilityPickup`, `SpriteRenderer` e `Animator`. Os clipes e controladores `habilitie_<forma>_idle` ficam em `Assets/Animations/Abilities`. Os clipes usam quatro frames em loop a 12 fps do atlas `Assets/Texture/animations/habilities_80x100.png`: equal (0–3), minus (4–7), plus (8–11), mult (12–15) e divis (16–19). Essa ordem difere da ordem das formas do Player.

A coleta começa automaticamente quando o Player termina o passo na célula do item. Não há collider sólido bloqueando a entrada. O item aparece uma célula do grid acima do Player, fica à frente do seu sprite e desce suavemente até ele enquanto perde opacidade. Ao terminar, o item é desativado e removido; somente no frame seguinte a forma é liberada e selecionada, atualizando o sprite e a animação do Player. Assim, o personagem ainda mantém a forma anterior quando o objeto desaparece. A transformação continua disponível pelo Tab depois disso. Durante o efeito, o Tab não pode selecionar a transformação ainda bloqueada. Coletar uma forma já liberada também a seleciona após a remoção do item, sem duplicar o desbloqueio. O efeito acompanha o Player se ele continuar andando. `Collection Duration` no Inspector define a duração (padrão: 0,8 s), e a altura respeita o tamanho da célula do grid.

O menu **Equation Shift > Configurar coletaveis de habilidades** gera ou atualiza as animações e configura os objetos `habilitie_<forma>` sob `Interactables` na cena ativa, salvando-a. Ele preserva as posições definidas na cena. Para adicionar outro coletável, duplique um desses objetos e ajuste `Form`, sprite e controlador; também é possível renomeá-lo com o sufixo correto e executar o menu novamente fora do Play Mode. Os coletáveis não são blocos de equação nem recebem `GridPushBlock`.

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

`EquationValidator` está no objeto `Blocks` da fase exemplo, com referências ao `LevelGrid`, ao pai dos blocos e ao Player. Para outras cenas, adicione esse componente e atribua as referências ao grid e aos blocos; se `Player` estiver vazio, ele será encontrado entre os filhos do mesmo `LevelGrid`, mesmo sendo um irmão de `Blocks`.

- A leitura segue células consecutivas: horizontal da esquerda para a direita e vertical de cima para baixo. Uma célula vazia interrompe a sequência. Dígitos consecutivos formam um único número em ambas as direções: `1`, `2`, `+`, `3`, `=`, `1`, `5` resulta em `12+3=15`.
- Na leitura vertical, cada célula numérica inclui também os dígitos contíguos à esquerda e à direita, lidos da esquerda para a direita. Por exemplo, um `3` abaixo do `=` passa a representar `13` quando um `1` encosta à sua esquerda. Todos esses blocos participam da equação, inclusive para a associação aos portões. Lacunas e operadores laterais encerram o número.
- Todas as sequências com pelo menos dois símbolos e um `=` são verificadas, incluindo cruzamentos. A sequência inteira deve formar uma equação, com exatamente um `=` e expressões válidas dos dois lados. Blocos sem uma equação não geram resultados.
- O Player ativo, parado e fora de `Default` ocupa sua célula como o operador da forma atual. Por exemplo, `1`, Player em `Plus`, `2`, `=`, `3` forma `1+2=3`; `2`, Player em `Equal`, `2` forma `2=2`. Ele pode completar os dois eixos de um cruzamento. Diagonais e células vazias não conectam símbolos; `Default` não preenche lacunas. O personagem continua separado dos blocos e não se torna empurrável.
- Multiplicação e divisão têm prioridade sobre adição e subtração; operações de mesma prioridade são resolvidas da esquerda para a direita. São aceitos números negativos com `-`. Divisão por zero, símbolos desconhecidos e contas incompletas retornam erro. Frações e inteiros grandes são calculados exatamente, sem arredondamento de ponto flutuante (`1/3*3=1` é verdadeiro).
- Blocos fixos também participam. Durante a movimentação de qualquer bloco monitorado ou do Player em forma de operador, o resultado fica suspenso. O componente recalcula após o fim do passo e quando valores, forma, quantidade ou ativação dos participantes mudam. O movimento em `Default` não suspende a validação. Dois participantes na mesma célula invalidam a configuração.

No Inspector, `Results` mostra texto, direção, célula inicial, validade sintática, resultado e eventual erro. `Log Results On Change` habilita mensagens no Console. O menu de contexto **Validar equacoes** permite conferir o layout também fora do Play Mode.

Para consultar por código, use `ValidateEquations()` (retorna se há alguma equação verdadeira), `HasTrueEquation`, `AllEquationsTrue`, `Results`, `IsWaitingForMovement` e `ValidationError`. `IsWaitingForBlocks` continua disponível como equivalente de `IsWaitingForMovement`, incluindo a espera pelo Player em forma de operador. `AllEquationsTrue` exige pelo menos uma equação e que todas as sequências encontradas sejam verdadeiras. Cada `EquationResult` contém `Expression`, `StartCell`, `Direction`, `IsWellFormed`, `IsTrue` e `Error`, além dos blocos em `ParticipatingBlocks` e da referência `ParticipatingPlayer` quando o personagem integra aquela equação.

Os UnityEvents `On Equation Satisfied` e `On Equation Invalidated` são disparados nas transições entre nenhuma equação verdadeira e pelo menos uma verdadeira. Uma movimentação suspende o resultado e pode disparar a invalidação. Use-os para ligar portas ou feedbacks reversíveis; a conclusão definitiva da fase pode consultar `AllEquationsTrue` quando necessário. O evento C# `ValidationChanged` avisa sobre cada recálculo.

O menu **Equation Shift > Configurar Fase Exemplo** reaplica a configuração à cena salva. Use-o somente para reconstruir a configuração inicial: ele abre a cena do exemplo e salva as alterações.

## Âncoras e portões

`GridPushBlock.IsAncor` marca um bloco como âncora imóvel, mesmo se `IsMovable` estiver habilitado. `AncorSide` é um enum com `T` (Top), `D` (Down), `L` (Left) e `R` (Right). O lado identifica a passagem associada; a equação pode ser horizontal ou vertical e incluir a âncora em qualquer posição.

Na fase exemplo, `block_2_Ancor_L` abre `Gate_Vertical` e `block_0_Ancor_D` abre `Gate_Horizontal`. Cada `EquationGate` tem referências explícitas à âncora, ao validador e ao `GateOpeningFeedback` da Main Camera. Ele só aceita uma equação verdadeira que contenha sua própria âncora e cujo lado corresponda ao da passagem. `EquationResult.ParticipatingBlocks` e `Contains(block)` expõem essa participação. Uma equação falsa, incompleta ou com erro não dispara tremor nem abertura.

Equações completadas pelo Player também podem abrir o portão da âncora que faz parte da mesma sequência. A forma do Player não o transforma em âncora.

O feedback bloqueia novos passos do jogador, aguarda o passo atual terminar, executa um tremor rápido de 0,16 s e inicia a abertura com um tremor mais lento durante o clipe de 1 s. Os parâmetros ficam no Inspector da Main Camera. Os estados `Closed`, `Opening`, `Open` e `Closing` dos Animators não repetem. A colisão só é desativada ao terminar a abertura; a solução permanece aceita para permitir reutilizar os blocos. O estado ainda não é salvo entre carregamentos de cena ou sessões.

`Is Open` no Inspector de cada `EquationGate` define se o portão começa aberto ou fechado. Fora do Play Mode, alterar essa opção atualiza o sprite e a colisão imediatamente. Durante o jogo, a mudança anima os oito quadros da abertura ou do fechamento; fechar bloqueia a passagem imediatamente, e abrir libera a passagem ao final. Por código, use `gate.IsOpen = true/false` ou `gate.SetOpen(estado)`; `SetOpen(estado, false)` aplica instantaneamente. `IsOpening` e `IsClosing` indicam a animação em andamento. É possível inverter o estado durante uma animação; uma abertura pendente pela equação não sobrescreve a alteração manual. O fechamento usa os quadros da abertura na ordem inversa.

Se duas passagens forem resolvidas juntas, as aberturas são enfileiradas e o movimento continua bloqueado até a última terminar. Desabilitar o feedback restaura a posição da câmera, cancela aberturas pendentes e libera o bloqueio de movimento pertencente a esse efeito. `TopDownGridPlayer.LockMovement(owner)` e `UnlockMovement(owner)` permitem coexistir com outros bloqueios.

Os prefabs ficam em `Assets/Prefabs/Gates` e as animações em `Assets/Animations/Gates`. Nos prefabs, as referências a objetos da cena ficam vazias; atribua âncora, validador e feedback nas novas instâncias. O menu **Equation Shift > Configurar portoes da Fase Exemplo** recria os assets e configura os objetos existentes sem mudar suas posições, salvando a cena. O gerador de blocos também reconhece o sufixo `_Ancor_T/D/L/R` sem transformar o prefab comum em âncora.

## Transições entre salas

As cenas usam dois caracteres alfanuméricos no início do nome, seguidos de hífen: `00-FaseExemplo_1`, `A0-FaseExemplo_2`, `1A-Sala`, `4G-Sala`. Cada código precisa identificar uma única cena habilitada em **Build Profiles > Scene List**. As duas cenas de exemplo já estão incluídas.

O prefab `Assets/Prefabs/Transitions/ScenePortal.prefab` é um objeto invisível com `ScenePortal` e `BoxCollider2D` marcado como trigger. Posicione-o depois do portão, fora da área visível, no centro de uma célula com chão. Mantenha chão em todo o corredor até o objeto; o movimento continua respeitando paredes e portões fechados. O contorno azul e a linha para a chegada aparecem somente no editor.

No Inspector do portal:

- **Destination Scene Id:** código de dois caracteres da sala de destino, por exemplo `A0`.
- **Spawn Direction:** direção deste portal para dentro da própria sala. O Player aparece sempre na célula imediatamente vizinha; valores antigos como `(0, 3)` também são tratados como uma única célula para cima.
- **Arrival Gate:** portão associado à chegada. Quando vazio, procura automaticamente um portão no centro da célula vizinha.
- **Transition Duration:** duração do deslizamento da câmera, em segundos (padrão: `0,5`).
- **Board:** `LevelGrid` da sala; pode ficar vazio quando o portal estiver sob o grid.

A vinculação usa somente os códigos das salas. Ao sair de `00` para `A0`, o sistema procura em `A0` a saída cujo **Destination Scene Id** seja `00`; ao voltar, procura em `00` a saída que aponta para `A0`. Saídas para outras salas são ignoradas. Mantenha uma única saída ativa para cada sala de destino dentro do mesmo grid; se houver mais de uma contraparte, a chegada usa uma célula livre perto do centro. O Player só troca de cena ao terminar o passo na célula do trigger. A nova instância recebe as formas coletadas e a forma selecionada, com posição e movimento alinhados ao novo grid. A chegada usa a célula vizinha indicada por Spawn Direction, mesmo sem portão ou com um trigger sobreposto; a troca de sala só dispara quando o Player ocupa a própria célula do portal. As salas e seus puzzles são recriados a cada carregamento; apenas as formas do Player são transportadas.

As salas de exemplo têm `RoomScene` no `Grid`. Ao criar outra sala, duplique uma delas ou adicione esse componente ao Grid; ele prepara a câmera, a iluminação e os atores antes da ativação de uma cena carregada junto da atual. Mantenha um Player ativo e uma câmera principal configurada em cada cena. Todos os objetos raiz do cenário são deslocados juntos automaticamente; não é necessário reorganizar a hierarquia.

`RoomTransitionController` carrega a próxima sala em modo aditivo, mantendo a anterior visível. Alinha a contraparte uma célula além da saída, no sentido oposto a `Spawn Direction`; sem contraparte, posiciona a próxima sala a uma tela de distância nessa direção. Durante o deslizamento, a câmera da origem é a única ativa, o personagem atravessa visualmente a passagem e os atores das duas salas ficam pausados. As luzes e o áudio da nova sala aguardam a conclusão, evitando duplicação durante o carregamento. O movimento usa tempo independente de `Time.timeScale`.

Ao terminar, a câmera da próxima sala assume o enquadramento, o cenário volta às suas coordenadas salvas sem salto visual, a sala anterior é descarregada e o movimento é liberado. Isso evita acumular deslocamentos ao ir e voltar. O fechamento do portão e seus tremores só podem começar depois da transição, quando o Player sair dele. Cenas sem Player ou câmera cancelam a transição, restaurando a sala de origem.

Se a chegada estiver em um portão, ele aparece aberto, com sprite aberto e colisão desativada, para receber o Player. `DefaultIsOpen` guarda o `Is Open` salvo originalmente na cena: se estava fechado, `IsWaitingForArrivalExit` aguarda o Player terminar de sair completamente do espaço do portão e então o fecha atrás dele. O fechamento usa a fila do `GateOpeningFeedback`: bloqueia novos passos, faz o mesmo tremor rápido, reproduz `Closing` com o mesmo tremor lento da abertura e restaura câmera e movimento ao terminar. Um portão configurado como aberto permanece aberto depois da saída. Mudanças manuais de estado cancelam a espera de chegada. Sem feedback disponível, o portão ainda fecha com a animação normal.

Com uma contraparte válida, a chegada usa a célula indicada mesmo sem portão. Somente ausência de chão ou uma parede nessa célula provocam a busca por uma célula livre perto do centro. Portões existentes abrem para receber o Player; se o terreno impedir a chegada, seu estado anterior é restaurado. A falta de uma contraparte única também exige essa busca. A busca alternativa rejeita paredes, blocos, colisões sólidas e outros portais. Código de cena inválido, ausente ou duplicado não inicia carregamento nem prende o movimento.

O exemplo liga `Grid/Transitions/Exit_Bottom_To_A0`, depois do portão inferior de `00`, a `Grid/Transitions/Exit_Top_To_00`, no topo de `A0`, definindo apenas `A0` e `00` como seus respectivos destinos. A chegada em `A0` é a célula `(-1, 7)` e a volta para `00` é `(-1, -8)`, ambas sobre os portões, imediatamente ao lado dos portais. Os dois portões estão configurados como fechados na cena e abrem temporariamente para receber quem chega, fechando após sua saída. Para voltar depois disso, o portão precisa ser aberto novamente pela equação ou por `Is Open`.

O menu **Equation Shift > Configurar transicoes das Fases Exemplo** reaplica essa ligação, gera as animações dos portões e registra as duas cenas no Build. Ele salva as cenas de exemplo e reabre a primeira.

## Aviso de sala indisponível

Ao entrar em uma transição cujo destino não está disponível nas cenas habilitadas do Build, o jogador permanece na sala atual e vê "Opa, aparentemente essa sala ainda está sendo montada...". O aviso usa `Assets/Fonts/Monocraft-ttf/Monocraft.ttf`, com texto branco e painel preto na parte inferior da tela, e desaparece após cinco segundos, sem bloquear o movimento. Sair e entrar novamente na passagem permite exibi-lo de novo. Uma transição válida remove o aviso imediatamente.

`RoomUnavailableNotice` carrega seu prefab em `Resources/UI` somente quando necessário. O prefab referencia a fonte diretamente, incluindo-a no jogo compilado. A chamada fica em `SceneTransition`, funcionando automaticamente em todas as salas que usam `ScenePortal`.

## Testes

`PlayerFormTests.cs` verifica os quatro sprites e o loop de cada forma, o início sem transformações, rejeição de formas bloqueadas por código e pelo Inspector, reativação do Player e os ciclos de Tab com todas ou apenas algumas formas liberadas.

`AbilityPickupTests.cs` verifica o desbloqueio de cada forma, duplicatas, espera pelo fim do passo, altura por célula, descida e transparência, acompanhamento do Player em movimento, remoção do item, isolamento entre grids, os quatro frames das cinco animações e a configuração dos coletáveis na cena exemplo.

Os testes em `Assets/Tests/PlayMode/GridMovementTests.cs` podem ser executados pelo Unity Test Runner em PlayMode. Cobrem deslocamento e alinhamento, colisão com paredes, ausência de chão, bloqueio por outro bloco, rejeição de diagonais, proteção durante deslocamento, colisão com o personagem e integração com a cena do exemplo.

`EquationValidatorTests.cs` cobre leitura nas duas direções, dezenas e centenas, precedência, frações, contas inválidas, lacunas, cruzamentos, blocos fixos, sobreposição, revalidação após movimento e integração com o pai `Blocks` da cena. Também verifica todas as formas do Player nos dois eixos, exclusão de `Default`, entrada e saída de uma equação, troca de forma, ativação e remoção do personagem, cruzamentos e preservação das regras de aritmética.

`EquationGateTests.cs` cobre a associação à âncora, rejeição de erros e de soluções em outras equações, abertura nas duas orientações, colisão, bloqueio do jogador, restauração da câmera, aberturas simultâneas, abertura com equação completada pelo Player e configuração da cena exemplo.

`SceneTransitionTests.cs` cobre os códigos de sala, resolução pelo Build, pareamento pelo destino de retorno entre saídas para outras salas, travessia real nos dois sentidos com chegada na célula vizinha, bloqueio pelo portão fechado, preservação das formas, proteção contra cargas simultâneas e chegada ao centro quando a contraparte não existe, aponta para outra sala, está duplicada ou tem chegada sem chão ou sobre parede. Também cobre a chegada sem portão e triggers sobrepostos. Também verifica que o portão recebe o Player aberto, aguarda o fim da saída e fecha com animação, tremor e bloqueio de movimento, restaurando a câmera depois. Os testes dos portões cobrem mudanças manuais e pelo Inspector, abertura, fechamento, inversão de animações e permanência aberta quando esse é o estado padrão da cena.

A transição visual também é verificada com as duas salas carregadas simultaneamente, câmera em posição intermediária, uma única câmera ativa, controles pausados e posições físicas dos blocos restauradas ao final. A ausência de câmera no destino é testada para confirmar a restauração dos controles e da sala de origem.
