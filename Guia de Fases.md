# Guia de Fases de Equation Shift

Este guia reúne o percurso da campanha, as soluções de referência e as instruções para criar novas salas no Unity. A tabela contém soluções de fases, inclusive da sala final.

- [Progressão e contas da campanha](#progressão-e-contas-da-campanha)
- [Montagem de fases no Unity](#montagem-de-fases-no-unity)

Para consultar detalhes dos scripts e das animações, veja a [documentação de gameplay](Assets/Scripts/Gameplay/README.md).

## Tela de título

Abra `Assets/Scenes/Title.unity` e entre no Play Mode. A cena usa Monocraft, com o título centralizado e 54 instâncias decorativas dos prefabs de blocos subindo na diagonal em velocidades diferentes. O botão **Jogar** inicia uma nova partida na Vila dos Sinais (`G1-Fase_1`), com uma breve transição para preto. Na tela de fim, **Jogar Novamente** retorna ao título. A cena de título é a primeira da lista do Build.

No objeto `Title Screen`, ajuste `Diagonal Velocity` para mudar a direção e a velocidade do fundo e `First Level` para escolher a fase inicial. Os blocos são apenas decorativos nessa cena: seus componentes de física e de tabuleiro foram removidos nas instâncias, preservando os prefabs originais. Para reconstruir o layout padrão, use **Equation Shift > Criar cena de titulo**; isso substitui a cena de título salva.

## Progressão e contas da campanha

A partida começa em **G1**, na Vila dos Sinais. Converse por proximidade com cinco NPCs diferentes, incluindo o Erro, para liberar a saída. As contas começam em **G2**. As salas 00 e A0 são exemplos de montagem. As primeiras contas pedem poucos empurrões. As seguintes combinam operadores, números de dois dígitos e mais blocos móveis. As cinco salas de obtenção têm uma conta curta para ensinar a nova forma.

### Ordem de progressão

1. **Igual** — G2 → G3 → H3. Pegue Igual em H3, use-o para voltar a G3 e siga por G4 e G5 até G6.
2. **Mais** — G6 → F6 → E6 → D6. Pegue Mais em D6 e use-o para voltar a E6. Retorne por F6 a G6 e abra a passagem para G7 com Mais.
3. **Menos** — Pegue Menos em G7 e use-o para sair rumo a G6. Volte por F6 a E6, use Menos para abrir E5 e depois use Mais para chegar a E4. Siga por D4 e C4 até B4.
4. **Vezes** — De B4, vá a B5 e pegue Vezes. Use-o na saída para B6 e, depois, na passagem de B7 para C7.
5. **Divisão e alavancas** — De C7, desça a D7, pegue Divisão e use-a para voltar a C7. Abra C6 com Divisão e acione sua alavanca. Volte por C7, B7, B6, B5 e B4 até B3. Use Divisão para chegar a B2, depois Vezes para chegar a C2 e acionar a segunda alavanca. Volte a B4 por B2 e B3.
6. **Fase final** — Entre em A4 pelo portão superior de B4. Após a fuga do Erro, resolva as quatro contas menores para liberar peças e monte a igualdade principal com as duas âncoras. O portão D inicia a sequência que leva à tela **Fim de Jogo**.

### Contas por portão

Cada caractere da conta ocupa uma célula. Os números entre parênteses na coluna de fixos indicam a posição na conta, contando a partir de **1**. Por exemplo, em `15-6=9`, o `1` e o `5` ocupam as células 1 e 2. Cada conta usa o valor da âncora que já existe na cena. A coluna de móveis lista as peças empurráveis da solução proposta. A validação verifica os blocos envolvidos e o resultado da igualdade. Uma montagem diferente também pode funcionar se atender às condições do portão.

Quando a tabela indica uma transformação, ocupe a célula indicada com o símbolo do Player. A exigência de uma forma específica em tempo de execução depende do campo `Required Player Form` da instância do portão. No Unity, use `*` para Vezes e `/` para Divisão. O símbolo do Player fica fora da lista de blocos móveis.

| Fase | Portão e destino | Conta a montar | Blocos fixos sugeridos | Blocos móveis | Forma na solução | Observação |
| --- | --- | --- | --- | --- | --- | --- |
| G2 | Direita → G3 | `2+4=6` | `2` (1), `+` (2), `=` (4), `6` (5, âncora) | `4` | Nenhuma | — |
| G3 | Baixo → H3 | `7-4=3` | `7` (1), `=` (4), `3` (5, âncora) | `-`, `4` | Nenhuma | Desvio para obter Igual. |
| H3 | Topo → G3 | `9=9` | `9` (1, âncora), `9` (3) | — | Igual `=` na célula 2 | Obtenção e tutorial. |
| G3 | Direita → G4 | `2+3=5` | `2` (1, âncora), `+` (2), `5` (5) | `3` | Igual `=` na célula 4 | — |
| G4 | Direita → G5 | `3+4-2=5` | `3` (1, âncora), `+` (2), `=` (6), `5` (7) | `4`, `-`, `2` | Nenhuma | — |
| G5 | Esquerda → G4 | `1+4=5` | `1` (1, âncora), `+` (2), `=` (4), `5` (5) | `4` | Nenhuma | Opcional. O portão começa aberto na cena atual. |
| G5 | Direita → G6 | `15-6+2=11` | `1` (1), `5` (2, âncora), `+` (5), `=` (7), `1` (9) | `-`, `6`, `2`, `1` | Nenhuma | — |
| G6 | Topo → F6 | `3*2+4=10` | `3` (1, âncora), `+` (4), `=` (6), `0` (8) | `*`, `2`, `4`, `1` | Nenhuma | Desvio para obter Mais. |
| F6 | Topo → E6 | `8+7*2=22` | `8` (1, âncora), `+` (2), `=` (6), `2` (8) | `7`, `*`, `2` ×2 | Nenhuma | — |
| E6 | Topo → D6 | `3*4+5=17` | `3` (1, âncora), `+` (4), `=` (6), `1` (7) | `*`, `4`, `5`, `7` | Nenhuma | — |
| D6 | Baixo → E6 | `3+5=8` | `3` (1), `5` (3), `=` (4), `8` (5, âncora) | — | Mais `+` na célula 2 | Obtenção e tutorial. |
| G6 | Direita → G7 | `3+5*2=13` | `3` (1, âncora), `*` (4), `=` (6), `1` (7) | `5`, `2`, `3` | Mais `+` na célula 2 | Voltar de D6 via E6 e F6. |
| G7 | Esquerda → G6 | `7-3=4` | `7` (1), `3` (3), `=` (4), `4` (5, âncora) | — | Menos `-` na célula 2 | Obtenção e tutorial. |
| E6 | Esquerda → E5 | `19-3*4+2=9` | `1` (1), `9` (2, âncora), `*` (5), `=` (9), `9` (10) | `3`, `4`, `+`, `2` | Menos `-` na célula 3 | — |
| E5 | Esquerda → E4 | `5+6*2-3=14` | `5` (1, âncora), `*` (4), `-` (6), `=` (8), `1` (9) | `6`, `2`, `3`, `4` | Mais `+` na célula 2 | — |
| E4 | Topo → D4 | `3*7-8+4=17` | `3` (1, âncora), `-` (4), `=` (8), `7` (10) | `*`, `7`, `8`, `+`, `4`, `1` | Nenhuma | — |
| D4 | Topo → C4 | `18-4*2+3=13` | `8` (2, âncora), `-` (3), `*` (5), `=` (9), `3` (11) | `1` ×2, `4`, `2`, `+`, `3` | Nenhuma | — |
| C4 | Topo → B4 | `27-3*5+2=14` | `2` (1), `7` (2, âncora), `*` (5), `=` (9), `4` (11) | `-`, `3`, `5`, `+`, `2`, `1` | Nenhuma | — |
| B4 | Topo → A4 | — | — | — | Nenhuma | Acionar as alavancas de C2 e C6. |
| B5 | Direita → B6 | `2*5=10` | `2` (1), `5` (3, âncora), `=` (4), `1` (5), `0` (6) | — | Vezes `*` na célula 2 | Obtenção e tutorial. |
| B6 | Direita → B7 | `5*4-3+2=19` | `5` (1, âncora), `*` (2), `-` (4), `=` (8), `9` (10) | `4`, `3`, `+`, `2`, `1` | Nenhuma | — |
| B7 | Baixo → C7 | `12+3*4-5=19` | `1` (1, âncora), `2` (2), `+` (3), `-` (7), `=` (9), `9` (11) | `3`, `4`, `5`, `1` | Vezes `*` na célula 5 | — |
| D7 | Topo → C7 | `9/3=3` | `9` (1, âncora), `3` (3), `=` (4), `3` (5) | — | Divisão `/` na célula 2 | Obtenção e tutorial. |
| C7 | Esquerda → C6 | `9/3+4*2-1=10` | `9` (1, âncora), `+` (4), `*` (6), `-` (8), `=` (10), `0` (12) | `3`, `4`, `2`, `1` ×2 | Divisão `/` na célula 2 | Alavanca de C6. |
| B3 | Esquerda → B2 | `45/5+3*4-2=19` | `4` (1), `5` (2, âncora), `+` (5), `*` (7), `-` (9), `=` (11), `9` (13) | `5`, `3`, `4`, `2`, `1` | Divisão `/` na célula 3 | Voltar de C6 pelo caminho de B4. |
| B2 | Baixo → C2 | `24-3*5+18/6=12` | `2` (1), `4` (2, âncora), `-` (3), `+` (7), `/` (10), `=` (12), `2` (14) | `3`, `5`, `1` ×2, `8`, `6` | Vezes `*` na célula 5 | Alavanca de C2. |
| A4 | `Gate_Horizontal_1` | `5=10/2` | `5` (1, `_Ancor_1`), `=` (2) | `1`, `0`, `2` | Divisão `/` na célula 5 | Libera o bloco `/`. |
| A4 | `Gate_Horizontal_2` | `9=8+1` | `9` (1, `_Ancor_2`), `=` (2) | `8`, `1` | Mais `+` na célula 4 | Libera o bloco `+`. |
| A4 | `Gate_Horizontal_3` | `2*2=4` | `=` (4), `4` (5, `_Ancor_3`) | `2` ×2 | Vezes `*` na célula 2 | Libera o bloco `*`. |
| A4 | `Gate_Horizontal_4` | `8-2=6` | `=` (4), `6` (5, `_Ancor_4`) | `8`, `2` | Menos `-` na célula 2 | Libera o bloco `-`. |
| A4 | `Gate_Horizontal_D` → sequência final | `48/2+11=20+2*9-3` | `4` (1), `1` (7, `_Ancor_D1`), `2` (9, `_Ancor_D2`), `+` (11), `9` (14), `3` (16) | `8`, `/`, `2` ×2, `+`, `1`, `0`, `*`, `-` | Igual `=` na célula 8 | Os dois lados valem 35. As duas âncoras precisam estar na mesma conta verdadeira. |

Em A4, reutilize os dígitos móveis nas contas menores e na principal. As formas indicadas na tabela preenchem lacunas das soluções propostas. Os portões numerados verificam a participação da âncora correspondente. No portão D, uma igualdade verdadeira com somente `_Ancor_D1` ou `_Ancor_D2` mostra a dica do protagonista. Para concluir, reúna as duas âncoras na mesma igualdade verdadeira.

### Configuração das salas da campanha

- **Salas sem portão de conta:** C2 e C6 têm as alavancas. O portão superior de B4 abre com as duas ativações. A passagem inferior de C7 para D7 é livre para que o jogador possa buscar Divisão.
- **Coletáveis:** Igual em H3, Mais em D6, Menos em G7, Vezes em B5 e Divisão em D7.
- **Obrigatoriedade das formas:** os 13 portões da tabela com transformação exigem que a equação inclua a âncora e o Player na forma indicada. A abertura temporária do portão permite entrar na sala. A saída pelo portal é liberada quando a conta é resolvida.
- **Blocos e âncoras:** os fixos marcam a linha horizontal da conta. Nas fases numeradas, os móveis começam deslocados na coluna da célula correspondente e podem ser empurrados pela parte de cima ou de baixo, conforme a sala.
- As cenas da campanha são numeradas de `G1-Fase_1` a `C2-Fase_25`, seguidas de `A4-Fase_Final`. A cena de C6 no Build é `C6-Fase_22`. A lista também inclui `Title`, `GameOver` e as duas salas de exemplo.

## Montagem de fases no Unity

Duplique uma cena de exemplo e aproveite a estrutura de Grid, câmera e componentes já configurada.

### 1. Começando uma fase

1. Saia do Play Mode e salve as alterações atuais.
2. Duplique `Assets/Scenes/00-FaseExemplo_1.unity` ou `Assets/Scenes/A0-FaseExemplo_2.unity`.
3. Renomeie a cópia com um **código único de dois caracteres**, seguido de hífen. Exemplo: `G2-Fase_2.unity`, `1A-SalaInicial.unity` ou `4G-SalaDivisao.unity`.
4. Abra a cópia e confira a hierarquia e as referências descritas abaixo.
5. Desenhe o chão, as paredes, o puzzle e suas passagens.
6. Configure os destinos das saídas nas duas salas envolvidas.
7. Adicione a nova cena à **Scene List em File > Build Profiles** e deixe-a habilitada.
8. Salve e teste a sala, inclusive entrando e saindo por suas conexões.

O código da sala vem do nome do arquivo. Não existe um cadastro separado de códigos. Ao procurar `G2`, o jogo busca uma cena habilitada no Build cujo nome comece com `G2-`.

**Não deixe duas cenas habilitadas com o mesmo código.** Ao duplicar uma cena, o Unity pode gerar um nome como `00-FaseExemplo_1 1`: renomeie o código antes de incluí-la no Build. Ter o arquivo em `Assets/Scenes` não basta para permitir a viagem até ele.

### 2. Hierarquia recomendada

Use esta organização como referência. Os nomes dos objetos podem ajudar na organização, mas as referências dos componentes é que ligam o funcionamento da fase.

```text
G2-Fase_2
├── Main Camera                    Camera + GateOpeningFeedback
├── Global Light 2D
├── BackgroundBlack                 Fundo visual
└── Grid                           Grid + LevelGrid + RoomScene
    ├── Ground                     Tilemap de chão
    ├── Walls                      Tilemap de paredes + TilemapCollider2D
    ├── Structs                    Tilemap de decoração
    ├── Player                     TopDownGridPlayer
    ├── Blocks                     EquationValidator
    │   ├── StaticBlocks            Números e operadores fixos; âncoras
    │   └── MovableBlocks           Números e operadores empurráveis
    ├── Gates                      Portões
    ├── Interactables              Coletáveis de transformações
    └── Transitions                Saídas invisíveis com ScenePortal
```

Mantenha uma câmera principal e um Player configurados em cada sala. Os blocos precisam estar dentro do grupo monitorado pelo `EquationValidator`; seguir `Grid/Blocks` evita deixar um bloco fora das contas por engano.

Use nomes que identifiquem a função dos objetos, como `Block_Static_Digit_6_Ancor_R`, `Block_Movable_Operator_Plus`, `Gate_Vertical_R`, `Exit_Right_To_G3` e `AbilityPickup_Plus`. Acrescente `_02`, `_03` e assim por diante quando houver blocos com o mesmo nome no grupo.

Confira estas referências depois de duplicar a cena ou adicionar objetos:

| Objeto/componente | O que conferir no Inspector |
|---|---|
| `Grid / LevelGrid` | `Ground` aponta para o chão desta sala; `Walls` aponta para suas paredes. |
| `Grid / RoomScene` | O componente existe e está habilitado. Prepara a sala para a transição com câmera. |
| `Player / TopDownGridPlayer` | `Board` aponta para o `LevelGrid` desta sala. |
| `Blocks / EquationValidator` | `Board` é o grid; `Blocks Root` é o grupo `Blocks`; `Player` é o personagem desta sala; `Validate Automatically` está habilitado. |
| `Main Camera / GateOpeningFeedback` | **`Player` aponta para o personagem desta sala. Não deixe vazio.** |

O `LevelGrid` é o componente que representa o tabuleiro. Nos campos `Board`, arraste o objeto `Grid` que contém esse componente.

### 3. Posição dos objetos no grid

O movimento acontece em quatro direções, uma célula por passo. Personagem e blocos devem começar **no centro das células**, sobre chão e sem sobreposição.

No grid padrão de células `1 × 1`, com origem XY em zero, os centros são posições como `(0.5, 0.5)`, `(1.5, 0.5)` e `(-0.5, -1.5)`. Por exemplo, o centro da célula `(2, 3)` fica em `(2.5, 3.5)`. Esse exemplo muda se você deslocar ou redimensionar o grid.

- Posicione os blocos com uma célula de distância entre seus centros quando eles devem ficar encostados na conta.
- Deixe X e Y dos grupos organizadores, como `Blocks` e `MovableBlocks`, em zero sempre que possível. Use escala `1` e evite rotação nesses grupos.
- Preserve as posições Z e as configurações de desenho da cena de exemplo. Elas organizam o que aparece na frente e atrás; não são andares diferentes do tabuleiro.
- Mantenha `Move Duration` do Player e dos blocos móveis igual. O padrão é `0,16` segundo.

O jogo ajusta Player e blocos ao centro da célula quando a fase começa. Posicioná-los corretamente no editor evita que sejam ajustados para uma célula diferente da pretendida.

Durante o jogo, **R** restaura os blocos às posições iniciais da sala, permitindo recuperar blocos presos nas extremidades. O personagem volta à entrada por onde chegou da última sala; na primeira sala, usa a posição inicial da cena. Se o portão dessa entrada estiver fechado, ele abre para receber o personagem e volta a fechar após sua saída. As transformações coletadas e a forma selecionada permanecem disponíveis, inclusive as trazidas de outras salas. O reset funciona automaticamente em cenas com `TopDownGridPlayer`, sem configurar outro componente.

### 4. Editando o cenário com Tilemaps

Selecione o Tilemap correto na hierarquia e use a **Tile Palette** para pintar ou apagar as células.

| Tilemap | Para que serve | Efeito no movimento |
|---|---|---|
| `Ground` | Chão da sala e dos corredores. | Sem tile de chão, o Player e os blocos não entram na célula. |
| `Walls` | Paredes e obstáculos fixos. | Uma célula com parede bloqueia a passagem, mesmo que tenha chão embaixo. |
| `Structs` | Detalhes e decoração. | No padrão do projeto, é visual. Se adicionar um collider sólido, ele também pode bloquear os atores. |

Para aumentar uma sala, pinte o novo chão em `Ground` e ajuste o contorno em `Walls`. Para criar um obstáculo interno, pinte a parede em `Walls`. Para criar uma área sem chão, apague o tile em `Ground`.

**Trocar apenas o desenho de um tile de chão por um desenho de buraco não cria um buraco lógico:** enquanto houver um tile em `Ground` e nenhuma parede, a célula continuará sendo considerada chão.

Para criar uma passagem:

1. Abra um espaço no Tilemap `Walls` no caminho que será atravessado.
2. Garanta chão em `Ground` desde o interior da sala, passando pelo portão, até a saída invisível.
3. Posicione o portão e o portal no centro das células desse caminho.
4. Confira a passagem com o portão aberto. Uma parede pintada atrás dele continuará bloqueando o Player.

Mantenha o `TilemapCollider2D` de `Walls` e tiles com colisão configurada. Não coloque um `BoxCollider2D` cobrindo toda a sala em `Ground`, `Walls` ou `Structs`: isso bloqueia também os espaços que deveriam estar livres. O chão não precisa de collider sólido.

Ao mudar bastante o tamanho da sala, confira o enquadramento da câmera e o fundo. Use enquadramentos semelhantes entre salas conectadas para manter o deslizamento da câmera consistente.

### 5. Colocando números e operadores

Os prefabs estão em:

- `Assets/Prefabs/Blocks/Static`: blocos fixos.
- `Assets/Prefabs/Blocks/Movable`: blocos empurráveis.

Arraste o prefab para o grupo correspondente em `Grid/Blocks` e alinhe-o ao centro de uma célula com chão.

Exemplos de nomes:

| Prefab | Uso |
|---|---|
| `Block_Static_Digit_6` | Número 6 fixo, que também pode ser configurado como âncora. |
| `Block_Movable_Digit_4` | Número 4 que pode ser empurrado. |
| `Block_Movable_Operator_Plus` | Operador `+` empurrável. |
| `Block_Movable_Operator_Equals` | Sinal `=` empurrável. |

Cada `GridPushBlock` guarda **um único caractere** em `Value`: um dígito de `0` a `9` ou um dos operadores `+`, `-`, `*`, `/`, `=`. Para formar `12`, use dois blocos consecutivos: `[1][2]`.

Confira `Board`, `Value` e `Is Movable`. O prefab já traz sprite, collider e Rigidbody2D adequados ao seu tipo. Nos prefabs de blocos, `Board` pode ficar vazio quando a instância está dentro do Grid: ele procura o `LevelGrid` nos pais. Atribuí-lo explicitamente à sala também é válido.

**Renomear um objeto não muda seu número nem seu sprite.** Para trocar um 4 por um 6, prefira substituir pelo prefab correto. Se editar manualmente, ajuste tanto `Value` quanto o sprite.

O Player empurra somente um bloco por vez. Ele precisa alcançar o lado de trás do bloco, e a célula seguinte precisa estar livre. Não há puxão, empurrão em cadeia ou deslocamento diagonal.

### 6. Como o jogo lê as contas

As contas são lidas em células consecutivas:

- Na horizontal, da esquerda para a direita.
- Na vertical, de cima para baixo.
- Uma célula vazia interrompe a sequência.
- Diagonais não conectam blocos.
- Blocos fixos e móveis participam da mesma maneira.
- Uma sequência precisa ter exatamente um `=` e expressões válidas dos dois lados.

Exemplos de soluções válidas:

```text
[1][+][2][=][3]                 1+2=3
[4][-][1][+][3][=][6]           4-1+3=6
[1][2][+][3][=][1][5]           12+3=15
[2][+][3][*][4][=][1][4]        2+3*4=14
[8][/][2][=][4]                 8/2=4
```

Multiplicação e divisão são calculadas antes de soma e subtração. Operações de mesma prioridade são resolvidas da esquerda para a direita. Números negativos são aceitos usando `-`. A divisão trabalha com frações exatas, mas divisão por zero é inválida. Use os símbolos dos blocos: `*` para multiplicar e `/` para dividir.

Uma conta pode cruzar outra. Nesse caso, o jogo verifica as sequências horizontal e vertical separadamente. Confira se o cruzamento não cria uma conta acidental ou um número diferente do esperado.

As contas aguardam o fim da movimentação dos blocos. Não coloque dois participantes na mesma célula: a sobreposição invalida a configuração.

Para conferir uma montagem, selecione `Blocks` e consulte `Results` no `EquationValidator`. O menu de contexto **Validar equacoes** permite verificar a posição montada também fora do Play Mode. Em `Results`, veja o texto lido, se a conta é verdadeira e possíveis erros. `Log Results On Change` ajuda a acompanhar isso no Console durante os testes.

#### O Player como operador

Depois de coletar uma transformação, o Player pode representar um operador enquanto está parado na célula:

| Forma | Símbolo |
|---|---|
| `Default` | Não participa da conta. |
| `Equal` | `=` |
| `Plus` | `+` |
| `Minus` | `-` |
| `Mult` | `*` |
| `Divis` | `/` |

Por exemplo, `[1][Player em Plus][2][=][3]` forma `1+2=3`. O Player também pode completar o `=` entre dois números iguais. Durante seu movimento como operador, a validação aguarda o fim do passo.

Use `Tab` para alternar entre as formas coletadas. Ao iniciar uma sala diretamente em Play, o Player começa em `Default`, sem transformações liberadas. Ao chegar por uma transição, ele recebe as formas coletadas anteriormente. Para testar uma conta que exige uma transformação, entre pelo caminho normal ou disponibilize o coletável necessário na fase; mudar somente o nome do Player ou escolher uma forma bloqueada não libera a transformação.

### 7. Ligando uma conta a um portão

Um portão de equação abre quando uma igualdade verdadeira inclui o bloco configurado como âncora. Se houver uma segunda âncora ou uma forma obrigatória, a mesma igualdade deve incluir esses participantes. Portões como o topo de B4 e a saída de G1 têm condições de alavanca ou de história.

#### Configurar a âncora

1. Escolha um bloco fixo que faça parte da solução.
2. Marque `Is Ancor` no `GridPushBlock`.
3. Defina `Ancor Side` com o lado da passagem.
4. Deixe `Is Movable` desmarcado. Uma âncora é imóvel mesmo se essa opção estiver marcada por engano.

| Lado | Significado | Prefab de portão usual |
|---|---|---|
| `T` | Topo | `Gate_Horizontal` |
| `D` | Baixo | `Gate_Horizontal` |
| `L` | Esquerda | `Gate_Vertical` |
| `R` | Direita | `Gate_Vertical` |

O lado identifica a passagem. Ele não obriga a conta a ter a mesma direção do portão, nem obriga a âncora a estar no fim da conta.

#### Configurar o portão

Arraste um prefab de `Assets/Prefabs/Gates` para `Grid/Gates`, centralizado na passagem. O horizontal tem área de `3 × 1`; o vertical, `1 × 3`. Use a orientação apropriada e mantenha suas animações e collider.

No `EquationGate`, confira:

| Campo | O que colocar |
|---|---|
| `Validator` | O `EquationValidator` de `Blocks` desta sala. |
| `Ancor` | O bloco escolhido como âncora. |
| `Additional Ancor` | Segunda âncora opcional, como em A4. As duas precisam participar da mesma igualdade. |
| `Required Lever Ids` | IDs das alavancas que abrem o portão. O portão superior de B4 usa as de C2 e C6. |
| `Passage Side` | O mesmo lado definido no `Ancor Side` da âncora. |
| `Required Player Form` | Forma que o Player precisa usar como parte da equação. Use `Default` quando a conta puder ser feita só com blocos. |
| `Feedback` | O `GateOpeningFeedback` da Main Camera desta sala. |
| `Opening Animation` / `Closing Animation` | Os clipes da orientação do prefab; normalmente já vêm preenchidos. |
| `Blocking Colliders` | Os colliders sólidos do portão; normalmente já vêm preenchidos. |
| `Is Open` | Se a sala deve começar com o portão aberto. |

Na **Main Camera**, confira também **`GateOpeningFeedback > Player`**. Esse campo precisa apontar para o Player. Se estiver vazio, a abertura pela equação não inicia, mesmo quando a conta está correta.

Ao resolver a conta, o jogo bloqueia novos passos, toca o som de acerto, executa o tremor e anima a abertura. A colisão é liberada ao final da animação. O portão continua aberto se você desmontar a conta na mesma visita, o que permite reutilizar as peças.

Ao chegar de outra sala sobre um portão, ele aparece aberto para receber o Player. Se `Is Open` estava desmarcado na cena, fecha atrás do personagem após ele sair completamente, com animação e tremor. Se estava marcado, permanece aberto. Essa regra de chegada é diferente de desmontar uma equação.

### 8. Colocando as saídas e conectando salas

Use `Assets/Prefabs/Transitions/ScenePortal.prefab`, dentro de `Grid/Transitions`. O portal é invisível durante o jogo e possui um `BoxCollider2D` com **Is Trigger** marcado.

Coloque-o **uma célula depois do portão, para o lado de fora da sala**. Assim, a célula vizinha voltada para dentro fica sobre o portão e recebe quem chega. Garanta chão até o portal, inclusive fora do enquadramento da câmera.

```text
Interior da sala → célula do portão → célula do portal invisível
```

Configure o `ScenePortal`:

| Campo | O que definir |
|---|---|
| `Destination Scene Id` | Código de dois caracteres da sala para onde essa saída leva. |
| `Spawn Direction` | Direção do portal para dentro da própria sala. Define a chegada de quem vem pela contraparte. |
| `Arrival Gate` | O portão que recebe o Player. Pode ficar vazio se ele estiver centralizado na célula vizinha. |
| `Transition Duration` | Tempo do deslizamento da câmera; padrão `0,5` segundo. |
| `Board` | O `LevelGrid` desta sala. Pode ficar vazio quando o portal está sob o Grid. |

Use estas direções:

| Local da saída nesta sala | `Spawn Direction` para dentro |
|---|---|
| Topo | `(0, -1)` — baixo |
| Baixo | `(0, 1)` — cima |
| Esquerda | `(1, 0)` — direita |
| Direita | `(-1, 0)` — esquerda |

Para ligar `G2` a `1A`, coloque destino `1A` na saída de `G2` e destino `G2` na saída de `1A`. **O pareamento usa somente o destino de retorno.** Os nomes dos objetos não precisam ser iguais e não existe `Connection Id`.

Mantenha uma única saída ativa para cada destino dentro de uma sala. A chegada usa a célula indicada por Spawn Direction mesmo sem portão ou com um trigger sobreposto. Se não houver uma contraparte única, ou essa célula estiver sem chão ou sobre uma parede, o jogo procura uma célula livre perto do centro. Essa alternativa evita uma chegada presa, mas a conexão correta deve ser testada junto ao portal.

#### Alinhar as saídas entre salas

**Geralmente, mantenha as saídas na mesma linha da outra sala.** Isso faz a passagem parecer um corredor contínuo e evita um deslizamento lateral inesperado da câmera.

- Conexão direita ↔ esquerda: use a mesma linha, isto é, a mesma coordenada **Y** das saídas, considerando grids com a mesma origem.
- Conexão baixo ↔ topo: use a mesma coluna, isto é, a mesma coordenada **X**.
- Mantenha também o portão alinhado com seu portal e com o corredor.

Por exemplo, uma saída à direita em `G2` com `Y = -1.5` deve ter sua contraparte à esquerda em `1A` geralmente também com `Y = -1.5`. Uma saída inferior com `X = -0.5` deve encontrar a entrada superior geralmente na coluna `X = -0.5`.

Mantenha o mesmo tamanho de célula, escala e origem XY dos grids nas salas conectadas. **Não desloque toda a cena no editor para colocá-la fisicamente ao lado da outra:** o controlador faz esse posicionamento temporário durante a transição e restaura as coordenadas ao terminar. As duas cenas de exemplo já têm `RoomScene` para preparar esse processo.

### 9. Usando prefabs sem misturar configurações de salas

O prefab guarda uma configuração reutilizável. A instância colocada na cena guarda os ajustes daquela sala.

- Use o prefab correto de número, operador e tipo de movimento.
- Na instância do bloco, ajuste posição, `Board` e configuração de âncora quando necessário.
- Na instância do portão, ajuste âncora, validador, feedback, lado e estado inicial.
- Na instância do portal, ajuste destino, direção de chegada, portão e duração da transição.
- Confira as referências depois de copiar um objeto entre cenas. Elas devem apontar para os objetos da sala nova.

**Mantenha esses ajustes específicos como Overrides da instância.** Evite usar `Apply All` para gravar destinos de salas ou configurações de âncoras no prefab compartilhado. Isso pode mudar outras instâncias que deveriam ter outra função. Referências a objetos da cena, como Player e validador, devem ser configuradas na própria cena.

Edite o asset do prefab quando a intenção for mudar todas as suas instâncias, como corrigir o sprite, a animação ou o tamanho padrão do collider. Antes de usar `Revert`, confira os Overrides: ele pode remover o destino do portal ou a ligação da âncora que você configurou.

### 10. Colocando transformações

Duplique um coletável já configurado em `Grid/Interactables`. Confira `Form`, sprite, Animator, `Board` e Player da sala. O objeto deve ter `AbilityPickup` e não deve receber `GridPushBlock` ou um collider sólido bloqueando sua coleta.

A transformação é coletada quando o Player termina um passo na célula do item. Depois da animação, o Player assume a nova forma e a dica “Use Tab para trocar de forma.” aparece. Posicione o coletável em um local acessível antes da conta que exige aquela transformação.

### 11. Dicas para criar as contas e os puzzles

#### Monte primeiro a solução

Escolha o portão e sua âncora. Depois, monte uma conta verdadeira e confira em `Results`. Só então afaste os blocos móveis para criar a posição inicial do desafio.

| Ideia | Solução possível | O que variar na montagem |
|---|---|---|
| Introdução à soma | `1+2=3` | Deslocar um número ou o `=` uma célula. |
| Soma e subtração | `4-1+3=6` | Usar o 6 como âncora e preparar o alinhamento dos móveis. |
| Multiplicação | `2*3=6` | Restringir onde o operador pode ser colocado. |
| Divisão | `8/2=4` | Exigir escolher a ordem correta dos números. |
| Números com dois dígitos | `12+3=15` | Trabalhar a união dos dígitos em células consecutivas. |
| Player como operador | `[2][Player em Equal][2]` | Reservar a célula central e disponibilizar a transformação `Equal`. |

Esses são exemplos de contas, não garantias de um puzzle solucionável: a posição inicial, as paredes e o espaço para empurrar também precisam ser testados.

#### Teste o caminho das peças

- O Player consegue chegar atrás de cada bloco que precisa empurrar?
- Há chão livre para o deslocamento e espaço para circular depois?
- Um bloco necessário pode ficar preso em um canto ou contra a parede?
- É possível montar a solução sem puxar blocos ou empurrar dois de uma vez?
- A solução contém a âncora do portão certo?
- Depois de abrir o portão, o caminho até a saída continua acessível?
- Ao chegar com o portão aberto temporariamente, o Player consegue sair dele e entrar na sala?
- Se houver duas contas, as peças podem ser reutilizadas sem criar um bloqueio inevitável?

#### Aumente a dificuldade aos poucos

Comece com poucos blocos móveis e uma operação conhecida. Depois introduza espaço limitado, mais de um operador, números com dois dígitos, cruzamentos ou a participação do Player. Ensine uma regra de cada vez antes de combiná-las.

Prefira aumentar a dificuldade pelo posicionamento e pelas escolhas de movimento. Uma conta numericamente grande não é necessariamente um puzzle melhor. Procure também soluções alternativas: uma conta diferente da planejada pode ser válida se usar a âncora correta. Decida se isso faz parte do desafio ou se o layout precisa ser ajustado.

Se uma tentativa prender peças, pressione `R` em jogo para devolver os blocos às posições iniciais e voltar ao ponto de entrada ou ao ponto inicial. Esse comando atua na sala carregada. Para testar toda a progressão desde o começo, entre pela tela `Title` e clique em **Jogar**.

### 12. Menus de configuração: quando usar

Para criar uma fase a partir dos prefabs existentes, normalmente basta duplicar a cena e ajustar seus objetos. Os menus abaixo têm funções específicas:

| Menu `Equation Shift` | Comportamento |
|---|---|
| `Configurar Fase Exemplo` | Abre e salva a primeira cena de exemplo; não configura automaticamente a sala nova que estiver aberta. |
| `Configurar portoes da Fase Exemplo` | Reconfigura os portões da primeira cena de exemplo e seus assets. |
| `Configurar transicoes das Fases Exemplo` | Reconfigura as duas cenas de exemplo e suas conexões; não é um cadastro geral de novas salas. |
| `Configurar coletaveis de transformacoes` | Configura os coletáveis reconhecidos na cena ativa. |
| `Padronizar blocos e criar prefabs` | Percorre as cenas de `Assets/Scenes` e processa os blocos com nomes reconhecidos `block_<valor>`, podendo atualizar cenas e prefabs. |

Salve o trabalho antes de usar um desses comandos e confira seu alcance. Na edição normal, o nome do bloco não define a conta; no gerador de blocos, os nomes reconhecidos são usados para preencher os valores.

### 13. Problemas comuns

| Problema | O que verificar |
|---|---|
| A conta está correta, mas o portão não abre. | `Results`; participação da âncora; `Is Ancor`; correspondência entre `Ancor Side` e `Passage Side`; referências de Validator e Feedback; **Player no GateOpeningFeedback da câmera**. |
| O texto lido é diferente do planejado. | Células vazias, dígitos encostados formando outro número, sobreposições e sentido de leitura. |
| Um bloco não participa da conta. | `Value`, ativação do objeto e se ele está dentro de `Blocks Root`. |
| O Player ou um bloco não anda. | Chão em `Ground`, parede em `Walls`, outros colliders sólidos, célula ocupada, âncora ou bloco fixo. |
| O portão abriu, mas a passagem continua bloqueada. | Tiles de parede atrás dele, falta de chão e colliders extras no corredor. |
| A saída não troca de sala. | Código do destino, nome do arquivo, cena habilitada no Build, códigos duplicados e trigger do portal. O Player precisa terminar o passo na célula do portal. |
| O Player aparece no centro da sala. | Contraparte ausente ou duplicada; destino de retorno errado; chegada sem chão ou sobre uma parede. |
| A câmera desliza de forma desalinhada. | Mesma linha/coluna das saídas, `Spawn Direction`, origem e escala dos grids e enquadramento das câmeras. |
| A transição tem problemas de iluminação ou câmera. | `RoomScene` no Grid, uma câmera principal configurada por sala e duplicação indevida de componentes. |
| Uma transformação não fica disponível. | Coleta da transformação; ao iniciar diretamente em Play, o Player começa sem as formas das salas anteriores. |

### 14. Conferência antes de considerar a fase pronta

- [ ] Nome com código único de dois caracteres e cena habilitada no Build.
- [ ] Grid com `LevelGrid` e `RoomScene`; referências de chão e paredes corretas.
- [ ] Player e blocos centralizados em células livres de chão.
- [ ] Paredes e áreas sem chão bloqueiam os caminhos esperados.
- [ ] `EquationValidator` monitora todos os blocos da sala e o Player correto.
- [ ] Contas planejadas aparecem corretamente em `Results`.
- [ ] Cada portão tem condição, lado, feedback e animações corretos. Confira âncoras e validador para portões de equação, IDs para portões de alavanca e a sequência para portões narrativos.
- [ ] `GateOpeningFeedback` da câmera aponta para o Player.
- [ ] Portais têm chão, trigger, destino de retorno e direção de chegada corretos.
- [ ] Saídas laterais geralmente na mesma linha; saídas verticais na mesma coluna.
- [ ] Ida e volta testadas, incluindo animação da câmera e fechamento do portão atrás do Player.
- [ ] Puzzle resolvido a partir da posição inicial, usando apenas os movimentos disponíveis no jogo.
- [ ] Referências e Overrides dos prefabs conferidos; cenas salvas fora do Play Mode.
